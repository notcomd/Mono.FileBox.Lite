// File-level documentation: Leader election that always elects the local node.
// Extracted from the original multi-type Defaults.cs.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems;

/// <summary>Leader election that always elects the local node.</summary>
public sealed class SingleNodeLeaderElection : ILeaderElection
{
    private readonly NodeInfo _self;
    public SingleNodeLeaderElection(NodeInfo self) => _self = self;

    public Task<NodeInfo> ElectAsync(CancellationToken ct) => Task.FromResult(_self);
}