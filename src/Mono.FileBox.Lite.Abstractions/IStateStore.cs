// 本文件包含类型 IStateStore：独立持久化对象的当前生命周期状态，通过版本计数器乐观处理并发。
namespace Mono.FileBox.Lite.Abstractions;

/// <summary>
/// Persists the current lifecycle state of an object independently of the in-memory
/// state machine. Concurrency is handled optimistically via a version counter.
/// 独立于内存状态机持久化对象的当前生命周期状态，并通过版本计数器以乐观方式处理并发。
/// </summary>
public interface IStateStore
{
    Task<ObjectState> LoadAsync(string contentHash, CancellationToken ct);

    /// <summary>
    /// Saves <paramref name="state"/> for <paramref name="contentHash"/>. Returns
    /// <c>false</c> on an optimistic-concurrency conflict (caller retries).
    /// </summary>
    Task<bool> SaveAsync(string contentHash, ObjectState state, CancellationToken ct);
}