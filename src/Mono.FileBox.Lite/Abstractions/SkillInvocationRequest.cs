namespace Mono.FileBox.Lite.Abstractions;

/// <summary>
/// 一次技能调用的请求参数。
/// </summary>
public sealed class SkillInvocationRequest
{
    /// <summary>用于替换参数中的 {input} 令牌的输入文本。</summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>追加的调用参数。</summary>
    public IReadOnlyList<string> Arguments { get; set; } = Array.Empty<string>();

    /// <summary>覆盖默认超时（秒）；小于等于 0 时使用描述符/管理器默认值。</summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>取消令牌。</summary>
    public CancellationToken CancellationToken { get; set; }
}