using Mono.FileBox.Lite.Abstractions;

namespace Mono.FileBox.Lite.StateMachine;

/// <summary>
/// Default mutable <see cref="IObjectContext"/>. Properties are writable so that
/// the state machine can update the current state and actions can record derived
/// values (such as the computed content hash) as the pipeline progresses.
/// 默认的可变 <see cref="IObjectContext"/>。其属性可写，以便状态机更新当前状态，且动作可在管道推进过程中记录派生的值（如计算出的内容哈希）。
/// </summary>
public sealed class DefaultObjectContext : IMutableObjectContext
{
    private readonly Dictionary<string, object> _items = new();

    public DefaultObjectContext(
        string namespaceId,
        string? contentHash = null,
        ObjectState currentState = ObjectState.Pending,
        string? objectKey = null)
    {
        NamespaceId = namespaceId;
        ContentHash = contentHash ?? string.Empty;
        CurrentState = currentState;
        ObjectKey = objectKey;
    }

    public string ContentHash { get; set; }

    public string NamespaceId { get; }

    public string? ObjectKey { get; set; }

    public ObjectState CurrentState { get; set; }

    public IDictionary<string, object> Items => _items;

    public IObjectContext AsReadOnly()
        => new ReadOnlyContext(this);

    private sealed class ReadOnlyContext : IObjectContext
    {
        private readonly IObjectContext _inner;
        public ReadOnlyContext(IObjectContext inner) => _inner = inner;
        public string ContentHash => _inner.ContentHash;
        public string NamespaceId => _inner.NamespaceId;
        public ObjectState CurrentState => _inner.CurrentState;
        public IDictionary<string, object> Items => _inner.Items;
    }
}