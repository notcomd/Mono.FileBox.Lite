// 本文件包含类型 IContentModerator：决定对象内容是否通过配置的审核策略。
namespace Mono.FileBox.Lite.Abstractions.Subsystems;

/// <summary>Decides whether object content passes the configured moderation policy. 
/// 决定对象内容是否通过所配置的内容审核策略。</summary>
public interface IContentModerator
{
    Task<ModerationResult> ModerateAsync(IObjectContext ctx, CancellationToken ct);
}