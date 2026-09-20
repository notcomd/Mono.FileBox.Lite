using Mono.FileBox.Lite.Abstractions.Backup;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Abstractions.Configuration;

/// <summary>Index consistency mode.</summary>
public enum IndexConsistencyMode { Strong, Eventual }

/// <summary>Log severity level.</summary>
public enum LogLevel { Trace, Debug, Information, Warning, Error, Critical }

// -------- Root --------

/// <summary>Root option bag for the engine.</summary>
public sealed class FileBoxOptions
{
    public StateMachineOptions StateMachine { get; set; } = new();
    public StorageOptions Storage { get; set; } = new();
    public IndexOptions Index { get; set; } = new();
    public BackupOptions Backup { get; set; } = new();
    public ClusterOptions Cluster { get; set; } = new();
    public ObservabilityOptions Observability { get; set; } = new();
    public LifecycleOptions Lifecycle { get; set; } = new();
}

// -------- State machine --------

public sealed class StateMachineOptions
{
    public bool ThrowOnGuardDenied { get; set; }
    public bool FailFastOnObserverError { get; set; }
    public TimeSpan TransitionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool AuditDeniedTransitions { get; set; } = true;
    public int MaxOptimisticRetries { get; set; } = 3;
    public TimeSpan RetryBackoff { get; set; } = TimeSpan.FromMilliseconds(50);
}

// -------- Storage --------

public sealed class StorageOptions
{
    public IList<PoolOptions> Pools { get; set; } = new List<PoolOptions>();
    public WriteOptions DefaultWrite { get; set; } = new();
    public IOPipelineOptions Pipeline { get; set; } = new();
    public string HashAlgorithm { get; set; } = "SHA-256";
    public DeduplicationMode Deduplication { get; set; } = DeduplicationMode.Global;
    public bool VerifyAfterWrite { get; set; }

    /// <summary>
    /// Object-internal fixed-size chunking. When disabled (default) each object is a
    /// single physical block. When enabled, objects larger than one chunk are split into
    /// fixed-size chunks, each stored as its own block plus an object-level manifest.
    /// </summary>
    public ChunkingOptions Chunking { get; set; } = new();
}

/// <summary>Options for object-internal fixed-size chunking.</summary>
public sealed class ChunkingOptions
{
    /// <summary>Enables chunking (per chunk-block write + object manifest).</summary>
    public bool Enabled { get; set; }

    /// <summary>Target bytes per chunk. Objects larger than this are split.</summary>
    public long ChunkSizeBytes { get; set; } = 8L * 1024 * 1024;
}

