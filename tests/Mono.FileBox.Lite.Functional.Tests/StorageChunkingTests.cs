using Mono.FileBox.Lite.Abstractions.Configuration;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Storage;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Storage;
using Mono.FileBox.Lite.Storage.DiskSelector;
using Mono.FileBox.Lite.Storage.IOPipeline;
using Mono.FileBox.Lite.Storage.ObjectWriter;
using Mono.FileBox.Lite.Storage.PhysicalDevice;

namespace Mono.FileBox.Lite.Functional.Tests;

/// <summary>Content-addressed storage: write/read round-trip, dedup, range reads, chunking.
/// 内容寻址存储：写读往返、去重、范围读取、分块。</summary>
public class StorageChunkingTests
{
    private static readonly CancellationToken None = CancellationToken.None;

    [Fact]
    public async Task WriteThenRead_RoundTrip_PreservesContent()
    {
        var harness = new StorageFixture(chunking: false);
        var payload = Content(3000);

        var hash = await harness.Writer.WriteAsync(new MemoryStream(payload), new WriteOptions(), None);
        using var read = await harness.Reader.ReadAsync(hash, 0, -1, None);
        Assert.Equal(ToBytes(read), payload);
    }

    [Fact]
    public async Task Dedup_SameContent_SameHash_NoExtraBlock()
    {
        var harness = new StorageFixture(chunking: false);
        var payload = Content(4096);

        var first = await harness.Writer.WriteAsync(new MemoryStream(payload), new WriteOptions(), None);
        var before = harness.BlockCount();
        var second = await harness.Writer.WriteAsync(new MemoryStream(payload), new WriteOptions(), None);

        Assert.Equal(first, second);
        Assert.Equal(before, harness.BlockCount()); // no new block written
        Assert.True(await harness.Writer.ExistsAsync(first, None));
    }

    [Fact]
    public async Task RangeRead_Boundaries_AreCorrect()
    {
        var harness = new StorageFixture(chunking: false);
        var payload = Content(1000);
        var hash = await harness.Writer.WriteAsync(new MemoryStream(payload), new WriteOptions(), None);

        // head
        Assert.Equal(payload[..3], await harness.ReadRangeAsync(hash, 0, 3));
        // middle
        Assert.Equal(payload[100..110], await harness.ReadRangeAsync(hash, 100, 10));
        // tail
        Assert.Equal(payload[995..], await harness.ReadRangeAsync(hash, 995, -1));
    }

    // -------- chunking --------

    [Fact]
    public async Task Chunking_Enabled_WritesChunks_AndManifest()
    {
        var harness = new StorageFixture(chunking: true, chunkSize: 1024);
        var payload = Content(3000); // 3 chunks of 1024

        var manifestBefore = harness.ManifestCount();
        var blocksBefore = harness.BlockCount();
        var hash = await harness.Writer.WriteAsync(new MemoryStream(payload), new WriteOptions(), None);

        Assert.Equal(3, harness.BlockCount() - blocksBefore); // 3 chunk blocks
        Assert.Equal(1, harness.ManifestCount() - manifestBefore); // 1 manifest
        Assert.Equal(64, hash.Length); // SHA-256 hex
    }

    [Fact]
    public async Task Chunked_Read_AcrossChunkBoundary_IsCorrect()
    {
        var harness = new StorageFixture(chunking: true, chunkSize: 1024);
        var payload = Content(3000);
        var hash = await harness.Writer.WriteAsync(new MemoryStream(payload), new WriteOptions(), None);

        // Span from 1021 → 1030 crosses chunks 0/1.
        Assert.Equal(payload[1021..1030], await harness.ReadRangeAsync(hash, 1021, 9));
        // Whole read.
        Assert.Equal(payload, await harness.ReadRangeAsync(hash, 0, -1));
    }

    [Fact]
    public async Task Chunking_Enabled_SmallObject_StaysSingleBlock()
    {
        var harness = new StorageFixture(chunking: true, chunkSize: 1024);
        var payload = Content(100); // ≤ one chunk

        var manifestBefore = harness.ManifestCount();
        var hash = await harness.Writer.WriteAsync(new MemoryStream(payload), new WriteOptions(), None);

        Assert.Equal(manifestBefore, harness.ManifestCount()); // no manifest
        Assert.True(await harness.Writer.ExistsAsync(hash, None));
    }

    [Fact]
    public async Task Chunked_Exists_Succeeds()
    {
        var harness = new StorageFixture(chunking: true, chunkSize: 1024);
        var payload = Content(3000);
        var hash = await harness.Writer.WriteAsync(new MemoryStream(payload), new WriteOptions(), None);
        Assert.True(await harness.Writer.ExistsAsync(hash, None));
    }

    // -------- helpers --------

    private static byte[] Content(int n)
    {
        var bytes = new byte[n];
        for (var i = 0; i < n; i++)
            bytes[i] = (byte)((uint)(i * 2654435761U) >> 24); // non-periodic, distinct chunk content
        return bytes;
    }

    private static byte[] ToBytes(Stream s)
    {
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }

    private sealed class StorageFixture
    {
        public StorageFixture(bool chunking, int chunkSize = 8 * 1024 * 1024)
        {
            Root = Path.Combine(Path.GetTempPath(), "filebox-func-tests", Guid.NewGuid().ToString("N"));
            Device = new LocalFileSystemDevice();
            Pipeline = new BufferedIOPipeline(Device);
            Selector = new ConsistentHashDiskSelector(new[]
            {
                new PoolOptions { PoolId = "p", RootPath = Path.Combine(Root, "pool"), Enabled = true, Tier = StorageTier.Hot }
            });
            Writer = new Sha256ObjectWriter(Selector, Pipeline, Device,
                chunking ? new ChunkingOptions { Enabled = true, ChunkSizeBytes = chunkSize } : null);
            Reader = new StorageObjectReader(Selector, Pipeline, Device);
        }

        public string Root { get; }
        public IPhysicalDevice Device { get; }
        public IIOPipeline Pipeline { get; }
        public IDiskSelector Selector { get; }
        public IObjectWriter Writer { get; }
        public IObjectReader Reader { get; }

        public long BlockCount() => Count(Path.Combine(Root, "pool", "blocks"));
        public long ManifestCount() => Count(Path.Combine(Root, "pool", "manifests"));

        private static long Count(string path)
            => Directory.Exists(path) ? Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).LongCount() : 0;

        public async Task<byte[]> ReadRangeAsync(string hash, long offset, long length)
        {
            using var s = await Reader.ReadAsync(hash, offset, length, None);
            return ToBytes(s);
        }
    }
}