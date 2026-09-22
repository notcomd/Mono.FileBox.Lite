// 本文件包含类型 IMutableObjectContext：供 transition action 与状态机记录派生值的 IObjectContext 可变视图。
namespace Mono.FileBox.Lite.Abstractions;

/// <summary>
/// Mutable view of an <see cref="IObjectContext"/> used by transition actions and the
/// state machine to record derived values (content hash, current state) as the pipeline
/// progresses. Implemented by the concrete mutable context type; guards, observers and
/// downstream primitives should depend on the immutable <see cref="IObjectContext"/>.
/// 中文翻译：IObjectContext 的可变视图，供转换操作与状态机在管线推进过程中记录派生值（内容哈希、当前状态）；守卫、观察器及下游原语应依赖不可变的 IObjectContext。
/// </summary>
public interface IMutableObjectContext : IObjectContext
{
    new string ContentHash { get; set; }
    new ObjectState CurrentState { get; set; }
}