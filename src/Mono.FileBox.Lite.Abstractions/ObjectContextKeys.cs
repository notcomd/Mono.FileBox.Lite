namespace Mono.FileBox.Lite.Abstractions;

/// <summary>
/// Well-known <see cref="IObjectContext.Items"/> keys shared by the storage actions,
/// the use-case layer and the index writer when passing data through a transition.
/// 存储操作、用例层与索引写入器在通过转换传递数据时共享的 IObjectContext.Items 已知键。
/// </summary>
public static class ObjectContextKeys
{
    /// <summary>Stream content to persist (held during the Put transition).</summary>
    public const string ContentStream = "ObjectContentStream";

    /// <summary>The <c>WriteOptions</c> to apply when persisting content.</summary>
    public const string WriteOptions = "ObjectWriteOptions";

    /// <summary>Optional object (file) key.</summary>
    public const string ObjectKey = "ObjectKey";

    /// <summary>Optional content type.</summary>
    public const string ContentType = "ContentType";

    /// <summary>Tags of the object (IReadOnlyDictionary&lt;string, string&gt;).</summary>
    public const string Tags = "ObjectTags";

    /// <summary>Attributes of the object (IReadOnlyDictionary&lt;string, object&gt;).</summary>
    public const string Attributes = "ObjectAttributes";

    /// <summary>Size in bytes, recorded by the content writer.</summary>
    public const string SizeBytes = "SizeBytes";
}