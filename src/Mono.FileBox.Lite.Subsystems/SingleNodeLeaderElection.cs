// File-level documentation: Leader election that always elects the local node.
// Extracted from the original multi-type Defaults.cs.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems;

/// <summary>Leader election that always elects the local node.
/// 始终选举本机节点为领导者的领导选举实现。</summary>
public sealed class SingleNodeLeaderElection : ILeaderElection
{
    private readonly NodeInfo _self;
    public SingleNodeLeaderElection(NodeInfo self) => _self = self;

    public Task<NodeInfo> ElectAsync(CancellationToken ct) => Task.FromResult(_self);
}