// 本文件包含类型 PredicateKind：提供方可声称支持的索引谓词种类。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>Kinds of index predicates that a provider may claim to serve.</summary>
public enum PredicateKind
{
    KeyPrefix,
    Tag,
    Attribute,
    Tier,
    State,
    CreatedAt,
    ModifiedAt,
    Size
}