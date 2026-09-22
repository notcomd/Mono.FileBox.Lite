// HotIndexOptions.cs — 热点索引缓存（HotCachingIndexStore）的可配置参数。
namespace Mono.FileBox.Lite.Index.Storage;

/// <summary>
/// Options for the hot-index cache: how many entries stay resident in memory and how
/// often an entry must be read before it is promoted into the hot set.
/// </summary>
public sealed class HotIndexOptions
{
    /// <summary>Maximum number of hot entries kept in memory (LRU-evicted on overflow).</summary>
    public int Capacity { get; set; } = 512;

    /// <summary>Read count required before an entry is promoted into the hot set.</summary>
    public int PromotionThreshold { get; set; } = 3;

    /// <summary>
    /// Path of the sidecar "hot index" file where promoted entries are persisted, so the
    /// process warm-starts with already-hot entries in memory (avoiding main-file parsing).
    /// When <c>null</c> hot persistence is disabled.
    /// </summary>
    public string? HotFilePath { get; set; }

    /// <summary>Whether to persist the hot set to <see cref="HotFilePath"/> on promotion.</summary>
    public bool PersistHot { get; set; } = true;
}