// 本文件包含类型 TimeRange：日期/时间的闭区间范围。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>Inclusive range of date/time values. 日期/时间值的闭区间范围。</summary>
public sealed class TimeRange
{
    public DateTimeOffset? Start { get; init; }
    public DateTimeOffset? End { get; init; }

    public bool Contains(DateTimeOffset value)
        => (!Start.HasValue || value >= Start.Value)
           && (!End.HasValue || value <= End.Value);

    public static TimeRange All() => new();
    public static TimeRange Between(DateTimeOffset start, DateTimeOffset end)
        => new() { Start = start, End = end };
}