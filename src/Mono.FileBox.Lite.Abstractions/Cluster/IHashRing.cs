// 本文件包含类型 IHashRing：将键映射到副本节点的一致哈希环。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>A consistent hash ring mapping keys to replica nodes. 中文翻译：将键映射到副本节点的一致哈希环。</summary>
public interface IHashRing
{
    IReadOnlyList<NodeInfo> SelectNodes(string key, int replicas);
    int VirtualNodeCount { get; }
    void Rebuild(IReadOnlyList<NodeInfo> nodes);
}