// 本文件包含类型 LongRange：64 位值的闭区间范围。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>Inclusive range of 64-bit values. 中文翻译：64 位整数值的闭区间范围。</summary>
public sealed class LongRange
{
    public long? Min { get; init; }
    public long? Max { get; init; }

    public static LongRange AtLeast(long min) => new() { Min = min };
    public static LongRange AtMost(long max) => new() { Max = max };
    public static LongRange Between(long min, long max) => new() { Min = min, Max = max };
}