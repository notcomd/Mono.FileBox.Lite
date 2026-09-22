// 本文件包含类型 IObjectStateMachine：生命周期状态机，对当前状态分派 trigger 并提交新状态。
namespace Mono.FileBox.Lite.Abstractions;

/// <summary>
/// The lifecycle state machine. Dispatches a trigger against the current state,
/// evaluating guards, observers and actions, and commits the new state on success.
/// 中文翻译：生命周期状态机。基于当前状态分派触发器，依次评估守卫、观察器与操作，成功后将新状态提交。
/// </summary>
public interface IObjectStateMachine
{
    /// <summary>
    /// Fires <paramref name="trigger"/> against <paramref name="ctx"/>. Returns the
    /// resulting state (unchanged when the transition is not allowed or a guard fails).
    /// </summary>
    Task<ObjectState> FireAsync(
        ObjectTrigger trigger, IObjectContext ctx, CancellationToken ct);
}