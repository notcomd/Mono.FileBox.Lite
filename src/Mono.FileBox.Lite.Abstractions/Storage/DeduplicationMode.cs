// 本文件包含类型 DeduplicationMode：去重策略。
namespace Mono.FileBox.Lite.Abstractions.Storage;

/// <summary>Deduplication strategy. 去重策略（全局/命名空间内/禁用）。</summary>
public enum DeduplicationMode
{
    /// <summary>Deduplicate against all existing content hash keys.</summary>
    Global,

    /// <summary>Deduplicate only within the same namespace.</summary>
    NamespaceScoped,

    /// <summary>No deduplication; always write a fresh physical block.</summary>
    Disabled
}