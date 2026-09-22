// 本文件包含类型 IOptionsChangeNotifier：在选项变更时通知订阅者并重载配置源。
namespace Mono.FileBox.Lite.Abstractions.Configuration;

/// <summary>Notifies subscribers when options change and reloads configuration sources. 中文翻译：在选项变更时通知订阅者，并负责重载配置源。</summary>
public interface IOptionsChangeNotifier
{
    IDisposable OnChange<T>(Action<T> callback);
    Task ReloadAsync(CancellationToken ct);
}