// StateBitmapProvider.cs — state bitmap index provider.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Index.Providers;

/// <summary>State bitmap provider.
/// 状态(State)位图索引提供方。
/// </summary>
public sealed class StateBitmapProvider : SingleKindProvider
{
    private readonly IEntryStore _store;
    public StateBitmapProvider(IEntryStore store) : base(PredicateKind.State, 50) => _store = store;
    protected override IEntryStore Store => _store;
}