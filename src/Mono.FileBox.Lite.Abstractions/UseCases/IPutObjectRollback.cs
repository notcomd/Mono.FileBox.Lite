// 本文件包含类型 IPutObjectRollback：按完成逆序回滚部分完成的放置管线（及其它多步流程）。
namespace Mono.FileBox.Lite.Abstractions.UseCases;

/// <summary>
/// Rolls back a partially completed put pipeline (and other multi-step flows) in
/// reverse order of completion.
/// 中文翻译：按完成的逆序回滚部分完成的放置管线（及其它多步流程）。
/// </summary>
public interface IPutObjectRollback
{
    Task RollbackAsync(IEnumerable<ObjectTrigger> completed, IObjectContext ctx, CancellationToken ct);
}