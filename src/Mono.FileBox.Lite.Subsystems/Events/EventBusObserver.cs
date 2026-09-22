// File-level documentation: Observer that publishes a transition event on the bus.
// Extracted from the original multi-type Events.cs.
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