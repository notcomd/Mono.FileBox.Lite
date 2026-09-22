// <copyright file="ScalingBuilder.cs" company="Mono.FileBox.Lite">
// Fluent builder for cluster scaling wiring.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Mono.FileBox.Lite.Abstractions.Backup;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.UseCases;

namespace Mono.FileBox.Lite.DependencyInjection.Builders;

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