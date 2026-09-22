// <copyright file="StorageBuilder.cs" company="Mono.FileBox.Lite">
// Fluent builder for the storage engine wiring.
// </copyright>

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