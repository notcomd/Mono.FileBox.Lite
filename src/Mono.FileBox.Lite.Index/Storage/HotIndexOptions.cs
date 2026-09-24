// HotIndexOptions.cs — 热点索引缓存（HotCachingIndexStore）的可配置参数。
namespace Mono.FileBox.Lite.Index.Storage;

/// <summary>
/// Options for the hot-index cache: how many entries stay resident in memory and how
/// often an entry must be read before it is promoted into the hot set.
/// 热点索引缓存的配置项：控制多少条目常驻内存，以及条目需被读取多少次才会被提升进热点集合。
/// </summary>
public sealed class HotIndexOptions
{
    /// <summary>Maximum number of hot entries kept in memory (LRU-evicted on overflow).
    /// 内存中最多保留的热点条目数量（超出容量时按 LRU 淘汰）。
    /// </summary>
    public int Capacity { get; set; } = 512;

    /// <summary>Read count required before an entry is promoted into the hot set.
    /// 条目在进入热点集合前所需达到的读取次数。
    /// </summary>
    public int PromotionThreshold { get; set; } = 3;

    /// <summary>
    /// Path of the sidecar "hot index" file where promoted entries are persisted, so the
    /// process warm-starts with already-hot entries in memory (avoiding main-file parsing).
    /// When <c>null</c> hot persistence is disabled.
    /// 热点索引旁路侧写文件路径，被提升的条目会持久化于此，使进程冷启动时即可在内存中
    /// 直接获得热点条目（避免解析主索引文件）；当为 <c>null</c> 时禁用热点持久化。
    /// </summary>
    public string? HotFilePath { get; set; }

    /// <summary>Whether to persist the hot set to <see cref="HotFilePath"/> on promotion.
    /// 是否在提升（促进入热点）时把热点集合持久化到 <see cref="HotFilePath"/>。
    /// </summary>
    public bool PersistHot { get; set; } = true;
}