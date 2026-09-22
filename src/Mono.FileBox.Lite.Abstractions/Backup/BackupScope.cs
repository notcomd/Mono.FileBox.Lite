// 本文件包含类型 BackupScope：备份中包含的对象范围。
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Scope of objects included in a backup. 中文翻译：一次备份所包含对象的范围（命名空间、状态筛选、键前缀）。</summary>
public sealed class BackupScope
{
    public string? NamespaceId { get; init; }
    public IReadOnlyList<ObjectState>? StateFilter { get; init; }
    public string? KeyPrefix { get; init; }
}