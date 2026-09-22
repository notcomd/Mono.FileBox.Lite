// 本文件包含类型 ILeaderElection：在应用启动时选举 leader，不参与转换。
using Mono.FileBox.Lite.Abstractions.Cluster;

namespace Mono.FileBox.Lite.Abstractions.Subsystems;

/// <summary>Elects a leader at application startup. Does not participate in transitions. 中文翻译：在应用启动时选举 leader，不参与状态机转换。</summary>
public interface ILeaderElection
{
    Task<NodeInfo> ElectAsync(CancellationToken ct);
}