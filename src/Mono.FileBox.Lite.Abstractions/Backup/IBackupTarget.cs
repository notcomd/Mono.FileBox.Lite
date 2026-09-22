// 本文件包含类型 IBackupTarget：抽象备份目标（本地目录、对象存储等）。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>An abstract backup destination (local dir, object store, ...).</summary>
public interface IBackupTarget
{
    Task<bool> ExistsAsync(string relativePath, CancellationToken ct);
    Task WriteAsync(string relativePath, Stream content, CancellationToken ct);
    Task<Stream> ReadAsync(string relativePath, CancellationToken ct);
    Task DeleteAsync(string relativePath, CancellationToken ct);
    Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken ct);
}