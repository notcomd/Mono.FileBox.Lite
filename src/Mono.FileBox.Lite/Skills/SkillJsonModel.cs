using System.Text.Json.Serialization;

namespace Mono.FileBox.Lite.Skills;

/// <summary>
/// skill.json 的反序列化模型。每个本地技能用一个文件夹 + skill.json 描述。
/// </summary>
internal sealed class SkillJsonModel
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>可执行文件路径或命令名。</summary>
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    /// <summary>工作目录，相对该文件夹。</summary>
    [JsonPropertyName("workingDirectory")]
    public string? WorkingDirectory { get; set; }

    [JsonPropertyName("arguments")]
    public List<string> Arguments { get; set; } = new();

    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; }
}