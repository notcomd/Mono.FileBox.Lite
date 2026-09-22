// 本文件包含类型 StorageTier：对象物理块所处的存储层。
namespace Mono.FileBox.Lite.Abstractions.Index;

/// <summary>Storage tier an object's physical block resides on.</summary>
public enum StorageTier
{
    Hot,
    Warm,
    Cold,
    Archive
}