// File-level documentation: Internal helper holding the state-key constant used by
// transition loggers to read the "from" state. Extracted from the original Events.cs.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems.Events;

internal static class TransitionLoggerStateKey
{
    public const string From = "TransitionFrom";
}