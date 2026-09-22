// HotCachingIndexStore.cs — 热点索引：把访问频繁的条目载入内存，减少对底层索引文档文件的解析。
// 作者：Mono.FileBox.Lite
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Index.Storage;

/// <summary>
/// A write-through hot-index decorator over any <see cref="IEntryStore"/> (e.g.
/// <see cref="JsonFileIndexStore"/>). Frequently read entries are promoted into a bounded
/// in-memory hot set after enough accesses; subsequent reads of those keys are served from
/// memory and never reach the backing (file-parsing) store. Cold misses still hit the
/// backing store and are counted toward promotion.
/// </summary>
/// <remarks>
/// <b>并发语义</b>：热点集合由 <c>_lock</c> 保护的 <see cref="Dictionary{TKey,TValue}"/> 维护，
/// 临界区仅为内存操作（微秒级）；底层 <see cref="IEntryStore"/> 的读取在锁外进行。
/// 热点集合在构造时从 <see cref="HotIndexOptions.HotFilePath"/> 预热，实现冷启动即免解析。
/// </remarks>
public sealed class HotCachingIndexStore : IEntryStore
{
    private readonly IEntryStore _inner;
    private readonly HotIndexOptions _opts;
    private readonly Dictionary<string, HotSlot> _hot = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _access = new(StringComparer.Ordinal);
    private readonly object _lock = new();
    private long _clock;

    public HotCachingIndexStore(IEntryStore inner, HotIndexOptions options)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _opts = options ?? throw new ArgumentNullException(nameof(options));
        WarmHotFile();
    }

    public Task<IndexEntry?> GetAsync(string contentHash, CancellationToken ct)
    {
        if (TryGetHot(contentHash, out var hot))
        {
            Touch(hot);
            return Task.FromResult<IndexEntry?>(hot.Entry);
        }
        var entry = _inner.GetAsync(contentHash, ct).GetAwaiter().GetResult();
        if (entry != null) Promote(contentHash, entry);
        return Task.FromResult(entry);
    }

    public async Task<IReadOnlyList<IndexEntry>> GetManyAsync(IEnumerable<string> hashes, CancellationToken ct)
    {
        var wanted = hashes.Distinct(StringComparer.Ordinal).ToArray();
        var map = new Dictionary<string, IndexEntry>(StringComparer.Ordinal);
        var misses = new List<string>();

        foreach (var h in wanted)
        {
            if (_hot.TryGetValue(h, out var hot))
            {
                Touch(hot);
                map[h] = hot.Entry;
            }
            else misses.Add(h);
        }

        if (misses.Count > 0)
        {
            var found = await _inner.GetManyAsync(misses, ct).ConfigureAwait(false);
            foreach (var e in found)
            {
                map[e.ContentHash] = e;
                Promote(e.ContentHash, e);
            }
        }
        return wanted.Where(map.ContainsKey).Select(h => map[h]).ToList();
    }

    public Task PutAsync(string contentHash, IndexEntry entry, CancellationToken ct)
    {
        lock (_lock)
            if (_hot.ContainsKey(contentHash))
                _hot[contentHash] = new HotSlot(entry, ++_clock, _access.TryGetValue(contentHash, out var c) ? c : 1);
        return _inner.PutAsync(contentHash, entry, ct);
    }

    public Task DeleteAsync(string contentHash, CancellationToken ct)
    {
        lock (_lock)
        {
            _hot.Remove(contentHash);
            _access.Remove(contentHash);
        }
        return _inner.DeleteAsync(contentHash, ct);
    }

    public Task<IReadOnlyList<IndexEntry>> ListByNamespaceAsync(string namespaceId, CancellationToken ct)
        => _inner.ListByNamespaceAsync(namespaceId, ct);

    public Task<IReadOnlyList<IndexEntry>> ListAllAsync(CancellationToken ct)
        => _inner.ListAllAsync(ct);

    public Task ClearAsync(CancellationToken ct)
    {
        lock (_lock)
        {
            _hot.Clear();
            _access.Clear();
            PersistHotFile();
        }
        return _inner.ClearAsync(ct);
    }

    /// <summary>尝试从热点集合取回（不触发访问计数以外的副作用）。</summary>
    private bool TryGetHot(string hash, out HotSlot slot)
    {
        lock (_lock) return _hot.TryGetValue(hash, out slot);
    }

    private void Touch(HotSlot slot)
    {
        lock (_lock)
        {
            slot.Accesses++;
            slot.LastAccessTicks = ++_clock;
            _access[slot.Entry.ContentHash] = slot.Accesses;
        }
    }

    /// <summary>记录一次读取；继续超过阈值则提升进热点集合（超容量时 LRU 淘汰）。</summary>
    private void Promote(string hash, IndexEntry entry)
    {
        var now = ++_clock;
        lock (_lock)
        {
            var count = _access.TryGetValue(hash, out var c) ? c + 1 : 1;
            _access[hash] = count;

            if (_hot.TryGetValue(hash, out var existing))
            {
                existing.Accesses = count;
                existing.LastAccessTicks = now;
                return;
            }
            if (count < _opts.PromotionThreshold) return;

            if (_hot.Count >= _opts.Capacity) EvictLru();
            _hot[hash] = new HotSlot(entry, now, count);
            PersistHotFile();
        }
    }

    private void EvictLru()
    {
        string? victim = null;
        long worst = long.MaxValue;
        foreach (var kv in _hot)
        {
            var score = kv.Value.Accesses * 1_000_000L - kv.Value.LastAccessTicks; // 低访问 + 更旧
            if (score < worst)
            {
                worst = score;
                victim = kv.Key;
            }
        }
        if (victim != null) _hot.Remove(victim);
    }

    private void WarmHotFile()
    {
        var hotFile = _opts.HotFilePath;
        if (!_opts.PersistHot || hotFile is null || hotFile.Length == 0) return;
        if (!File.Exists(hotFile)) return;
        var entries = JsonFileIndexStore.ReadDocument(hotFile);
        lock (_lock)
        {
            foreach (var e in entries)
            {
                if (_hot.Count >= _opts.Capacity) break;
                _hot[e.ContentHash] = new HotSlot(e, ++_clock, _opts.PromotionThreshold);
            }
        }
    }

    private void PersistHotFile()
    {
        var hotFile = _opts.HotFilePath;
        if (!_opts.PersistHot || hotFile is null || hotFile.Length == 0) return;
        JsonFileIndexStore.WriteDocument(hotFile, _hot.Values.Select(s => s.Entry).ToList());
    }

    /// <summary>热点槽位：条目 + 访问计数 + 最近访问时间。</summary>
    private sealed class HotSlot
    {
        public IndexEntry Entry { get; }
        public int Accesses;
        public long LastAccessTicks;

        public HotSlot(IndexEntry entry, long ticks, int accesses)
        {
            Entry = entry;
            LastAccessTicks = ticks;
            Accesses = accesses;
        }
    }
}