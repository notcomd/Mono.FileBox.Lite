// 本文件包含类型 RestoreObjectResult：恢复对象结果。
namespace Mono.FileBox.Lite.Abstractions.UseCases;

public sealed class RestoreObjectResult
{
    public string ContentHash { get; init; } = string.Empty;
    public ObjectState State { get; init; }
    public bool Restored { get; init; }

    public static RestoreObjectResult Success(string hash, ObjectState state)
        => new() { ContentHash = hash, State = state, Restored = true };
}