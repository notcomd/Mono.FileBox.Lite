// 本文件包含类型 Page<T>：带不透明续接游标的稳定结果页。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>A stable page of results with an opaque continuation cursor.</summary>
public sealed class Page<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public string? NextCursor { get; init; }
    public bool HasMore { get; init; }

    public static Page<T> Empty(string? cursor = null) => new()
    {
        Items = Array.Empty<T>(),
        NextCursor = cursor,
        HasMore = false
    };
}