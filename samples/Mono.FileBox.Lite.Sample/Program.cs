using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Configuration;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.UseCases;
using Mono.FileBox.Lite.DependencyInjection;

namespace Mono.FileBox.Lite.Sample;

/// <summary>
/// Stress verification: uploads a large object and samples the process's memory
/// (working set + managed heap), CPU usage and the on-disk footprint written by the
/// storage engine.
/// </summary>
public static class Program
{
    public static async Task<int> Main()
    {
        try
        {
            // 1 GiB default; override with MONOFILEBOX_SAMPLE_SIZE_MB.
            var sizeMb = long.TryParse(
                Environment.GetEnvironmentVariable("MONOFILEBOX_SAMPLE_SIZE_MB"), out var mb)
                ? mb : 1024L;
            var sizeBytes = sizeMb * 1024L * 1024L;

            await using var provider = BuildServices();
            var poolRoot = ResolvePoolRoot(provider);

            var process = Process.GetCurrentProcess();
            process.Refresh();

            var baselineWorkingSet = process.WorkingSet64;
            var baselineManaged = GC.GetTotalMemory(false);
            var baselineDisk = DirectorySize(poolRoot);

            Console.WriteLine("== Mono.FileBox.Lite Large-Upload Stress Test ==");
            Console.WriteLine($"  target size    : {FormatBytes(sizeBytes)}");
            Console.WriteLine($"  baseline       : workingSet={FormatBytes(baselineWorkingSet)} " +
                              $"managed={FormatBytes(baselineManaged)} disk(used)={FormatBytes(baselineDisk)}");
            Console.WriteLine($"  logical CPUs   : {Environment.ProcessorCount}");

            // Deterministic, replayable source stream: no full payload buffer in memory.
            Console.WriteLine("  using replayable generating stream (O(1) payload memory) ...");
            var contentSeed = DateTime.UtcNow.Ticks;
            using var content = new PatternStream(sizeBytes, seed: contentSeed);

            var blocksBefore = FileCount(poolRoot, "blocks");
            var manifestsBefore = FileCount(poolRoot, "manifests");

            // Start the resource sampler.
            var cts = new CancellationTokenSource();
            var metric = new Metric();
            var sampler = Task.Run(() => SampleLoop(process, metric, cts.Token));

            var stopwatch = Stopwatch.StartNew();
            var put = provider.GetRequiredService<IPutObjectUseCase>();
            var putResult = await put.ExecuteAsync(new PutObjectCommand
            {
                NamespaceId = "ns1",
                ObjectKey = "/big/blob.bin",
                Content = content,
                ContentType = "application/octet-stream",
                Tags = new Dictionary<string, string> { ["size"] = sizeMb.ToString() }
            }, CancellationToken.None);
            stopwatch.Stop();

            cts.Cancel();
            await sampler;

            if (!putResult.Succeeded)
            {
                Console.Error.WriteLine($"Put failed: {putResult.Error}");
                return 1;
            }

            process.Refresh();
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            var peakManaged = GC.GetTotalMemory(false);

            var diskAfter = DirectorySize(poolRoot);
            var written = diskAfter - baselineDisk;
            var uploadSecs = stopwatch.Elapsed.TotalSeconds;

            Console.WriteLine();
            Console.WriteLine("-- Results --");
            Console.WriteLine($"  Put state      : {putResult.State}");
            Console.WriteLine($"  Content hash   : {putResult.ContentHash}");
            Console.WriteLine($"  upload time    : {stopwatch.Elapsed.TotalSeconds:F2}s " +
                              $"({FormatBytes(written)}) -> {FormatBytes(written / Math.Max(uploadSecs, 1e-9))}/s");
            Console.WriteLine($"  memory peak    : workingSet={FormatBytes(metric.PeakWorkingSet)} " +
                              $"(managed sampled peak={FormatBytes(metric.PeakManaged)})");
            Console.WriteLine($"  memory now     : workingSet={FormatBytes(process.WorkingSet64)} " +
                              $"managed(GC)={FormatBytes(peakManaged)}");
            Console.WriteLine($"  CPU            : peak={metric.PeakCpuPct:F1}% avg={metric.AvgCpuPct:F1}%");
            Console.WriteLine($"  disk           : used-before={FormatBytes(baselineDisk)} " +
                              $"used-after={FormatBytes(diskAfter)} delta={FormatBytes(written)}");
            Console.WriteLine($"  overshoot vs payload : {(metric.PeakWorkingSet - baselineWorkingSet):N0} B");

            // Confirm the object is queryable in the index and servable.
            var index = provider.GetRequiredService<IIndexReader>();
            var page = await index.QueryAsync(new IndexQuery
            {
                NamespaceId = "ns1",
                Tags = new Dictionary<string, string> { ["size"] = sizeMb.ToString() }
            }, CancellationToken.None);
            Console.WriteLine($"  index verify   : matched={page.Items.Count} (content-addressed)");

            var blocksAfter = FileCount(poolRoot, "blocks");
            var manifestsAfter = FileCount(poolRoot, "manifests");
            Console.WriteLine($"  chunk layout   : +{blocksAfter - blocksBefore} block files " +
                              $"+{manifestsAfter - manifestsBefore} manifest file(s) " +
                              $"(≈{Math.Max(1, (long)Math.Ceiling((double)sizeBytes / (8L * 1024 * 1024)))} chunks @8 MiB)");

            // Read a window that spans two chunks and verify every returned byte matches the source pattern.
            const long chunkSize = 8L * 1024 * 1024;
            var spanStart = chunkSize - 3;
            var get = provider.GetRequiredService<IGetObjectUseCase>();
            var back = await get.ExecuteAsync(new GetObjectCommand
            {
                ContentHash = putResult.ContentHash,
                NamespaceId = "ns1",
                Offset = spanStart,
                Length = 6
            }, CancellationToken.None);
            if (back.Content is null)
            {
                Console.Error.WriteLine("Range read returned no content.");
                return 1;
            }
            var gotBytes = new byte[6];
            var gotRead = await back.Content.ReadAsync(gotBytes, CancellationToken.None);
            if (gotRead != 6)
            {
                Console.Error.WriteLine($"Range read short: expected 6 got {gotRead}.");
                return 1;
            }
            for (var i = 0; i < 6; i++)
            {
                var n = unchecked((uint)(spanStart + i));
                var expected = (byte)((n * 2654435761U) >> 24 ^ (byte)(contentSeed >> 8));
                if (gotBytes[i] != expected)
                {
                    Console.Error.WriteLine($"Range read mismatch at +{i}: got {gotBytes[i]:X2} expected {expected:X2}.");
                    return 1;
                }
            }
            Console.WriteLine("  range read across chunks: OK (6 bytes spanning a chunk boundary verified)");

            Console.WriteLine();
            Console.WriteLine("Sample completed successfully.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Sample failed: {ex}");
            return 1;
        }
    }

