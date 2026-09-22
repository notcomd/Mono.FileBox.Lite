using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.UseCases;

namespace Mono.FileBox.Lite.UseCases;

// File: PutObjectRollback — reverses completed put transitions on failure.
/// <summary>
/// Reverses completed put transitions. Only the index entry is removed (leaving the
/// physical block intact), mirroring the documented rollback to <c>Stored</c>.
/// 中文翻译：在失败时撤销已完成的 put 状态流转。仅移除索引条目（保留物理块不动），与文档中回滚至 Stored 的说明保持一致。
/// </summary>
public sealed class PutObjectRollback : IPutObjectRollback
{
    private readonly IIndexWriter? _index;

    public PutObjectRollback(IIndexWriter? index = null) => _index = index;

    public async Task RollbackAsync(IEnumerable<ObjectTrigger> completed, IObjectContext ctx, CancellationToken ct)
    {
        if (_index is null || string.IsNullOrWhiteSpace(ctx.ContentHash)) return;

        if (completed.Contains(ObjectTrigger.Index)
            || completed.Contains(ObjectTrigger.Audit))
        {
            await _index.RemoveAsync(ctx.ContentHash, ct).ConfigureAwait(false);
        }
    }
}