using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.UseCases;

namespace Mono.FileBox.Lite.UseCases;

// File: ArchiveObjectUseCase — archive use case that fires the Archive transition.
/// <summary>Archive: fires the <c>Archive</c> transition.
/// 归档：触发 Archive 状态流转。</summary>
public sealed class ArchiveObjectUseCase : IArchiveObjectUseCase
{
    private readonly IObjectStateMachine _machine;
    private readonly IObjectContextFactory _ctxFactory;
    private readonly IEntryStore _index;

    public ArchiveObjectUseCase(
        IObjectStateMachine machine, IObjectContextFactory ctxFactory, IEntryStore index)
    {
        _machine = machine;
        _ctxFactory = ctxFactory;
        _index = index;
    }

    public async Task<ArchiveObjectResult> ExecuteAsync(ArchiveObjectCommand cmd, CancellationToken ct)
    {
        var state = await DeleteObjectUseCase.ResolveStateAsync(_index, cmd.ContentHash, ObjectState.Available, ct)
            .ConfigureAwait(false);
        var ctx = _ctxFactory.CreateFromExisting(cmd.ContentHash, cmd.NamespaceId, state);
        ctx.Items[ObjectContextKeys.WriteOptions] = new WriteOptions { Tier = cmd.Tier };

        var result = await _machine.FireAsync(ObjectTrigger.Archive, ctx, ct).ConfigureAwait(false);
        return ArchiveObjectResult.Success(cmd.ContentHash, result);
    }
}