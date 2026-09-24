// 本文件包含类型 RestoreReport：恢复操作的结果。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Outcome of a restore operation. 一次恢复操作的结果报告。</summary>
public sealed class RestoreReport
{
    public long RestoredCount { get; init; }
    public long SkippedCount { get; init; }
    public long FailedCount { get; init; }
    public IReadOnlyList<string> FailedHashes { get; init; } = Array.Empty<string>();
    public bool Succeeded => FailedCount == 0;
}