using Mono.FileBox.Lite.Abstractions.Backup;
using Mono.FileBox.Lite.Abstractions.Configuration;

namespace Mono.FileBox.Lite.Configuration;

/// <summary>
/// Validates a <see cref="FileBoxOptions"/> against the documented built-in rules.
/// Startup validation failure rejects startup; reload validation failure keeps the old
/// configuration.
/// </summary>
public sealed class FileBoxOptionsValidator : IOptionsValidator<FileBoxOptions>
{
    public OptionsValidationResult Validate(FileBoxOptions options)
    {
        var errors = new List<ValidationError>();
        if (options is null)
        {
            errors.Add(new ValidationError { Domain = "Root", Field = "", Message = "Options must not be null." });
            return new OptionsValidationResult { Errors = errors };
        }

        // StateMachine
        if (options.StateMachine.TransitionTimeout <= TimeSpan.Zero)
            errors.Add(Err("StateMachine", "TransitionTimeout", "Must be > 0."));
        if (options.StateMachine.MaxOptimisticRetries < 0)
            errors.Add(Err("StateMachine", "MaxOptimisticRetries", "Must be >= 0."));

        // Storage
        var enabledPools = options.Storage.Pools.Where(p => p.Enabled).ToList();
        if (enabledPools.Count == 0)
            errors.Add(Err("Storage", "Pools", "At least one enabled pool is required."));
        if (enabledPools.Any(p => string.IsNullOrWhiteSpace(p.RootPath)))
            errors.Add(Err("Storage", "Pools", "RootPath must not be empty on enabled pools."));
        if (enabledPools.Select(p => p.PoolId).Distinct().Count() != enabledPools.Count)
            errors.Add(Err("Storage", "Pools", "PoolId must be unique."));

        // Storage.Pipeline
        if (options.Storage.Pipeline.BufferSize <= 0)
            errors.Add(Err("Storage.Pipeline", "BufferSize", "Must be > 0."));
        if (options.Storage.Pipeline.MaxConcurrency <= 0)
            errors.Add(Err("Storage.Pipeline", "MaxConcurrency", "Must be > 0."));

        // Index
        if (options.Index.DefaultPageSize > options.Index.MaxPageSize)
            errors.Add(Err("Index", "DefaultPageSize", "Must be <= MaxPageSize."));
        if (options.Index.MaxPageSize <= 0)
            errors.Add(Err("Index", "MaxPageSize", "Must be > 0."));
        if (options.Index.Sharding.ShardCount <= 0)
            errors.Add(Err("Index.Sharding", "ShardCount", "Must be > 0."));

        // Backup
        var targetIds = new HashSet<string>(options.Backup.Targets.Select(t => t.TargetId), StringComparer.Ordinal);
        foreach (var schedule in options.Backup.Schedules)
        {
            if (!targetIds.Contains(schedule.TargetId))
                errors.Add(Err("Backup", $"Schedules[{schedule.ScheduleId}].TargetId",
                    "TargetId must reference a registered backup target."));
        }
        if (options.Backup.DefaultTargetId is not null && !targetIds.Contains(options.Backup.DefaultTargetId))
            errors.Add(Err("Backup", "DefaultTargetId", "Must reference a registered backup target."));

        // Cluster
        var replication = options.Cluster.Replication;
        if (replication.WriteQuorum + replication.ReadQuorum <= replication.Factor)
            errors.Add(Err("Cluster", "Replication", "WriteQuorum + ReadQuorum must be > Factor."));
        if (string.IsNullOrWhiteSpace(options.Cluster.NodeId))
            errors.Add(Err("Cluster", "NodeId", "Must not be empty."));

        // Lifecycle
        if (options.Lifecycle.ArchiveAfter.HasValue
            && options.Lifecycle.DeleteAfter.HasValue
            && options.Lifecycle.ArchiveAfter.Value >= options.Lifecycle.DeleteAfter.Value)
            errors.Add(Err("Lifecycle", "ArchiveAfter", "Must be < DeleteAfter when both are set."));

        return new OptionsValidationResult { Errors = errors };
    }

    private static ValidationError Err(string domain, string field, string message)
        => new() { Domain = domain, Field = field, Message = message };
}