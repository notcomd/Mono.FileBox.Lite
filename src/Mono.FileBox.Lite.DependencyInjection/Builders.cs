using Microsoft.Extensions.DependencyInjection;
using Mono.FileBox.Lite.Abstractions.Backup;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.UseCases;

namespace Mono.FileBox.Lite.DependencyInjection.Builders;

/// <summary>Fluent builder for the storage engine wiring.</summary>
public sealed class StorageBuilder
{
    private readonly IServiceCollection _services;
    public StorageBuilder(IServiceCollection services) => _services = services;

    public StorageBuilder UseObjectWriter<T>() where T : class, IObjectWriter
    { _services.AddSingleton<IObjectWriter, T>(); return this; }
    public StorageBuilder UseDiskSelector<T>() where T : class, IDiskSelector
    { _services.AddSingleton<IDiskSelector, T>(); return this; }
    public StorageBuilder UseIOPipeline<T>() where T : class, IIOPipeline
    { _services.AddSingleton<IIOPipeline, T>(); return this; }
    public StorageBuilder UsePhysicalDevice<T>() where T : class, IPhysicalDevice
    { _services.AddSingleton<IPhysicalDevice, T>(); return this; }
    public StorageBuilder UseReader<T>() where T : class, IObjectReader
    { _services.AddSingleton<IObjectReader, T>(); return this; }
}

/// <summary>Fluent builder for the structured index wiring.</summary>
public sealed class IndexBuilder
{
    private readonly IServiceCollection _services;
    public IndexBuilder(IServiceCollection services) => _services = services;

    public IndexBuilder UseOrderedKeyValueStore<T>() where T : class, IOrderedKeyValueStore
    { _services.AddSingleton<IOrderedKeyValueStore, T>(); return this; }
    public IndexBuilder UseEntryStore<T>() where T : class, IEntryStore
    { _services.AddSingleton<IEntryStore, T>(); return this; }
    public IndexBuilder UseCursorCodec<T>() where T : class, ICursorCodec
    { _services.AddSingleton<ICursorCodec, T>(); return this; }
    public IndexBuilder AddProvider<T>() where T : class, IIndexProvider
    { _services.AddSingleton<IIndexProvider, T>(); return this; }
    public IndexBuilder UsePlanner<T>() where T : class, IQueryPlanner
    { _services.AddSingleton<IQueryPlanner, T>(); return this; }
    public IndexBuilder UseExecutor<T>() where T : class, IQueryExecutor
    { _services.AddSingleton<IQueryExecutor, T>(); return this; }
    public IndexBuilder UseReader<T>() where T : class, IIndexReader
    { _services.AddSingleton<IIndexReader, T>(); return this; }
    public IndexBuilder UseWriter<T>() where T : class, IIndexWriter
    { _services.AddSingleton<IIndexWriter, T>(); return this; }
    public IndexBuilder UseMaintainer<T>() where T : class, IIndexMaintainer
    { _services.AddSingleton<IIndexMaintainer, T>(); return this; }
    public IndexBuilder UseReplicator<T>() where T : class, IIndexReplicator
    { _services.AddSingleton<IIndexReplicator, T>(); return this; }
}

/// <summary>Fluent builder for the backup wiring and target registration.</summary>
public sealed class BackupBuilder
{
    private readonly IServiceCollection _services;
    private readonly Mono.FileBox.Lite.Abstractions.Configuration.FileBoxOptions _options;
    private readonly IBackupTargetResolver _resolver;

    public BackupBuilder(
        IServiceCollection services,
        Mono.FileBox.Lite.Abstractions.Configuration.FileBoxOptions options,
        IBackupTargetResolver resolver)
    {
        _services = services;
        _options = options;
        _resolver = resolver;
    }

