// 本文件包含类型 PoolOptions：存储池选项。
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class PoolOptions
{
    public string PoolId { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public StorageTier Tier { get; set; } = StorageTier.Hot;
    public long? CapacityBytes { get; set; }
    public int Priority { get; set; }
    public bool Enabled { get; set; } = true;
}