namespace Mono.FileBox.Lite.Abstractions;

/// <summary>
/// Lifecycle states of a file object. Each stored object is an instance of the
/// object lifecycle state machine.
/// </summary>
public enum ObjectState
{
    /// <summary>Write request received, bytes not yet persisted.</summary>
    Pending,

    /// <summary>Content persisted and addressed by SHA-256. Immutable hash.</summary>
    Stored,

    /// <summary>Metadata written to the index.</summary>
    Indexed,

    /// <summary>Content moderation passed.</summary>
    Audited,

    /// <summary>Publicly readable.</summary>
    Available,

    /// <summary>Archived on a low-cost tier. Requires Restore to become readable.</summary>
    Archived,

    /// <summary>Logically expired, awaiting purge.</summary>
    Expired,

    /// <summary>Logically deleted (recovery window preserved).</summary>
    Deleted,

    /// <summary>Physically erased. Terminal state.</summary>
    Purged
}