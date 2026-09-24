using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Index.Planning;
using Mono.FileBox.Lite.Index.Providers;
using Mono.FileBox.Lite.Index.Storage;
using Xunit;

namespace Mono.FileBox.Lite.Index.Tests;

/// <summary>
/// Exercises the query engine directly: predicate filtering (prefix/tag/tier/state/
/// time/size), sorting and stable cursor pagination.
/// 直接测试索引查询引擎：谓词过滤（前缀/标签/存储层/状态/时间/大小）、排序及稳定的游标分页。
/// </summary>
public class IndexQueryTests
{
    private const string Ns = "app";

    [Fact]
    public async Task Query_ByPrefix_FiltersKeys()
    {
        var ctx = Build(Seed());
        var page = await ctx.Reader.QueryAsync(new IndexQuery { NamespaceId = Ns, KeyPrefix = "/docs/" }, None);

        Assert.True(page.Items.All(e => e.ObjectKey!.StartsWith("/docs/", StringComparison.Ordinal)));
        Assert.Equal(3, page.Items.Count); // /docs/a, /docs/b, /docs/c
    }

    [Fact]
    public async Task Query_ByTag_MatchesOwner()
    {
        var ctx = Build(Seed());
        var page = await ctx.Reader.QueryAsync(new IndexQuery
        {
            NamespaceId = Ns,
            Tags = new Dictionary<string, string> { ["owner"] = "alice" }
        }, None);

        Assert.Equal(2, page.Items.Count);
        Assert.All(page.Items, e => Assert.Equal("alice", e.Tags["owner"]));
    }

    [Fact]
    public async Task Query_ByTier_And_State_CombinesFilters()
    {
        var ctx = Build(Seed());
        var page = await ctx.Reader.QueryAsync(new IndexQuery
        {
            NamespaceId = Ns,
            Tiers = new[] { StorageTier.Cold },
            States = new[] { ObjectState.Available }
        }, None);

        Assert.Single(page.Items);
        Assert.Equal("/logs/old.log", page.Items[0].ObjectKey);
    }

    [Fact]
    public async Task Query_BySizeRange()
    {
        var ctx = Build(Seed());
        var page = await ctx.Reader.QueryAsync(new IndexQuery
        {
            NamespaceId = Ns,
            SizeRange = LongRange.Between(1000, 100000)
        }, None);

        Assert.Equal(2, page.Items.Count);
    }

    [Fact]
    public async Task Query_ByCreatedAtRange()
    {
        var ctx = Build(Seed());
        var start = DateTimeOffset.UtcNow.AddMinutes(-3);
        var end = DateTimeOffset.UtcNow.AddMinutes(-1);
        var page = await ctx.Reader.QueryAsync(new IndexQuery
        {
            NamespaceId = Ns,
            CreatedAtRange = TimeRange.Between(start, end)
        }, None);

        Assert.Single(page.Items);
    }

    [Fact]
    public async Task Query_DoesNotLeak_AcrossNamespaces()
    {
        var store = Build(Seed());
        await store.Store.PutAsync("ns-other", new IndexEntry
        {
            ContentHash = "zz", NamespaceId = "other", ObjectKey = "/x", Tier = StorageTier.Hot,
            State = ObjectState.Available, CreatedAt = DateTimeOffset.UtcNow
        }, None);

        var page = await store.Reader.QueryAsync(new IndexQuery { NamespaceId = Ns }, None);
        Assert.Equal(5, page.Items.Count);
    }

    [Fact]
    public async Task Query_NoMatch_ReturnsEmpty()
    {
        var ctx = Build(Seed());
        var page = await ctx.Reader.QueryAsync(new IndexQuery
        {
            NamespaceId = Ns,
            Tags = new Dictionary<string, string> { ["owner"] = "nobody" }
        }, None);

        Assert.Empty(page.Items);
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task Query_SortBySize_Descending_Stable()
    {
        var ctx = Build(Seed());
        var page = await ctx.Reader.QueryAsync(new IndexQuery
        {
            NamespaceId = Ns,
            Sort = new IndexSort { Field = "SizeBytes", Direction = SortDirection.Descending }
        }, None);

        var sizes = page.Items.Select(e => e.SizeBytes).ToArray();
        Assert.Equal(sizes.OrderByDescending(x => x), sizes);
    }

    [Fact]
    public async Task Query_CursorPagination_IsStable_AndComplete()
    {
        var store = Build(Seed());

        var collected = new List<string>();
        string? cursor = null;
        var pages = 0;
        do
        {
            var page = await store.Reader.QueryAsync(new IndexQuery
            {
                NamespaceId = Ns,
                Page = new PageRequest { Size = 2, Cursor = cursor }
            }, None);
            collected.AddRange(page.Items.Select(e => e.ContentHash));
            pages++;
            cursor = page.NextCursor;
        }
        while (cursor is not null);

        // 5 seed entries -> ceil(5/2)=3 pages, no duplicates.
        Assert.Equal(3, pages);
        Assert.Equal(5, collected.Distinct().Count());
    }

    // -------- helpers --------

    private static readonly CancellationToken None = CancellationToken.None;

    private static IndexHarness Build(IEnumerable<(string hash, string key, string owner, StorageTier tier,
        ObjectState state, long size, int ageMinutes)> rows)
    {
        var store = new InMemoryEntryStore();
        foreach (var r in rows)
        {
            store.PutAsync(r.hash, new IndexEntry
            {
                ContentHash = r.hash,
                NamespaceId = Ns,
                ObjectKey = r.key,
                Tags = new Dictionary<string, string> { ["owner"] = r.owner },
                Tier = r.tier,
                State = r.state,
                SizeBytes = r.size,
                ContentType = "application/octet-stream",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-r.ageMinutes),
                ModifiedAt = DateTimeOffset.UtcNow.AddMinutes(-r.ageMinutes)
            }, None).GetAwaiter().GetResult();
        }

        IIndexProvider[] providers =
        {
            new PrefixIndexProvider(store),
            new TagInvertedIndexProvider(store),
            new AttributeIndexProvider(store),
            new TierBitmapProvider(store),
            new StateBitmapProvider(store),
            new TimeIndexProvider(store),
            new SizeIndexProvider(store)
        };
        var planner = new QueryPlanner(providers, store);
        var executor = new QueryExecutor(store, new Base64JsonCursorCodec());
        var reader = new DefaultIndexReader(planner, executor);
        return new IndexHarness(store, reader);
    }

    private static IEnumerable<(string hash, string key, string owner, StorageTier tier,
        ObjectState state, long size, int ageMinutes)> Seed()
    {
        yield return ("a1", "/docs/a.txt", "alice", StorageTier.Hot, ObjectState.Available, 50, 5);
        yield return ("a2", "/docs/b.txt", "bob", StorageTier.Hot, ObjectState.Available, 5000, 10);
        yield return ("a3", "/docs/c.txt", "bob", StorageTier.Warm, ObjectState.Available, 50000, 25);
        yield return ("a4", "/media/photo.png", "alice", StorageTier.Cold, ObjectState.Archived, 1048576, 40);
        yield return ("a5", "/logs/old.log", "sys", StorageTier.Cold, ObjectState.Available, 200, 2);
    }

    private sealed record IndexHarness(InMemoryEntryStore Store, IIndexReader Reader);
}