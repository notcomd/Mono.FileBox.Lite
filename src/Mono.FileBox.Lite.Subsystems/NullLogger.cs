// File-level documentation: Transition logger that discards output (no-op).
// Extracted from the original multi-type Defaults.cs.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems;

/// <summary>Transition logger that discards output. Also an observer role.</summary>
public sealed class NullLogger : ITransitionLogger, ITransitionObserver
{
    public void Log(ObjectTrigger t, ObjectState from, ObjectState to) { }
    public Task OnBeforeAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct) => Task.CompletedTask;
    public Task OnAfterAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct) => Task.CompletedTask;
}