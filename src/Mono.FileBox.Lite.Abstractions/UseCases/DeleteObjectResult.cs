// 本文件包含类型 DeleteObjectResult：删除对象结果。
namespace Mono.FileBox.Lite.Abstractions.UseCases;

public sealed class DeleteObjectResult
{
    public string ContentHash { get; init; } = string.Empty;
    public ObjectState State { get; init; }
    public bool Deleted { get; init; }

    public static DeleteObjectResult Success(string hash, ObjectState state)
        => new() { ContentHash = hash, State = state, Deleted = true };
}