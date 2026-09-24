// 本文件包含类型 IObjectUseCase<in TCommand, TResult>：通用对象用例——接收命令，返回结果。
namespace Mono.FileBox.Lite.Abstractions.UseCases;

/// <summary>A generic object use-case: takes a command, returns a result. 通用对象用例：接收命令，返回结果。</summary>
public interface IObjectUseCase<in TCommand, TResult>
{
    Task<TResult> ExecuteAsync(TCommand command, CancellationToken ct);
}