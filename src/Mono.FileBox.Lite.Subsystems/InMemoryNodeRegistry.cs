// File-level documentation: In-memory node registry (single node in standalone mode).
// Extracted from the original multi-type Defaults.cs.
using System.Collections.Concurrent;
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems;

/// <summary>In-memory node registry (single node in standalone mode).
/// 内存节点注册表（单机模式下的单节点）。</summary>
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