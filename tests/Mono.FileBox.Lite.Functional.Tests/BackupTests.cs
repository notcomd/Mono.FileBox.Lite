using Microsoft.Extensions.DependencyInjection;
using Mono.FileBox.Lite.Abstractions.Backup;
using Mono.FileBox.Lite.Abstractions.Configuration;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Abstractions.UseCases;
using Mono.FileBox.Lite.DependencyInjection;

namespace Mono.FileBox.Lite.Functional.Tests;

/// <summary>Object backup: full -> verify -> restore, incremental only-new, point queries.
/// 对象备份：全量 → 校验 → 恢复、增量仅新增、时间点查询。</summary>
public class BackupTests
{
    private const string Ns = "app";
    private static readonly CancellationToken None = CancellationToken.None;

    [Fact]
    public async Task FullBackup_Verify_Restore_AreConsistent()
    {
        await using var sp = Fixture.Build();
        var put = sp.GetRequiredService<IPutObjectUseCase>();
        var a = await put.ExecuteAsync(New("/a.dat", Data(64)), None);
        Assert.True(a.Succeeded);

        var writer = sp.GetRequiredService<IBackupWriter>();
        var point = await writer.CreateAsync(new BackupRequest { TargetId = "local", Scope = new BackupScope { NamespaceId = Ns } }, None);

        Assert.Equal(BackupStatus.Completed, point.Status);
        Assert.True(point.ObjectCount >= 1);

        var restorer = sp.GetRequiredService<IBackupRestorer>();
        var verify = await restorer.VerifyAsync(point.BackupId, None);
        Assert.True(verify.ConsistencyOk);
        Assert.Empty(verify.CorruptedBlocks);

        var restore = await restorer.RestoreToPointAsync(point.BackupId, new RestoreRequest { TargetId = "local" }, None);
        Assert.True(restore.Succeeded);
        Assert.Equal(point.ObjectCount, restore.RestoredCount);

        var reader = sp.GetRequiredService<IBackupReader>();
        var fetched = await reader.GetAsync(point.BackupId, None);
        Assert.NotNull(fetched);
        Assert.Equal(point.BackupId, fetched!.BackupId);
    }

    [Fact]
    public async Task Incremental_Backup_UnionsParentManifest()
    {
        await using var sp = Fixture.Build();
        var put = sp.GetRequiredService<IPutObjectUseCase>();
        await put.ExecuteAsync(New("/one.dat", Data(32)), None);

        var writer = sp.GetRequiredService<IBackupWriter>();
        var full = await writer.CreateAsync(new BackupRequest { TargetId = "local", Scope = new BackupScope { NamespaceId = Ns } }, None);
        Assert.Equal(BackupKind.Full, full.Kind);

        // A second object only exists in the incremental.
        await put.ExecuteAsync(New("/two.dat", Data(48)), None);
        var inc = await writer.ContinueAsync(full.BackupId, new BackupRequest { TargetId = "local", Scope = new BackupScope { NamespaceId = Ns } }, None);

        Assert.Equal(BackupKind.Incremental, inc.Kind);
        Assert.Equal(full.BackupId, inc.ParentBackupId);
        Assert.True(inc.ObjectCount >= full.ObjectCount);

        // Verify the incremental point is consistent and restorable.
        var restorer = sp.GetRequiredService<IBackupRestorer>();
        Assert.True((await restorer.VerifyAsync(inc.BackupId, None)).ConsistencyOk);
        var restore = await restorer.RestoreToPointAsync(inc.BackupId, new RestoreRequest { TargetId = "local" }, None);
        Assert.True(restore.Succeeded);
    }

    [Fact]
    public async Task Backup_Listing_ReturnsCreatedPoints()
    {
        await using var sp = Fixture.Build();
        var writer = sp.GetRequiredService<IBackupWriter>();
        await writer.CreateAsync(new BackupRequest { TargetId = "local", Scope = new BackupScope { NamespaceId = Ns } }, None);

        var reader = sp.GetRequiredService<IBackupReader>();
        var list = await reader.ListAsync(new BackupQuery(), None);
        Assert.NotEmpty(list);
    }

    private static PutObjectCommand New(string key, byte[] data) => new()
    {
        NamespaceId = Ns, ObjectKey = key, Content = new MemoryStream(data)
    };

    private static byte[] Data(int n)
    {
        var b = new byte[n];
        for (var i = 0; i < n; i++) b[i] = (byte)(i * 13);
        return b;
    }

    private static class Fixture
    {
        public static ServiceProvider Build()
        {
            var root = Path.Combine(Path.GetTempPath(), "filebox-func-backup", Guid.NewGuid().ToString("N"));
            var options = new FileBoxOptions
            {
                Cluster = { NodeId = "node" },
                Storage =
                {
                    Pools =
                    {
                        new PoolOptions { PoolId = "p", RootPath = Path.Combine(root, "pool"), Enabled = true, Tier = StorageTier.Hot }
                    }
                }
            };
            var services = new ServiceCollection();
            services.AddMonoFileBoxLite(options);
            services.AddMonoFileBoxLiteStorage();
            services.AddMonoFileBoxLiteIndex();
            services.AddMonoFileBoxLiteUseCases();
            services.AddMonoFileBoxLiteBackup();
            return services.BuildServiceProvider();
        }
    }
}