    public BackupBuilder UseTargetResolver<T>() where T : class, IBackupTargetResolver
    { _services.AddSingleton<IBackupTargetResolver, T>(); return this; }
    public BackupBuilder UseTarget(string id, Func<IServiceProvider, IBackupTarget> factory)
    { _resolver.Register(id, factory(_services.BuildServiceProvider())); return this; }
    public BackupBuilder UsePointStore<T>() where T : class, IBackupPointStore
    { _services.AddSingleton<IBackupPointStore, T>(); return this; }
    public BackupBuilder UseManifestStore<T>() where T : class, IBackupManifestStore
    { _services.AddSingleton<IBackupManifestStore, T>(); return this; }
    public BackupBuilder UseWriter<T>() where T : class, IBackupWriter
    { _services.AddSingleton<IBackupWriter, T>(); return this; }
    public BackupBuilder UseReader<T>() where T : class, IBackupReader
    { _services.AddSingleton<IBackupReader, T>(); return this; }
    public BackupBuilder UseRestorer<T>() where T : class, IBackupRestorer
    { _services.AddSingleton<IBackupRestorer, T>(); return this; }
    public BackupBuilder UseScheduler<T>() where T : class, IBackupScheduler
    { _services.AddSingleton<IBackupScheduler, T>(); return this; }
    public BackupBuilder UseGarbageCollector<T>() where T : class, IBackupGarbageCollector
    { _services.AddSingleton<IBackupGarbageCollector, T>(); return this; }
    public BackupBuilder Schedule(BackupSchedule schedule)
    {
        _options.Backup.Schedules.Add(new Mono.FileBox.Lite.Abstractions.Configuration.BackupScheduleOptions
        {
            ScheduleId = schedule.ScheduleId,
            Cron = schedule.Cron,
            Kind = schedule.Kind,
            TargetId = schedule.TargetId,
            NamespaceId = schedule.NamespaceId,
            Retention = schedule.Retention,
            MaxKeep = schedule.MaxKeep,
            Enabled = schedule.Enabled
        });
        return this;
    }
}

/// <summary>Fluent builder for cluster scaling wiring.</summary>
public sealed class ScalingBuilder
{
    private readonly IServiceCollection _services;
    private readonly Mono.FileBox.Lite.Abstractions.Configuration.FileBoxOptions _options;

    public ScalingBuilder(
        IServiceCollection services,
        Mono.FileBox.Lite.Abstractions.Configuration.FileBoxOptions options)
    {
        _services = services;
        _options = options;
    }

    public ScalingBuilder Configure(Action<Mono.FileBox.Lite.Abstractions.Configuration.ClusterOptions> configure)
    { configure(_options.Cluster); return this; }
    public ScalingBuilder UseHashRing<T>() where T : class, IHashRing
    { _services.AddSingleton<IHashRing, T>(); return this; }
    public ScalingBuilder UseTopology<T>() where T : class, IClusterTopology
    { _services.AddSingleton<IClusterTopology, T>(); return this; }
    public ScalingBuilder UseExpander<T>() where T : class, IClusterExpander
    { _services.AddSingleton<IClusterExpander, T>(); return this; }
    public ScalingBuilder UseShrinker<T>() where T : class, IClusterShrinker
    { _services.AddSingleton<IClusterShrinker, T>(); return this; }
    public ScalingBuilder UseCapacityMonitor<T>() where T : class, ICapacityMonitor
    { _services.AddSingleton<ICapacityMonitor, T>(); return this; }
    public ScalingBuilder UseTierManager<T>() where T : class, ITierManager
    { _services.AddSingleton<ITierManager, T>(); return this; }
    public ScalingBuilder UseSharding<T>() where T : class, IIndexSharding
    { _services.AddSingleton<IIndexSharding, T>(); return this; }
}

/// <summary>Fluent builder for use-case registration.</summary>
public sealed class UseCasesBuilder
{
    private readonly IServiceCollection _services;
    public UseCasesBuilder(IServiceCollection services) => _services = services;

    public UseCasesBuilder AddPutObject<T>() where T : class, IPutObjectUseCase
    { _services.AddSingleton<IPutObjectUseCase, T>(); return this; }
    public UseCasesBuilder AddGetObject<T>() where T : class, IGetObjectUseCase
    { _services.AddSingleton<IGetObjectUseCase, T>(); return this; }
    public UseCasesBuilder AddDeleteObject<T>() where T : class, IDeleteObjectUseCase
    { _services.AddSingleton<IDeleteObjectUseCase, T>(); return this; }
    public UseCasesBuilder AddArchiveObject<T>() where T : class, IArchiveObjectUseCase
    { _services.AddSingleton<IArchiveObjectUseCase, T>(); return this; }
    public UseCasesBuilder AddRestoreObject<T>() where T : class, IRestoreObjectUseCase
    { _services.AddSingleton<IRestoreObjectUseCase, T>(); return this; }
}