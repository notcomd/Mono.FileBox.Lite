// 本文件包含类型 ArchiveObjectResult：归档对象结果。
namespace Mono.FileBox.Lite.Abstractions.UseCases;

public sealed class ArchiveObjectResult
{
    public string ContentHash { get; init; } = string.Empty;
    public ObjectState State { get; init; }
    public bool Archived { get; init; }

    public static ArchiveObjectResult Success(string hash, ObjectState state)
        => new() { ContentHash = hash, State = state, Archived = true };
}