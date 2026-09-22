// PageCursor.cs — opaque pagination cursor carrying the last content hash.
using System.Text.Json;
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Index.Storage;

/// <summary>
/// Opaque pagination cursor. Carries the last content hash so paging is stable even
/// under concurrent writes.
/// </summary>
public sealed class PageCursor
{
    public string? AfterHash { get; set; }
}