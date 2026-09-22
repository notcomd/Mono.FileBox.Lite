// SingleKindProvider.cs — base helper for single-kind index providers.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Index.Providers;

/// <summary>Base helper for single-kind providers.
/// 中文翻译：单一谓词种类索引提供方的基类辅助。
/// </summary>
public abstract class SingleKindProvider : IIndexProvider
{
    private readonly PredicateKind _kind;
    private readonly int _priority;
    protected SingleKindProvider(PredicateKind kind, int priority)
    {
        _kind = kind;
        _priority = priority;
    }

    public string Name => $"idx:{_kind.ToString().ToLowerInvariant()}";
    public int Priority => _priority;

    public virtual bool CanHandle(IndexPredicate predicate) => predicate.Kind == _kind;

    public virtual double EstimateSelectivity(IndexPredicate predicate) => 1.0 / 32.0;

    public IIndexScan CreateScan(IndexQuery query, IndexPredicate predicate)
        => new PredicateScan(Store, query.NamespaceId, predicate);

    protected abstract IEntryStore Store { get; }
}