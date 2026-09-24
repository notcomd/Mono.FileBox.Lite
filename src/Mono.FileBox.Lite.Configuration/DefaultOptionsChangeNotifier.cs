using Mono.FileBox.Lite.Abstractions.Configuration;

namespace Mono.FileBox.Lite.Configuration;

/// <summary>
/// Simple options change notifier. Subscribers receive callbacks on reload; reloading
/// a fresh snapshot is delegated to the caller via an injected refresh function.
/// 简单的选项变更通知器。订阅者在重新加载时收到回调；刷新最新快照的逻辑通过注入的刷新函数委派给调用方。
/// </summary>
public sealed class DefaultOptionsChangeNotifier : IOptionsChangeNotifier
{
    private readonly Func<CancellationToken, Task> _reload;
    private readonly object _lock = new();
    private readonly Dictionary<Type, List<Delegate>> _subscriptions = new();

    public DefaultOptionsChangeNotifier(Func<CancellationToken, Task>? reload = null)
        => _reload = reload ?? (_ => Task.CompletedTask);

    public IDisposable OnChange<T>(Action<T> callback)
    {
        Type key = typeof(T);
        var reference = callback;
        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(key, out var list))
                _subscriptions[key] = list = new List<Delegate>();
            list.Add(reference);
        }
        return new Subscription(this, key, reference);
    }

    public Task ReloadAsync(CancellationToken ct) => _reload(ct);

    private void Notify(object? value)
    {
        if (value is null) return;
        List<Delegate>? list;
        lock (_lock) _subscriptions.TryGetValue(value.GetType(), out list);
        if (list is null) return;
        foreach (var d in list)
        {
            try { d.DynamicInvoke(value); }
            catch { /* subscriber errors are isolated */ }
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly DefaultOptionsChangeNotifier _owner;
        private readonly Type _type;
        private readonly Delegate _callback;
        public Subscription(DefaultOptionsChangeNotifier owner, Type type, Delegate callback)
        { _owner = owner; _type = type; _callback = callback; }

        public void Dispose()
        {
            lock (_owner._lock)
            {
                if (_owner._subscriptions.TryGetValue(_type, out var list))
                    list.Remove(_callback);
            }
        }
    }
}