// LocalDirectoryOptions.cs - Options for a local-directory backup target.

namespace Mono.FileBox.Lite.Backup.Targets;

/// <summary>Options for a local-directory backup target.</summary>
public sealed class LocalDirectoryOptions
{
    public string RootPath { get; set; } = string.Empty;
}