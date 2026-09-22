// 本文件包含类型 BackupScope：备份中包含的对象范围。
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Scope of objects included in a backup.</summary>
public sealed class BackupScope
{
    public string? NamespaceId { get; init; }
    public IReadOnlyList<ObjectState>? StateFilter { get; init; }
    public string? KeyPrefix { get; init; }
}