// 本文件包含类型 OptionsValidationResult：选项校验结果。
namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class OptionsValidationResult
{
    public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();
    public bool IsValid => Errors.Count == 0;
}