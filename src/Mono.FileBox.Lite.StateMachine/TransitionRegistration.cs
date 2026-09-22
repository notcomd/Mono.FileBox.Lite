// File-level: defines a single registered transition, grouping trigger, source/target
// states and the guard/action/observer component lists used by the registry.

using Mono.FileBox.Lite.Abstractions;

namespace Mono.FileBox.Lite.StateMachine;

/// <summary>A single registered transition: trigger, source/target states and component lists.
/// 中文翻译：一条已注册的转移：触发器、源/目标状态以及组件列表。</summary>
public sealed class TransitionRegistration
{
    public ObjectTrigger Trigger { get; set; }
    public ObjectState From { get; set; }
    public ObjectState To { get; set; }
    public List<Type> Guards { get; } = new();
    public List<Type> Actions { get; } = new();
    public List<Type> Observers { get; } = new();
}