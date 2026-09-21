using Microsoft.Extensions.DependencyInjection;
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Configuration;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.UseCases;
using Mono.FileBox.Lite.DependencyInjection;

namespace Mono.FileBox.Lite.Functional.Tests;

/// <summary>
/// Use-case orchestration over the full engine: put/get/delete/purge/archive/restore,
/// exercising the state machine, storage and index together.
/// </summary>
public class UseCasesLifecycleTests
{
    private const string Ns = "app";
    private static readonly CancellationToken None = CancellationToken.None;

    [Fact]
    public async Task PutThenGet_ReturnsExactContent_AtAvailable()
    {
        await using var sp = Fixture.Build();
        var put = sp.GetRequiredService<IPutObjectUseCase>();
        var get = sp.GetRequiredService<IGetObjectUseCase>();

        var content = Data("hello-filebox");
        var res = await put.ExecuteAsync(new PutObjectCommand
        {
            NamespaceId = Ns, ObjectKey = "/a.txt", Content = new MemoryStream(content)
        }, None);
        Assert.True(res.Succeeded);
        Assert.Equal(ObjectState.Available, res.State);

        var read = await get.ExecuteAsync(new GetObjectCommand { ContentHash = res.ContentHash, NamespaceId = Ns }, None);
        Assert.NotNull(read.Content);
        using var ms = new MemoryStream();
        await read.Content!.CopyToAsync(ms);
        Assert.Equal(content, ms.ToArray());
    }

    [Fact]
    public async Task Get_UnknownHash_ReturnsNotFound()
    {
        await using var sp = Fixture.Build();
        var get = sp.GetRequiredService<IGetObjectUseCase>();
        var read = await get.ExecuteAsync(new GetObjectCommand { ContentHash = "zz", NamespaceId = Ns }, None);
        Assert.Null(read.Content);
    }

    [Fact]
    public async Task Delete_ThenPurge_RemovesPhysicalBlockAndIndex()
    {
        await using var sp = Fixture.Build();
        var put = sp.GetRequiredService<IPutObjectUseCase>();
        var del = sp.GetRequiredService<IDeleteObjectUseCase>();

        var res = await put.ExecuteAsync(new PutObjectCommand
        {
            NamespaceId = Ns, ObjectKey = "/del.bin", Content = new MemoryStream(Data("to-delete"))
        }, None);
        Assert.True(res.Succeeded);

        var delRes = await del.ExecuteAsync(new DeleteObjectCommand { ContentHash = res.ContentHash, NamespaceId = Ns }, None);
        Assert.Equal(ObjectState.Deleted, delRes.State);

        // Physical block still present before purge (delayed clear).
        var writer = sp.GetRequiredService<IObjectWriter>();
        Assert.True(await writer.ExistsAsync(res.ContentHash, None));

        // Purge (Deleted → Purged).
        var machine = sp.GetRequiredService<IObjectStateMachine>();
        var factory = sp.GetRequiredService<IObjectContextFactory>();
        var ctx = factory.CreateFromExisting(res.ContentHash, Ns, ObjectState.Deleted);
        var state = await machine.FireAsync(ObjectTrigger.Purge, ctx, None);
        Assert.Equal(ObjectState.Purged, state);

        Assert.False(await writer.ExistsAsync(res.ContentHash, None));

        // Index entry removed.
        var index = sp.GetRequiredService<IIndexReader>();
        var page = await index.QueryAsync(new IndexQuery { NamespaceId = Ns }, None);
        Assert.DoesNotContain(page.Items, e => e.ContentHash == res.ContentHash);
    }

    [Fact]
    public async Task ArchiveThenRestore_ReturnsToAvailable()
    {
        await using var sp = Fixture.Build();
        var put = sp.GetRequiredService<IPutObjectUseCase>();
        var archive = sp.GetRequiredService<IArchiveObjectUseCase>();
        var restore = sp.GetRequiredService<IRestoreObjectUseCase>();

        var res = await put.ExecuteAsync(new PutObjectCommand
        {
            NamespaceId = Ns, ObjectKey = "/cold.log", Content = new MemoryStream(Data("cold"))
        }, None);
        Assert.True(res.Succeeded);

        var ar = await archive.ExecuteAsync(new ArchiveObjectCommand { ContentHash = res.ContentHash, NamespaceId = Ns }, None);
        Assert.Equal(ObjectState.Archived, ar.State);

        var rr = await restore.ExecuteAsync(new RestoreObjectCommand { ContentHash = res.ContentHash, NamespaceId = Ns }, None);
        Assert.Equal(ObjectState.Available, rr.State);
    }

    [Fact]
    public async Task ChunkedObject_Lifecycle_StillWorks()
    {
        await using var sp = Fixture.Build(chunking: true, chunkSize: 1024);
        var put = sp.GetRequiredService<IPutObjectUseCase>();
        var get = sp.GetRequiredService<IGetObjectUseCase>();

        var content = Data(3000); // > chunk size → chunked
        var res = await put.ExecuteAsync(new PutObjectCommand
        {
            NamespaceId = Ns, ObjectKey = "/big.bin", Content = new MemoryStream(content)
        }, None);
        Assert.True(res.Succeeded);

        var read = await get.ExecuteAsync(new GetObjectCommand { ContentHash = res.ContentHash, NamespaceId = Ns }, None);
        using var ms = new MemoryStream();
        await read.Content!.CopyToAsync(ms);
        Assert.Equal(content, ms.ToArray());
    }

    private static byte[] Data(string s) => System.Text.Encoding.UTF8.GetBytes(s);
    private static byte[] Data(int n)
    {
        var b = new byte[n];
        for (var i = 0; i < n; i++) b[i] = (byte)(i * 31);
        return b;
    }

    private static class Fixture
    {
        public static ServiceProvider Build(bool chunking = false, int chunkSize = 8 * 1024 * 1024)
        {
            var root = Path.Combine(Path.GetTempPath(), "filebox-func-usecase", Guid.NewGuid().ToString("N"));
            var options = new FileBoxOptions
            {
                Cluster = { NodeId = "node" },
                Storage =
                {
                    Pools =
                    {
                        new PoolOptions { PoolId = "p", RootPath = Path.Combine(root, "pool"), Enabled = true, Tier = StorageTier.Hot }
                    },
                    Chunking = { Enabled = chunking, ChunkSizeBytes = chunkSize }
                }
            };
            var services = new ServiceCollection();
            services.AddMonoFileBoxLite(options);
            services.AddSingleton<ILifecyclePolicy, AllowPolicy>(); // permit archive
            services.AddMonoFileBoxLiteStorage();
            services.AddMonoFileBoxLiteIndex();
            services.AddMonoFileBoxLiteUseCases();
            return services.BuildServiceProvider();
        }
    }

    private sealed class AllowPolicy : ILifecyclePolicy, IGuard
    {
        public Task<bool> CanEnterAsync(IObjectContext ctx, CancellationToken ct) => Task.FromResult(true);
    }
}