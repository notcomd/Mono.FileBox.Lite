// File-level: fluent builder for declaring a single transition on a TransitionRegistry,
// mirroring the documented composition API.

using Mono.FileBox.Lite.Abstractions;

namespace Mono.FileBox.Lite.StateMachine;

/// <summary>Fluent builder for a single transition, mirroring the documented composition API.</summary>
public sealed class TransitionBuilder
{
    private readonly TransitionRegistry _registry;
    private readonly TransitionRegistration _registration;

    internal TransitionBuilder(TransitionRegistry registry, ObjectTrigger trigger)
    {
        _registry = registry;
        _registration = new TransitionRegistration { Trigger = trigger };
    }


    public TransitionBuilder From(ObjectState from)
    {
        _registration.From = from;
        return this;
    }

    public TransitionBuilder To(ObjectState to)
    {
        _registration.To = to;
        _registry.Register(_registration);
        return this;
    }

    /// <summary>
    /// Registers a guard component. The type is resolved through DI at fire time and
    /// cast to <see cref="IGuard"/>; its concrete implementation must therefore also
    /// implement <see cref="IGuard"/>. This mirrors the documented
    /// <c>.Guard&lt;IDistributedLock&gt;()</c> usage where the service and role interfaces
    /// are implemented by the same concrete type.
    /// </summary>
    public TransitionBuilder Guard<T>() where T : class
    {
        _registration.Guards.Add(typeof(T));
        return this;
    }

    /// <summary>Registers an action component (resolved and cast to <see cref="ITransitionAction"/>).</summary>
    public TransitionBuilder Do<T>() where T : class
    {
        _registration.Actions.Add(typeof(T));
        return this;
    }

    /// <summary>Registers an observer component (resolved and cast to <see cref="ITransitionObserver"/>).</summary>
    public TransitionBuilder Observe<T>() where T : class
    {
        _registration.Observers.Add(typeof(T));
        return this;
    }
}