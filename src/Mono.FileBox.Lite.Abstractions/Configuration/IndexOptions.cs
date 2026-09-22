// 本文件包含类型 IndexOptions：索引选项。
namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class IndexOptions
{
    public IndexConsistencyMode Consistency { get; set; } = IndexConsistencyMode.Strong;
    public int DefaultPageSize { get; set; } = 100;
    public int MaxPageSize { get; set; } = 1000;
    public IndexFeatureOptions Features { get; set; } = new();
    public IndexShardingOptions Sharding { get; set; } = new();
    public OrderedKvOptions Store { get; set; } = new();
}