// 本文件包含类型 IOPipelineOptions：I/O 管道选项。
namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class IOPipelineOptions
{
    public int BufferSize { get; set; } = 64 * 1024;
    public int MaxBatchSize { get; set; } = 64;
    public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan WriteTimeout { get; set; } = TimeSpan.FromSeconds(60);
    public bool EnableZeroCopy { get; set; } = true;
    public int MaxConcurrency { get; set; } = 64;
}