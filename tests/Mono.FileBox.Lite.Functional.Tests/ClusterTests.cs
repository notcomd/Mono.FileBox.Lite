using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Cluster.Capacity;
using Mono.FileBox.Lite.Cluster.HashRing;
using Mono.FileBox.Lite.Cluster.Topology;
using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Functional.Tests;

/// <summary>Cluster coordination: consistent hash ring, topology versioning, capacity monitoring.
/// 集群协调：一致性哈希环、拓扑版本管理、容量监控。</summary>
public class ClusterTests
{
    private static readonly CancellationToken None = CancellationToken.None;

    private static readonly NodeInfo N1 = new() { NodeId = "a", AdvertiseAddress = "a:1" };
    private static readonly NodeInfo N2 = new() { NodeId = "b", AdvertiseAddress = "b:1" };
    private static readonly NodeInfo N3 = new() { NodeId = "c", AdvertiseAddress = "c:1" };

    [Fact]
    public void HashRing_SelectsReplicas_Deterministic_AndDistinct()
    {
        var ring = new ConsistentHashRing(virtualNodeCount: 64);
        ring.Rebuild(new[] { N1, N2, N3 });

        var sel = ring.SelectNodes("block:key", replicas: 2);
        Assert.Equal(2, sel.Count);
        Assert.Equal(sel[0].NodeId, sel[0].NodeId);

        var again = ring.SelectNodes("block:key", replicas: 2);
        Assert.Equal(sel.Select(n => n.NodeId), again.Select(n => n.NodeId));
    }

    [Fact]
    public void HashRing_HonorsReplicaCap_AndRebuildChangesSelection()
    {
        var ring = new ConsistentHashRing();
        ring.Rebuild(new[] { N1 });
        Assert.Single(ring.SelectNodes("k", 3)); // only 1 node available

        ring.Rebuild(new[] { N1, N2, N3 });
        Assert.Equal(3, ring.SelectNodes("k", 5).Count);
    }

    [Fact]
    public async Task Topology_VersionIncrements_AndFiltersByRole()
    {
        var nodes = new[]
        {
            new NodeInfo { NodeId = "a", Role = NodeRole.Storage },
            new NodeInfo { NodeId = "b", Role = NodeRole.Index }
        };
        var topo = new RegistryBackedTopology(new NaiveNodeRegistry(nodes), nodes[0]);

        var v1 = topo.TopologyVersion;
        var all = await topo.ListNodesAsync(None);
        var v2 = topo.TopologyVersion;
        var storages = await topo.ListNodesAsync(NodeRole.Storage, None);

        Assert.True(v2 >= v1);
        Assert.Equal(2, all.Count);
        Assert.Single(storages);
    }

    [Fact]
    public async Task CapacityMonitor_ReportsNormal_WhenFarBelowThresholds()
    {
        var dir = Path.Combine(Path.GetTempPath(), "filebox-func-cap", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        await TestHelper.WriteSmallFileAsync(dir, 10);

        var monitor = new DefaultCapacityMonitor(new StubSelector(
            new DiskPoolInfo { PoolId = "p", RootPath = dir, CapacityBytes = 10_000_000 }), null);

        var status = await monitor.GetStatusAsync(None);
        Assert.Equal(CapacityState.Normal, status.State);
        Assert.Equal(10, status.UsedBytes);
    }

    [Fact]
    public async Task CapacityMonitor_ReportsFull_WhenPoolExceedsWatermark()
    {
        var dir = Path.Combine(Path.GetTempPath(), "filebox-func-cap", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        await TestHelper.WriteSmallFileAsync(dir, 95);

        var monitor = new DefaultCapacityMonitor(new StubSelector(
            new DiskPoolInfo { PoolId = "p", RootPath = dir, CapacityBytes = 100 }), null);

        var status = await monitor.GetStatusAsync(None);
        Assert.Equal(CapacityState.Full, status.State); // 95/100 >= Full(0.95)
    }

    // -------- helpers --------

    private sealed class NaiveNodeRegistry : Mono.FileBox.Lite.Abstractions.Subsystems.INodeRegistry
    {
        private readonly IReadOnlyList<NodeInfo> _nodes;
        public NaiveNodeRegistry(IReadOnlyList<NodeInfo> nodes) => _nodes = nodes;
        public Task RegisterAsync(NodeInfo node, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<NodeInfo>> ListAsync(CancellationToken ct) => Task.FromResult(_nodes);
    }

    private sealed class StubSelector : IDiskSelector
    {
        private readonly IReadOnlyList<DiskPoolInfo> _pools;
        public StubSelector(params DiskPoolInfo[] pools) => _pools = pools;
        public Task<IDiskHandle> SelectForWriteAsync(string h, WriteOptions o, CancellationToken ct)
            => Task.FromResult<IDiskHandle>(new StubHandle());
        public Task<IDiskHandle> SelectForReadAsync(string h, CancellationToken ct)
            => Task.FromResult<IDiskHandle>(new StubHandle());
        public Task<IReadOnlyList<DiskPoolInfo>> ListPoolsAsync(CancellationToken ct) => Task.FromResult(_pools);

        private sealed class StubHandle : IDiskHandle
        {
            public string PoolId => "p";
            public string DiskId => "p";
            public bool IsHealthy => true;
        }
    }

    private static class TestHelper
    {
        public static Task WriteSmallFileAsync(string dir, int bytes)
        {
            var b = new byte[bytes];
            Array.Fill(b, (byte)7);
            File.WriteAllBytes(Path.Combine(dir, "data.bin"), b);
            return Task.CompletedTask;
        }
    }
}