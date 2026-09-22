// 本文件包含类型 BackupTargetOptions：备份目标选项。
namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class BackupTargetOptions
{
    public string TargetId { get; set; } = string.Empty;
    public string Type { get; set; } = "local";
    public string? RootPath { get; set; }
    public string? Endpoint { get; set; }
    public string? Bucket { get; set; }
    public bool Enabled { get; set; } = true;
}