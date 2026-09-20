namespace Mono.FileBox.Lite.Abstractions;

/// <summary>
/// Triggers that drive transitions on the object lifecycle state machine.
/// </summary>
public enum ObjectTrigger
{
    /// <summary>Pending → Stored (content received, persisted).</summary>
    Put,

    /// <summary>Stored → Indexed (metadata written).</summary>
    Index,

    /// <summary>Indexed → Audited (content moderation passes).</summary>
    Audit,

    /// <summary>Audited → Available (publicly readable).</summary>
    Publish,

    /// <summary>Available → Archived (low-cost tier).</summary>
    Archive,

    /// <summary>Archived → Available (restored).</summary>
    Restore,

    /// <summary>Available → Expired (TTL elapsed).</summary>
    Expire,

    /// <summary>Available → Deleted (logical delete).</summary>
    Delete,

    /// <summary>Deleted/Expired → Purged (physical erase, terminal).</summary>
    Purge
}