// 本文件包含类型 TracingOptions：跟踪选项。
namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class TracingOptions
{
    public bool Enabled { get; set; }
    public string? Endpoint { get; set; }
    public double SampleRate { get; set; } = 1.0;
}