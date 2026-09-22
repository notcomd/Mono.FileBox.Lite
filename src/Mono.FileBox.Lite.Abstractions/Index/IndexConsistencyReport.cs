// 本文件包含类型 IndexConsistencyReport：对照权威物理块校验索引一致性的结果。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>Verifies index consistency against the authoritative physical blocks.</summary>
public sealed class IndexConsistencyReport
{
    public IReadOnlyList<string> MissingIndexEntries { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> OrphanedIndexEntries { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> StateMismatches { get; init; } = Array.Empty<string>();
    public bool IsConsistent => MissingIndexEntries.Count == 0
                                && OrphanedIndexEntries.Count == 0
                                && StateMismatches.Count == 0;
}