// 本文件包含类型 ILifecyclePolicy：判断对象是否满足过期/归档策略的 guard。
namespace Mono.FileBox.Lite.Abstractions.Subsystems;

/// <summary>Guard that decides whether an object satisfies an expire/archive policy. 
/// 判断对象是否满足过期/归档策略的守卫。</summary>
public interface ILifecyclePolicy
{
    Task<bool> CanEnterAsync(IObjectContext ctx, CancellationToken ct);
}