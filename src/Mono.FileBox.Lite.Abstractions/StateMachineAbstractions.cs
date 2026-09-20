namespace Mono.FileBox.Lite.Abstractions;

/// <summary>
/// Transition pre-condition. Guards are executed in registration order; if any
/// returns <c>false</c> the transition is rejected and no action or observer runs.
/// Guards must be stateless.
/// </summary>
public interface IGuard
{
    Task<bool> CanEnterAsync(IObjectContext ctx, CancellationToken ct);
}

/// <summary>
/// Transition execution body. Actions run sequentially in registration order after
/// guards pass. Actions must be stateless.
/// </summary>
public interface ITransitionAction
{
    Task ExecuteAsync(IObjectContext ctx, CancellationToken ct);
}

/// <summary>
/// Transition side-channel. Observers run before and after a committed transition
/// and, by default, do not block the main transition when they throw.
/// </summary>
public interface ITransitionObserver
{
    Task OnBeforeAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct);
    Task OnAfterAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct);
}

/// <summary>
/// The lifecycle state machine. Dispatches a trigger against the current state,
/// evaluating guards, observers and actions, and commits the new state on success.
/// </summary>
public interface IObjectStateMachine
{
    /// <summary>
    /// Fires <paramref name="trigger"/> against <paramref name="ctx"/>. Returns the
    /// resulting state (unchanged when the transition is not allowed or a guard fails).
    /// </summary>
    Task<ObjectState> FireAsync(
        ObjectTrigger trigger, IObjectContext ctx, CancellationToken ct);
}

/// <summary>
/// Persists the current lifecycle state of an object independently of the in-memory
/// state machine. Concurrency is handled optimistically via a version counter.
/// </summary>
public interface IStateStore
{
    Task<ObjectState> LoadAsync(string contentHash, CancellationToken ct);

    /// <summary>
    /// Saves <paramref name="state"/> for <paramref name="contentHash"/>. Returns
    /// <c>false</c> on an optimistic-concurrency conflict (caller retries).
    /// </summary>
    Task<bool> SaveAsync(string contentHash, ObjectState state, CancellationToken ct);
}

/// <summary>Outcome classification for a transition attempt.</summary>
public enum TransitionResult
{
    /// <summary>Transition executed and the state committed.</summary>
    Allowed,

    /// <summary>The (trigger, state) pair is not registered; state unchanged.</summary>
    NotAllowed,

    /// <summary>At least one guard rejected the transition; state unchanged.</summary>
    Denied
}