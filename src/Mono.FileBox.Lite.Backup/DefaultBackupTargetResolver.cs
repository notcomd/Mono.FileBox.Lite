// DefaultBackupTargetResolver.cs - Default registry-based target resolver.

using Mono.FileBox.Lite.Abstractions.Backup;

namespace Mono.FileBox.Lite.Backup.Targets;

/// <summary>Default registry-based target resolver.</summary>
public sealed class DefaultBackupTargetResolver : IBackupTargetResolver
{
    private readonly Dictionary<string, IBackupTarget> _targets = new(StringComparer.Ordinal);

    public IBackupTarget Resolve(string targetId)
    {
        if (_targets.TryGetValue(targetId, out var t)) return t;
        throw new KeyNotFoundException($"No backup target registered as '{targetId}'.");
    }

    public void Register(string targetId, IBackupTarget target)
        => _targets[targetId] = target;

    public IReadOnlyList<string> TargetIds => _targets.Keys.ToArray();
}