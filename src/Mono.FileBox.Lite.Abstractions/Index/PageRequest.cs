// 本文件包含类型 PageRequest：分页游标请求。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>Pagination cursor request. 中文翻译：分页游标请求（当前页大小与续接游标）。</summary>
public sealed class PageRequest
{
    public int Size { get; init; } = 100;
    public string? Cursor { get; init; }
}