namespace Mono.FileBox.Lite.Abstractions;

/// <summary>
/// 一次技能调用的结果。
/// </summary>
public sealed class SkillResult
{
    /// <summary>是否成功（退出码为 0 且未超时/未取消/无启动异常）。</summary>
    public bool IsSuccess => ExitCode == 0 && !IsTimedOut && !IsCanceled && Error == null;

    /// <summary>进程退出码。</summary>
    public int ExitCode { get; set; }

    /// <summary>标准输出内容。</summary>
    public string StandardOutput { get; set; } = string.Empty;

    /// <summary>标准错误内容。</summary>
    public string StandardError { get; set; } = string.Empty;

    /// <summary>是否因超时被终止。</summary>
    public bool IsTimedOut { get; set; }

    /// <summary>是否因取消令牌被放弃。</summary>
    public bool IsCanceled { get; set; }

    /// <summary>启动进程时的异常信息；无则 null。</summary>
    public string? Error { get; set; }

    /// <summary>实际执行的命令。</summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>实际执行的参数。</summary>
    public string Arguments { get; set; } = string.Empty;

    /// <summary>开始时间。</summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>结束时间。</summary>
    public DateTimeOffset FinishedAt { get; set; }

    /// <summary>耗时（毫秒）。</summary>
    public long DurationMilliseconds => Math.Max(0, (long)(FinishedAt - StartedAt).TotalMilliseconds);
}