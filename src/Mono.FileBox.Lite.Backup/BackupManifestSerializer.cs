// BackupManifestSerializer.cs - Serializes backup manifests to and from JSON.

using Mono.FileBox.Lite.Abstractions.Backup;

namespace Mono.FileBox.Lite.Backup.Writer;

internal static class BackupManifestSerializer
{
    private static readonly System.Text.Json.JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static BackupManifest Read(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return System.Text.Json.JsonSerializer.Deserialize<BackupManifest>(reader.ReadToEnd(), Json) ?? new BackupManifest();
    }
}