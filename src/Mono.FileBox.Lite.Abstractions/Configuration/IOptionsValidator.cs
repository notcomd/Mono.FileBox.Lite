// 本文件包含类型 IOptionsValidator<in TOptions>：在启动时和重载时校验选项对象。
namespace Mono.FileBox.Lite.Abstractions.Configuration;

/// <summary>Validates an options object at startup and on reload. 中文翻译：在启动时及配置重载时校验选项对象。</summary>
public interface IOptionsValidator<in TOptions>
{
    OptionsValidationResult Validate(TOptions options);
}