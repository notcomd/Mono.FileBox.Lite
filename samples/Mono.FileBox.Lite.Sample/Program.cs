using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
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
/// 压测验证：上传一个超大对象并采样进程内存（工作集 + 托管堆）、CPU 使用率以及存储引擎写入的磁盘占用。
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
            using var content = new PatternStream(sizeBytes, seed: 42);

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

            // 8. Concurrent small-file upload (fixed concurrency).
            var concurrentFiles = long.TryParse(
                Environment.GetEnvironmentVariable("MONOFILEBOX_CONCURRENT_FILES"), out var cf)
                ? (int)cf : 500;
            var concurrency = long.TryParse(
                Environment.GetEnvironmentVariable("MONOFILEBOX_CONCURRENCY"), out var cc)
                ? (int)cc : 500;
            var concurrentOk = await ConcurrentUploadAsync(provider, concurrentFiles, concurrency);

            Console.WriteLine();
            if (concurrentOk != 0)
            {
                Console.WriteLine("Sample failed during concurrent upload.");
                return 1;
            }
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

    /// <summary>
    /// 并发压测：以固定 <paramref name="concurrency"/>（示例为 500）用
    /// <c>Parallel.ForEachAsync</c> 并发上传 <paramref name="files"/> 个随机大小对象
    /// （1–20 MB）。用于验证引擎在单进程多线程（线程池）并发调用下：不同对象并行、无失败/
    /// 冲突、去重哈希唯一、全部入索引。
    /// </summary>
    private static async Task<int> ConcurrentUploadAsync(
        IServiceProvider provider, int files, int concurrency)
    {
        var put = provider.GetRequiredService<IPutObjectUseCase>();
        var index = provider.GetRequiredService<IIndexReader>();
        var process = Process.GetCurrentProcess();

        var cts = new CancellationTokenSource();
        var metric = new Metric();
        var sampler = Task.Run(() => SampleLoop(process, metric, cts.Token));

        const int minSize = 1024 * 1024;          // 1 MB
        const int maxSize = 20 * 1024 * 1024;     // 20 MB
        var hashes = new ConcurrentQueue<string>();
        var errors = new ConcurrentQueue<string>();
        var totalBytes = 0L;
        var stopwatch = Stopwatch.StartNew();

        await Parallel.ForEachAsync(Enumerable.Range(0, files),
            new ParallelOptions { MaxDegreeOfParallelism = concurrency, CancellationToken = cts.Token },
            async (i, ct) =>
            {
                var size = Random.Shared.Next(minSize, maxSize + 1); // random in [1..20] MB
                Interlocked.Add(ref totalBytes, size);
                // 可回放生成流：按位置现场算字节，O(1) 内存，不物化整份文件；
                // 以 i 作种子保证不同文件内容不同、哈希唯一。
                using var content = new PatternStream(size, seed: i);
                var res = await put.ExecuteAsync(new PutObjectCommand
                {
                    NamespaceId = "ns1",
                    ObjectKey = $"/small/{i}.bin",
                    Content = content,
                    ContentType = "application/octet-stream"
                }, ct);
                if (res.Succeeded) hashes.Enqueue(res.ContentHash);
                else errors.Enqueue(res.Error ?? "unknown");
            }).ConfigureAwait(false);
        stopwatch.Stop();

        cts.Cancel();
        await sampler;

        var secs = Math.Max(stopwatch.Elapsed.TotalSeconds, 1e-9);
        var totalMiB = totalBytes / (1024.0 * 1024);
        Console.WriteLine("-- Concurrent random-size upload (1–20 MB) --");
        Console.WriteLine($"  files={files} size=[{minSize / (1024 * 1024)}..{maxSize / (1024 * 1024)}] MB " +
                          $"(total={totalMiB:F0} MiB) concurrency={concurrency} " +
                          $"time={stopwatch.Elapsed.TotalSeconds:F2}s " +
                          $"({files / secs:F0} files/s, {totalMiB / secs:F1} MiB/s)");
        Console.WriteLine($"  succeeded={hashes.Count} failed={errors.Count} " +
                          $"distinctHashes={hashes.Distinct().Count()}");

        var page = await index.QueryAsync(new IndexQuery
        {
            NamespaceId = "ns1",
            KeyPrefix = "/small/",
            Page = new PageRequest { Size = 100000 }
        }, CancellationToken.None);
        Console.WriteLine($"  index matched(/small/)={page.Items.Count}");
        Console.WriteLine($"  memory peak workingSet={FormatBytes(metric.PeakWorkingSet)} " +
                          $"CPU peak={metric.PeakCpuPct:F1}% avg={metric.AvgCpuPct:F1}%");

        // 9. Concurrent downloads (simulate many users reading file content).
        var downloadOk = await ConcurrentDownloadAsync(provider, hashes.ToArray(), concurrency);

        return errors.IsEmpty && downloadOk == 0 ? 0 : 1;
    }

    /// <summary>
    /// 并发下载：模拟多个用户以固定并发读取已上传的对象，并对每个返回流重算 SHA-256 与存储
    /// 的 content hash 比对，校验读取正确性（含分块对象的跨块拼接）。
    /// </summary>
    private static async Task<int> ConcurrentDownloadAsync(
        IServiceProvider provider, string[] hashes, int concurrency)
    {
        var get = provider.GetRequiredService<IGetObjectUseCase>();
        var process = Process.GetCurrentProcess();

        var cts = new CancellationTokenSource();
        var metric = new Metric();
        var sampler = Task.Run(() => SampleLoop(process, metric, cts.Token));

        var ok = 0;
        var failed = 0;
        long totalBytes = 0;
        var stopwatch = Stopwatch.StartNew();

        await Parallel.ForEachAsync(hashes,
            new ParallelOptions { MaxDegreeOfParallelism = concurrency, CancellationToken = cts.Token },
            async (hash, ct) =>
            {
                var res = await get.ExecuteAsync(new GetObjectCommand
                {
                    ContentHash = hash,
                    NamespaceId = "ns1"
                }, ct);
                if (res.Content is null)
                {
                    Interlocked.Increment(ref failed);
                    return;
                }
                using var stream = res.Content;
                var sha = ComputeSha256(stream);
                Interlocked.Add(ref totalBytes, stream.Length);
                if (string.Equals(sha, hash, StringComparison.OrdinalIgnoreCase))
                    Interlocked.Increment(ref ok);
                else
                    Interlocked.Increment(ref failed);
            }).ConfigureAwait(false);
        stopwatch.Stop();

        cts.Cancel();
        await sampler;

        var secs = Math.Max(stopwatch.Elapsed.TotalSeconds, 1e-9);
        var totalMiB = totalBytes / (1024.0 * 1024);
        Console.WriteLine("-- Concurrent downloads (multi-user reads) --");
        Console.WriteLine($"  reads={hashes.Length} concurrency={concurrency} " +
                          $"time={stopwatch.Elapsed.TotalSeconds:F2}s " +
                          $"({totalMiB / secs:F1} MiB/s, {hashes.Length / secs:F0} reads/s)");
        Console.WriteLine($"  sha256-verified={ok} failed={failed} (total {totalMiB:F0} MiB read)");
        Console.WriteLine($"  memory peak workingSet={FormatBytes(metric.PeakWorkingSet)} " +
                          $"CPU peak={metric.PeakCpuPct:F1}% avg={metric.AvgCpuPct:F1}%");

        return failed == 0 ? 0 : 1;
    }

    private static string ComputeSha256(Stream stream)
    {
        using var sha = SHA256.Create();
        var buffer = new byte[81920];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            sha.TransformBlock(buffer, 0, read, buffer, 0);
        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return BitConverter.ToString(sha.Hash!).Replace("-", "").ToLowerInvariant();
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
                }
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
    /// 一个可寻址、确定性的数据流，按读取位置按需生成字节（O(1) 内存）。由于可回放，引擎先跑一遍哈希再回卷流式写入，两次读到完全相同的字节——因此即使几 GB 的上传也无需在内存中实例化整个负载。
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
                unchecked
                {
                    buffer[offset + i] = (byte)((_pos + i) * 31 + _seed);
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