// 本文件包含类型 IIndexMaintainer：以物理块为权威来源重建、校验并修复索引。
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Abstractions.Subsystems;

/// <summary>Rebuilds, verifies and repairs the index using the physical blocks as the authoritative source. 中文翻译：以物理块为权威来源，对索引进行重建、校验与修复。</summary>
public interface IIndexMaintainer
{
    Task RebuildAsync(IndexRebuildOptions options, CancellationToken ct);
    Task<IndexConsistencyReport> VerifyAsync(CancellationToken ct);
    Task RepairAsync(IndexConsistencyReport report, CancellationToken ct);
}