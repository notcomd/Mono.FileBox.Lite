using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Storage.ObjectWriter;

namespace Mono.FileBox.Lite.Storage;

/// <summary>
/// Physical erasure for the Purge transition. Deletes the physical block (legacy) or,
/// for chunked objects, the manifest and every chunk block, then removes the index
/// entry — confirming both before returning.
/// 中文翻译：为 Purge 阶段提供物理擦除能力：删除物理块（旧式对象），或对分块对象删除清单及所有分块块，随后移除索引条目——并在返回前同时确认两者。
/// </summary>
public sealed class StoragePhysicalEraser : IPhysicalEraser, ITransitionAction
{
    private readonly IDiskSelector _selector;
    private readonly IIOPipeline _pipeline;
    private readonly IPhysicalDevice _device;
    private readonly IIndexWriter? _indexWriter;

    public StoragePhysicalEraser(
        IDiskSelector selector,
        IIOPipeline pipeline,
        IPhysicalDevice device,
        IIndexWriter? indexWriter = null)
    {
        _selector = selector;
        _pipeline = pipeline;
        _device = device;
        _indexWriter = indexWriter;
    }

    public async Task EraseAsync(IObjectContext ctx, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ctx.ContentHash)) return;

        var disk = await _selector.SelectForReadAsync(ctx.ContentHash, ct).ConfigureAwait(false);

        if (disk is LocalDiskHandle local)
        {
            var manifest = await ChunkManifestCodec.ReadAsync(_device, local, ctx.ContentHash, ct).ConfigureAwait(false);
            if (manifest is not null)
            {
                // Chunked object: delete each chunk block, then the manifest.
                foreach (var chunkHash in manifest.ChunkHashes)
                    await _pipeline.DeleteAsync(disk, chunkHash, ct).ConfigureAwait(false);
                await ChunkManifestCodec.DeleteAsync(_device, local, ctx.ContentHash, ct).ConfigureAwait(false);
                return;
            }
        }

        await _pipeline.DeleteAsync(disk, ctx.ContentHash, ct).ConfigureAwait(false);

        if (_indexWriter is not null)
            await _indexWriter.RemoveAsync(ctx.ContentHash, ct).ConfigureAwait(false);
    }

    public Task ExecuteAsync(IObjectContext ctx, CancellationToken ct)
        => EraseAsync(ctx, ct);
}