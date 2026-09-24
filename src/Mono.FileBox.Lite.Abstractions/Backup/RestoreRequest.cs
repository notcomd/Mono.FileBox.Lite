// 本文件包含类型 RestoreRequest：从备份恢复对象的请求。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Request to restore objects from a backup. 从备份恢复对象的请求对象。</summary>
public sealed class RestoreRequest
{
    public string TargetId { get; init; } = string.Empty;
    public string? NamespaceId { get; init; }
    public IReadOnlyList<string>? ContentHashes { get; init; }
    public string? KeyPrefix { get; init; }
    public bool PublishAfterRestore { get; init; } = true;
}