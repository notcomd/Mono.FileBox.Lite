// 本文件包含类型 IPhysicalEraser：确认物理块及其索引条目已被抹除。
namespace Mono.FileBox.Lite.Abstractions.Subsystems;

/// <summary>Confirms a physical block and its index entries have been erased.</summary>
public interface IPhysicalEraser
{
    Task EraseAsync(IObjectContext ctx, CancellationToken ct);
}