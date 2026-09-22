// 本文件包含类型 IMutableObjectContext：供 transition action 与状态机记录派生值的 IObjectContext 可变视图。
namespace Mono.FileBox.Lite.Abstractions;

/// <summary>
/// Mutable view of an <see cref="IObjectContext"/> used by transition actions and the
/// state machine to record derived values (content hash, current state) as the pipeline
/// progresses. Implemented by the concrete mutable context type; guards, observers and
/// downstream primitives should depend on the immutable <see cref="IObjectContext"/>.
/// </summary>
public interface IMutableObjectContext : IObjectContext
{
    new string ContentHash { get; set; }
    new ObjectState CurrentState { get; set; }
}