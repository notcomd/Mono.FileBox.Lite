// 本文件包含类型 ITransitionAction：转换的执行体，guard 通过后按注册顺序依次执行。
namespace Mono.FileBox.Lite.Abstractions;

/// <summary>
/// Transition execution body. Actions run sequentially in registration order after
/// guards pass. Actions must be stateless.
/// 中文翻译：转换执行体。guard 通过后按注册顺序依次执行；action 必须无状态。
/// </summary>
public interface ITransitionAction
{
    Task ExecuteAsync(IObjectContext ctx, CancellationToken ct);
}