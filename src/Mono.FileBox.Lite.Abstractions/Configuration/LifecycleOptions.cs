// 本文件包含类型 LifecycleOptions：生命周期选项。
namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class LifecycleOptions
{
    public TimeSpan ScanInterval { get; set; } = TimeSpan.FromMinutes(5);
    public int ScanBatchSize { get; set; } = 1000;
    public TimeSpan? ArchiveAfter { get; set; }
    public TimeSpan? DeleteAfter { get; set; }
}