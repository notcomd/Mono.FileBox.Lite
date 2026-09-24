// <copyright file="BackupBuilder.cs" company="Mono.FileBox.Lite">
// Fluent builder for the backup wiring and target registration.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Mono.FileBox.Lite.Abstractions.Backup;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Configuration;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.UseCases;

namespace Mono.FileBox.Lite.DependencyInjection.Builders;

/// <summary>Fluent builder for the backup wiring and target registration.
/// 用于备份装配与目标注册的流式（fluent）构建器。</summary>
public sealed class BackupBuilder
{
    private readonly IServiceCollection _services;
    private readonly FileBoxOptions _options;
    private readonly IBackupTargetResolver _resolver;

    public BackupBuilder(
        IServiceCollection services,
        FileBoxOptions options,
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
        _options.Backup.Schedules.Add(item: new BackupScheduleOptions
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