// 本文件包含类型 IBackupTargetResolver：将逻辑目标 id 解析为具体的 IBackupTarget。
namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Resolves a logical target id to a concrete <see cref="IBackupTarget"/>.</summary>
public interface IBackupTargetResolver
{
    IBackupTarget Resolve(string targetId);
    void Register(string targetId, IBackupTarget target);
    IReadOnlyList<string> TargetIds { get; }
}