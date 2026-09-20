using Microsoft.Extensions.DependencyInjection;
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Backup;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Configuration;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.UseCases;
using Mono.FileBox.Lite.Backup;
using Mono.FileBox.Lite.Backup.Reader;
using Mono.FileBox.Lite.Backup.Restorer;
using Mono.FileBox.Lite.Backup.Scheduler;
using Mono.FileBox.Lite.Backup.Targets;
using Mono.FileBox.Lite.Backup.Writer;
using Mono.FileBox.Lite.Cluster;
using Mono.FileBox.Lite.Cluster.Capacity;
using Mono.FileBox.Lite.Cluster.HashRing;
using Mono.FileBox.Lite.Cluster.Tiering;
using Mono.FileBox.Lite.Cluster.Topology;
using Mono.FileBox.Lite.Configuration;
using Mono.FileBox.Lite.DependencyInjection.Builders;
using Mono.FileBox.Lite.Index.Providers;
using Mono.FileBox.Lite.Index.Storage;
using Mono.FileBox.Lite.StateMachine;
using Mono.FileBox.Lite.Storage;
using Mono.FileBox.Lite.Storage.DiskSelector;
using Mono.FileBox.Lite.Storage.IOPipeline;
using Mono.FileBox.Lite.Storage.ObjectWriter;
using Mono.FileBox.Lite.Storage.PhysicalDevice;
using Mono.FileBox.Lite.Subsystems;
using Mono.FileBox.Lite.Subsystems.Index;
using Mono.FileBox.Lite.Subsystems.Lifecycle;
using Mono.FileBox.Lite.UseCases;

namespace Mono.FileBox.Lite.DependencyInjection;

/// <summary>Composes all Mono.FileBox.Lite components into an <see cref="IServiceCollection"/>.</summary>
public static class FileBoxServiceCollectionExtensions
{
    // -------- Core (L0/L3 + subsystems defaults) --------

    public static IServiceCollection AddMonoFileBoxLite(
        this IServiceCollection services,
        FileBoxOptions? options = null,
        Action<TransitionRegistry>? machine = null)
    {
        options ??= new FileBoxOptions();
        services.AddSingleton(options);
        services.AddSingleton<IOptionsValidator<FileBoxOptions>, FileBoxOptionsValidator>();
        services.AddSingleton<IOptionsChangeNotifier>(_ => new DefaultOptionsChangeNotifier());

        services.AddSingleton<IObjectContextFactory, DefaultObjectContextFactory>();
        services.AddSingleton<IStateStore, InMemoryStateStore>();

        var self = new NodeInfo
        {
            NodeId = options.Cluster.NodeId,
            Role = options.Cluster.Role,
            AdvertiseAddress = options.Cluster.AdvertiseAddress ?? "local:0"
        };
        services.AddSingleton(self);
        services.AddSingleton<ILeaderElection>(_ => new SingleNodeLeaderElection(self));
        services.AddSingleton<INodeRegistry, InMemoryNodeRegistry>();

        // Default primitives (single-node friendly).
        services.AddSingleton<IDistributedLock, NoOpDistributedLock>();
        services.AddSingleton<ILifecyclePolicy, NeverExpirePolicy>();
        services.AddSingleton<IContentModerator, PassThroughModerator>();
        services.AddSingleton<IEventBus, NullEventBus>();
        services.AddSingleton<ITransitionLogger, NullLogger>();
        services.AddSingleton<ILifecycleScheduler, DefaultLifecycleScheduler>();

        var registry = new TransitionRegistry();
        if (machine is not null) machine(registry);
        else AddDefaultObjectTransitions(registry);
        services.AddSingleton(registry);

        services.AddSingleton<IObjectStateMachine>(sp => new DefaultObjectStateMachine(
            sp.GetRequiredService<TransitionRegistry>(),
            sp,
            sp.GetRequiredService<IStateStore>(),
            options.StateMachine.ThrowOnGuardDenied,
            options.StateMachine.FailFastOnObserverError,
            options.StateMachine.MaxOptimisticRetries,
            options.StateMachine.RetryBackoff));

        return services;
    }

