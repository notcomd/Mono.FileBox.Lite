// File-level documentation: Observer that records lifecycle transitions.
// Extracted from the original multi-type Events.cs.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems.Events;

/// <summary>
/// Transition observer that records lifecycle transitions. Acts as the
/// <see cref="ITransitionObserver"/> used by the <c>Publish</c> transition.
/// 记录生命周期转移的转移观察者，作为 <c>Publish</c> 转移所使用的 <see cref="ITransitionObserver"/>。
/// </summary>
public sealed class TransitionLoggerObserver : ITransitionObserver
{
    private readonly ITransitionLogger _logger;

    public TransitionLoggerObserver(ITransitionLogger logger) => _logger = logger;

    public Task OnBeforeAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct)
        => Task.CompletedTask;

    public Task OnAfterAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct)
    {
        _logger.Log(t, ctx.Items.TryGetValue(TransitionLoggerStateKey.From, out var f)
            ? (ObjectState)f : ctx.CurrentState, ctx.CurrentState);
        return Task.CompletedTask;
    }
}