using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.UseCases;

namespace Mono.FileBox.Lite.UseCases;

// File: RestoreObjectUseCase — restore use case that fires the Restore transition.
/// <summary>Restore: fires the <c>Restore</c> transition.</summary>
public sealed class RestoreObjectUseCase : IRestoreObjectUseCase
{
    private readonly IObjectStateMachine _machine;
    private readonly IObjectContextFactory _ctxFactory;
    private readonly IEntryStore _index;

    public RestoreObjectUseCase(
        IObjectStateMachine machine, IObjectContextFactory ctxFactory, IEntryStore index)
    {
        _machine = machine;
        _ctxFactory = ctxFactory;
        _index = index;
    }

    public async Task<RestoreObjectResult> ExecuteAsync(RestoreObjectCommand cmd, CancellationToken ct)
    {
        var state = await DeleteObjectUseCase.ResolveStateAsync(_index, cmd.ContentHash, ObjectState.Archived, ct)
            .ConfigureAwait(false);
        var ctx = _ctxFactory.CreateFromExisting(cmd.ContentHash, cmd.NamespaceId, state);
        var result = await _machine.FireAsync(ObjectTrigger.Restore, ctx, ct).ConfigureAwait(false);
        return RestoreObjectResult.Success(cmd.ContentHash, result);
    }
}