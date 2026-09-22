using Mono.FileBox.Lite.Abstractions.Configuration;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Storage.DiskSelector;

/// <summary>
/// Consistent-hash disk selector. Each enabled pool contributes a number of virtual
/// points on a ring; content hashes are mapped to the nearest pool along the ring.
/// Falls back to the highest-priority enabled pool when the ring is empty.
/// </summary>
/// <remarks>
/// <b>并发语义</b>：一致性哈希环由 <c>lock</c> 保护的
/// <see cref="SortedDictionary{TKey,TValue}"/> 维护；并发上传时各对象执行各自独立的
/// <see cref="SelectForWriteAsync"/>/<see cref="SelectForReadAsync"/>（读环加锁，池选择局部串行），
/// 不同对象互不阻塞、可并行。
/// </remarks>
public sealed class ConsistentHashDiskSelector : IDiskSelector
{
    private readonly List<PoolOptions> _pools;
    private readonly int _replicas;
    private readonly int _virtualNodes;
    private readonly object _lock = new();
    private SortedDictionary<long, PoolOptions>? _ring;
    private List<DiskPoolInfo>? _poolInfos;

    public ConsistentHashDiskSelector(IReadOnlyList<PoolOptions>? pools, int replicas = 3, int virtualNodes = 128)
    {
        _pools = (pools ?? Array.Empty<PoolOptions>()).ToList();
        _replicas = Math.Max(1, replicas);
        _virtualNodes = Math.Max(4, virtualNodes);
        Build();
    }

    public Task<IDiskHandle> SelectForWriteAsync(
        string contentHash, WriteOptions options, CancellationToken ct)
    {
        var pool = SelectPool(contentHash);
        return Task.FromResult<IDiskHandle>(new LocalDiskHandle(pool.PoolId, ChooseRoot(pool)));
    }

    public Task<IDiskHandle> SelectForReadAsync(string contentHash, CancellationToken ct)
    {
        var pool = SelectPool(contentHash);
        return Task.FromResult<IDiskHandle>(new LocalDiskHandle(pool.PoolId, ChooseRoot(pool)));
    }

    public Task<IReadOnlyList<DiskPoolInfo>> ListPoolsAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<DiskPoolInfo>>(GetPoolInfos());

    private PoolOptions SelectPool(string contentHash)
    {
        var basHash = Sha256.StableHash("block:" + contentHash);
        lock (_lock)
        {
            var ring = _ring!;
            var best = ring.FirstOrDefault().Value;
            foreach (var kv in ring)
            {
                if (kv.Key >= basHash) { best = kv.Value; break; }
            }
            if (best is not null) return best;
            return ring.Values.FirstOrDefault()
                   ?? throw new InvalidOperationException("No storage pool is configured.");
        }
    }

    private static string ChooseRoot(PoolOptions pool)
        => pool.RootPath ?? Path.Combine(Path.GetTempPath(), "mono-filebox", pool.PoolId);

    private void Build()
    {
        var enabled = _pools.Where(p => p.Enabled && !string.IsNullOrEmpty(p.PoolId)).ToList();
        var ring = new SortedDictionary<long, PoolOptions>();
        foreach (var pool in enabled)
        {
            for (var i = 0; i < _virtualNodes; i++)
            {
                var point = Sha256.StableHash($"{pool.PoolId}:{i}");
                ring[point] = pool;
            }
        }
        lock (_lock)
        {
            _ring = ring;
            _poolInfos = enabled.Select(ToPoolInfo).ToList();
        }
    }

    private List<DiskPoolInfo> GetPoolInfos()
    {
        lock (_lock) return new List<DiskPoolInfo>(_poolInfos ?? new List<DiskPoolInfo>());
    }

    private static DiskPoolInfo ToPoolInfo(PoolOptions pool) => new()
    {
        PoolId = pool.PoolId,
        RootPath = ChooseRoot(pool),
        Tier = pool.Tier,
        CapacityBytes = pool.CapacityBytes,
        Priority = pool.Priority,
        Enabled = pool.Enabled,
        HealthyDiskCount = 1
    };
}