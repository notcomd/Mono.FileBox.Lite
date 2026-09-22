// Base64JsonCursorCodec.cs — encodes/decodes a PageCursor as base64 JSON.
using System.Text;
using System.Text.Json;
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Index.Storage;

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