// 本文件包含类型 IIndexReplicator：异步的跨节点索引同步。
namespace Mono.FileBox.Lite.Abstractions.Subsystems;

/// <summary>Asynchronous cross-node index synchronization.</summary>
public interface IIndexReplicator
{
    Task PushAsync(string namespaceId, CancellationToken ct);
    Task PullAsync(string namespaceId, CancellationToken ct);
}