// 本文件包含类型 IEventBus：向感兴趣的消费者发布对象生命周期事件。
namespace Mono.FileBox.Lite.Abstractions.Subsystems;

/// <summary>Publishes object lifecycle events to interested consumers. 
/// 向感兴趣的消费者发布对象生命周期事件。</summary>
public interface IEventBus
{
    Task PublishAsync(string topic, object payload, CancellationToken ct);
}