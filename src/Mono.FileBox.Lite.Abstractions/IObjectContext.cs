namespace Mono.FileBox.Lite.Abstractions;

/// <summary>
/// The per-object context threaded through the lifecycle state machine and all
/// of its guards, actions and observers.
/// </summary>
public interface IObjectContext
{
    /// <summary>Content hash (SHA-256). Immutable once the object enters <see cref="ObjectState.Stored"/>.</summary>
    string ContentHash { get; }

    /// <summary>Namespace / logical group (the engine does not enforce isolation semantics).</summary>
    string NamespaceId { get; }

    /// <summary>Current lifecycle state, maintained by the state machine.</summary>
    ObjectState CurrentState { get; }

    /// <summary>
    /// Extension data slot passed through a transition. All object-level mutable data
    /// must travel here rather than being held as instance state in guards/actions.
    /// </summary>
    IDictionary<string, object> Items { get; }
}

/// <summary>
/// Creates a fresh <see cref="IObjectContext"/> for an incoming command. Contexts
/// are cheap, transient workflow objects tied to a single object instance.
/// </summary>
public interface IObjectContextFactory
{
    IObjectContext Create(NamespaceIdValue ns, string? objectKey = null);

    IObjectContext CreateFromExisting(string contentHash, string namespaceId, ObjectState currentState);
}

/// <summary>Lightweight value wrapper for a namespace id.</summary>
public sealed class NamespaceIdValue
{
    public NamespaceIdValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("NamespaceId must not be empty.", nameof(value));
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}

/// <summary>
/// Mutable view of an <see cref="IObjectContext"/> used by transition actions and the
/// state machine to record derived values (content hash, current state) as the pipeline
/// progresses. Implemented by the concrete mutable context type; guards, observers and
/// downstream primitives should depend on the immutable <see cref="IObjectContext"/>.
/// </summary>
public interface IMutableObjectContext : IObjectContext
{
    new string ContentHash { get; set; }
    new ObjectState CurrentState { get; set; }
}