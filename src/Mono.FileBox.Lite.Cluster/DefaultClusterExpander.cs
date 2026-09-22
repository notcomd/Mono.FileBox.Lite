// Contains the DefaultClusterExpander type, the standalone cluster expander implementation.
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Cluster;

/// <summary>
/// Cluster expander for standalone operation. Joining registers the node; rebalancing
/// is a no-op because a single node already holds every block.
/// 中文翻译：独立运行模式的集群扩展器。加入仅注册节点；由于单节点已持有所有数据块，因此再平衡为空操作。
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