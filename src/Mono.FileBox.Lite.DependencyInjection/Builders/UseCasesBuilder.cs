// <copyright file="UseCasesBuilder.cs" company="Mono.FileBox.Lite">
// Fluent builder for use-case registration.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Mono.FileBox.Lite.Abstractions.Backup;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.UseCases;

namespace Mono.FileBox.Lite.DependencyInjection.Builders;

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