// File-level documentation: Event bus that drops all events.
// Extracted from the original multi-type Defaults.cs.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems;

/// <summary>Event bus that drops all events. Also an observer role for transitions.
/// 中文翻译：丢弃所有事件的事件总线，同时充当转移的观察者角色。</summary>
public sealed class NullEventBus : IEventBus, ITransitionObserver
{
    public Task PublishAsync(string topic, object payload, CancellationToken ct)
        => Task.CompletedTask;
    public Task OnBeforeAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct) => Task.CompletedTask;
    public Task OnAfterAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct) => Task.CompletedTask;
}