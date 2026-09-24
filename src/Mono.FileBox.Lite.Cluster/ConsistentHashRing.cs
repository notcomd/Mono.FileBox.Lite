// Contains the ConsistentHashRing type, a consistent hashing ring used to map keys to replica nodes.
using System.Security.Cryptography;
using System.Text;
using Mono.FileBox.Lite.Abstractions.Cluster;

namespace Mono.FileBox.Lite.Cluster.HashRing;

/// <summary>
/// Consistent hash ring. Each node contributes <c>virtualNodeCount</c> points (this
/// implementation uses a fixed 96 per node by default); a key maps to the first replica
/// nodes reached clockwise from the key's hash.
/// 一致性哈希环。每个节点贡献 <c>virtualNodeCount</c> 个哈希点（本实现默认每节点固定 96 个）；键(key)映射到从该键哈希值顺时针遇到的首个副本节点。
/// </summary>
public sealed class ConsistentHashRing : IHashRing
{
    private readonly object _lock = new();
    private SortedDictionary<long, NodeInfo> _ring = new();
    private List<NodeInfo> _nodes = new();

    public ConsistentHashRing(int virtualNodeCount = 96)
    {
        VirtualNodeCount = Math.Max(4, virtualNodeCount);
    }

    public int VirtualNodeCount { get; }

    public void Rebuild(IReadOnlyList<NodeInfo> nodes)
    {
        lock (_lock)
        {
            _nodes = nodes.ToList();
            _ring = new SortedDictionary<long, NodeInfo>();
            foreach (var node in nodes)
            {
                for (var i = 0; i < VirtualNodeCount; i++)
                    _ring[StableHash.Compute($"{node.NodeId}:{i}")] = node;
            }
        }
    }

    public IReadOnlyList<NodeInfo> SelectNodes(string key, int replicas)
    {
        lock (_lock)
        {
            if (_nodes.Count == 0) return Array.Empty<NodeInfo>();
            var position = StableHash.Compute(key);
            var selected = new List<NodeInfo>();
            var visited = new HashSet<string>(StringComparer.Ordinal);

            // Collect all ring points starting at `position`.
            var points = _ring
                .Where(kv => kv.Key >= position)
                .Concat(_ring.Where(kv => kv.Key < position))
                .ToList();

            foreach (var kv in points)
            {
                if (selected.Count >= replicas) break;
                if (visited.Add(kv.Value.NodeId))
                    selected.Add(kv.Value);
            }

            return selected;
        }
    }
}