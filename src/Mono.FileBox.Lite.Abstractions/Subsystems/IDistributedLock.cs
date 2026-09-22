// 本文件包含类型 IDistributedLock：作为转换前置条件的分布式锁，单节点默认实现恒为 true。
namespace Mono.FileBox.Lite.Abstractions.Subsystems;

/// <summary>
/// Distributed lock used as a transition pre-condition. Its single-node default
/// implementation always returns <c>true</c>.
/// </summary>
public interface IDistributedLock
{
    Task<bool> CanEnterAsync(IObjectContext ctx, CancellationToken ct);
}