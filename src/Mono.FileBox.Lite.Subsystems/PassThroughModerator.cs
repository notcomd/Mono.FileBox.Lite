// File-level documentation: Moderator that passes every object (no-op).
// Extracted from the original multi-type Defaults.cs.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems;

/// <summary>Moderator that passes every object (no-op). Also an action for the Audit transition.</summary>
public sealed class PassThroughModerator : IContentModerator, ITransitionAction
{
    public Task<ModerationResult> ModerateAsync(IObjectContext ctx, CancellationToken ct)
        => Task.FromResult(ModerationResult.Pass("pass-through-v1"));

    public Task ExecuteAsync(IObjectContext ctx, CancellationToken ct)
        => Task.CompletedTask;
}