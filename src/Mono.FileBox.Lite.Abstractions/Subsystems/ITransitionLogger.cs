// 本文件包含类型 ITransitionLogger：记录生命周期转换，不得阻塞主转换。
namespace Mono.FileBox.Lite.Abstractions.Subsystems;

/// <summary>Logs lifecycle transitions. Must not block the main transition.</summary>
public interface ITransitionLogger
{
    void Log(ObjectTrigger t, ObjectState from, ObjectState to);
}