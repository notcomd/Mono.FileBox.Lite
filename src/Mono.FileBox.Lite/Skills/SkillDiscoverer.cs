using System.Text.Json;
using Mono.FileBox.Lite.Abstractions;

namespace Mono.FileBox.Lite.Skills;

/// <summary>
/// 负责在本地目录中发现技能及其元数据（skill.json）。
/// </summary>
public static class SkillDiscoverer
{
    private const string ManifestFileName = "skill.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// 在指定根目录中发现技能描述符。
    /// </summary>
    /// <param name="rootDirectory">技能根目录。</param>
    /// <param name="searchSubdirectories">是否递归搜索子目录。</param>
    /// <returns>发现的技能描述符列表（无效条目会被跳过/记录）。</returns>
    public static IReadOnlyList<SkillDescriptor> Discover(string rootDirectory, bool searchSubdirectories = true)
    {
        if (!Directory.Exists(rootDirectory))
        {
            return Array.Empty<SkillDescriptor>();
        }

        var manifests = searchSubdirectories
            ? Directory.EnumerateFiles(rootDirectory, ManifestFileName, SearchOption.AllDirectories)
            : Directory.EnumerateFiles(rootDirectory, ManifestFileName, SearchOption.TopDirectoryOnly);

        var descriptors = new List<SkillDescriptor>();
        foreach (var manifestPath in manifests)
        {
            var descriptor = TryLoad(manifestPath);
            if (descriptor != null)
            {
                descriptors.Add(descriptor);
            }
        }

        return descriptors;
    }

    private static SkillDescriptor? TryLoad(string manifestPath)
    {
        try
        {
            var model = JsonSerializer.Deserialize<SkillJsonModel>(
                File.ReadAllText(manifestPath),
                JsonOptions);

            if (model is null || string.IsNullOrWhiteSpace(model.Command))
            {
                return default;
            }

            var folder = Path.GetDirectoryName(manifestPath) ?? Path.GetDirectoryName(Path.GetFullPath(manifestPath))!;
            var workingDir = !string.IsNullOrWhiteSpace(model.WorkingDirectory)
                ? Path.GetFullPath(Path.Combine(folder, model.WorkingDirectory))
                : folder;

            return new SkillDescriptor
            {
                Id = string.IsNullOrWhiteSpace(model.Id) ? Path.GetFileName(folder) : model.Id,
                Name = string.IsNullOrWhiteSpace(model.Name) ? Path.GetFileName(folder) : model.Name,
                Description = model.Description ?? string.Empty,
                Version = string.IsNullOrWhiteSpace(model.Version) ? "1.0.0" : model.Version,
                Command = model.Command,
                WorkingDirectory = workingDir,
                Arguments = model.Arguments ?? new List<string>(),
                TimeoutSeconds = model.TimeoutSeconds,
                Source = manifestPath,
            };
        }
        catch (Exception)
        {
            // 单个技能解析失败不应阻断其它技能发现。
            return default;
        }
    }
}