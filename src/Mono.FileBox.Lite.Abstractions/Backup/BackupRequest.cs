// 本文件包含类型 BackupRequest：发起备份的请求。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Request to start a backup.</summary>
public sealed class BackupRequest
{
    public string TargetId { get; init; } = string.Empty;
    public BackupScope Scope { get; init; } = new();
    public string? Description { get; init; }
}