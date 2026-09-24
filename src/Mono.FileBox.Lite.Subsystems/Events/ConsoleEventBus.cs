// File-level documentation: Console event bus writing published topics to the console.
// Extracted from the original multi-type Events.cs.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems.Events;

/// <summary>Console event bus: writes published topics to the console. Also an observer role.
/// 控制台事件总线，将发布的话题输出到控制台，同时兼具观察者角色。</summary>
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