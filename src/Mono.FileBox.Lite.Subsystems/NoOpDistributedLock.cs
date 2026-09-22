// File-level documentation: No-op distributed lock that always grants.
// Extracted from the original multi-type Defaults.cs and kept, by convention,
// as a standalone top-level type so each file contains exactly one type.
using System.Collections.Concurrent;
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems;

/// <summary>Distributed lock that always grants. Single-node default implementation.</summary>
public sealed class NoOpDistributedLock : IDistributedLock, IGuard
{
    public Task<bool> CanEnterAsync(IObjectContext ctx, CancellationToken ct)
        => Task.FromResult(true);
}