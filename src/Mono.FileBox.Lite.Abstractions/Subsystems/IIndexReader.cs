// 本文件包含类型 IIndexReader：索引上的多维查询读取器。
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Abstractions.Subsystems;

/// <summary>Multi-dimensional query reader over the index.</summary>
public interface IIndexReader
{
    Task<Page<IndexEntry>> QueryAsync(IndexQuery query, CancellationToken ct);
}