// 本文件包含类型 IIndexWriter：写入/更新/移除索引条目，与状态机保持同步。
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Abstractions.Subsystems;

/// <summary>Writes/updates/removes index entries, kept in sync with the state machine.</summary>
public interface IIndexWriter
{
    Task WriteAsync(IObjectContext ctx, CancellationToken ct);
    Task UpdateAsync(string contentHash, IndexUpdate update, CancellationToken ct);
    Task RemoveAsync(string contentHash, CancellationToken ct);
}