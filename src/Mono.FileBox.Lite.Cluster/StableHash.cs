// Contains the StableHash type used to place nodes/virtual nodes on the consistent hash ring.
using System.Security.Cryptography;
using System.Text;

namespace Mono.FileBox.Lite.Cluster.HashRing;

/// <summary>Stable 64-bit hash used to place nodes/virtual nodes on the ring.</summary>
internal static class StableHash
{
    public static long Compute(string value)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        long result = 0;
        for (var i = 0; i < 16; i++)
            result = result * 31 + hash[i];
        return result;
    }
}