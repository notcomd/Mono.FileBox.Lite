// 本文件包含类型 RetentionPolicy：保留策略。
namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class RetentionPolicy
{
    public TimeSpan? Retention { get; set; }
    public int? MaxKeep { get; set; }
}