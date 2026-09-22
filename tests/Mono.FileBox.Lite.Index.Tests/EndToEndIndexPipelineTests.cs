using Microsoft.Extensions.DependencyInjection;
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Configuration;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.UseCases;
using Mono.FileBox.Lite.DependencyInjection;
using Xunit;

namespace Mono.FileBox.Lite.Index.Tests;

/// <summary>
/// End-to-end: objects are persisted through the state-machine Put pipeline and the
/// index writer, then read back through <see cref="IIndexReader"/> with tag/prefix and
/// pagination, confirming write→query coherence and state synchronization.
/// 中文翻译：端到端验证：对象经状态机 Put 流水线写入并同步到索引，随后可通过索引读取（标签/前缀/分页），确认写入→查询一致性与状态同步。
/// </summary>
public class EndToEndIndexPipelineTests
{
    private const string Ns = "app";
    private static readonly CancellationToken None = CancellationToken.None;

    [Fact]
    public async Task PutPipeline_Then_Query_ByTag_And_State()
    {
        await using var provider = BuildServices();

        var put = provider.GetRequiredService<IPutObjectUseCase>();
        var r1 = await put.ExecuteAsync(New("/docs/a.txt", "hello", owner: "alice", sizeTag: "small"), None);
        var r2 = await put.ExecuteAsync(New("/docs/b.txt", "world hello bigger", owner: "bob", sizeTag: "big"), None);
        Assert.True(r1.Succeeded && r2.Succeeded);
        Assert.Equal(ObjectState.Available, r1.State);

        var index = provider.GetRequiredService<IIndexReader>();

        // Query by tag.
        var byOwner = await index.QueryAsync(new IndexQuery
        {
            NamespaceId = Ns,
            Tags = new Dictionary<string, string> { ["owner"] = "alice" }
        }, None);
        Assert.Single(byOwner.Items);
        Assert.Equal(r1.ContentHash, byOwner.Items[0].ContentHash);

        // Query by prefix + state.
        var byPrefix = await index.QueryAsync(new IndexQuery
        {
            NamespaceId = Ns,
            KeyPrefix = "/docs/",
            States = new[] { ObjectState.Available }
        }, None);
        Assert.Equal(2, byPrefix.Items.Count);
    }

    [Fact]
    public async Task IndexEntry_State_Syncs_ToAvailable_AfterPublish()
    {
        await using var provider = BuildServices();
        var put = provider.GetRequiredService<IPutObjectUseCase>();
        var res = await put.ExecuteAsync(New("/k", "data", owner: "sys", sizeTag: "s"), None);
        Assert.True(res.Succeeded);

        var index = provider.GetRequiredService<IIndexReader>();
        var page = await index.QueryAsync(new IndexQuery { NamespaceId = Ns }, None);
        var entry = Assert.Single(page.Items);
        Assert.Equal(ObjectState.Available, entry.State);
        Assert.Equal("data".Length, entry.SizeBytes);
    }

    [Fact]
    public async Task PaginatedQuery_ReturnsAll_WithoutDuplicates()
    {
        await using var provider = BuildServices();
        var put = provider.GetRequiredService<IPutObjectUseCase>();
        for (var i = 0; i < 7; i++)
        {
            var res = await put.ExecuteAsync(New($"/bulk/{i}.bin", $"payload-{i}-".PadRight(64, '-'), owner: "bulk", sizeTag: "b"), None);
            Assert.True(res.Succeeded);
        }

        var index = provider.GetRequiredService<IIndexReader>();
        var collected = new List<string>();
        string? cursor = null;
        do
        {
            var page = await index.QueryAsync(new IndexQuery
            {
                NamespaceId = Ns,
                Tags = new Dictionary<string, string> { ["owner"] = "bulk" },
                Page = new PageRequest { Size = 3, Cursor = cursor }
            }, None);
            collected.AddRange(page.Items.Select(e => e.ContentHash));
            cursor = page.NextCursor;
        }
        while (cursor is not null);

        Assert.Equal(7, collected.Count);
        Assert.Equal(7, collected.Distinct().Count());
    }

    private static PutObjectCommand New(string key, string content, string owner, string sizeTag)
        => new()
        {
            NamespaceId = Ns,
            ObjectKey = key,
            Content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)),
            ContentType = "text/plain",
            Tags = new Dictionary<string, string>
            {
                ["owner"] = owner,
                ["size"] = sizeTag
            }
        };

    private static ServiceProvider BuildServices()
    {
        var root = Path.Combine(Path.GetTempPath(), "mono-filebox-index-tests");
        Directory.CreateDirectory(root);
        var options = new FileBoxOptions
        {
            Cluster = { NodeId = "test-node" },
            Storage =
            {
                Pools =
                {
                    new PoolOptions
                    {
                        PoolId = "test",
                        RootPath = Path.Combine(root, "pool"),
                        Tier = StorageTier.Hot,
                        Enabled = true
                    }
                }
            }
        };

        var services = new ServiceCollection();
        services.AddMonoFileBoxLite(options);
        services.AddMonoFileBoxLiteStorage();
        services.AddMonoFileBoxLiteIndex();
        services.AddMonoFileBoxLiteUseCases();
        return services.BuildServiceProvider();
    }
}