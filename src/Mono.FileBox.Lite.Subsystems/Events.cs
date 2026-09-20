using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems.Events;

/// <summary>
/// Observers that publish an event on the bus. Used as a side-channel (observer) role.
/// </summary>
public sealed class EventBusObserver : ITransitionObserver
{
    private readonly IEventBus _bus;

    public EventBusObserver(IEventBus bus) => _bus = bus;

    public Task OnBeforeAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct)
        => Task.CompletedTask;

    public Task OnAfterAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct)
        => _bus.PublishAsync($"object.{t}".ToLowerInvariant(), ctx, ct);
}

/// <summary>
/// Transition observer that records lifecycle transitions. Acts as the
/// <see cref="ITransitionObserver"/> used by the <c>Publish</c> transition.
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

internal static class TransitionLoggerStateKey
{
    public const string From = "TransitionFrom";
}

/// <summary>
/// Console transition logger. Writes one line per committed transition. Also acts as an
/// <see cref="ITransitionObserver"/> so it can be attached via
/// <c>.Observe&lt;ITransitionLogger&gt;()</c>.
/// </summary>
public sealed class DefaultTransitionLogger : ITransitionLogger, ITransitionObserver
{
    private readonly TextWriter _output;

    public DefaultTransitionLogger(TextWriter? output = null) => _output = output ?? Console.Out;

    public void Log(ObjectTrigger t, ObjectState from, ObjectState to)
        => _output.WriteLine($"[state-machine] {from} --({t})--> {to}");

    public Task OnBeforeAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct)
        => Task.CompletedTask;

    public Task OnAfterAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct)
    {
        Log(t, ctx.Items.TryGetValue(TransitionLoggerStateKey.From, out var f)
            ? (ObjectState)f : ctx.CurrentState, ctx.CurrentState);
        return Task.CompletedTask;
    }
}

/// <summary>Console event bus: writes published topics to the console. Also an observer role.</summary>
public sealed class ConsoleEventBus : IEventBus, ITransitionObserver
{
    private readonly TextWriter _output;

    public ConsoleEventBus(TextWriter? output = null) => _output = output ?? Console.Out;

    public Task PublishAsync(string topic, object payload, CancellationToken ct)
    {
        var hash = payload is IObjectContext ctx ? ctx.ContentHash : "(n/a)";
        _output.WriteLine($"[event] {topic} hash={hash}");
        return Task.CompletedTask;
    }

    public Task OnBeforeAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct)
        => Task.CompletedTask;

    public Task OnAfterAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct)
        => PublishAsync($"object.{t}".ToLowerInvariant(), ctx, ct);
}