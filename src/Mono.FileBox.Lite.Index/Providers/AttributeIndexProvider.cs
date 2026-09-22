// AttributeIndexProvider.cs — attribute range index provider.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Index.Providers;

/// <summary>Attribute range provider.
/// 中文翻译：属性(Attribute)范围索引提供方。
/// </summary>
public sealed class AttributeIndexProvider : SingleKindProvider
{
    private readonly IEntryStore _store;
    public AttributeIndexProvider(IEntryStore store) : base(PredicateKind.Attribute, 30) => _store = store;
    protected override IEntryStore Store => _store;
}