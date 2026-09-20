using Mono.FileBox.Lite.Abstractions;

namespace Mono.FileBox.Lite.StateMachine;

/// <summary>A single registered transition: trigger, source/target states and component lists.</summary>
public sealed class TransitionRegistration
{
    public ObjectTrigger Trigger { get; set; }
    public ObjectState From { get; set; }
    public ObjectState To { get; set; }
    public List<Type> Guards { get; } = new();
    public List<Type> Actions { get; } = new();
    public List<Type> Observers { get; } = new();
}

/// <summary>
/// Registry of allowed state transitions, keyed by (trigger, from). A transition is
/// allowed only if a registration exists for its current (trigger, from) pair.
/// </summary>
public sealed class TransitionRegistry
{
    private readonly Dictionary<(ObjectTrigger, ObjectState), TransitionRegistration> _map = new();

    /// <summary>Begins configuring the transition triggered by <paramref name="trigger"/>.</summary>
    public TransitionBuilder On(ObjectTrigger trigger) => new(this, trigger);

    internal void Register(TransitionRegistration registration)
    {
        var key = (registration.Trigger, registration.From);
        if (_map.ContainsKey(key))
            throw new InvalidOperationException(
                $"Transition for trigger '{registration.Trigger}' from '{registration.From}' is already registered.");
        _map[key] = registration;
    }

    /// <summary>Looks up a registration for (trigger, from). Returns false when disallowed.</summary>
    public bool TryGet(ObjectTrigger trigger, ObjectState from, out TransitionRegistration registration)
        => _map.TryGetValue((trigger, from), out registration!);

    /// <summary>True when firing <paramref name="trigger"/> from <paramref name="from"/> is registered.</summary>
    public bool IsAllowed(ObjectTrigger trigger, ObjectState from)
        => _map.ContainsKey((trigger, from));

    public IReadOnlyCollection<TransitionRegistration> All => _map.Values;
}

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

    /// <summary>Registers a <see cref="IGuard"/> to run as a pre-condition.</summary>
    public TransitionBuilder Guard<T>() where T : class, IGuard
    {
        _registration.Guards.Add(typeof(T));
        return this;
    }

    /// <summary>Registers an <see cref="ITransitionAction"/> to execute.</summary>
    public TransitionBuilder Do<T>() where T : class, ITransitionAction
    {
        _registration.Actions.Add(typeof(T));
        return this;
    }

    /// <summary>Registers an <see cref="ITransitionObserver"/> as a side-channel.</summary>
    public TransitionBuilder Observe<T>() where T : class, ITransitionObserver
    {
        _registration.Observers.Add(typeof(T));
        return this;
    }
}