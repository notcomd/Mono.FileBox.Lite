// 本文件包含类型 ReplicationPolicy：哈希环与一致性计算使用的复制策略。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Replication policy used by the hash ring and consistency computations. 中文翻译：哈希环与一致性计算所使用的复制策略。</summary>
public sealed class ReplicationPolicy
{
    public int Factor { get; init; } = 3;
    public int WriteQuorum { get; init; } = 2;
    public int ReadQuorum { get; init; } = 2;
    public bool CrossRack { get; init; } = true;
    public bool CrossRegion { get; init; }
}