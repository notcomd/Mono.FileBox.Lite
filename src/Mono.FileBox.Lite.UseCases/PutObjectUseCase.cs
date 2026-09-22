using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.UseCases;

namespace Mono.FileBox.Lite.UseCases;

// File: PutObjectUseCase — write-path use case for storing an object.
/// <summary>
/// Put pipeline: <c>Put → Index → Audit → Publish</c>. On failure the completed
/// transitions are rolled back in reverse order (leaving the physical block intact).
/// 中文翻译：Put 流水线：Put → Index → Audit → Publish。失败时按相反顺序回滚已完成的流转（保留物理块不动）。
/// </summary>
public sealed class PutObjectUseCase : IPutObjectUseCase
{
    private readonly IObjectStateMachine _machine;
    private readonly IObjectContextFactory _ctxFactory;
    private readonly IPutObjectRollback _rollback;

    public PutObjectUseCase(
        IObjectStateMachine machine,
        IObjectContextFactory ctxFactory,
        IPutObjectRollback rollback)
    {
        _machine = machine;
        _ctxFactory = ctxFactory;
        _rollback = rollback;
    }

    public async Task<PutObjectResult> ExecuteAsync(PutObjectCommand cmd, CancellationToken ct)
    {
        var ctx = CreateContext(cmd);
        var completed = new Stack<ObjectTrigger>();
        try
        {
            await FireAndRecord(ObjectTrigger.Put, ctx, completed, ct);
            await FireAndRecord(ObjectTrigger.Index, ctx, completed, ct);
            await FireAndRecord(ObjectTrigger.Audit, ctx, completed, ct);
            await _machine.FireAsync(ObjectTrigger.Publish, ctx, ct).ConfigureAwait(false);

            return PutObjectResult.Success(ctx.ContentHash, ctx.CurrentState);
        }
        catch (Exception ex)
        {
            await _rollback.RollbackAsync(completed, ctx, ct).ConfigureAwait(false);
            return PutObjectResult.Failure(ex, ctx.CurrentState);
        }
    }

    private async Task FireAndRecord(
        ObjectTrigger trigger, IObjectContext ctx, Stack<ObjectTrigger> completed, CancellationToken ct)
    {
        await _machine.FireAsync(trigger, ctx, ct).ConfigureAwait(false);
        completed.Push(trigger);
    }

    private IObjectContext CreateContext(PutObjectCommand cmd)
    {
        var ctx = _ctxFactory.Create(new NamespaceIdValue(cmd.NamespaceId), cmd.ObjectKey);
        ctx.Items[ObjectContextKeys.ContentStream] = cmd.Content;
        ctx.Items[ObjectContextKeys.WriteOptions] = cmd.Write ?? new WriteOptions();
        if (cmd.ContentType is not null) ctx.Items[ObjectContextKeys.ContentType] = cmd.ContentType;
        if (cmd.ObjectKey is not null) ctx.Items[ObjectContextKeys.ObjectKey] = cmd.ObjectKey;
        if (cmd.Tags is not null) ctx.Items[ObjectContextKeys.Tags] = cmd.Tags;
        if (cmd.Attributes is not null) ctx.Items[ObjectContextKeys.Attributes] = cmd.Attributes;
        return ctx;
    }
}