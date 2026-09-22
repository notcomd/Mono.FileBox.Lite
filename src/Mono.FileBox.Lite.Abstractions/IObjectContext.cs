// 本文件包含类型 IObjectContext：贯穿生命周期状态机及其 guard/action/observer 的 per-object 上下文。
namespace Mono.FileBox.Lite.Abstractions;

/// <summary>
/// The per-object context threaded through the lifecycle state machine and all
/// of its guards, actions and observers.
/// </summary>
public interface IObjectContext
{
    /// <summary>Content hash (SHA-256). Immutable once the object enters <see cref="ObjectState.Stored"/>.</summary>
    string ContentHash { get; }

    /// <summary>Namespace / logical group (the engine does not enforce isolation semantics).</summary>
    string NamespaceId { get; }

    /// <summary>Current lifecycle state, maintained by the state machine.</summary>
    ObjectState CurrentState { get; }

    /// <summary>
    /// Extension data slot passed through a transition. All object-level mutable data
    /// must travel here rather than being held as instance state in guards/actions.
    /// </summary>
    IDictionary<string, object> Items { get; }
}