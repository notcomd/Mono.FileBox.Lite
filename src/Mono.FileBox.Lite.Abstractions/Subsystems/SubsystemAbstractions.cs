using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Abstractions.Subsystems;

// -------- Content layer --------

/// <summary>Content-addressed object writer (SHA-256 key, deduplication).</summary>
public interface IObjectWriter
{
    Task<string> WriteAsync(Stream content, WriteOptions options, CancellationToken ct);
    Task<bool> ExistsAsync(string contentHash, CancellationToken ct);
}

/// <summary>Content-addressed object reader for range reads.</summary>
public interface IObjectReader
{
    Task<Stream> ReadAsync(
        string contentHash, long offset, long length, CancellationToken ct);
}

// -------- Index --------

/// <summary>Writes/updates/removes index entries, kept in sync with the state machine.</summary>
public interface IIndexWriter
{
    Task WriteAsync(IObjectContext ctx, CancellationToken ct);
    Task UpdateAsync(string contentHash, IndexUpdate update, CancellationToken ct);
    Task RemoveAsync(string contentHash, CancellationToken ct);
}

/// <summary>Multi-dimensional query reader over the index.</summary>
public interface IIndexReader
{
    Task<Page<IndexEntry>> QueryAsync(IndexQuery query, CancellationToken ct);
}

/// <summary>Rebuilds, verifies and repairs the index using the physical blocks as the authoritative source.</summary>
public interface IIndexMaintainer
{
    Task RebuildAsync(IndexRebuildOptions options, CancellationToken ct);
    Task<IndexConsistencyReport> VerifyAsync(CancellationToken ct);
    Task RepairAsync(IndexConsistencyReport report, CancellationToken ct);
}

/// <summary>Asynchronous cross-node index synchronization.</summary>
public interface IIndexReplicator
{
    Task PushAsync(string namespaceId, CancellationToken ct);
    Task PullAsync(string namespaceId, CancellationToken ct);
}

// -------- Moderation --------

/// <summary>Decides whether object content passes the configured moderation policy.</summary>
public interface IContentModerator
{
    Task<ModerationResult> ModerateAsync(IObjectContext ctx, CancellationToken ct);
}

/// <summary>Outcome of a content-moderation evaluation.</summary>
public sealed class ModerationResult
{
    public bool Passed { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? PolicyVersion { get; init; }

    public static ModerationResult Pass(string? policyVersion = null) => new()
    {
        Passed = true,
        Reason = "Passed moderation.",
        PolicyVersion = policyVersion
    };

    public static ModerationResult Fail(string reason, string? policyVersion = null) => new()
    {
        Passed = false,
        Reason = reason,
        PolicyVersion = policyVersion
    };
}

// -------- Physical erase --------

/// <summary>Confirms a physical block and its index entries have been erased.</summary>
public interface IPhysicalEraser
{
    Task EraseAsync(IObjectContext ctx, CancellationToken ct);
}

// -------- Cluster coordination --------

/// <summary>
/// Distributed lock used as a transition pre-condition. Its single-node default
/// implementation always returns <c>true</c>.
/// </summary>
public interface IDistributedLock
{
    Task<bool> CanEnterAsync(IObjectContext ctx, CancellationToken ct);
}

/// <summary>Registers and lists nodes. Does not participate in state-machine transitions.</summary>
public interface INodeRegistry
{
    Task RegisterAsync(NodeInfo node, CancellationToken ct);
    Task<IReadOnlyList<NodeInfo>> ListAsync(CancellationToken ct);
}

/// <summary>Elects a leader at application startup. Does not participate in transitions.</summary>
public interface ILeaderElection
{
    Task<NodeInfo> ElectAsync(CancellationToken ct);
}

// -------- Lifecycle --------

/// <summary>Guard that decides whether an object satisfies an expire/archive policy.</summary>
public interface ILifecyclePolicy
{
    Task<bool> CanEnterAsync(IObjectContext ctx, CancellationToken ct);
}

/// <summary>Periodically scans objects and fires Expire/Archive/Purge transitions.</summary>
public interface ILifecycleScheduler
{
    Task ScanAsync(CancellationToken ct);
}

// -------- Observability --------

/// <summary>Publishes object lifecycle events to interested consumers.</summary>
public interface IEventBus
{
    Task PublishAsync(string topic, object payload, CancellationToken ct);
}

/// <summary>Logs lifecycle transitions. Must not block the main transition.</summary>
public interface ITransitionLogger
{
    void Log(ObjectTrigger t, ObjectState from, ObjectState to);
}