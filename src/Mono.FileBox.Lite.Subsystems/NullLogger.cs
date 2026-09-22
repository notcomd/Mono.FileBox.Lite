// File-level documentation: Transition logger that discards output (no-op).
// Extracted from the original multi-type Defaults.cs.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems;

/// <summary>Transition logger that discards output. Also an observer role.
/// 中文翻译：丢弃输出（空操作）的转移日志记录器，同时兼具观察者角色。</summary>
public sealed class NullLogger : ITransitionLogger, ITransitionObserver
{
    public void Log(ObjectTrigger t, ObjectState from, ObjectState to) { }
    public Task OnBeforeAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct) => Task.CompletedTask;
    public Task OnAfterAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct) => Task.CompletedTask;
}