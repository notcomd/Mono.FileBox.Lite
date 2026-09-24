using Microsoft.Extensions.DependencyInjection;
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.StateMachine;

namespace Mono.FileBox.Lite.Functional.Tests;

/// <summary>State machine semantics: transitions, guards, observers, idempotency.
/// 状态机语义：转移、守卫、观察者、幂等性。</summary>
public class StateMachineTests
{
    private static readonly CancellationToken None = CancellationToken.None;

    [Fact]
    public async Task Fire_LegalTransition_ExecutesActionAndCommitsState()
    {
        var (machine, fakes) = Build(registry =>
        {
            registry.On(ObjectTrigger.Put).From(ObjectState.Pending).To(ObjectState.Stored)
                .Guard<AlwaysGuard>().Do<NoopAction>().Observe<HappyObserver>();
        });

        var ctx = Ctx();
        var result = await machine.FireAsync(ObjectTrigger.Put, ctx, None);

        Assert.Equal(ObjectState.Stored, result);
        Assert.Equal(ObjectState.Stored, ctx.CurrentState);
        Assert.Equal(1, fakes.Action.RunCount);
        Assert.Equal(1, fakes.Observer.AfterCount);
    }

    [Fact]
    public async Task Fire_UnregisteredTransition_ReturnsSameState_NoSideEffects()
    {
        var (machine, fakes) = Build(registry =>
        {
            registry.On(ObjectTrigger.Put).From(ObjectState.Pending).To(ObjectState.Stored)
                .Guard<AlwaysGuard>().Do<NoopAction>().Observe<HappyObserver>();
        });

        var ctx = Ctx();
        var result = await machine.FireAsync(ObjectTrigger.Publish, ctx, None); // unregistered from Pending

        Assert.Equal(ObjectState.Pending, result);
        Assert.Equal(0, fakes.Action.RunCount);
        Assert.Equal(0, fakes.Observer.AfterCount);
    }

    [Fact]
    public async Task GuardDenied_RejectsTransition_ActionsAndObserversDoNotRun()
    {
        var (machine, fakes) = Build(registry =>
        {
            registry.On(ObjectTrigger.Put).From(ObjectState.Pending).To(ObjectState.Stored)
                .Guard<DenyGuard>().Do<NoopAction>().Observe<HappyObserver>();
        });

        var ctx = Ctx();
        var result = await machine.FireAsync(ObjectTrigger.Put, ctx, None);

        Assert.Equal(ObjectState.Pending, result);
        Assert.Equal(0, fakes.Action.RunCount);
        Assert.Equal(0, fakes.Observer.AfterCount);
    }

    [Fact]
    public async Task GuardDenied_WithThrowFlag_Throws()
    {
        var (machine, _) = Build(registry =>
        {
            registry.On(ObjectTrigger.Put).From(ObjectState.Pending).To(ObjectState.Stored).Guard<DenyGuard>();
        }, throwOnGuardDenied: true);

        await Assert.ThrowsAsync<GuardDeniedException>(
            () => machine.FireAsync(ObjectTrigger.Put, Ctx(), None));
    }

    [Fact]
    public async Task ObserverThrows_WithoutFailFast_DoesNotBlockTransition()
    {
        var (machine, fakes) = Build(registry =>
        {
            registry.On(ObjectTrigger.Put).From(ObjectState.Pending).To(ObjectState.Stored)
                .Do<NoopAction>().Observe<ThrowObserver>();
        }, failFast: false);

        var result = await machine.FireAsync(ObjectTrigger.Put, Ctx(), None);

        Assert.Equal(ObjectState.Stored, result);
        Assert.Equal(1, fakes.Action.RunCount);
    }

    [Fact]
    public async Task ObserverThrows_WithFailFast_BlocksTransition()
    {
        var (machine, fakes) = Build(registry =>
        {
            registry.On(ObjectTrigger.Put).From(ObjectState.Pending).To(ObjectState.Stored)
                .Do<NoopAction>().Observe<ThrowObserver>();
        }, failFast: true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => machine.FireAsync(ObjectTrigger.Put, Ctx(), None));
        Assert.Equal(1, fakes.Action.RunCount); // action ran before the after-observer threw
    }

