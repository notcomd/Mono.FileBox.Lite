// 本文件包含类型 NamespaceIdValue：namespace id 的轻量值包装。
namespace Mono.FileBox.Lite.Abstractions;

/// <summary>Lightweight value wrapper for a namespace id. namespace id 的轻量值包装，用于表示对象所属的逻辑分组。</summary>
public sealed class NamespaceIdValue
{
    public NamespaceIdValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("NamespaceId must not be empty.", nameof(value));
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}