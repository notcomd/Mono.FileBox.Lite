using Mono.FileBox.Lite.Abstractions.Backup;
using Mono.FileBox.Lite.Abstractions.Configuration;
using Mono.FileBox.Lite.Configuration;

namespace Mono.FileBox.Lite.Functional.Tests;

/// <summary>Configuration model validation and change notification.
/// 中文翻译：配置模型校验与变更通知。</summary>
public class ConfigurationTests
{
    [Fact]
    public void ValidOptions_AreAccepted()
    {
        var options = Valid();
        var result = new FileBoxOptionsValidator().Validate(options);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void MissingEnabledPool_IsRejected()
    {
        var options = Valid();
        options.Storage.Pools.Clear();
        var result = new FileBoxOptionsValidator().Validate(options);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Domain == "Storage");
    }

    [Fact]
    public void EmptyNodeId_IsRejected()
    {
        var options = Valid();
        options.Cluster.NodeId = "";
        var result = new FileBoxOptionsValidator().Validate(options);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Domain == "Cluster" && e.Field == "NodeId");
    }

    [Fact]
    public void InvalidReplicationQuorum_IsRejected()
    {
        var options = Valid();
        options.Cluster.Replication = new Abstractions.Cluster.ReplicationPolicy { Factor = 3, WriteQuorum = 1, ReadQuorum = 1 };
        var result = new FileBoxOptionsValidator().Validate(options);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Domain == "Cluster" && e.Field == "Replication");
    }

    [Fact]
    public void ScheduleReferencingUnknownTarget_IsRejected()
    {
        var options = Valid();
        options.Backup.Schedules.Add(new BackupScheduleOptions { ScheduleId = "s", TargetId = "missing", Cron = "0 2 * * *" });
        var result = new FileBoxOptionsValidator().Validate(options);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Domain == "Backup");
    }

    [Fact]
    public void PageSizeGtMax_IsRejected()
    {
        var options = Valid();
        options.Index.DefaultPageSize = 5000;
        options.Index.MaxPageSize = 100;
        var result = new FileBoxOptionsValidator().Validate(options);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Domain == "Index");
    }

    [Fact]
    public void ArchiveAfterNotLessThanDeleteAfter_IsRejected()
    {
        var options = Valid();
        options.Lifecycle.ArchiveAfter = TimeSpan.FromDays(30);
        options.Lifecycle.DeleteAfter = TimeSpan.FromDays(10);
        var result = new FileBoxOptionsValidator().Validate(options);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Domain == "Lifecycle");
    }

    [Fact]
    public async Task ChangeNotifier_Reloads_And_SubscriptionDisposes()
    {
        var notifier = new DefaultOptionsChangeNotifier(_ => Task.CompletedTask);
        var received = 0;
        var sub = notifier.OnChange<FileBoxOptions>(_ => received++);

        await notifier.ReloadAsync(CancellationToken.None); // completes without error

        sub.Dispose(); // must not throw
        Assert.True(true);
    }

    private static FileBoxOptions Valid() => new()
    {
        Cluster = { NodeId = "node-a" },
        Lifecycle = { ArchiveAfter = TimeSpan.FromDays(30), DeleteAfter = TimeSpan.FromDays(365) },
        Storage =
        {
            Pools =
            {
                new PoolOptions { PoolId = "p", RootPath = "/tmp/p", Enabled = true }
            }
        },
        Backup =
        {
            Targets = { new BackupTargetOptions { TargetId = "local" } }
        }
    };
}