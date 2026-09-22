// 本文件包含类型 INodeRegistry：注册并列出节点，不参与状态机转换。
using Mono.FileBox.Lite.Abstractions.Cluster;

namespace Mono.FileBox.Lite.Abstractions.Subsystems;

/// <summary>Registers and lists nodes. Does not participate in state-machine transitions.</summary>
public interface INodeRegistry
{
    Task RegisterAsync(NodeInfo node, CancellationToken ct);
    Task<IReadOnlyList<NodeInfo>> ListAsync(CancellationToken ct);
}