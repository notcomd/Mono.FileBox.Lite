using System.Text;
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

/// <summary>Encodes/decodes a <see cref="PageCursor"/> as base64 JSON.</summary>
public sealed class Base64JsonCursorCodec : ICursorCodec
{
    public string Encode(object cursor)
        => Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(cursor));

    public object Decode(string encoded)
    {
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new PageCursor
        {
            AfterHash = root.TryGetProperty("AfterHash", out var h) ? h.GetString() : null
        };
    }
}