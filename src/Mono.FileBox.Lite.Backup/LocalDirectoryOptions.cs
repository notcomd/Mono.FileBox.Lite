// LocalDirectoryOptions.cs - Options for a local-directory backup target.

namespace Mono.FileBox.Lite.Backup.Targets;

/// <summary>Options for a local-directory backup target.
/// 本地目录备份目标的选项。</summary>
public sealed class LocalDirectoryOptions
{
    public string RootPath { get; set; } = string.Empty;
}