using System.Reflection;

namespace Mono.FileBox.Lite.Abstractions;

/// <summary>
/// 描述一个本地技能的元数据（来源通常是 skill.json 配置或动态构建）。
/// </summary>
public sealed class SkillDescriptor
{
    /// <summary>唯一标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>显示名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>描述信息。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>版本号，可选。</summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>可执行文件路径或可在 PATH 中解析的命令名。</summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>技能运行所在的工作目录；为空时继承调用方当前目录。</summary>
    public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>默认参数，调用时会与请求参数合并。</summary>
    public IReadOnlyList<string> Arguments { get; set; } = Array.Empty<string>();

    /// <summary>秒为单位的默认超时，小于等于 0 表示使用管理器默认值。</summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>命令来源路径，例如 skill.json 的完整路径。</summary>
    public string Source { get; set; } = string.Empty;
}