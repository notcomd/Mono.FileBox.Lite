using System.Text.Json;
using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Storage.ObjectWriter;

/// <summary>
/// Object-level manifest produced when an object is split into fixed-size chunks.
/// Chunks are owned by the object (no cross-object sharing): the manifest lists the
/// chunk hashes in order, and the physical chunk blocks live under the shared block
/// pool keyed by chunk hash.
/// </summary>
public sealed class ObjectChunkManifest
{
    public string ContentHash { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public long ChunkSize { get; set; }
    public List<string> ChunkHashes { get; set; } = new();
}

/// <summary>Serializes/deserializes <see cref="ObjectChunkManifest"/> and reads/writes it via the physical device.</summary>
public static class ChunkManifestCodec
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static byte[] Encode(ObjectChunkManifest manifest)
        => JsonSerializer.SerializeToUtf8Bytes(manifest, Json);

    public static ObjectChunkManifest Decode(byte[] data)
        => JsonSerializer.Deserialize<ObjectChunkManifest>(data, Json)
           ?? new ObjectChunkManifest();

    /// <summary>Writes the manifest for <paramref name="contentHash"/> under the pool of <paramref name="disk"/>.</summary>
    public static Task WriteAsync(IPhysicalDevice device, LocalDiskHandle disk,
        ObjectChunkManifest manifest, CancellationToken ct)
        => device.WriteBlockAsync(ResolveManifest(disk, manifest.ContentHash), Encode(manifest), ct);

    /// <summary>Reads the manifest for <paramref name="contentHash"/> or <c>null</c> if the object is not chunked.</summary>
    public static async Task<ObjectChunkManifest?> ReadAsync(IPhysicalDevice device, LocalDiskHandle disk,
        string contentHash, CancellationToken ct)
    {
        var path = ResolveManifest(disk, contentHash);
        if (!await device.ExistsAsync(path, ct).ConfigureAwait(false)) return null;
        var data = await device.ReadBlockAsync(path, ct).ConfigureAwait(false);
        return Decode(data.ToArray());
    }

    /// <summary>Deletes the manifest (the caller still deletes the chunks).</summary>
    public static Task DeleteAsync(IPhysicalDevice device, LocalDiskHandle disk,
        string contentHash, CancellationToken ct)
        => device.DeleteBlockAsync(ResolveManifest(disk, contentHash), ct);

    private static string ResolveManifest(LocalDiskHandle disk, string contentHash)
        => ObjectPathMapper.ResolveManifest(disk.RootPath, contentHash);
}