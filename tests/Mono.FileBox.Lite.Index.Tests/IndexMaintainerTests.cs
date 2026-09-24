using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Index.Storage;
using Mono.FileBox.Lite.Index;
using Xunit;

namespace Mono.FileBox.Lite.Index.Tests;

/// <summary>
/// Verifies index-consistency maintenance: the physical blocks are the authoritative
/// source, so VerifyAsync must flag entries whose block is missing and RepairAsync must
/// prune them.
/// 验证索引一致性维护：物理数据块才是权威来源，VerifyAsync 必须标记数据块缺失的条目，RepairAsync 必须将其清理删除。
/// </summary>
public class IndexMaintainerTests
{
    private static readonly CancellationToken None = CancellationToken.None;

    [Fact]
    public async Task Verify_FlagsEntry_WhoseBlockIsMissing()
    {
        var store = new InMemoryEntryStore();
        await store.PutAsync("hash-present", Entry("hash-present"), None);
        await store.PutAsync("hash-missing", Entry("hash-missing"), None);

        var content = new FakeObjectWriter(new HashSet<string> { "hash-present" });
        var maintainer = new IndexMaintainer(store, content);

        var report = await maintainer.VerifyAsync(None);

        Assert.False(report.IsConsistent);
        Assert.Contains("hash-missing", report.MissingIndexEntries);
        Assert.DoesNotContain("hash-present", report.MissingIndexEntries);
    }

    [Fact]
    public async Task Repair_Removes_OrphanedEntry_Keeps_GoodOnes()
    {
        var store = new InMemoryEntryStore();
        var good = Entry("hash-present");
        var orphan = Entry("hash-missing");
        await store.PutAsync(good.ContentHash, good, None);
        await store.PutAsync(orphan.ContentHash, orphan, None);

        var content = new FakeObjectWriter(new HashSet<string> { "hash-present" });
        var maintainer = new IndexMaintainer(store, content);

        var report = await maintainer.VerifyAsync(None);
        await maintainer.RepairAsync(report, None);

        Assert.NotNull(await store.GetAsync("hash-present", None));
        Assert.Null(await store.GetAsync("hash-missing", None));
    }

    [Fact]
    public async Task Rebuild_Clears_AllEntries()
    {
        var store = new InMemoryEntryStore();
        await store.PutAsync("a", Entry("a"), None);
        await store.PutAsync("b", Entry("b"), None);

        var maintainer = new IndexMaintainer(store, new FakeObjectWriter(new HashSet<string>()));
        await maintainer.RebuildAsync(new IndexRebuildOptions(), None);

        Assert.Empty(await store.ListAllAsync(None));
    }

    private static IndexEntry Entry(string hash) => new()
    {
        ContentHash = hash,
        NamespaceId = "ns",
        ObjectKey = "/k/" + hash,
        Tier = StorageTier.Hot,
        State = ObjectState.Available,
        SizeBytes = 1,
        CreatedAt = DateTimeOffset.UtcNow,
        ModifiedAt = DateTimeOffset.UtcNow
    };

    /// <summary>A fake object writer whose existence reflects a set of "physical" hashes.
    /// 一个假对象写入器，其存在性反映一组“物理”哈希。</summary>
    private sealed class FakeObjectWriter : IObjectWriter
    {
        private readonly HashSet<string> _present;
        public FakeObjectWriter(HashSet<string> present) => _present = present;
        public Task<string> WriteAsync(Stream content, WriteOptions options, CancellationToken ct)
            => Task.FromResult("fake-hash");
        public Task<bool> ExistsAsync(string contentHash, CancellationToken ct)
            => Task.FromResult(_present.Contains(contentHash));
    }
}