    /// <summary>Registers the full lifecycle transition table from the documentation.</summary>
    public static void AddDefaultObjectTransitions(TransitionRegistry registry)
    {
        registry.On(ObjectTrigger.Put).From(ObjectState.Pending).To(ObjectState.Stored)
            .Guard<IDistributedLock>().Do<IObjectWriter>().Observe<IEventBus>();
        registry.On(ObjectTrigger.Index).From(ObjectState.Stored).To(ObjectState.Indexed)
            .Do<IIndexWriter>().Observe<IEventBus>();
        registry.On(ObjectTrigger.Audit).From(ObjectState.Indexed).To(ObjectState.Audited)
            .Do<IContentModerator>().Observe<IndexStateSyncObserver>();
        registry.On(ObjectTrigger.Publish).From(ObjectState.Audited).To(ObjectState.Available)
            .Observe<ITransitionLogger>().Observe<IndexStateSyncObserver>();
        registry.On(ObjectTrigger.Archive).From(ObjectState.Available).To(ObjectState.Archived)
            .Guard<ILifecyclePolicy>().Observe<IndexStateSyncObserver>();
        registry.On(ObjectTrigger.Restore).From(ObjectState.Archived).To(ObjectState.Available)
            .Observe<IndexStateSyncObserver>();
        registry.On(ObjectTrigger.Expire).From(ObjectState.Available).To(ObjectState.Expired)
            .Guard<ILifecyclePolicy>().Observe<IndexStateSyncObserver>();
        registry.On(ObjectTrigger.Delete).From(ObjectState.Available).To(ObjectState.Deleted)
            .Observe<IndexStateSyncObserver>();
        registry.On(ObjectTrigger.Purge).From(ObjectState.Deleted).To(ObjectState.Purged)
            .Do<IPhysicalEraser>().Observe<IEventBus>();
        registry.On(ObjectTrigger.Purge).From(ObjectState.Expired).To(ObjectState.Purged)
            .Do<IPhysicalEraser>().Observe<IEventBus>();
        registry.On(ObjectTrigger.Delete).From(ObjectState.Archived).To(ObjectState.Deleted)
            .Observe<IndexStateSyncObserver>();
    }

    // -------- Storage (L1) --------

    public static IServiceCollection AddMonoFileBoxLiteStorage(
        this IServiceCollection services, Action<StorageBuilder>? configure = null)
    {
        services.AddSingleton<IPhysicalDevice, LocalFileSystemDevice>();
        services.AddSingleton<IIOPipeline, BufferedIOPipeline>();
        services.AddSingleton<IDiskSelector>(sp => new ConsistentHashDiskSelector(GetEnabledPools(sp)));
        services.AddSingleton<IObjectWriter, Sha256ObjectWriter>();
        services.AddSingleton<IObjectReader, StorageObjectReader>();
        services.AddSingleton<IPhysicalEraser, StoragePhysicalEraser>();

        configure?.Invoke(new StorageBuilder(services));
        return services;
    }

    // -------- Index (L2) --------

    public static IServiceCollection AddMonoFileBoxLiteIndex(
        this IServiceCollection services, Action<IndexBuilder>? configure = null)
    {
        services.AddSingleton<IOrderedKeyValueStore, InMemoryOrderedKeyValueStore>();
        services.AddSingleton<IEntryStore, InMemoryEntryStore>();
        services.AddSingleton<ICursorCodec, Base64JsonCursorCodec>();

        services.AddSingleton<IIndexProvider, PrefixIndexProvider>();
        services.AddSingleton<IIndexProvider, TagInvertedIndexProvider>();
        services.AddSingleton<IIndexProvider, AttributeIndexProvider>();
        services.AddSingleton<IIndexProvider, TierBitmapProvider>();
        services.AddSingleton<IIndexProvider, StateBitmapProvider>();
        services.AddSingleton<IIndexProvider, TimeIndexProvider>();
        services.AddSingleton<IIndexProvider, SizeIndexProvider>();

        services.AddSingleton<IQueryPlanner>(sp =>
            new Mono.FileBox.Lite.Index.Planning.QueryPlanner(
                sp.GetServices<IIndexProvider>(), sp.GetRequiredService<IEntryStore>()));
        services.AddSingleton<IQueryExecutor, Mono.FileBox.Lite.Index.Planning.QueryExecutor>();
        services.AddSingleton<IIndexReader, Mono.FileBox.Lite.Index.Planning.DefaultIndexReader>();
        services.AddSingleton<IIndexMaintainer, Mono.FileBox.Lite.Index.IndexMaintainer>();

        services.AddSingleton<IIndexWriter, DefaultIndexWriter>();
        services.AddSingleton<IndexStateSyncObserver>();

        configure?.Invoke(new IndexBuilder(services));
        return services;
    }

