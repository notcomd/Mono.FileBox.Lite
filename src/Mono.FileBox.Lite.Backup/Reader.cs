using Mono.FileBox.Lite.Abstractions.Backup;

namespace Mono.FileBox.Lite.Backup.Reader;

/// <summary>Default backup reader backed by the metadata stores.</summary>
public sealed class DefaultBackupReader : IBackupReader
{
    private readonly IBackupPointStore _points;
    private readonly IBackupManifestStore _manifests;

    public DefaultBackupReader(IBackupPointStore points, IBackupManifestStore manifests)
    {
        _points = points;
        _manifests = manifests;
    }

    public Task<IReadOnlyList<BackupPoint>> ListAsync(BackupQuery query, CancellationToken ct)
        => _points.ListAsync(query, ct);

    public Task<BackupPoint?> GetAsync(string backupId, CancellationToken ct)
        => _points.GetAsync(backupId, ct);

    public Task<BackupManifest> ReadManifestAsync(string backupId, CancellationToken ct)
    {
        async Task<BackupManifest> Read()
        {
            var manifest = await _manifests.GetAsync(backupId, ct).ConfigureAwait(false);
            return manifest ?? new BackupManifest { BackupId = backupId };
        }
        return Read();
    }
}