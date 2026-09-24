// TimeIndexProvider.cs — time index provider (handles CreatedAt and ModifiedAt).
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Index.Providers;

/// <summary>Time index provider (handles CreatedAt and ModifiedAt).
/// 时间索引提供方（处理 CreatedAt 与 ModifiedAt）。
/// </summary>
public sealed class TimeIndexProvider : SingleKindProvider
{
    private readonly IEntryStore _store;
    public TimeIndexProvider(IEntryStore store) : base(PredicateKind.CreatedAt, 60) => _store = store;
    protected override IEntryStore Store => _store;
    public override bool CanHandle(IndexPredicate p)
        => p.Kind == PredicateKind.CreatedAt || p.Kind == PredicateKind.ModifiedAt;
}