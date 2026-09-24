// 本文件包含类型 IBackupTargetResolver：将逻辑目标 id 解析为具体的 IBackupTarget。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Resolves a logical target id to a concrete <see cref="IBackupTarget"/>. 将逻辑目标 id 解析为具体 IBackupTarget 的解析器。</summary>
public interface IBackupTargetResolver
{
    IBackupTarget Resolve(string targetId);
    void Register(string targetId, IBackupTarget target);
    IReadOnlyList<string> TargetIds { get; }
}