// 本文件包含类型 ILifecycleScheduler：周期性扫描对象并触发 Expire/Archive/Purge 转换。
namespace Mono.FileBox.Lite.Abstractions.Subsystems;

/// <summary>Periodically scans objects and fires Expire/Archive/Purge transitions.</summary>
public interface ILifecycleScheduler
{
    Task ScanAsync(CancellationToken ct);
}