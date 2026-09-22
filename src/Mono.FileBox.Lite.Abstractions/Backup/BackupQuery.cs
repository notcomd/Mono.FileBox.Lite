// 本文件包含类型 BackupQuery：列出备份点的查询过滤器。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Query filter for listing backup points.</summary>
public sealed class BackupQuery
{
    public string? TargetId { get; init; }
    public BackupKind? Kind { get; init; }
    public DateTimeOffset? CreatedAfter { get; init; }
}