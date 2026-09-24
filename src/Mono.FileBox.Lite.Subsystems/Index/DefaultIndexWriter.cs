// File-level documentation: Index writer persisting IndexEntry blobs through the L0
// entry store and mirroring them into the ordered key/value store. The private nested
// IndexEntryBuilder helper is retained here (internal, nested in its host type).
// Extracted from the original multi-type Index/DefaultIndexWriter.cs.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems.Index;

/// <summary>
/// Index writer that persists <see cref="IndexEntry"/> blobs through the L0
/// <see cref="IEntryStore"/> and mirrors them into the ordered key/value store for
/// B-tree-style primary scanning. It also acts as the <c>Index</c> transition action.
/// 索引写入器，通过 L0 <see cref="IEntryStore"/> 持久化 <see cref="IndexEntry"/> 数据块，并将其镜像到有序键值存储以支持类 B 树的主键扫描；同时充当 <c>Index</c> 转移动作。
/// </summary>
public sealed class DefaultIndexWriter : IIndexWriter, ITransitionAction
{
    private readonly IEntryStore _entries;

    public DefaultIndexWriter(IEntryStore entries) => _entries = entries;

    public Task WriteAsync(IObjectContext ctx, CancellationToken ct)
    {
        var entry = EntryFromContext(ctx);
        return PutEntryAsync(entry, ct);
    }

    public async Task UpdateAsync(string contentHash, IndexUpdate update, CancellationToken ct)
    {
        var current = await _entries.GetAsync(contentHash, ct).ConfigureAwait(false);
        var entry = current is null
            ? new IndexEntry
            {
                ContentHash = contentHash,
                NamespaceId = "default",
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow
            }
            : current;

        var builder = new IndexEntryBuilder(entry)
            .WithObjectKey(update.ObjectKey)
            .WithTier(update.Tier)
            .WithState(update.State)
            .WithSize(update.SizeBytes)
            .WithContentType(update.ContentType)
            .WithModifiedAt(update.ModifiedAt);

        if (update.Tags is not null)
            builder.WithTags(update.Tags);
        if (update.Attributes is not null)
            builder.WithAttributes(update.Attributes);
        if (update.RemoveTags is not null)
            builder.RemoveTags(update.RemoveTags);
        if (update.RemoveAttributes is not null)
            builder.RemoveAttributes(update.RemoveAttributes);

        await PutEntryAsync(builder.Build(), ct).ConfigureAwait(false);
    }

    public Task RemoveAsync(string contentHash, CancellationToken ct)
        => _entries.DeleteAsync(contentHash, ct);

    public async Task ExecuteAsync(IObjectContext ctx, CancellationToken ct)
        => await WriteAsync(ctx, ct).ConfigureAwait(false);

    private async Task PutEntryAsync(IndexEntry entry, CancellationToken ct)
        => await _entries.PutAsync(entry.ContentHash, entry, ct).ConfigureAwait(false);

    private static IndexEntry EntryFromContext(IObjectContext ctx)
    {
        long size = 0;
        if (ctx.Items.TryGetValue(ObjectContextKeys.SizeBytes, out var sizeObj) && sizeObj is long s)
            size = s;

        var tier = StorageTier.Hot;
        if (ctx.Items.TryGetValue(ObjectContextKeys.WriteOptions, out var w) && w is WriteOptions wo)
            tier = wo.Tier;

        return new IndexEntry
        {
            ContentHash = ctx.ContentHash,
            NamespaceId = ctx.NamespaceId,
            ObjectKey = ctx.Items.TryGetValue(ObjectContextKeys.ObjectKey, out var ok) ? ok as string : null,
            ContentType = ctx.Items.TryGetValue(ObjectContextKeys.ContentType, out var ctm) ? ctm as string : null,
            Tags = ctx.Items.TryGetValue(ObjectContextKeys.Tags, out var tg) && tg is IReadOnlyDictionary<string, string> tags
                ? tags : new Dictionary<string, string>(),
            Attributes = ctx.Items.TryGetValue(ObjectContextKeys.Attributes, out var at) && at is IReadOnlyDictionary<string, object> attrs
                ? attrs : new Dictionary<string, object>(),
            Tier = tier,
            SizeBytes = size,
            State = ctx.CurrentState,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };
    }

    private sealed class IndexEntryBuilder
    {
        private IndexEntry _e;
        public IndexEntryBuilder(IndexEntry entry) => _e = entry;

        public IndexEntryBuilder WithObjectKey(string? v) { if (v is not null) _e = _e with { ObjectKey = v }; return this; }
        public IndexEntryBuilder WithTier(StorageTier? v) { if (v.HasValue) _e = _e with { Tier = v.Value }; return this; }
        public IndexEntryBuilder WithState(ObjectState? v) { if (v.HasValue) _e = _e with { State = v.Value }; return this; }
        public IndexEntryBuilder WithSize(long? v) { if (v.HasValue) _e = _e with { SizeBytes = v.Value }; return this; }
        public IndexEntryBuilder WithContentType(string? v) { if (v is not null) _e = _e with { ContentType = v }; return this; }
        public IndexEntryBuilder WithModifiedAt(DateTimeOffset? v) { if (v.HasValue) _e = _e with { ModifiedAt = v.Value }; return this; }
        public IndexEntryBuilder WithTags(IReadOnlyDictionary<string, string> tags)
            { var d = _e.Tags.ToDictionary(kv => kv.Key, kv => kv.Value); foreach (var kv in tags) d[kv.Key] = kv.Value; _e = _e with { Tags = d }; return this; }
        public IndexEntryBuilder WithAttributes(IReadOnlyDictionary<string, object> attrs)
            { var d = _e.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value); foreach (var kv in attrs) d[kv.Key] = kv.Value; _e = _e with { Attributes = d }; return this; }
        public IndexEntryBuilder RemoveTags(IReadOnlyList<string> keys)
            { var d = _e.Tags.ToDictionary(kv => kv.Key, kv => kv.Value); foreach (var k in keys) d.Remove(k); _e = _e with { Tags = d }; return this; }
        public IndexEntryBuilder RemoveAttributes(IReadOnlyList<string> keys)
            { var d = _e.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value); foreach (var k in keys) d.Remove(k); _e = _e with { Attributes = d }; return this; }

        public IndexEntry Build() => _e;
    }
}