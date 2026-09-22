// RemoteObjectOptions.cs - Options for a remote object-store backup target.

namespace Mono.FileBox.Lite.Backup.Targets;

/// <summary>Options for a remote object-store backup target.</summary>
public sealed class RemoteObjectOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string? LocalMirrorPath { get; set; }
}