using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Cluster.Topology;

/// <summary>
/// Cluster topology view backed by the node registry. Tracks a monotonically increasing
/// version and raises <see cref="TopologyChanged"/> whenever the membership is refreshed.
/// 中文翻译：基于节点注册表实现的集群拓扑视图。维护单调递增的版本号，并在成员集合刷新时触发 <see cref="TopologyChanged"/> 事件。
/// </summary>
public sealed class RegistryBackedTopology : IClusterTopology
{
    private readonly INodeRegistry _registry;
    private long _version;

    public RegistryBackedTopology(INodeRegistry registry, NodeInfo self)
    {
        _registry = registry;
        Self = self;
    }

    public NodeInfo Self { get; }

    public long TopologyVersion
    {
        get
        {
            lock (this) return _version;
        }
    }

    public event EventHandler<TopologyChangedEventArgs>? TopologyChanged;

    public async Task<IReadOnlyList<NodeInfo>> ListNodesAsync(CancellationToken ct)
    {
        var nodes = (await _registry.ListAsync(ct).ConfigureAwait(false));
        var version = System.Threading.Interlocked.Increment(ref _version);
        TopologyChanged?.Invoke(this, new TopologyChangedEventArgs
        {
            NewVersion = version,
            Nodes = nodes
        });
        return nodes;
    }

    public async Task<IReadOnlyList<NodeInfo>> ListNodesAsync(NodeRole role, CancellationToken ct)
    {
        var nodes = await ListNodesAsync(ct).ConfigureAwait(false);
        return nodes.Where(n => n.Role == role).ToArray();
    }
}