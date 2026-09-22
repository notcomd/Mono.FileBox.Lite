// 本文件包含类型 GetObjectResult：获取对象结果。
namespace Mono.FileBox.Lite.Abstractions.UseCases;

public sealed class GetObjectResult
{
    public string ContentHash { get; init; } = string.Empty;
    public Stream? Content { get; init; }
    public long Length { get; init; }
    public string? ContentType { get; init; }

    public static GetObjectResult NotFound(string contentHash) => new() { ContentHash = contentHash };
}