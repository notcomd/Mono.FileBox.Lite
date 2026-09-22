// 本文件包含类型 CapacityState：容量水位状态。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Capacity watermark state.</summary>
public enum CapacityState { Normal, Warning, Critical, Full }