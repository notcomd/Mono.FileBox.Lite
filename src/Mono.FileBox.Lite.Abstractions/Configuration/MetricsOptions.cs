// 本文件包含类型 MetricsOptions：指标选项。
namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class MetricsOptions
{
    public bool Enabled { get; set; } = true;
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(15);
    public int? Port { get; set; }
}