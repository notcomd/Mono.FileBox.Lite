// 本文件包含类型 IndexShardingOptions：索引分片选项。
using Mono.FileBox.Lite.Abstractions.Cluster;

namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class IndexShardingOptions
{
    public bool Enabled { get; set; }
    public int ShardCount { get; set; } = 1;
    public IndexReplicationPolicy Replication { get; set; } = new();
}