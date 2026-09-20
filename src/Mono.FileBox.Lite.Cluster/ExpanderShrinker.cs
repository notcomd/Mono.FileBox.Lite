using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Cluster;

/// <summary>
/// Cluster expander for standalone operation. Joining registers the node; rebalancing
/// is a no-op because a single node already holds every block.
/// </summary>
public sealed class DefaultClusterExpander : IClusterExpander
{
    private readonly INodeRegistry _registry;
    private readonly IHashRing _ring;
    private long _completed;
    private long _total = -1;

    public DefaultClusterExpander(INodeRegistry registry, IHashRing ring)
    {
        _registry = registry;
        _ring = ring;
    }

    public Task JoinAsync(NodeInfo node, CancellationToken ct)
        => _registry.RegisterAsync(node, ct);

    public async Task RebalanceAsync(RebalanceOptions options, CancellationToken ct)
    {
        var nodes = await _registry.ListAsync(ct).ConfigureAwait(false);
        _ring.Rebuild(nodes);
        // In a single-node setup the ring is already converged.
        _total = 1;
        _completed = 1;
    }

    public Task<RebalanceProgress> GetProgressAsync(CancellationToken ct)
        => Task.FromResult(new RebalanceProgress
        {
            TotalBlocks = _total < 0 ? 0 : _total,
            CompletedBlocks = _completed,
            MovedBlocks = 0
        });
}

/// <summary>Cluster shrinker for standalone operation (no-op drain/remove).</summary>
public sealed class DefaultClusterShrinker : IClusterShrinker
{
    private readonly INodeRegistry _registry;

    public DefaultClusterShrinker(INodeRegistry registry) => _registry = registry;

    public Task DecommissionAsync(string nodeId, CancellationToken ct) => Task.CompletedTask;
    public Task WaitForDrainAsync(string nodeId, CancellationToken ct) => Task.CompletedTask;
    public Task RemoveAsync(string nodeId, CancellationToken ct) => Task.CompletedTask;
}