    [Fact]
    public async Task Purge_AfterPurged_IsNotAllowed()
    {
        var (machine, _) = Build(registry =>
        {
            registry.On(ObjectTrigger.Purge).From(ObjectState.Deleted).To(ObjectState.Purged);
        });

        var deleted = Ctx(ObjectState.Deleted);
        var purged = await machine.FireAsync(ObjectTrigger.Purge, deleted, None);
        Assert.Equal(ObjectState.Purged, purged);

        var again = await machine.FireAsync(ObjectTrigger.Purge, Ctx(ObjectState.Purged), None);
        Assert.Equal(ObjectState.Purged, again);
    }

    [Fact]
    public async Task Purge_FromExpired_IsAllowed()
    {
        var (machine, _) = Build(registry =>
        {
            registry.On(ObjectTrigger.Purge).From(ObjectState.Expired).To(ObjectState.Purged);
        });

        var purged = await machine.FireAsync(ObjectTrigger.Purge, Ctx(ObjectState.Expired), None);
        Assert.Equal(ObjectState.Purged, purged);
    }

    // -------- helpers / fakes --------

    private static IObjectContext Ctx(ObjectState state = ObjectState.Pending)
        => new DefaultObjectContext("ns1", currentState: state);

    private static (IObjectStateMachine machine, Fakes fakes) Build(
        Action<TransitionRegistry> configure,
        bool failFast = false,
        bool throwOnGuardDenied = false)
    {
        var registry = new TransitionRegistry();
        configure(registry);

        var fakes = new Fakes();
        var services = new ServiceCollection();
        services.AddSingleton(registry);
        // Concrete types are what the registry references; role interfaces are also exposed.
        services.AddSingleton<AlwaysGuard>(fakes.Always);
        services.AddSingleton<DenyGuard>(fakes.Deny);
        services.AddSingleton<NoopAction>(fakes.Action);
        services.AddSingleton<HappyObserver>(fakes.Observer);
        services.AddSingleton<ThrowObserver>(fakes.Thrower);
        services.AddSingleton<IGuard>(fakes.Always);
        services.AddSingleton<IGuard>(fakes.Deny);
        services.AddSingleton<ITransitionAction>(fakes.Action);
        services.AddSingleton<ITransitionObserver>(fakes.Observer);
        services.AddSingleton<ITransitionObserver>(fakes.Thrower);
        var sp = services.BuildServiceProvider();

        var machine = new DefaultObjectStateMachine(
            registry, sp,
            failFastOnObserverError: failFast,
            throwOnGuardDenied: throwOnGuardDenied);
        return (machine, fakes);
    }

    private sealed class Fakes
    {
        public Fakes()
        {
            Always = new AlwaysGuard();
            Deny = new DenyGuard();
            Action = new NoopAction();
            Observer = new HappyObserver();
            Thrower = new ThrowObserver();
        }

        public AlwaysGuard Always { get; }
        public DenyGuard Deny { get; }
        public NoopAction Action { get; }
        public HappyObserver Observer { get; }
        public ThrowObserver Thrower { get; }
    }

    private sealed class AlwaysGuard : IGuard
    {
        public Task<bool> CanEnterAsync(IObjectContext ctx, CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class DenyGuard : IGuard
    {
        public Task<bool> CanEnterAsync(IObjectContext ctx, CancellationToken ct) => Task.FromResult(false);
    }

    private sealed class NoopAction : ITransitionAction
    {
        public int RunCount;
        public Task ExecuteAsync(IObjectContext ctx, CancellationToken ct)
        {
            RunCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowObserver : ITransitionObserver
    {
        public int AfterCount;
        public Task OnBeforeAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct) => Task.CompletedTask;
        public Task OnAfterAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct)
        {
            AfterCount++;
            throw new InvalidOperationException("observer failure");
        }
    }

    private sealed class HappyObserver : ITransitionObserver
    {
        public int AfterCount;
        public Task OnBeforeAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct) => Task.CompletedTask;
        public Task OnAfterAsync(ObjectTrigger t, IObjectContext ctx, CancellationToken ct)
        {
            AfterCount++;
            return Task.CompletedTask;
        }
    }
}