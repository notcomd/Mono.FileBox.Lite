using Mono.FileBox.Lite.Abstractions.Backup;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Backup.Targets;

namespace Mono.FileBox.Lite.Backup.Writer;

/// <summary>
/// Creates full and incremental backup points over the shared block pool. Content
/// addressing means blocks are never rewritten when they already exist; an incremental
/// backup only writes the blocks not referenced by its parent manifest.
/// </summary>
public sealed class DefaultBackupWriter : IBackupWriter
{
    private readonly IBackupTargetResolver _targets;
    private readonly IBackupPointStore _points;
    private readonly IBackupManifestStore _manifests;
    private readonly IIndexReader _index;
    private readonly IObjectReader _content;

    public DefaultBackupWriter(
        IBackupTargetResolver targets,
        IBackupPointStore points,
        IBackupManifestStore manifests,
        IIndexReader index,
        IObjectReader content)
    {
        _targets = targets;
        _points = points;
        _manifests = manifests;
        _index = index;
        _content = content;
    }

    public Task<BackupPoint> CreateAsync(BackupRequest request, CancellationToken ct)
        => CreateInternalAsync(request, null, ct);

    public Task<BackupPoint> ContinueAsync(
        string parentBackupId, BackupRequest request, CancellationToken ct)
        => CreateInternalAsync(request, parentBackupId, ct);

    public async Task CancelAsync(string backupId, CancellationToken ct)
    {
        var point = await _points.GetAsync(backupId, ct).ConfigureAwait(false);
        if (point is null) return;
        point.Status = BackupStatus.Failed;
        point.CompletedAt = DateTimeOffset.UtcNow;
        await _points.SaveAsync(point, ct).ConfigureAwait(false);
    }

    private async Task<BackupPoint> CreateInternalAsync(
        BackupRequest request, string? parentBackupId, CancellationToken ct)
    {
        var target = _targets.Resolve(request.TargetId);
        var isIncremental = parentBackupId is not null;
        var backupId = Guid.NewGuid().ToString("N");

        var point = new BackupPoint
        {
            BackupId = backupId,
            ParentBackupId = parentBackupId,
            Kind = isIncremental ? BackupKind.Incremental : BackupKind.Full,
            Scope = request.Scope,
            TargetId = request.TargetId,
            CreatedAt = DateTimeOffset.UtcNow,
            Status = BackupStatus.Running
        };
        await _points.SaveAsync(point, ct).ConfigureAwait(false);

        var candidates = await CollectCandidatesAsync(request.Scope, ct).ConfigureAwait(false);
        var parentHashes = isIncremental
            ? new HashSet<string>((await LoadManifestAsync(target, parentBackupId!, ct).ConfigureAwait(false)).Entries
                .Select(e => e.ContentHash), StringComparer.Ordinal)
            : null;

        var entries = new List<BackupEntry>();
        foreach (var entry in candidates)
        {
            if (ct.IsCancellationRequested) break;
            if (parentHashes is not null && parentHashes.Contains(entry.ContentHash)) continue;

            var blockRel = BackupLayout.BlockPath(entry.ContentHash);
            if (!await target.ExistsAsync(blockRel, ct).ConfigureAwait(false))
            {
                using var stream = await _content.ReadAsync(entry.ContentHash, 0, -1, ct).ConfigureAwait(false);
                await target.WriteAsync(blockRel, stream, ct).ConfigureAwait(false);
            }

            entries.Add(new BackupEntry
            {
                ContentHash = entry.ContentHash,
                NamespaceId = entry.NamespaceId,
                ObjectKey = entry.ObjectKey,
                SizeBytes = entry.SizeBytes,
                CreatedAt = entry.CreatedAt,
                Tags = entry.Tags,
                Attributes = entry.Attributes,
                Tier = entry.Tier,
                State = entry.State,
                BlockPath = blockRel
            });
        }

        // Incremental manifest = parent manifest ∪ new entries.
        if (parentHashes is not null)
        {
            var parentEntries = (await LoadManifestAsync(target, parentBackupId!, ct).ConfigureAwait(false)).Entries;
            var union = new Dictionary<string, BackupEntry>(StringComparer.Ordinal);
            foreach (var e in parentEntries) union[e.ContentHash] = e;
            foreach (var e in entries) union[e.ContentHash] = e;
            entries = union.Values.ToList();
        }

        var manifest = new BackupManifest
        {
            BackupId = backupId,
            Entries = entries,
            ManifestHash = ComputeHash(entries)
        };
        await BackupLayout.WriteManifestAsync(target, backupId, manifest, ct).ConfigureAwait(false);
        await _manifests.SaveAsync(backupId, manifest, ct).ConfigureAwait(false);

        point.ManifestHash = manifest.ManifestHash;
        point.ObjectCount = entries.Count;
        point.TotalBytes = entries.Sum(e => e.SizeBytes);
        point.CompletedAt = DateTimeOffset.UtcNow;
        point.Status = BackupStatus.Completed;
        await _points.SaveAsync(point, ct).ConfigureAwait(false);

        return point;
    }

    private async Task<IReadOnlyList<IndexEntry>> CollectCandidatesAsync(
        BackupScope scope, CancellationToken ct)
    {
        var ns = string.IsNullOrWhiteSpace(scope.NamespaceId) ? "default" : scope.NamespaceId!;
        var result = new List<IndexEntry>();
        string? cursor = null;
        do
        {
            var page = await _index.QueryAsync(new IndexQuery
            {
                NamespaceId = ns,
                States = scope.StateFilter,
                KeyPrefix = scope.KeyPrefix,
                Page = new PageRequest { Size = 500, Cursor = cursor }
            }, ct).ConfigureAwait(false);
            result.AddRange(page.Items);
            cursor = page.NextCursor;
        }
        while (cursor is not null);
        return result;
    }

    private async Task<BackupManifest> LoadManifestAsync(
        IBackupTarget target, string backupId, CancellationToken ct)
    {
        var local = await _manifests.GetAsync(backupId, ct).ConfigureAwait(false);
        if (local is not null) return local;

        var rel = BackupLayout.ManifestPath(backupId);
        using var stream = await target.ReadAsync(rel, ct).ConfigureAwait(false);
        return BackupManifestSerializer.Read(stream);
    }

    private static string ComputeHash(IReadOnlyList<BackupEntry> entries)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        foreach (var e in entries.OrderBy(e => e.ContentHash, StringComparer.Ordinal))
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(e.ContentHash);
            sha.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
        }
        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return BitConverter.ToString(sha.Hash!).Replace("-", "").ToLowerInvariant();
    }
}

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