// 本文件包含类型 PredicateKind：提供方可声称支持的索引谓词种类。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>Kinds of index predicates that a provider may claim to serve. 中文翻译：索引提供方可声称支持的谓词种类（键前缀/标签/属性/层级/状态/时间/大小等）。</summary>
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