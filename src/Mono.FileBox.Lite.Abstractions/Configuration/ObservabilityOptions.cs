// 本文件包含类型 ObservabilityOptions：可观测性选项。
namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class ObservabilityOptions
{
    public LoggingOptions Logging { get; set; } = new();
    public MetricsOptions Metrics { get; set; } = new();
    public TracingOptions Tracing { get; set; } = new();
}