// 本文件包含类型 BackupRequest：发起备份的请求。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Request to start a backup. 中文翻译：发起一次备份操作的请求对象。</summary>
public sealed class BackupRequest
{
    public string TargetId { get; init; } = string.Empty;
    public BackupScope Scope { get; init; } = new();
    public string? Description { get; init; }
}