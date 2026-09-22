// 本文件包含类型 LoggingOptions：日志选项。
namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class LoggingOptions
{
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
    public IList<LogSinkOptions> Sinks { get; set; } = new List<LogSinkOptions>();
    public bool IncludeScopes { get; set; } = true;
    public string OutputTemplate { get; set; }
        = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
}