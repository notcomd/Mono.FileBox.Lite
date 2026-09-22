// File-level: the transition registry that stores and looks up allowed transitions
// keyed by (trigger, from), exposing the fluent Registration API.

using Mono.FileBox.Lite.Abstractions;

namespace Mono.FileBox.Lite.StateMachine;

/// <summary>
/// Registry of allowed state transitions, keyed by (trigger, from). A transition is
/// allowed only if a registration exists for its current (trigger, from) pair.
/// </summary>
public sealed class TransitionRegistry
{
    private readonly Dictionary<(ObjectTrigger, ObjectState), TransitionRegistration> _map = new();

    /// <summary>Begins configuring the transition triggered by <paramref name="trigger"/>.</summary>
    public TransitionBuilder On(ObjectTrigger trigger)
    {
        return new(this, trigger);
    }


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