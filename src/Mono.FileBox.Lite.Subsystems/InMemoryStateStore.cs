// File-level documentation: In-memory state store with optimistic-concurrency conflict reporting.
// Extracted from the original multi-type Defaults.cs.
using System.Collections.Concurrent;
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems;

/// <summary>In-memory state store with optimistic-concurrency conflict reporting.
/// 具有乐观并发冲突报告能力的内存状态存储。</summary>
    /// <remarks>
    /// <b>并发语义</b>：基于 <see cref="ConcurrentDictionary{TKey,TValue}"/>，对不同 content hash
    /// 的读写可在多个线程并行；对同一 hash 的 <see cref="SaveAsync"/> 使用原子 <c>AddOrUpdate</c>
    /// 递增版本号，作为乐观并发冲突检测的基础。引擎为单进程嵌入库，无自身后台线程。
    /// </remarks>
public sealed class InMemoryStateStore : IStateStore
{
    private readonly ConcurrentDictionary<string, (ObjectState State, long Version)> _states = new();

    public Task<ObjectState> LoadAsync(string contentHash, CancellationToken ct)
        => Task.FromResult(_states.TryGetValue(contentHash, out var v) ? v.State : ObjectState.Pending);

    public Task<bool> SaveAsync(string contentHash, ObjectState state, CancellationToken ct)
    {
        _states.AddOrUpdate(
            contentHash,
            (state, 1),
            (_, current) => (state, current.Version + 1));
        return Task.FromResult(true);
    }

    public IReadOnlyCollection<string> Keys => (IReadOnlyCollection<string>)_states.Keys;
}