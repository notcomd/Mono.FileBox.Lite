// 本文件包含类型 PageRequest：分页游标请求。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>Pagination cursor request.</summary>
public sealed class PageRequest
{
    public int Size { get; init; } = 100;
    public string? Cursor { get; init; }
}