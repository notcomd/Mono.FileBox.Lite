// 本文件包含类型 ICursorCodec：编码/解码不透明分页游标。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>Encodes/decodes opaque pagination cursors.</summary>
public interface ICursorCodec
{
    string Encode(object cursor);
    object Decode(string cursor);
}