    // -------- Backup (L2) --------

    public static IServiceCollection AddMonoFileBoxLiteBackup(
        this IServiceCollection services, Action<BackupBuilder>? configure = null)
    {
        var options = services.GetOrAddOptions();
        var storeOptions = new BackupStoreOptions
        {
            RootPath = Path.Combine(Path.GetTempPath(), "mono-filebox-backup-meta")
        };
        services.AddSingleton(storeOptions);
        services.AddSingleton<IBackupPointStore, FileSystemBackupPointStore>();
        services.AddSingleton<IBackupManifestStore, FileSystemBackupManifestStore>();

        var resolver = new DefaultBackupTargetResolver();
        resolver.Register("local", new LocalDirectoryTarget(new LocalDirectoryOptions
        {
            RootPath = Path.Combine(Path.GetTempPath(), "mono-filebox-backups", "local")
        }));
        services.AddSingleton(resolver);
        services.AddSingleton<IBackupTargetResolver>(resolver);

        services.AddSingleton<IBackupWriter, DefaultBackupWriter>();
        services.AddSingleton<IBackupReader, DefaultBackupReader>();
        services.AddSingleton<IBackupRestorer, DefaultBackupRestorer>();
        services.AddSingleton<IBackupScheduler, CronBackupScheduler>();
        services.AddSingleton<IBackupGarbageCollector, NoOpBackupGarbageCollector>();

        configure?.Invoke(new BackupBuilder(services, options, resolver));
        return services;
    }

    // -------- Scaling / cluster (L2) --------

    public static IServiceCollection AddMonoFileBoxLiteScaling(
        this IServiceCollection services, Action<ScalingBuilder>? configure = null)
    {
        var options = services.GetOrAddOptions();
        services.AddSingleton<IHashRing, ConsistentHashRing>();
        services.AddSingleton<IClusterTopology>(sp => new RegistryBackedTopology(
            sp.GetRequiredService<INodeRegistry>(), sp.GetRequiredService<NodeInfo>()));
        services.AddSingleton<IClusterExpander, DefaultClusterExpander>();
        services.AddSingleton<IClusterShrinker, DefaultClusterShrinker>();
        services.AddSingleton<ICapacityMonitor>(sp => new DefaultCapacityMonitor(
            sp.GetRequiredService<IDiskSelector>(), options.Cluster.Thresholds));
        services.AddSingleton<ITierManager, InMemoryTierManager>();
        services.AddSingleton<IIndexSharding, DefaultIndexSharding>();

        configure?.Invoke(new ScalingBuilder(services, options));
        return services;
    }

    // -------- Use cases (L4) --------

    public static IServiceCollection AddMonoFileBoxLiteUseCases(
        this IServiceCollection services, Action<UseCasesBuilder>? configure = null)
    {
        services.AddSingleton<IPutObjectRollback, PutObjectRollback>();
        services.AddSingleton<IPutObjectUseCase, PutObjectUseCase>();
        services.AddSingleton<IGetObjectUseCase, GetObjectUseCase>();
        services.AddSingleton<IDeleteObjectUseCase, DeleteObjectUseCase>();
        services.AddSingleton<IArchiveObjectUseCase, ArchiveObjectUseCase>();
        services.AddSingleton<IRestoreObjectUseCase, RestoreObjectUseCase>();

        configure?.Invoke(new UseCasesBuilder(services));
        return services;
    }

    private static IReadOnlyList<PoolOptions> GetEnabledPools(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<FileBoxOptions>();
        return options.Storage.Pools.ToList();
    }

    private static FileBoxOptions GetOrAddOptions(this IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(FileBoxOptions));
        if (descriptor is not null && descriptor.ImplementationInstance is FileBoxOptions existing)
            return existing;

        var options = new FileBoxOptions();
        services.AddSingleton(options);
        return options;
    }
}