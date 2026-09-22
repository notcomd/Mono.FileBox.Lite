// 本文件包含类型 ValidationError：校验错误。
namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class ValidationError
{
    public string Domain { get; init; } = string.Empty;
    public string Field { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    public override string ToString() => $"[{Domain}] {Field}: {Message}";
}