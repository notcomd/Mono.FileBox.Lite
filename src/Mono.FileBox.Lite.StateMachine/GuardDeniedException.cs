// File-level: exception thrown when a transition guard rejects the transition and the
// machine is configured to throw.

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