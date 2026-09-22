using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Subsystems;

namespace Mono.FileBox.Lite.Subsystems.Lifecycle;

/// <summary>
/// Periodically scans objects in the <see cref="ObjectState.Available"/> state and, when
/// the configured <see cref="ILifecyclePolicy"/> permits, attempts an <c>Archive</c> or
/// <c>Expire</c> transition. Runs as part of the host's scheduled scan loop.
/// </summary>
/// <remarks>
/// <b>并发语义</b>：调度器自身不启动任何线程；它是宿主（托管进程）在某定时循环里调用的
/// <see cref="ScanAsync"/> 一次性扫描。因此并发度由宿主决定；引擎是嵌入宿主进程的库，无常驻后台线程。
/// </remarks>
public sealed class DefaultLifecycleScheduler : ILifecycleScheduler
{
    private readonly IIndexReader _reader;
    private readonly IObjectStateMachine _machine;
    private readonly ILifecyclePolicy _policy;
    private readonly IObjectContextFactory _ctxFactory;

    public DefaultLifecycleScheduler(
        IIndexReader reader,
        IObjectStateMachine machine,
        ILifecyclePolicy policy,
        IObjectContextFactory ctxFactory)
    {
        _reader = reader;
        _machine = machine;
        _policy = policy;
        _ctxFactory = ctxFactory;
    }

    public async Task ScanAsync(CancellationToken ct)
    {
        var available = new[] { ObjectState.Available };
        string? cursor = null;
        do
        {
            var page = await _reader.QueryAsync(new IndexQuery
            {
                States = available,
                Page = new PageRequest { Size = 500, Cursor = cursor }
            }, ct).ConfigureAwait(false);

            foreach (var entry in page.Items)
            {
                if (ct.IsCancellationRequested) return;
                var ctx = _ctxFactory.CreateFromExisting(entry.ContentHash, entry.NamespaceId, ObjectState.Available);

                if (await _policy.CanEnterAsync(ctx, ct).ConfigureAwait(false))
                    await _machine.FireAsync(ObjectTrigger.Archive, ctx, ct).ConfigureAwait(false);
                else
                    await _machine.FireAsync(ObjectTrigger.Expire, ctx, ct).ConfigureAwait(false);
            }

            cursor = page.NextCursor;
        }
        while (cursor is not null);
    }
}