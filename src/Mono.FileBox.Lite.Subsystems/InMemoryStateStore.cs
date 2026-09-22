// File-level documentation: In-memory state store with optimistic-concurrency conflict reporting.
// Extracted from the original multi-type Defaults.cs.
using System.Collections.Concurrent;
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems;

/// <summary>In-memory state store with optimistic-concurrency conflict reporting.</summary>
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