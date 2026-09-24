using Mono.FileBox.Lite.Abstractions.Backup;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Backup.Restorer;

/// <summary>
/// Restores objects from a backup point. Blocks are pulled from the shared block pool
/// and written back through the content-addressed writer (deduplication), then the
/// index entries are rebuilt.
/// 从某个备份点恢复对象。先从共享块池拉取块，再通过内容寻址写入器写回（去重），随后重建索引条目。
/// </summary>
public sealed class DefaultBackupRestorer : IBackupRestorer
{
    private readonly IBackupTargetResolver _targets;
    private readonly IBackupPointStore _points;
    private readonly IBackupManifestStore _manifests;
    private readonly IObjectWriter _content;
    private readonly IIndexWriter? _index;

    public DefaultBackupRestorer(
        IBackupTargetResolver targets,
        IBackupPointStore points,
        IBackupManifestStore manifests,
        IObjectWriter content,
        IIndexWriter? index = null)
    {
        _targets = targets;
        _points = points;
        _manifests = manifests;
        _content = content;
        _index = index;
    }

    public async Task<RestoreReport> RestoreAsync(RestoreRequest request, CancellationToken ct)
    {
        // Restore the most recent point for the target.
        var point = (await _points.ListAsync(new BackupQuery { TargetId = request.TargetId }, ct).ConfigureAwait(false))
            .Where(p => p.Status == BackupStatus.Completed)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault();
        if (point is null) return new RestoreReport { FailedCount = 1 };

        return await RestoreToPointAsync(point.BackupId, request, ct).ConfigureAwait(false);
    }

    public async Task<RestoreReport> RestoreToPointAsync(
        string backupId, RestoreRequest request, CancellationToken ct)
    {
        var manifest = await _manifests.GetAsync(backupId, ct).ConfigureAwait(false)
                       ?? new BackupManifest { BackupId = backupId };
        var target = _targets.Resolve(request.TargetId);

        IEnumerable<BackupEntry> entries = manifest.Entries;
        if (request.ContentHashes is not null)
        {
            var wanted = new HashSet<string>(request.ContentHashes, StringComparer.Ordinal);
            entries = entries.Where(e => wanted.Contains(e.ContentHash));
        }
        if (request.KeyPrefix is not null)
            entries = entries.Where(e => e.ObjectKey?.StartsWith(request.KeyPrefix, StringComparison.Ordinal) == true);

        long restored = 0, skipped = 0;
        var failed = new List<string>();
        foreach (var entry in entries)
        {
            try
            {
                using var stream = await target.ReadAsync(entry.BlockPath, ct).ConfigureAwait(false);
                var hash = await _content.WriteAsync(stream, new WriteOptions { Tier = entry.Tier }, ct).ConfigureAwait(false);
                if (!string.Equals(hash, entry.ContentHash, StringComparison.Ordinal))
                {
                    // Content mismatch: content addressing refused the block.
                    failed.Add(entry.ContentHash);
                    continue;
                }
                if (_index is not null)
                {
                    await _index.UpdateAsync(hash, new IndexUpdate
                    {
                        State = entry.State,
                        Tier = entry.Tier,
                        ModifiedAt = DateTimeOffset.UtcNow
                    }, ct).ConfigureAwait(false);
                }
                restored++;
            }
            catch
            {
                failed.Add(entry.ContentHash);
            }
        }

        return new RestoreReport
        {
            RestoredCount = restored,
            SkippedCount = skipped,
            FailedCount = failed.Count,
            FailedHashes = failed
        };
    }

    public async Task<BackupVerificationReport> VerifyAsync(string backupId, CancellationToken ct)
    {
        var manifest = await _manifests.GetAsync(backupId, ct).ConfigureAwait(false)
                       ?? new BackupManifest { BackupId = backupId };

        var point = await _points.GetAsync(backupId, ct).ConfigureAwait(false);
        var targetId = point?.TargetId ?? _targets.TargetIds.FirstOrDefault() ?? "local";
        var target = _targets.Resolve(targetId);

        var corrupted = new List<string>();
        foreach (var entry in manifest.Entries)
        {
            if (!await target.ExistsAsync(entry.BlockPath, ct).ConfigureAwait(false))
                corrupted.Add(entry.ContentHash);
        }

        return new BackupVerificationReport
        {
            VerifiedCount = manifest.Entries.Count - corrupted.Count,
            CorruptedBlocks = corrupted,
            ConsistencyOk = corrupted.Count == 0
        };
    }
}