using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.UseCases;

namespace Mono.FileBox.Lite.UseCases;

// File: DeleteObjectUseCase — logical-delete use case that fires the Delete transition.
/// <summary>Delete: fires the <c>Delete</c> transition (logical delete, recovery window kept).</summary>
public sealed class DeleteObjectUseCase : IDeleteObjectUseCase
{
    private readonly IObjectStateMachine _machine;
    private readonly IObjectContextFactory _ctxFactory;
    private readonly IEntryStore _index;

    public DeleteObjectUseCase(
        IObjectStateMachine machine, IObjectContextFactory ctxFactory, IEntryStore index)
    {
        _machine = machine;
        _ctxFactory = ctxFactory;
        _index = index;
    }

    public async Task<DeleteObjectResult> ExecuteAsync(DeleteObjectCommand cmd, CancellationToken ct)
    {
        var state = await ResolveStateAsync(_index, cmd.ContentHash, ObjectState.Available, ct).ConfigureAwait(false);
        var ctx = _ctxFactory.CreateFromExisting(cmd.ContentHash, cmd.NamespaceId, state);
        var result = await _machine.FireAsync(ObjectTrigger.Delete, ctx, ct).ConfigureAwait(false);
        return DeleteObjectResult.Success(cmd.ContentHash, result);
    }

    internal static async Task<ObjectState> ResolveStateAsync(
        IEntryStore index, string hash, ObjectState fallback, CancellationToken ct)
    {
        var entry = await index.GetAsync(hash, ct).ConfigureAwait(false);
        return entry is null ? fallback : entry.State;
    }
}