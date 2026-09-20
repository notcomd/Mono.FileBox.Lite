using System.Collections.Concurrent;
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems;

/// <summary>Distributed lock that always grants. Single-node default implementation.</summary>
public sealed class NoOpDistributedLock : IDistributedLock
{
    public Task<bool> CanEnterAsync(IObjectContext ctx, CancellationToken ct)
        => Task.FromResult(true);
}

/// <summary>Lifecycle policy that never expires/archives an object.</summary>
public sealed class NeverExpirePolicy : ILifecyclePolicy
{
    public Task<bool> CanEnterAsync(IObjectContext ctx, CancellationToken ct)
        => Task.FromResult(false);
}

/// <summary>Moderator that passes every object (no-op).</summary>
public sealed class PassThroughModerator : IContentModerator
{
    public Task<ModerationResult> ModerateAsync(IObjectContext ctx, CancellationToken ct)
        => Task.FromResult(ModerationResult.Pass("pass-through-v1"));
}

/// <summary>Event bus that drops all events.</summary>
public sealed class NullEventBus : IEventBus
{
    public Task PublishAsync(string topic, object payload, CancellationToken ct)
        => Task.CompletedTask;
}

/// <summary>Transition logger that discards output.</summary>
public sealed class NullLogger : ITransitionLogger
{
    public void Log(ObjectTrigger t, ObjectState from, ObjectState to)
    {
    }
}

/// <summary>Leader election that always elects the local node.</summary>
public sealed class SingleNodeLeaderElection : ILeaderElection
{
    private readonly NodeInfo _self;
    public SingleNodeLeaderElection(NodeInfo self) => _self = self;

    public Task<NodeInfo> ElectAsync(CancellationToken ct) => Task.FromResult(_self);
}

/// <summary>In-memory node registry (single node in standalone mode).</summary>
public sealed class InMemoryNodeRegistry : INodeRegistry
{
    private readonly ConcurrentDictionary<string, NodeInfo> _nodes = new();

    public Task RegisterAsync(NodeInfo node, CancellationToken ct)
    {
        _nodes[node.NodeId] = node;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<NodeInfo>> ListAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<NodeInfo>>(_nodes.Values
            .OrderBy(n => n.NodeId).ToArray());
}

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