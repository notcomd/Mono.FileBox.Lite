// 本文件包含类型 IClusterShrinker：收缩/退役节点。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Shrinks / decommissions nodes. 收缩/退役节点，包含排空等待与移除能力。</summary>
public interface IClusterShrinker
{
    Task DecommissionAsync(string nodeId, CancellationToken ct);
    Task WaitForDrainAsync(string nodeId, CancellationToken ct);
    Task RemoveAsync(string nodeId, CancellationToken ct);
}