    private static async Task SampleLoop(Process process, Metric metric, CancellationToken ct)
    {
        var watch = Stopwatch.StartNew();
        var lastCpu = process.TotalProcessorTime;
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(100, CancellationToken.None).ConfigureAwait(false);
            if (ct.IsCancellationRequested) break;

            process.Refresh();
            var cpu = process.TotalProcessorTime;
            var elapsed = watch.Elapsed.TotalMilliseconds;
            var pct = elapsed > 0
                ? (cpu - lastCpu).TotalMilliseconds / (Environment.ProcessorCount * elapsed) * 100
                : 0;
            lastCpu = cpu;
            watch.Restart();

            metric.Observe(process.WorkingSet64, GC.GetTotalMemory(false), pct);
        }
    }

    private sealed class Metric
    {
        private readonly object _lock = new();
        private long _peakWs;
        private long _peakManaged;
        private double _peakCpu;
        private double _cpuSum;
        private long _cpuSamples;

        public void Observe(long workingSet, long managed, double cpuPct)
        {
            lock (_lock)
            {
                _peakWs = Math.Max(_peakWs, workingSet);
                _peakManaged = Math.Max(_peakManaged, managed);
                _peakCpu = Math.Max(_peakCpu, cpuPct);
                _cpuSum += cpuPct;
                _cpuSamples++;
            }
        }

        public long PeakWorkingSet { get { lock (_lock) return _peakWs; } }
        public long PeakManaged { get { lock (_lock) return _peakManaged; } }
        public double PeakCpuPct { get { lock (_lock) return _peakCpu; } }
        public double AvgCpuPct => _cpuSamples == 0 ? 0 : _cpuSum / _cpuSamples;
    }

    private static string ResolvePoolRoot(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<FileBoxOptions>();
        return options.Storage.Pools.First(p => p.Enabled).RootPath;
    }

    private static long DirectorySize(string path)
    {
        if (!Directory.Exists(path)) return 0;
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

    private static long FileCount(string poolRoot, string subPath)
    {
        var path = Path.Combine(poolRoot, subPath);
        if (!Directory.Exists(path)) return 0;
        try
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Count();
        }
        catch
        {
            return 0;
        }
    }

    private static string FormatBytes(double bytes)
    {
        if (bytes < 1024) return $"{bytes:N0} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024:N1} KiB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024 * 1024):N1} MiB";
        return $"{bytes / (1024.0 * 1024 * 1024):N2} GiB";
    }

    private static ServiceProvider BuildServices()
    {
        var root = Path.Combine(Path.GetTempPath(), "mono-filebox-sample");
        Directory.CreateDirectory(root);

        var options = new FileBoxOptions
        {
            Cluster = { NodeId = "node-a" },
            Storage =
            {
                Pools =
                {
                    new PoolOptions
                    {
                        PoolId = "hot-1",
                        RootPath = Path.Combine(root, "hot"),
                        Tier = StorageTier.Hot,
                        Enabled = true
                    }
                },
                Chunking = { Enabled = true }
            }
        };

        var services = new ServiceCollection();
        services.AddMonoFileBoxLite(options);
        services.AddMonoFileBoxLiteStorage();
        services.AddMonoFileBoxLiteIndex();
        services.AddMonoFileBoxLiteUseCases();
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// A seekable, deterministic stream that generates bytes from the read position on
    /// demand (O(1) memory). Because it is replayable, the engine hashes a pass and then
    /// stream-writes a rewind, both reading identical bytes — so even a multi-gigabyte
    /// upload never materializes the payload in memory.
    /// </summary>
    private sealed class PatternStream : Stream
    {
        private readonly long _length;
        private readonly long _seed;
        private long _pos;

        public PatternStream(long length, long seed)
        {
            _length = length;
            _seed = seed;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _length;
        public override long Position { get => _pos; set => _pos = value; }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_pos >= _length) return 0;
            count = (int)Math.Min(count, _length - _pos);
            for (var i = 0; i < count; i++)
            {
                // Multiplicative (Knuth) mixing so consecutive chunks carry distinct bytes
                // even when chunk boundaries align to periods of simple arithmetic.
                var n = unchecked((uint)(_pos + i));
                buffer[offset + i] = (byte)((n * 2654435761U) >> 24 ^ (byte)(_seed >> 8));
            }
            _pos += count;
            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            _pos = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => checked(_pos + offset),
                _ => _length + offset
            };
            return _pos;
        }

        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}