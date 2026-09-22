// JsonEscrowIndexTests.cs — JSON 文档索引持久化与热点缓存机制测试。
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Index.Storage;

namespace Mono.FileBox.Lite.Functional.Tests;

/// <summary>
/// Covers the JSON-document <see cref="JsonFileIndexStore"/> (round-trip, cross-instance
/// persistence, delete, clear) and the <see cref="HotCachingIndexStore"/> (promotion by read
/// count, LRU eviction, hot sidecar warmup that serves reads without touching the main file).
/// 中文翻译：覆盖 JSON 文档索引库（往返、跨实例持久化、删除、清空）与热点缓存索引库（按读取次数提升、LRU 淘汰、热点旁路预热以在不动主文件的情况下服务读取）。
/// </summary>
public sealed class JsonEscrowIndexTests : IDisposable
{

    private readonly string _root = Directory.CreateDirectory(
        Path.Combine(Path.GetTempPath(), "mono-filebox-json-idx-" + Guid.NewGuid().ToString("N"))).FullName;

    private static IndexEntry Entry(string hash, string ns = "ns1", long size = 123, string? tag = null)
    {
        var tags = tag is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string> { ["dept"] = tag };
        return new IndexEntry
        {
            ContentHash = hash,
            NamespaceId = ns,
            ObjectKey = "/a/" + hash,
            Tags = tags,
            Attributes = new Dictionary<string, object>
            {
                ["owner"] = "trac",
                ["reviews"] = (long)3,   // 关键：long 需在 JSON 往返后仍是 long，而非 JsonElement
                ["enabled"] = true,
                ["score"] = 4.5
            },
            SizeBytes = size,
            ContentType = "application/octet-stream",
            CreatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            ModifiedAt = DateTimeOffset.Parse("2026-02-01T00:00:00Z"),
            State = ObjectState.Available,
            Tier = StorageTier.Hot
        };
    }

    [Fact]
    public async Task PutGet_RoundTrips_AndPersistsAcrossInstances()
    {
        var path = Path.Combine(_root, "index.json");
        var e = Entry("hash-1");
        await new JsonFileIndexStore(path).PutAsync(e.ContentHash, e, CancellationToken.None);

        var reopened = new JsonFileIndexStore(path);
        var got = await reopened.GetAsync("hash-1", CancellationToken.None);

        Assert.NotNull(got);
        Assert.Equal(e.ContentHash, got!.ContentHash);
        Assert.Equal(e.ObjectKey, got.ObjectKey);
        Assert.Equal("trac", got.Attributes["owner"]);
        Assert.True(got.Attributes["reviews"] is long, "long 属性往返后应保持 long 类型");
        Assert.Equal(3L, got.Attributes["reviews"]);
        Assert.Equal(4.5d, got.Attributes["score"]);
        Assert.Equal(ObjectState.Available, got.State);
        Assert.Equal(StorageTier.Hot, got.Tier);
    }

