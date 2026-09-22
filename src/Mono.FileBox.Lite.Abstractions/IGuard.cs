// 本文件包含类型 IGuard：转换的前置条件，按注册顺序执行，任一返回 false 则拒绝转换。
namespace Mono.FileBox.Lite.Abstractions;

/// <summary>
/// Transition pre-condition. Guards are executed in registration order; if any
/// returns <c>false</c> the transition is rejected and no action or observer runs.
/// Guards must be stateless.
/// 中文翻译：转换前置条件。守卫按注册顺序执行，任一返回 false 即拒绝转换，且不执行任何 action 或 observer；守卫必须无状态。
/// </summary>
public interface IGuard
{
    Task<bool> CanEnterAsync(IObjectContext ctx, CancellationToken ct);
}