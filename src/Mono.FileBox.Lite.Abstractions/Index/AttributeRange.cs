// 本文件包含类型 AttributeRange：数值属性值的闭区间范围。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>Inclusive range over a numeric attribute value. 数值属性值的闭区间范围。</summary>
public sealed class AttributeRange
{
    public string AttributeName { get; init; } = string.Empty;
    public object? Min { get; init; }
    public object? Max { get; init; }

    public bool Contains(object? value)
    {
        if (value is not IComparable comparable) return true;
        if (Min is IComparable minCmp && comparable.CompareTo(minCmp) < 0) return false;
        if (Max is IComparable maxCmp && comparable.CompareTo(maxCmp) > 0) return false;
        return true;
    }
}