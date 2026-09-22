// <copyright file="IndexBuilder.cs" company="Mono.FileBox.Lite">
// Fluent builder for the structured index wiring.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Mono.FileBox.Lite.Abstractions.Backup;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.UseCases;

namespace Mono.FileBox.Lite.DependencyInjection.Builders;

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