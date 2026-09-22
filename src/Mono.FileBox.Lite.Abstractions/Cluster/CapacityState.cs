// 本文件包含类型 CapacityState：容量水位状态。
namespace Mono.FileBox.Lite.Abstractions.Cluster;

/// <summary>Capacity watermark state. 中文翻译：容量水位状态（正常/告警/临界/已满）。</summary>
public enum CapacityState { Normal, Warning, Critical, Full }