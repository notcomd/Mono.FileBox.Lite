// 本文件包含类型 TransitionResult：转换尝试的结果分类。
namespace Mono.FileBox.Lite.Abstractions;

/// <summary>Outcome classification for a transition attempt. 一次转换尝试的结果分类。</summary>
public enum TransitionResult
{
    /// <summary>Transition executed and the state committed.</summary>
    Allowed,

    /// <summary>The (trigger, state) pair is not registered; state unchanged.</summary>
    NotAllowed,

    /// <summary>At least one guard rejected the transition; state unchanged.</summary>
    Denied
}