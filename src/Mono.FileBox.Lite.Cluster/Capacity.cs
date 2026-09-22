using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Storage;

namespace Mono.FileBox.Lite.Cluster.Capacity;

/// <summary>
/// Capacity monitor that aggregates per-pool usage from the disk selector and raises
/// threshold callbacks whenever the cluster crosses a watermark.
/// 中文翻译：容量监视器，从磁盘选择器聚合各存储池（pool）的使用情况，并在集群跨越水位线时触发阈值回调。
/// </summary>
public sealed class DefaultCapacityMonitor : ICapacityMonitor
{
    private readonly IDiskSelector _selector;
    private readonly CapacityThresholds _thresholds;
    private readonly object _lock = new();
    private readonly List<ThresholdCallback> _callbacks = new();
    private CapacityState _lastState = CapacityState.Normal;

    public DefaultCapacityMonitor(IDiskSelector selector, CapacityThresholds? thresholds)
    {
        _selector = selector;
        _thresholds = thresholds ?? new CapacityThresholds();
    }

    public async Task<CapacityStatus> GetStatusAsync(CancellationToken ct)
    {
        var pools = await _selector.ListPoolsAsync(ct).ConfigureAwait(false);
        var poolCaps = new Dictionary<string, PoolCapacity>();
        var totalBytes = 0L;
        var usedBytes = 0L;

        foreach (var pool in pools)
        {
            var total = pool.CapacityBytes ?? 0;
            var used = pool.RootPath is not null && Directory.Exists(pool.RootPath)
                ? DirectorySize(pool.RootPath)
                : 0L;
            var ratio = total == 0 ? 0 : (double)used / total;

            poolCaps[pool.PoolId] = new PoolCapacity
            {
                PoolId = pool.PoolId,
                TotalBytes = total,
                UsedBytes = used,
                State = Evaluate(ratio)
            };
            totalBytes += total;
            usedBytes += used;
        }

        var overallRatio = totalBytes == 0 ? 0 : (double)usedBytes / totalBytes;
        var status = new CapacityStatus
        {
            TotalBytes = totalBytes,
            UsedBytes = usedBytes,
            Pools = poolCaps,
            State = Evaluate(overallRatio)
        };

        NotifyIfChanged(status);
        return status;
    }

    public void OnThreshold(ThresholdCallback callback)
    {
        lock (_lock) _callbacks.Add(callback);
    }

    private void NotifyIfChanged(CapacityStatus status)
    {
        ThresholdCallback[] callbacks;
        lock (_lock)
        {
            if (status.State == _lastState) return;
            _lastState = status.State;
            callbacks = _callbacks.ToArray();
        }
        foreach (var cb in callbacks)
            cb(status);
    }

    private CapacityState Evaluate(double ratio)
    {
        if (ratio >= _thresholds.Full) return CapacityState.Full;
        if (ratio >= _thresholds.Critical) return CapacityState.Critical;
        if (ratio >= _thresholds.Warning) return CapacityState.Warning;
        return CapacityState.Normal;
    }

    private static long DirectorySize(string path)
    {
        try
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                .Sum(f => new FileInfo(f).Length);
        }
        catch
        {
            return 0;
        }
    }
}