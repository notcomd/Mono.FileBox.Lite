// File-level documentation: Console transition logger writing one line per committed transition.
// Extracted from the original multi-type Events.cs.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems.Events;

/// <summary>
/// Console transition logger. Writes one line per committed transition. Also acts as an
/// <see cref="ITransitionObserver"/> so it can be attached via
/// <c>.Observe&lt;ITransitionLogger&gt;()</c>.
/// 中文翻译：控制台转移日志记录器，每次已提交的转移写入一行日志；同时实现 <see cref="ITransitionObserver"/>，可通过 <c>.Observe&lt;ITransitionLogger&gt;()</c> 挂接。
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