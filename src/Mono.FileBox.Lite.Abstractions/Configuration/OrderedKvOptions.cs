// 本文件包含类型 OrderedKvOptions：有序键值存储选项。
namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class OrderedKvOptions
{
    public string Provider { get; set; } = "sqlite";
    public string? ConnectionString { get; set; }
    public int CacheSizeMb { get; set; } = 64;
    public bool SyncWrites { get; set; } = true;
}