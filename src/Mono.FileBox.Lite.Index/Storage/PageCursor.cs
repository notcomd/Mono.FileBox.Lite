// PageCursor.cs — opaque pagination cursor carrying the last content hash.
using System.Text.Json;
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Index.Storage;

/// <summary>
/// Opaque pagination cursor. Carries the last content hash so paging is stable even
/// under concurrent writes.
/// 不透明的分页游标。携带最后一个内容哈希，使分页在并发写入下依然保持稳定。
/// </summary>
public sealed class PageCursor
{
    public string? AfterHash { get; set; }
}