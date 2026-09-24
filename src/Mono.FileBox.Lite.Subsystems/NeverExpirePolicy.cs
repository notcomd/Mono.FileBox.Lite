// File-level documentation: Lifecycle policy that never expires/archives an object.
// Extracted from the original multi-type Defaults.cs.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems;

/// <summary>Lifecycle policy that never expires/archives an object.
/// 永不使对象过期或归档的生命周期策略。</summary>
public sealed class NeverExpirePolicy : ILifecyclePolicy, IGuard
{
    public Task<bool> CanEnterAsync(IObjectContext ctx, CancellationToken ct)
        => Task.FromResult(false);
}