using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.UseCases;

namespace Mono.FileBox.Lite.UseCases;

/// <summary>
/// Read path. Performs no transition — it only reads objects that are in the
/// <see cref="ObjectState.Available"/> state.
/// </summary>
public sealed class GetObjectUseCase : IGetObjectUseCase
{
    private readonly IEntryStore _index;
    private readonly IObjectReader _reader;

    public GetObjectUseCase(IEntryStore index, IObjectReader reader)
    {
        _index = index;
        _reader = reader;
    }

    public async Task<GetObjectResult> ExecuteAsync(GetObjectCommand cmd, CancellationToken ct)
    {
        var entry = await _index.GetAsync(cmd.ContentHash, ct).ConfigureAwait(false);
        if (entry is null || entry.State != ObjectState.Available)
            return GetObjectResult.NotFound(cmd.ContentHash);

        var length = cmd.Length ?? -1;
        var stream = await _reader.ReadAsync(cmd.ContentHash, cmd.Offset, length, ct).ConfigureAwait(false);

        return new GetObjectResult
        {
            ContentHash = cmd.ContentHash,
            Content = stream,
            Length = stream.CanSeek ? stream.Length : (cmd.Length ?? 0),
            ContentType = entry.ContentType
        };
    }
}

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

/// <summary>Archive: fires the <c>Archive</c> transition.</summary>
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