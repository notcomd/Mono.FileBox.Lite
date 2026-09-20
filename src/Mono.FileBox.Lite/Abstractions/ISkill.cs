using System.Reflection;

namespace Mono.FileBox.Lite.Abstractions;

/// <summary>
/// 技能的抽象契约。实现类负责把一次调用请求执行并返回结果。
/// </summary>
public interface ISkill
{
    /// <summary>唯一标识。</summary>
    string Id { get; }

    /// <summary>显示名称。</summary>
    string Name { get; }

    /// <summary>描述信息。</summary>
    string Description { get; }

    /// <summary>执行一次技能调用。</summary>
    /// <param name="request">调用请求参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>调用结果。</returns>
    Task<SkillResult> ExecuteAsync(SkillInvocationRequest request, CancellationToken cancellationToken = default);
}