// 本文件包含类型 WriteOptions：写入对象内容时应用的选项。
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Abstractions.Storage;

/// <summary>Options applied when writing an object's content. 中文翻译：写入对象内容时应用的选项。</summary>
public sealed class WriteOptions
{
    public StorageTier Tier { get; set; } = StorageTier.Hot;
    public string? PoolId { get; set; }
    public string? NamespaceId { get; set; }
    public long? BandwidthLimit { get; set; }
}