using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.UseCases;

namespace Mono.FileBox.Lite.UseCases;

// File: GetObjectUseCase — read-path use case for retrieving an available object.
/// <summary>
/// Read path. Performs no transition — it only reads objects that are in the
/// <see cref="ObjectState.Available"/> state.
/// 中文翻译：读取路径。不执行任何状态流转——仅读取处于 Available（可用）状态的对象。
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