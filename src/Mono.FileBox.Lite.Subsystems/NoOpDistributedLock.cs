// File-level documentation: No-op distributed lock that always grants.
// Extracted from the original multi-type Defaults.cs and kept, by convention,
// as a standalone top-level type so each file contains exactly one type.
using System.Collections.Concurrent;
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Cluster;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems;

/// <summary>Distributed lock that always grants. Single-node default implementation.
    /// 中文翻译：总是授予的分布式锁，单节点默认实现。</summary>
    /// <remarks>
    /// <b>并发语义</b>：默认为空实现（恒返回 <c>true</c>，无真实锁）。因此引擎默认
    /// <list type="bullet">
    /// <item>不同内容对象的转移可被调用方并发并行执行；</item>
    /// <item>同一内容对象的多线程并发操作 <b>不会</b>被强制串行，依赖「每对象独立
    /// context + 内容寻址幂等」兜底。</item>
    /// </list>
    /// 若需对同一 hash 强制串行，应替换为带真实 per-hash 锁的实现。
    /// </remarks>
public sealed class NoOpDistributedLock : IDistributedLock, IGuard
{
    public Task<bool> CanEnterAsync(IObjectContext ctx, CancellationToken ct)
        => Task.FromResult(true);
}