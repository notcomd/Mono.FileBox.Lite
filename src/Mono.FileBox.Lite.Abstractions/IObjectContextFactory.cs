// 本文件包含类型 IObjectContextFactory：为传入的命令创建全新的 IObjectContext。
namespace Mono.FileBox.Lite.Abstractions;

/// <summary>
/// Creates a fresh <see cref="IObjectContext"/> for an incoming command. Contexts
/// are cheap, transient workflow objects tied to a single object instance.
/// </summary>
public interface IObjectContextFactory
{
    IObjectContext Create(NamespaceIdValue ns, string? objectKey = null);

    IObjectContext CreateFromExisting(string contentHash, string namespaceId, ObjectState currentState);
}