using System.Security.Cryptography;
using System.Text;

namespace Mono.FileBox.Lite.Storage;

/// <summary>SHA-256 content-hash helpers.</summary>
public static class Sha256
{
    /// <summary>Computes the lowercase hex SHA-256 of a byte buffer.</summary>
    public static string Compute(byte[] data)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(data);
        return ToHex(hash);
    }

    /// <summary>Computes the lowercase hex SHA-256 of the content of a stream from its current position.</summary>
    public static string Compute(Stream content, CancellationToken ct = default)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            int read;
            while ((read = content.Read(buffer, 0, buffer.Length)) > 0)
            {
                ct.ThrowIfCancellationRequested();
                sha.TransformBlock(buffer, 0, read, buffer, 0);
            }
            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return ToHex(sha.Hash!);
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>Stable 64-bit hash of a string, used by the consistent-hash ring.</summary>
    public static long StableHash(string value)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        // Fold the first 16 bytes into a long.
        long result = 0;
        for (var i = 0; i < 16; i++)
            result = result * 31 + bytes[i];
        return result;
    }

    private static string ToHex(byte[] hash)
    {
        var builder = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            builder.Append(b.ToString("x2"));
        return builder.ToString();
    }
}