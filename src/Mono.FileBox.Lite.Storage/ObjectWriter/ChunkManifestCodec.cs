// <file>
// ChunkManifestCodec: serializes ObjectChunkManifest and reads/writes it via the physical device.
// </file>

using System.Text.Json;
using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Storage.ObjectWriter;

/// <summary>Serializes/deserializes <see cref="ObjectChunkManifest"/> and reads/writes it via the physical device.
/// 负责 <see cref="ObjectChunkManifest"/> 的序列化/反序列化，并通过物理设备对其进行读写。</summary>
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

    /// <summary>Writes the given <paramref name="manifest"/> under the pool of <paramref name="device"/>.
    /// 将指定的 <paramref name="manifest"/> 写入 <paramref name="device"/> 对应的存储池。</summary>
    public static Task WriteAsync(IPhysicalDevice device, LocalDiskHandle disk,
        ObjectChunkManifest manifest, CancellationToken ct)
        => device.WriteBlockAsync(ResolveManifest(disk, manifest.ContentHash), Encode(manifest), ct);

    /// <summary>Reads the manifest for <paramref name="contentHash"/> or <c>null</c> if the object is not chunked.
    /// 读取 <paramref name="contentHash"/> 对应的清单；若对象未分块则返回 <c>null</c>。</summary>
    public static async Task<ObjectChunkManifest?> ReadAsync(IPhysicalDevice device, LocalDiskHandle disk,
        string contentHash, CancellationToken ct)
    {
        var path = ResolveManifest(disk, contentHash);
        if (!await device.ExistsAsync(path, ct).ConfigureAwait(false)) return null;
        var data = await device.ReadBlockAsync(path, ct).ConfigureAwait(false);
        return Decode(data.ToArray());
    }

    /// <summary>Deletes the manifest (the caller still deletes the chunks).
    /// 删除清单文件（分块仍由调用方负责删除）。</summary>
    public static Task DeleteAsync(IPhysicalDevice device, LocalDiskHandle disk,
        string contentHash, CancellationToken ct)
        => device.DeleteBlockAsync(ResolveManifest(disk, contentHash), ct);

    private static string ResolveManifest(LocalDiskHandle disk, string contentHash)
        => ObjectPathMapper.ResolveManifest(disk.RootPath, contentHash);
}