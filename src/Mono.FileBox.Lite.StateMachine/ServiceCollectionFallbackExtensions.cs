// File-level: internal collection helper used by DefaultObjectStateMachine to add a
// fallback state store without duplicating an already-registered instance.

namespace Mono.FileBox.Lite.StateMachine;

internal static class ServiceCollectionFallbackExtensions
{
    public static void AddIfMissing<T>(this ICollection<T> list, T item)
    {
        if (!list.Contains(item)) list.Add(item);
    }
}