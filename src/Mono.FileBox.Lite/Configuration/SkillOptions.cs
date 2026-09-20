namespace Mono.FileBox.Lite.Configuration;

/// <summary>
/// 技能管理器配置项。
/// </summary>
public sealed class SkillOptions
{
    /// <summary>默认技能根目录。可以是多个用系统路径分隔符分隔。</summary>
    public string SkillRootDirectory { get; set; } = string.Empty;

    /// <summary>默认超时（秒）。</summary>
    public int DefaultTimeoutSeconds { get; set; } = 60;

    /// <summary>是否递归搜索子目录中的 skill.json。</summary>
    public bool SearchSubdirectories { get; set; } = true;
}