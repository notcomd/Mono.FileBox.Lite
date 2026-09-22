// 本文件包含类型 StateMachineOptions：状态机选项。
namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class StateMachineOptions
{
    public bool ThrowOnGuardDenied { get; set; }
    public bool FailFastOnObserverError { get; set; }
    public TimeSpan TransitionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool AuditDeniedTransitions { get; set; } = true;
    public int MaxOptimisticRetries { get; set; } = 3;
    public TimeSpan RetryBackoff { get; set; } = TimeSpan.FromMilliseconds(50);
}