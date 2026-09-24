// 本文件包含类型 ICapacityMonitor：监控容量并触发阈值回调。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Monitors capacity and raises threshold callbacks. 监控容量水位并在达到阈值时触发回调。</summary>
public interface ICapacityMonitor
{
    Task<CapacityStatus> GetStatusAsync(CancellationToken ct);
    void OnThreshold(ThresholdCallback callback);
}