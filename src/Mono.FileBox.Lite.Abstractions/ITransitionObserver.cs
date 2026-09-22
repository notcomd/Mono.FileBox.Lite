// 本文件包含类型 ITransitionObserver：转换的旁路观察者，默认异常不影响主转换。
namespace Mono.FileBox.Lite.Abstractions;

/// <summary>
/// Transition side-channel. Observers run before and after a committed transition
/// and, by default, do not block the main transition when they throw.
/// 中文翻译：转换旁路通道。观察者在转换提交前后执行，默认抛出异常时不影响主转换流程。
/// </summary>
public interface ITransitionObserver
{
    Task OnBeforeAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct);
    Task OnAfterAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct);
}