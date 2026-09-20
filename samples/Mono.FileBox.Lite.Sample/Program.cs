using Microsoft.Extensions.DependencyInjection;
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Backup;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Configuration;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.UseCases;
using Mono.FileBox.Lite.DependencyInjection;

namespace Mono.FileBox.Lite.Sample;

/// <summary>
/// End-to-end demonstration: Pending → Purged lifecycle, index query, backup/verify,
/// cluster shaping and capacity. Runs entirely against a temporary local directory.
/// </summary>
public static class Program
{
    public static async Task<int> Main()
    {
        try
        {
            await using var provider = BuildServices();

            Console.WriteLine("== Mono.FileBox.Lite Sample ==");

            // 1. Put an object through the full Put → Index → Audit → Publish pipeline.
            var put = provider.GetRequiredService<IPutObjectUseCase>();
            var content = "hello filebox lite".ToBytes();
            var putResult = await put.ExecuteAsync(new PutObjectCommand
            {
                NamespaceId = "ns1",
                ObjectKey = "/docs/readme.txt",
                Content = new MemoryStream(content),
                ContentType = "text/plain",
                Tags = new Dictionary<string, string> { ["department"] = "platform", ["env"] = "dev" },
                Attributes = new Dictionary<string, object> { ["owner"] = "trac", ["reviews"] = 3L }
            }, CancellationToken.None);

            if (!putResult.Succeeded)
            {
                Console.WriteLine($"Put failed: {putResult.Error}");
                return 1;
            }
            Console.WriteLine($"Put OK: hash={putResult.ContentHash} state={putResult.State}");

            // 2. Read it back (pure query, no transition).
            var get = provider.GetRequiredService<IGetObjectUseCase>();
            var read = await get.ExecuteAsync(new GetObjectCommand
            {
                ContentHash = putResult.ContentHash,
                NamespaceId = "ns1"
            }, CancellationToken.None);
            if (read.Content is not null)
            {
                using var ms = new MemoryStream();
                await read.Content.CopyToAsync(ms);
                Console.WriteLine($"Get OK: bytes={ms.ToArray().Count()}");

                // 3. Query the index by a tag predicate.
                var index = provider.GetRequiredService<IIndexReader>();
                var page = await index.QueryAsync(new IndexQuery
                {
                    NamespaceId = "ns1",
                    Tags = new Dictionary<string, string> { ["department"] = "platform" }
                }, CancellationToken.None);
                Console.WriteLine($"Index query OK: matched={page.Items.Count}");

                // 4. Index consistency verify (physical layers are authoritative).
                var maintainer = provider.GetRequiredService<IIndexMaintainer>();
                var report = await maintainer.VerifyAsync(CancellationToken.None);
                Console.WriteLine($"Index verify OK: consistent={report.IsConsistent}");

                // 5. Full backup + verify.
                var backupWriter = provider.GetRequiredService<IBackupWriter>();
                var point = await backupWriter.CreateAsync(new BackupRequest
                {
                    TargetId = "local",
                    Scope = new BackupScope { NamespaceId = "ns1" }
                }, CancellationToken.None);
                Console.WriteLine($"Backup OK: id={point.BackupId} objects={point.ObjectCount}");

                var restorer = provider.GetRequiredService<IBackupRestorer>();
                var v = await restorer.VerifyAsync(point.BackupId, CancellationToken.None);
                Console.WriteLine($"Backup verify OK: consistent={v.ConsistencyOk}");

                // 6. Lifecycle: archive then restore, then delete then purge.
                var archive = provider.GetRequiredService<IArchiveObjectUseCase>();
                var ar = await archive.ExecuteAsync(new ArchiveObjectCommand
                {
                    ContentHash = putResult.ContentHash, NamespaceId = "ns1"
                }, CancellationToken.None);
                Console.WriteLine($"Archive OK: hash={ar.ContentHash} state={ar.State}");

                var restore = provider.GetRequiredService<IRestoreObjectUseCase>();
                var rr = await restore.ExecuteAsync(new RestoreObjectCommand
                {
                    ContentHash = putResult.ContentHash, NamespaceId = "ns1"
                }, CancellationToken.None);
                Console.WriteLine($"Restore OK: hash={rr.ContentHash} state={rr.State}");

                var del = provider.GetRequiredService<IDeleteObjectUseCase>();
                var dr = await del.ExecuteAsync(new DeleteObjectCommand
                {
                    ContentHash = putResult.ContentHash, NamespaceId = "ns1"
                }, CancellationToken.None);
                Console.WriteLine($"Delete OK: hash={dr.ContentHash} state={dr.State}");

                // 7. Cluster shaping + capacity (single-node mode).
                var expander = provider.GetRequiredService<IClusterExpander>();
                await expander.JoinAsync(new NodeInfo
                {
                    NodeId = "node-a", Role = NodeRole.Hybrid
                }, CancellationToken.None);
                await expander.RebalanceAsync(new RebalanceOptions(), CancellationToken.None);
                var progress = await expander.GetProgressAsync(CancellationToken.None);
                Console.WriteLine($"Rebalance OK: complete={progress.IsComplete}");

                var capacity = provider.GetRequiredService<ICapacityMonitor>();
                var status = await capacity.GetStatusAsync(CancellationToken.None);
                Console.WriteLine($"Capacity OK: total={status.TotalBytes} used={status.UsedBytes} state={status.State}");

                Console.WriteLine("\nSample completed successfully.");
                return 0;
            }

            Console.WriteLine("Get returned no content (object not available).");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Sample failed: {ex}");
            return 1;
        }
    }

    private static ServiceProvider BuildServices()
    {
        var root = Path.Combine(Path.GetTempPath(), "mono-filebox-sample");
        Directory.CreateDirectory(root);

        var options = new FileBoxOptions
        {
            Cluster = { NodeId = "node-a" },
            Storage =
            {
                Pools =
                {
                    new PoolOptions
                    {
                        PoolId = "hot-1",
                        RootPath = Path.Combine(root, "hot"),
                        Tier = StorageTier.Hot,
                        Enabled = true
                    }
                }
            }
        };

        var services = new ServiceCollection();
        services.AddMonoFileBoxLite(options);
        // Override the default never-expire policy so the Archive transition is permitted in the demo.
        services.AddSingleton<ILifecyclePolicy, AllowArchivePolicy>();
        services.AddMonoFileBoxLiteStorage();
        services.AddMonoFileBoxLiteIndex();
        services.AddMonoFileBoxLiteUseCases();
        services.AddMonoFileBoxLiteScaling();
        services.AddMonoFileBoxLiteBackup();
        return services.BuildServiceProvider();
    }

    private static byte[] ToBytes(this string value) => System.Text.Encoding.UTF8.GetBytes(value);

    /// <summary>Permissive lifecycle policy: allows archive (and expire) transitions.</summary>
    private sealed class AllowArchivePolicy : ILifecyclePolicy, IGuard
    {
        public Task<bool> CanEnterAsync(IObjectContext ctx, CancellationToken ct)
            => Task.FromResult(true);
    }
}