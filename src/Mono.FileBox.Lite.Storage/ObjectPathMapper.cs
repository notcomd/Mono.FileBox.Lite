// <file>
// ObjectPathMapper: maps a content hash to its physical layout within a storage pool.
// </file>

namespace Mono.FileBox.Lite.Storage;

/// <summary>
/// Maps a content hash to its physical layout within a pool:
/// <c>{root}/blocks/{hash[0:2]}/{hash[2:4]}/{hash}</c>.
/// 将内容哈希映射到存储池内的物理布局：<c>{root}/blocks/{hash[0:2]}/{hash[2:4]}/{hash}</c>。
/// </summary>
public static class ObjectPathMapper
{
    /// <summary>Relative block path for a content hash.
    /// 给定内容哈希返回其相对块路径。</summary>
    public static string RelativeBlockPath(string contentHash)
    {
        if (string.IsNullOrWhiteSpace(contentHash))
            throw new ArgumentException("Content hash must not be empty.", nameof(contentHash));
        return Path.Combine("blocks", contentHash.Substring(0, 2), contentHash.Substring(2, 2), contentHash);
    }

    /// <summary>Absolute block path given a pool root and a content hash.
    /// 给定池根目录与内容哈希返回绝对块路径。</summary>
    public static string Resolve(string poolRoot, string contentHash)
        => Path.Combine(poolRoot, RelativeBlockPath(contentHash));

    /// <summary>Relative object-manifest path for a (chunked) object content hash.
    /// 给定（分块）对象的内容哈希返回其相对对象清单路径。</summary>
    public static string RelativeManifestPath(string contentHash)
        => Path.Combine("manifests", contentHash.Substring(0, 2), contentHash.Substring(2, 2), contentHash + ".manifest");

    /// <summary>Absolute object-manifest path given a pool root and a content hash.
    /// 给定池根目录与内容哈希返回绝对对象清单路径。</summary>
    public static string ResolveManifest(string poolRoot, string contentHash)
        => Path.Combine(poolRoot, RelativeManifestPath(contentHash));
}