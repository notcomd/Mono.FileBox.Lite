using Mono.FileBox.Lite.Abstractions;

namespace Mono.FileBox.Lite.Skills;

/// <summary>
/// 基于 <see cref="SkillDescriptor"/> 的本地技能适配器，通过 <see cref="LocalSkillExecutor"/> 执行。
/// </summary>
public sealed class LocalSkill : ISkill
{
    private readonly LocalSkillExecutor _executor;
    private readonly SkillDescriptor _descriptor;

    /// <summary>创建一个本地技能实例。</summary>
    public LocalSkill(SkillDescriptor descriptor, LocalSkillExecutor executor)
    {
        _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    /// <inheritdoc />
    public string Id => _descriptor.Id;

    /// <inheritdoc />
    public string Name => _descriptor.Name;

    /// <inheritdoc />
    public string Description => _descriptor.Description;

    /// <summary>底层描述符。</summary>
    public SkillDescriptor Descriptor => _descriptor;

    /// <inheritdoc />
    public Task<SkillResult> ExecuteAsync(
        SkillInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        return _executor.ExecuteAsync(
            _descriptor.Command,
            _descriptor.Arguments,
            request,
            _descriptor.WorkingDirectory,
            cancellationToken);
    }
}