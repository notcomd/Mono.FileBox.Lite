// TierBitmapProvider.cs — tier bitmap index provider.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Index.Providers;

/// <summary>Tier bitmap provider.
/// 中文翻译：层级(Tier)位图索引提供方。
/// </summary>
public sealed class TierBitmapProvider : SingleKindProvider
{
    private readonly IEntryStore _store;
    public TierBitmapProvider(IEntryStore store) : base(PredicateKind.Tier, 40) => _store = store;
    protected override IEntryStore Store => _store;
}