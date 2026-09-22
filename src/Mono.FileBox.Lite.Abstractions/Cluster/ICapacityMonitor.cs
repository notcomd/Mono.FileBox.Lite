// 本文件包含类型 ICapacityMonitor：监控容量并触发阈值回调。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Monitors capacity and raises threshold callbacks.</summary>
public interface ICapacityMonitor
{
    Task<CapacityStatus> GetStatusAsync(CancellationToken ct);
    void OnThreshold(ThresholdCallback callback);
}