public sealed class PoolOptions
{
    public string PoolId { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public StorageTier Tier { get; set; } = StorageTier.Hot;
    public long? CapacityBytes { get; set; }
    public int Priority { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class IOPipelineOptions
{
    public int BufferSize { get; set; } = 64 * 1024;
    public int MaxBatchSize { get; set; } = 64;
    public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan WriteTimeout { get; set; } = TimeSpan.FromSeconds(60);
    public bool EnableZeroCopy { get; set; } = true;
    public int MaxConcurrency { get; set; } = 64;
}

// -------- Index --------

public sealed class IndexOptions
{
    public IndexConsistencyMode Consistency { get; set; } = IndexConsistencyMode.Strong;
    public int DefaultPageSize { get; set; } = 100;
    public int MaxPageSize { get; set; } = 1000;
    public IndexFeatureOptions Features { get; set; } = new();
    public IndexShardingOptions Sharding { get; set; } = new();
    public OrderedKvOptions Store { get; set; } = new();
}

public sealed class IndexFeatureOptions
{
    public bool EnablePrefix { get; set; } = true;
    public bool EnableTagInverted { get; set; } = true;
    public bool EnableAttribute { get; set; } = true;
    public bool EnableTierBitmap { get; set; } = true;
    public bool EnableStateBitmap { get; set; } = true;
    public bool EnableTimeIndex { get; set; } = true;
    public bool EnableSizeIndex { get; set; } = true;
}

public sealed class IndexShardingOptions
{
    public bool Enabled { get; set; }
    public int ShardCount { get; set; } = 1;
    public IndexReplicationPolicy Replication { get; set; } = new();
}

public sealed class OrderedKvOptions
{
    public string Provider { get; set; } = "sqlite";
    public string? ConnectionString { get; set; }
    public int CacheSizeMb { get; set; } = 64;
    public bool SyncWrites { get; set; } = true;
}

// -------- Backup --------

public sealed class BackupOptions
{
    public IList<BackupTargetOptions> Targets { get; set; } = new List<BackupTargetOptions>();
    public IList<BackupScheduleOptions> Schedules { get; set; } = new List<BackupScheduleOptions>();
    public string? DefaultTargetId { get; set; }
    public int MaxConcurrency { get; set; } = 4;
    public long BandwidthLimit { get; set; }
    public int MaxRetries { get; set; } = 3;
    public RetentionPolicy Retention { get; set; } = new();
}

public sealed class BackupTargetOptions
{
    public string TargetId { get; set; } = string.Empty;
    public string Type { get; set; } = "local";
    public string? RootPath { get; set; }
    public string? Endpoint { get; set; }
    public string? Bucket { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class BackupScheduleOptions
{
    public string ScheduleId { get; set; } = string.Empty;
    public string Cron { get; set; } = string.Empty;
    public BackupKind Kind { get; set; } = BackupKind.Incremental;
    public string TargetId { get; set; } = string.Empty;
    public string? NamespaceId { get; set; }
    public TimeSpan? Retention { get; set; }
    public int? MaxKeep { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class RetentionPolicy
{
    public TimeSpan? Retention { get; set; }
    public int? MaxKeep { get; set; }
}

// -------- Cluster --------

public sealed class ClusterOptions
{
    public NodeRole Role { get; set; } = NodeRole.Hybrid;
    public string NodeId { get; set; } = string.Empty;
    public string? AdvertiseAddress { get; set; }
    public IList<string> SeedNodes { get; set; } = new List<string>();
    public ReplicationPolicy Replication { get; set; } = new();
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan NodeTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan LockLease { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan LeaderLease { get; set; } = TimeSpan.FromSeconds(15);
    public CapacityThresholds Thresholds { get; set; } = new();
}

// -------- Observability --------

public sealed class ObservabilityOptions
{
    public LoggingOptions Logging { get; set; } = new();
    public MetricsOptions Metrics { get; set; } = new();
    public TracingOptions Tracing { get; set; } = new();
}

public sealed class LoggingOptions
{
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
    public IList<LogSinkOptions> Sinks { get; set; } = new List<LogSinkOptions>();
    public bool IncludeScopes { get; set; } = true;
    public string OutputTemplate { get; set; }
        = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
}

public sealed class LogSinkOptions
{
    public string Type { get; set; } = "console";
    public string? Path { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class MetricsOptions
{
    public bool Enabled { get; set; } = true;
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(15);
    public int? Port { get; set; }
}

public sealed class TracingOptions
{
    public bool Enabled { get; set; }
    public string? Endpoint { get; set; }
    public double SampleRate { get; set; } = 1.0;
}

// -------- Lifecycle --------

public sealed class LifecycleOptions
{
    public TimeSpan ScanInterval { get; set; } = TimeSpan.FromMinutes(5);
    public int ScanBatchSize { get; set; } = 1000;
    public TimeSpan? ArchiveAfter { get; set; }
    public TimeSpan? DeleteAfter { get; set; }
}

// -------- Validation --------

public sealed class ValidationError
{
    public string Domain { get; init; } = string.Empty;
    public string Field { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    public override string ToString() => $"[{Domain}] {Field}: {Message}";
}

public sealed class OptionsValidationResult
{
    public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();
    public bool IsValid => Errors.Count == 0;
}

/// <summary>Validates an options object at startup and on reload.</summary>
public interface IOptionsValidator<in TOptions>
{
    OptionsValidationResult Validate(TOptions options);
}

/// <summary>Notifies subscribers when options change and reloads configuration sources.</summary>
public interface IOptionsChangeNotifier
{
    IDisposable OnChange<T>(Action<T> callback);
    Task ReloadAsync(CancellationToken ct);
}