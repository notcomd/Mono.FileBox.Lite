// 本文件包含类型 LogSinkOptions：日志输出目标选项。
namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class LogSinkOptions
{
    public string Type { get; set; } = "console";
    public string? Path { get; set; }
    public bool Enabled { get; set; } = true;
}