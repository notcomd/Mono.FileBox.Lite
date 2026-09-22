// 本文件包含类型 ThresholdCallback：容量阈值被跨越时调用的回调。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Callback invoked when a capacity threshold is crossed.</summary>
public delegate void ThresholdCallback(CapacityStatus status);