using System.Security.Cryptography;
using System.Text;
using Mono.FileBox.Lite.Abstractions.Cluster;

namespace Mono.FileBox.Lite.Cluster.HashRing;

/// <summary>Stable 64-bit hash used to place nodes/virtual nodes on the ring.</summary>
internal static class StableHash
{
    public static long Compute(string value)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        long result = 0;
        for (var i = 0; i < 16; i++)
            result = result * 31 + hash[i];
        return result;
    }
}

/// <summary>
/// Consistent hash ring. Each node contributes <c>virtualNodeCount</c> points (this
/// implementation uses a fixed 96 per node by default); a key maps to the first replica
/// nodes reached clockwise from the key's hash.
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