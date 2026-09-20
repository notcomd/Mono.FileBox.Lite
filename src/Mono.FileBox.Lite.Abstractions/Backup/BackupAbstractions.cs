using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Abstractions.Backup;

/// <summary>Type of a backup.</summary>
public enum BackupKind { Full, Incremental, Differential }

/// <summary>Status of a backup point.</summary>
public enum BackupStatus { Pending, Running, Completed, Failed, Corrupted }

/// <summary>Scope of objects included in a backup.</summary>
public sealed class BackupScope
{
    public string? NamespaceId { get; init; }
    public IReadOnlyList<ObjectState>? StateFilter { get; init; }
    public string? KeyPrefix { get; init; }
}

/// <summary>Immutable snapshot representing one backup operation.</summary>
public sealed class BackupPoint
{
    public string BackupId { get; set; } = string.Empty;
    public string? ParentBackupId { get; set; }
    public BackupKind Kind { get; set; }
    public BackupScope Scope { get; set; } = new();
    public string TargetId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public BackupStatus Status { get; set; }
    public long ObjectCount { get; set; }
    public long TotalBytes { get; set; }
    public string ManifestHash { get; set; } = string.Empty;
}

/// <summary>References all blocks belonging to a backup point.</summary>
public sealed class BackupManifest
{
    public string BackupId { get; init; } = string.Empty;
    public IReadOnlyList<BackupEntry> Entries { get; init; } = Array.Empty<BackupEntry>();
    public string ManifestHash { get; init; } = string.Empty;
}

/// <summary>A single object entry inside a backup manifest.</summary>
public sealed class BackupEntry
{
    public string ContentHash { get; init; } = string.Empty;
    public string NamespaceId { get; init; } = string.Empty;
    public string? ObjectKey { get; init; }
    public long SizeBytes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, object> Attributes { get; init; } = new Dictionary<string, object>();
    public StorageTier Tier { get; init; }
    public ObjectState State { get; init; }
    public string BlockPath { get; init; } = string.Empty;
}

/// <summary>Request to start a backup.</summary>
public sealed class BackupRequest
{
    public string TargetId { get; init; } = string.Empty;
    public BackupScope Scope { get; init; } = new();
    public string? Description { get; init; }
}

/// <summary>Query filter for listing backup points.</summary>
public sealed class BackupQuery
{
    public string? TargetId { get; init; }
    public BackupKind? Kind { get; init; }
    public DateTimeOffset? CreatedAfter { get; init; }
}

/// <summary>Request to restore objects from a backup.</summary>
public sealed class RestoreRequest
{
    public string TargetId { get; init; } = string.Empty;
    public string? NamespaceId { get; init; }
    public IReadOnlyList<string>? ContentHashes { get; init; }
    public string? KeyPrefix { get; init; }
    public bool PublishAfterRestore { get; init; } = true;
}

/// <summary>Outcome of a restore operation.</summary>
public sealed class RestoreReport
{
    public long RestoredCount { get; init; }
    public long SkippedCount { get; init; }
    public long FailedCount { get; init; }
    public IReadOnlyList<string> FailedHashes { get; init; } = Array.Empty<string>();
    public bool Succeeded => FailedCount == 0;
}

/// <summary>Outcome of a backup-verification operation.</summary>
public sealed class BackupVerificationReport
{
    public long VerifiedCount { get; init; }
    public IReadOnlyList<string> CorruptedBlocks { get; init; } = Array.Empty<string>();
    public bool ConsistencyOk { get; init; }
}

/// <summary>A backup schedule (cron expression, kind, target, retention).</summary>
public sealed class BackupSchedule
{
    public string ScheduleId { get; init; } = string.Empty;
    public string Cron { get; init; } = string.Empty;
    public BackupKind Kind { get; init; } = BackupKind.Incremental;
    public string TargetId { get; init; } = string.Empty;
    public string? NamespaceId { get; init; }
    public TimeSpan? Retention { get; init; }
    public int? MaxKeep { get; init; }
    public bool Enabled { get; init; } = true;
}

/// <summary>Creates and manages backup points.</summary>
public interface IBackupWriter
{
    Task<BackupPoint> CreateAsync(BackupRequest request, CancellationToken ct);
    Task<BackupPoint> ContinueAsync(
        string parentBackupId, BackupRequest request, CancellationToken ct);
    Task CancelAsync(string backupId, CancellationToken ct);
}

/// <summary>Lists, reads and inspects backup points and their manifests.</summary>
public interface IBackupReader
{
    Task<IReadOnlyList<BackupPoint>> ListAsync(BackupQuery query, CancellationToken ct);
    Task<BackupPoint?> GetAsync(string backupId, CancellationToken ct);
    Task<BackupManifest> ReadManifestAsync(string backupId, CancellationToken ct);
}

/// <summary>Restores objects from a backup point and verifies backup integrity.</summary>
public interface IBackupRestorer
{
    Task<RestoreReport> RestoreAsync(RestoreRequest request, CancellationToken ct);
    Task<RestoreReport> RestoreToPointAsync(
        string backupId, RestoreRequest request, CancellationToken ct);
    Task<BackupVerificationReport> VerifyAsync(string backupId, CancellationToken ct);
}

/// <summary>An abstract backup destination (local dir, object store, ...).</summary>
public interface IBackupTarget
{
    Task<bool> ExistsAsync(string relativePath, CancellationToken ct);
    Task WriteAsync(string relativePath, Stream content, CancellationToken ct);
    Task<Stream> ReadAsync(string relativePath, CancellationToken ct);
    Task DeleteAsync(string relativePath, CancellationToken ct);
    Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken ct);
}

/// <summary>Registers/unregisters backup schedules and lists registered ones.</summary>
public interface IBackupScheduler
{
    Task ScheduleAsync(BackupSchedule schedule, CancellationToken ct);
    Task UnscheduleAsync(string scheduleId, CancellationToken ct);
    Task<IReadOnlyList<BackupSchedule>> ListAsync(CancellationToken ct);
}

/// <summary>Scans the shared backing block pool and reclaims unreferenced blocks.</summary>
public interface IBackupGarbageCollector
{
    Task<long> CollectAsync(CancellationToken ct);
}

/// <summary>Persists backup-point metadata.</summary>
public interface IBackupPointStore
{
    Task SaveAsync(BackupPoint point, CancellationToken ct);
    Task<BackupPoint?> GetAsync(string backupId, CancellationToken ct);
    Task DeleteAsync(string backupId, CancellationToken ct);
    Task<IReadOnlyList<BackupPoint>> ListAsync(BackupQuery query, CancellationToken ct);
}

/// <summary>Persists backup manifests keyed by backup id.</summary>
public interface IBackupManifestStore
{
    Task SaveAsync(string backupId, BackupManifest manifest, CancellationToken ct);
    Task<BackupManifest?> GetAsync(string backupId, CancellationToken ct);
    Task DeleteAsync(string backupId, CancellationToken ct);
}

/// <summary>Resolves a logical target id to a concrete <see cref="IBackupTarget"/>.</summary>
public interface IBackupTargetResolver
{
    IBackupTarget Resolve(string targetId);
    void Register(string targetId, IBackupTarget target);
    IReadOnlyList<string> TargetIds { get; }
}