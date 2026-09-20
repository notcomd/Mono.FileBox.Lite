using Microsoft.Extensions.DependencyInjection;
using Mono.FileBox.Lite.Abstractions;

namespace Mono.FileBox.Lite.StateMachine;

/// <summary>Thrown when a transition guard rejects the transition and the machine is configured to throw.</summary>
public sealed class GuardDeniedException : InvalidOperationException
{
    public GuardDeniedException(ObjectTrigger trigger, ObjectState from)
        : base($"Guard denied transition '{trigger}' from '{from}'.")
    {
        Trigger = trigger;
        From = from;
    }

    public ObjectTrigger Trigger { get; }
    public ObjectState From { get; }
}

/// <summary>
/// Default implementation of <see cref="IObjectStateMachine"/>. Fires a trigger by:
/// resolving the transition for (trigger, currentState), running guards, before
/// observers, actions, committing the state, persisting it, then after observers.
/// </summary>
public sealed class DefaultObjectStateMachine : IObjectStateMachine
{
    private readonly TransitionRegistry _registry;
    private readonly IServiceProvider _services;
    private readonly IStateStore? _stateStore;
    private readonly bool _throwOnGuardDenied;
    private readonly bool _failFastOnObserverError;
    private readonly int _maxOptimisticRetries;
    private readonly TimeSpan _retryBackoff;

    public DefaultObjectStateMachine(
        TransitionRegistry registry,
        IServiceProvider services,
        IStateStore? stateStore = null,
        bool throwOnGuardDenied = false,
        bool failFastOnObserverError = false,
        int maxOptimisticRetries = 3,
        TimeSpan? retryBackoff = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _stateStore = stateStore;
        _throwOnGuardDenied = throwOnGuardDenied;
        _failFastOnObserverError = failFastOnObserverError;
        _maxOptimisticRetries = Math.Max(0, maxOptimisticRetries);
        _retryBackoff = retryBackoff ?? TimeSpan.FromMilliseconds(50);
    }

    public async Task<ObjectState> FireAsync(
        ObjectTrigger trigger, IObjectContext ctx, CancellationToken ct)
    {
        if (!_registry.TryGet(trigger, ctx.CurrentState, out var reg))
            return ctx.CurrentState; // TransitionResult.NotAllowed

        foreach (var guardType in reg.Guards)
        {
            ct.ThrowIfCancellationRequested();
            var guard = (IGuard)Resolve(guardType);
            var allowed = await guard.CanEnterAsync(ctx, ct).ConfigureAwait(false);
            if (!allowed)
            {
                if (_throwOnGuardDenied)
                    throw new GuardDeniedException(trigger, ctx.CurrentState);
                return ctx.CurrentState; // TransitionResult.Denied
            }
        }

        foreach (var observerType in reg.Observers)
        {
            ct.ThrowIfCancellationRequested();
            var observer = (ITransitionObserver)Resolve(observerType);
            await SafeObserverAsync(() => observer.OnBeforeAsync(trigger, ctx, ct)).ConfigureAwait(false);
        }

        foreach (var actionType in reg.Actions)
        {
            ct.ThrowIfCancellationRequested();
            var action = (ITransitionAction)Resolve(actionType);
            await action.ExecuteAsync(ctx, ct).ConfigureAwait(false);
        }

        SetState(ctx, reg.To);
        await PersistAsync(ctx, reg.To, ct).ConfigureAwait(false);

        foreach (var observerType in reg.Observers)
        {
            ct.ThrowIfCancellationRequested();
            var observer = (ITransitionObserver)Resolve(observerType);
            await SafeObserverAsync(() => observer.OnAfterAsync(trigger, ctx, ct)).ConfigureAwait(false);
        }

        return reg.To;
    }

    private async Task SafeObserverAsync(Func<Task> observer)
    {
        try
        {
            await observer().ConfigureAwait(false);
        }
        catch (Exception) when (!_failFastOnObserverError)
        {
            // Side-channel observers must not block the main transition by default.
        }
    }

    private async Task PersistAsync(IObjectContext ctx, ObjectState state, CancellationToken ct)
    {
        if (_stateStore is null) return;

        var contentHash = ContentHashOf(ctx);
        // Resolve every registered state store (typically a single instance).
        var stores = _services.GetServices(typeof(IStateStore)).Cast<IStateStore>().ToList();
        stores.AddIfMissing(_stateStore);

        for (var attempt = 0; attempt <= _maxOptimisticRetries; attempt++)
        {
            var conflicted = false;
            foreach (var store in stores)
            {
                if (!await store.SaveAsync(contentHash, state, ct).ConfigureAwait(false))
                {
                    conflicted = true;
                    break;
                }
            }
            if (!conflicted) return;
            if (_retryBackoff > TimeSpan.Zero)
                await Task.Delay(_retryBackoff, ct).ConfigureAwait(false);
        }
    }

    private static string ContentHashOf(IObjectContext ctx)
        => ctx is DefaultObjectContext d && !string.IsNullOrWhiteSpace(d.ContentHash)
            ? d.ContentHash
            : $"ns:{ctx.NamespaceId}:{Guid.NewGuid():N}";

    private static void SetState(IObjectContext ctx, ObjectState state)
    {
        if (ctx is IMutableObjectContext mutable)
            mutable.CurrentState = state;
    }

    private object Resolve(Type type)
        => _services.GetService(type)
           ?? throw new InvalidOperationException(
               $"Service '{type.FullName}' is referenced by a transition but is not registered for dependency injection.");
}

internal static class ServiceCollectionFallbackExtensions
{
    public static void AddIfMissing<T>(this ICollection<T> list, T item)
    {
        if (!list.Contains(item)) list.Add(item);
    }
}