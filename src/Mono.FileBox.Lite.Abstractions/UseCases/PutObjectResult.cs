// 本文件包含类型 PutObjectResult：完整放置管线结果，含产出的内容哈希。
namespace Mono.FileBox.Lite.Abstractions.UseCases;

/// <summary>Result of a full put pipeline. Contains the produced content hash. 完整放置（put）管线结果，包含产出的内容哈希。</summary>
public sealed class PutObjectResult
{
    public string ContentHash { get; init; } = string.Empty;
    public ObjectState State { get; init; }
    public bool Succeeded { get; init; }
    public string? Error { get; init; }

    public static PutObjectResult Success(string hash, ObjectState state)
        => new() { ContentHash = hash, State = state, Succeeded = true };

    public static PutObjectResult Failure(Exception ex, ObjectState state)
        => new() { State = state, Succeeded = false, Error = ex.Message };
}