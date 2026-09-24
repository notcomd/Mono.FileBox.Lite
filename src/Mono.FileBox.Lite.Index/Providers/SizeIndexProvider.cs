// SizeIndexProvider.cs — size B-tree index provider.
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Index;

namespace Mono.FileBox.Lite.Index.Providers;

/// <summary>Size B-tree provider.
/// 大小(Size)的 B 树索引提供方。
/// </summary>
public sealed class SizeIndexProvider : SingleKindProvider
{
    private readonly IEntryStore _store;
    public SizeIndexProvider(IEntryStore store) : base(PredicateKind.Size, 70) => _store = store;
    protected override IEntryStore Store => _store;
}