// 本文件包含类型 IndexFeatureOptions：索引功能选项。
namespace Mono.FileBox.Lite.Abstractions.Configuration;

public sealed class IndexFeatureOptions
{
    public bool EnablePrefix { get; set; } = true;
    public bool EnableTagInverted { get; set; } = true;
    public bool EnableAttribute { get; set; } = true;
    public bool EnableTierBitmap { get; set; } = true;
    public bool EnableStateBitmap { get; set; } = true;
    public bool EnableTimeIndex { get; set; } = true;
    public bool EnableSizeIndex { get; set; } = true;
}