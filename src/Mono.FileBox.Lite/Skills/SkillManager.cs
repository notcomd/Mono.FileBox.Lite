using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Configuration;

namespace Mono.FileBox.Lite.Skills;

/// <summary>
/// 本地技能管理器：负责发现、注册并调用本地技能。
/// </summary>
public sealed class SkillManager
{
    private readonly SkillOptions _options;
    private readonly LocalSkillExecutor _executor;
    private readonly List<LocalSkill> _skills = new();

    /// <summary>创建一个技能管理器。</summary>
    public SkillManager(SkillOptions options, LocalSkillExecutor executor)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        Refresh();
    }

    /// <summary>当前已注册的技能列表。</summary>
    public IReadOnlyList<ISkill> Skills => _skills;

    /// <summary>按根目录重新发现并注册技能。</summary>
    /// <returns>新发现的技能数量。</returns>
    public int Refresh()
    {
        _skills.Clear();

        if (string.IsNullOrWhiteSpace(_options.SkillRootDirectory))
        {
            return 0;
        }

        var roots = SplitRoots(_options.SkillRootDirectory);
        var descriptors = new List<SkillDescriptor>();
        foreach (var root in roots)
        {
            descriptors.AddRange(
                SkillDiscoverer.Discover(root.Trim(), _options.SearchSubdirectories));
        }

        foreach (var descriptor in descriptors)
        {
            _skills.Add(new LocalSkill(descriptor, _executor));
        }

        return _skills.Count;
    }

    /// <summary>按 id 获取技能。</summary>
    public ISkill? GetSkill(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return _skills.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 通过 id 调用技能。
    /// </summary>
    /// <param name="id">技能 id。</param>
    /// <param name="request">调用请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>调用结果；未找到技能时返回 Error 非空的结果。</returns>
    public async Task<SkillResult> ExecuteAsync(
        string id,
        SkillInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var skill = GetSkill(id);
        if (skill is null)
        {
            return new SkillResult { Error = $"未找到技能 '{id}'。" };
        }

        return await skill.ExecuteAsync(request, cancellationToken);
    }

    private static IEnumerable<string> SplitRoots(string value)
    {
        var separators = new[] { Path.PathSeparator, ';' };
        return value
            .Split(separators, StringSplitOptions.RemoveEmptyEntries |
                              StringSplitOptions.TrimEntries);
    }
}