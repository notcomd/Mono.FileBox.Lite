// File-level: default IObjectStateMachine implementation that fires a trigger by
// resolving the transition, running guards/observers/actions, committing and persisting state.

using Microsoft.Extensions.DependencyInjection;
using Mono.FileBox.Lite.Abstractions;

namespace Mono.FileBox.Lite.StateMachine;

/// <summary>
/// Default implementation of <see cref="IObjectStateMachine"/>. Fires a trigger by:
/// resolving the transition for (trigger, currentState), running guards, before
/// observers, actions, committing the state, persisting it, then after observers.
/// </summary>
/// <remarks>
/// <b>并发语义</b>：一次 <see cref="FireAsync"/> 只操作传入的单个 <see cref="IObjectContext"/>，
/// 类本身不持有对象级可变状态，因此
/// <list type="bullet">
/// <item>不同 content hash 的对象可并发触发转移（跨对象并行）；</item>
/// <item>同一对象的并发触发是否串行取决于
/// <see cref="Mono.FileBox.Lite.Abstractions.Subsystems.IDistributedLock"/> 实现 ——
/// 默认 <c>NoOpDistributedLock</c> 恒放行，即默认不强制同一对象串行，靠内容寻址幂等兜底。</item>
/// </list>
/// </remarks>
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