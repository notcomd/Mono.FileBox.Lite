// 本文件包含类型 BackupVerificationReport：备份校验操作的结果。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Outcome of a backup-verification operation. 备份校验操作的结果报告。</summary>
public sealed class BackupVerificationReport
{
    public long VerifiedCount { get; init; }
    public IReadOnlyList<string> CorruptedBlocks { get; init; } = Array.Empty<string>();
    public bool ConsistencyOk { get; init; }
}