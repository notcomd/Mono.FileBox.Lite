// 本文件包含类型 FileBoxOptions：引擎的根选项包。
namespace Mono.FileBox.Lite.Abstractions.Configuration;

/// <summary>Root option bag for the engine. 中文翻译：引擎的根选项包，聚合各子系统配置。</summary>
public sealed class FileBoxOptions
{
    public StateMachineOptions StateMachine { get; set; } = new();
    public StorageOptions Storage { get; set; } = new();
    public IndexOptions Index { get; set; } = new();
    public BackupOptions Backup { get; set; } = new();
    public ClusterOptions Cluster { get; set; } = new();
    public ObservabilityOptions Observability { get; set; } = new();
    public LifecycleOptions Lifecycle { get; set; } = new();
}