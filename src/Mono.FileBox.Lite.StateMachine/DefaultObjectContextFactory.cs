using Mono.FileBox.Lite.Abstractions;

namespace Mono.FileBox.Lite.StateMachine;

/// <summary>
/// Creates <see cref="DefaultObjectContext"/> instances for the use-case layer.
/// </summary>
public sealed class DefaultObjectContextFactory : IObjectContextFactory
{
    public IObjectContext Create(NamespaceIdValue ns, string? objectKey = null)
        => new DefaultObjectContext(ns.Value, objectKey: objectKey);

    public IObjectContext CreateFromExisting(
        string contentHash, string namespaceId, ObjectState currentState)
        => new DefaultObjectContext(namespaceId, contentHash, currentState);
}