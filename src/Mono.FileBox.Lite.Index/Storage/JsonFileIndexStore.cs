// JsonFileIndexStore.cs — 将索引以「JSON 文档文件」方式持久化的 IEntryStore 实现。
// 该实现让 IEntryStore 保持不变（SQLite 等 DB 接口仍可后续接入），仅引入本文件作为本地保存。
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Index.Storage;

/// <summary>
/// A durable <see cref="IEntryStore"/> backed by a single JSON document file.
/// The file is authoritative: every mutation rewrites the whole document atomically,
/// and reads parse the document on demand (the memory-lean baseline that the hot-index
/// layer sits in front of). Does <b>not</b> keep the full index resident in memory.
/// </summary>
/// <remarks>
/// <b>并发语义</b>：所有文档读写经 <c>_lock</c> 串行化，避免并发写交错损坏文件；
/// 单次读/写本身是原子的（写走 临时文件 + 移动）。不同内容 hash 的读写虽在同一文件上
/// 串行，但临界区仅为文件 IO，开销极低的缓存命中由 <see cref="HotCachingIndexStore"/>
/// 在内存中完成，从而减少对本文档文件的反复解析。
/// </remarks>
public sealed class JsonFileIndexStore : IEntryStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    private readonly string _path;
    private readonly object _lock = new();

    public JsonFileIndexStore(string path) => _path = path;

    public Task PutAsync(string contentHash, IndexEntry entry, CancellationToken ct)
    {
        lock (_lock)
        {
            var entries = new Dictionary<string, IndexEntry>(StringComparer.Ordinal);
            foreach (var e in ReadDocument(_path))
                entries[e.ContentHash] = e;
            entries[contentHash] = entry;
            WriteDocument(_path, entries.Values.OrderBy(e => e.ContentHash).ToList());
        }
        return Task.CompletedTask;
    }

    public Task<IndexEntry?> GetAsync(string contentHash, CancellationToken ct)
    {
        foreach (var e in ReadDocument(_path))
            if (string.Equals(e.ContentHash, contentHash, StringComparison.Ordinal))
                return Task.FromResult<IndexEntry?>(e);
        return Task.FromResult<IndexEntry?>(null);
    }

    public Task DeleteAsync(string contentHash, CancellationToken ct)
    {
        lock (_lock)
        {
            var entries = ReadDocument(_path).Where(e =>
                !string.Equals(e.ContentHash, contentHash, StringComparison.Ordinal)).ToList();
            WriteDocument(_path, entries);
        }
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<IndexEntry>> GetManyAsync(IEnumerable<string> hashes, CancellationToken ct)
    {
        var wanted = new HashSet<string>(hashes, StringComparer.Ordinal);
        var byHash = ReadDocument(_path)
            .Where(e => wanted.Contains(e.ContentHash))
            .ToDictionary(e => e.ContentHash, StringComparer.Ordinal);
        return wanted.Where(byHash.ContainsKey).Select(h => byHash[h]).ToList();
    }

    public Task<IReadOnlyList<IndexEntry>> ListByNamespaceAsync(string namespaceId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IndexEntry>>(ReadDocument(_path)
            .Where(e => string.Equals(e.NamespaceId, namespaceId, StringComparison.Ordinal))
            .OrderBy(e => e.ContentHash, StringComparer.Ordinal).ToList());

    public Task<IReadOnlyList<IndexEntry>> ListAllAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IndexEntry>>(ReadDocument(_path));

    public Task ClearAsync(CancellationToken ct)
    {
        lock (_lock) WriteDocument(_path, new List<IndexEntry>());
        return Task.CompletedTask;
    }

    // ---- JSON 编解码（亦供 HotCachingIndexStore 写热点旁路文件复用） ----

    /// <summary>把单个 IndexEntry 序列化为 JSON 元素（枚举以字符串存、属性字典保留原生标量类型）。</summary>
    internal static JsonElement EntryToElement(IndexEntry e)
        => JsonSerializer.SerializeToElement(Dto.From(e), Options);

    /// <summary>从 JSON 元素还原 IndexEntry。</summary>
    internal static IndexEntry ElementToEntry(JsonElement e)
        => Dto.ToEntry(JsonSerializer.Deserialize<Dto>(e.GetRawText(), Options) ?? new Dto());

    internal static List<IndexEntry> ReadDocument(string path)
    {
        if (!File.Exists(path)) return new List<IndexEntry>();
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            if (!root.TryGetProperty("Entries", out var arr)) return new List<IndexEntry>();
            var result = new List<IndexEntry>();
            foreach (var item in arr.EnumerateArray())
            {
                var dto = JsonSerializer.Deserialize<Dto>(item.GetRawText(), Options);
                if (dto != null) result.Add(Dto.ToEntry(dto));
            }
            return result;
        }
        catch (JsonException)
        {
            return new List<IndexEntry>();
        }
    }

    internal static void WriteDocument(string path, IReadOnlyList<IndexEntry> entries)
    {
        if (entries is null) throw new ArgumentNullException(nameof(entries));
        var doc = new DocumentDto
        {
            Version = 1,
            Entries = entries.Select(Dto.From).ToList()
        };
        var json = JsonSerializer.Serialize(doc, Options);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, json, Encoding.UTF8);
        if (File.Exists(path)) File.Delete(path);
        File.Move(tmp, path);
    }

    /// <summary>把 JSON 标量还原为原生 CLR 类型（数字尽量还原为 long，其次 double）。</summary>
    internal static object? ToNative(JsonElement e)
    {
        switch (e.ValueKind)
        {
            case JsonValueKind.String: return e.GetString();
            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Number:
                // 注意：小心 `<see cref="double"/>` 的目标类型推断 —— 三目运算符会把 long 提升为 double，
                // 故必须用 if/else 显式分支以保留 long 类型。
                if (e.TryGetInt64(out var l)) return l;
                return e.GetDouble();
            case JsonValueKind.Null: return null;
            default: return e.GetRawText();
        }
    }

    /// <summary>文档根（版本 + 条目数组）。</summary>
    private sealed class DocumentDto
    {
        public int Version { get; set; } = 1;
        public List<Dto> Entries { get; set; } = new();
    }

    /// <summary>IndexEntry 的可序列化 DTO：枚举以字符串存储，属性字典用 JsonElement 保留标量类型。</summary>
    private sealed class Dto
    {
        public string ContentHash { get; set; } = string.Empty;
        public string NamespaceId { get; set; } = string.Empty;
        public string? ObjectKey { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
        public Dictionary<string, JsonElement> Attributes { get; set; } = new();
        public string Tier { get; set; } = StorageTier.Hot.ToString();
        public long SizeBytes { get; set; }
        public string? ContentType { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
        public string State { get; set; } = ObjectState.Pending.ToString();

        public static Dto From(IndexEntry e) => new()
        {
            ContentHash = e.ContentHash,
            NamespaceId = e.NamespaceId,
            ObjectKey = e.ObjectKey,
            Tags = e.Tags.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal),
            Attributes = e.Attributes.ToDictionary(
                kv => kv.Key,
                kv => JsonSerializer.SerializeToElement(kv.Value, kv.Value.GetType(), Options),
                StringComparer.Ordinal),
            Tier = e.Tier.ToString(),
            SizeBytes = e.SizeBytes,
            ContentType = e.ContentType,
            CreatedAt = e.CreatedAt,
            ModifiedAt = e.ModifiedAt,
            State = e.State.ToString()
        };

        public static IndexEntry ToEntry(Dto d)
        {
            var attributes = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var kv in d.Attributes)
            {
                var native = ToNative(kv.Value);
                if (native != null) attributes[kv.Key] = native;
            }
            return new IndexEntry
            {
                ContentHash = d.ContentHash,
                NamespaceId = d.NamespaceId,
                ObjectKey = d.ObjectKey,
                Tags = d.Tags,
                Attributes = attributes,
                Tier = Enum.TryParse<StorageTier>(d.Tier, true, out var tier) ? tier : StorageTier.Hot,
                SizeBytes = d.SizeBytes,
                ContentType = d.ContentType,
                CreatedAt = d.CreatedAt,
                ModifiedAt = d.ModifiedAt,
                State = Enum.TryParse<ObjectState>(d.State, true, out var st) ? st : ObjectState.Pending
            };
        }
    }
}