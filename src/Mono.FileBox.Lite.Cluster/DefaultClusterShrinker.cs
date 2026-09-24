// Contains the DefaultClusterShrinker type, the standalone cluster shrinker implementation.
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Cluster;

/// <summary>Cluster shrinker for standalone operation (no-op drain/remove)
/// 用于独立运行模式的集群收缩器（排空/移除均为空操作）
/// </summary>
public sealed class DefaultClusterShrinker : IClusterShrinker
{
    private readonly INodeRegistry _registry;

    public DefaultClusterShrinker(INodeRegistry registry) => _registry = registry;

    public Task DecommissionAsync(string nodeId, CancellationToken ct) => Task.CompletedTask;
    public Task WaitForDrainAsync(string nodeId, CancellationToken ct) => Task.CompletedTask;
    public Task RemoveAsync(string nodeId, CancellationToken ct) => Task.CompletedTask;
}