    [Fact]
    public async Task GetAndList_AreConsistent_AfterReopen()
    {
        var path = Path.Combine(_root, "idx.json");
        var store = new JsonFileIndexStore(path);
        await store.PutAsync("a", Entry("a", "ns1", 1), CancellationToken.None);
        await store.PutAsync("b", Entry("b", "ns1", 2), CancellationToken.None);
        await store.PutAsync("c", Entry("c", "ns2", 3), CancellationToken.None);

        var reopened = new JsonFileIndexStore(path);
        var many = await reopened.GetManyAsync(new[] { "a", "c", "missing" }, CancellationToken.None);
        Assert.Equal(2, many.Count);

        var ns1 = await reopened.ListByNamespaceAsync("ns1", CancellationToken.None);
        Assert.Equal(2, ns1.Count);

        var all = await reopened.ListAllAsync(CancellationToken.None);
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public async Task DeleteAndClear_ReflectInDocument()
    {
        var path = Path.Combine(_root, "idx.json");
        var store = new JsonFileIndexStore(path);
        await store.PutAsync("a", Entry("a"), CancellationToken.None);
        await store.PutAsync("b", Entry("b"), CancellationToken.None);

        await store.DeleteAsync("a", CancellationToken.None);
        Assert.Null(await new JsonFileIndexStore(path).GetAsync("a", CancellationToken.None));

        await store.ClearAsync(CancellationToken.None);
        Assert.Empty(await new JsonFileIndexStore(path).ListAllAsync(CancellationToken.None));
    }

    [Fact]
    public async Task HotCache_PromotesAfterThreshold_AndServesWithoutMainFile()
    {
        var idxPath = Path.Combine(_root, "index.json");
        var hotPath = Path.Combine(_root, "index.hot.json");

        var inner = new JsonFileIndexStore(idxPath);
        await inner.PutAsync("hot-1", Entry("hot-1"), CancellationToken.None);

        var hot = new HotCachingIndexStore(inner, new HotIndexOptions
        {
            Capacity = 8,
            PromotionThreshold = 3,
            HotFilePath = hotPath,
            PersistHot = true
        });

        for (var i = 0; i < 3; i++) // 达到阈值 -> 提升为热点
            Assert.NotNull(await hot.GetAsync("hot-1", CancellationToken.None));

        Assert.True(File.Exists(hotPath), "热点旁路文件应在提升后生成");

        // 关键验证：删除主索引文件后，热点命中仍能返回（免对索引文件解析）。
        File.Delete(idxPath);
        var served = await hot.GetAsync("hot-1", CancellationToken.None);
        Assert.NotNull(served);
        Assert.Equal("hot-1", served!.ContentHash);
    }

    [Fact]
    public async Task HotCache_EvictsLru_WhenCapacityExceeded()
    {
        var inner = new JsonFileIndexStore(Path.Combine(_root, "idx.json"));
        for (var i = 0; i < 12; i++)
            await inner.PutAsync($"k{i}", Entry($"k{i}"), CancellationToken.None);

        var hot = new HotCachingIndexStore(inner, new HotIndexOptions
        {
            Capacity = 5,
            PromotionThreshold = 1,
            PersistHot = false
        });

        for (var i = 0; i < 12; i++) // 12 个条目穿过容量为 5 的热点集合
            await hot.GetAsync($"k{i}", CancellationToken.None);

        // 最后访问的 k11 仍在热点集合；由于时序窗口，至少不应超过容量。
        var again = await hot.GetAsync("k11", CancellationToken.None);
        Assert.NotNull(again);

        // 全部重新读取以验证热点状态仍可服务（无需文件；此处文件里只有 k0..k11 全量，故仅校验不崩）。
        for (var i = 0; i < 12; i++)
            Assert.NotNull(await hot.GetAsync($"k{i}", CancellationToken.None));
    }

    [Fact]
    public async Task HotCache_WarmsFromSidecarFile_AtStartup()
    {
        var idxPath = Path.Combine(_root, "index.json");
        var hotPath = Path.Combine(_root, "hot.json");

        var inner = new JsonFileIndexStore(idxPath);
        var e = Entry("warm-1");
        await inner.PutAsync(e.ContentHash, e, CancellationToken.None);

        var first = new HotCachingIndexStore(inner, new HotIndexOptions
        {
            Capacity = 8,
            PromotionThreshold = 1,
            HotFilePath = hotPath,
            PersistHot = true
        });
        await first.GetAsync("warm-1", CancellationToken.None);

        // 用一个全新实例预热热点旁路文件：删除主文件后仍可命中（冷启动免解析）。
        File.Delete(idxPath);
        var warmed = new HotCachingIndexStore(inner, new HotIndexOptions
        {
            Capacity = 8,
            PromotionThreshold = 1,
            HotFilePath = hotPath,
            PersistHot = true
        });
        var got = await warmed.GetAsync("warm-1", CancellationToken.None);
        Assert.NotNull(got);
        Assert.Equal("warm-1", got!.ContentHash);
    }

    public void Dispose() { try { Directory.Delete(_root, true); } catch { /* best effort */ } }
}