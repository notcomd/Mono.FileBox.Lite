// 本文件包含类型 ModerationResult：内容审核评估的结果。
namespace Mono.FileBox.Lite.Abstractions.Subsystems;

/// <summary>Outcome of a content-moderation evaluation. 中文翻译：内容审核评估的结果。</summary>
public sealed class ModerationResult
{
    public bool Passed { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? PolicyVersion { get; init; }

    public static ModerationResult Pass(string? policyVersion = null) => new()
    {
        Passed = true,
        Reason = "Passed moderation.",
        PolicyVersion = policyVersion
    };

    public static ModerationResult Fail(string reason, string? policyVersion = null) => new()
    {
        Passed = false,
        Reason = reason,
        PolicyVersion = policyVersion
    };
}