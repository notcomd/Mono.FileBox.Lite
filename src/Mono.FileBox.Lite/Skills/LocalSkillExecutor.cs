using System.ComponentModel;
using System.Diagnostics;
using Mono.FileBox.Lite.Abstractions;

namespace Mono.FileBox.Lite.Skills;

/// <summary>
/// 通过启动本地进程（CLI）来执行技能的调用。这是“调用本地技能”的核心执行器。
/// </summary>
public sealed class LocalSkillExecutor
{
    private const string InputToken = "{input}";

    /// <summary>默认超时（秒），小于等于 0 时不作超时限制。</summary>
    public int DefaultTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// 执行一次本地进程调用。
    /// </summary>
    /// <param name="command">可执行文件或命令名。</param>
    /// <param name="baseArguments">描述符基础参数。</param>
    /// <param name="request">调用请求。</param>
    /// <param name="workingDirectory">工作目录，可为空。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>调用结果。</returns>
    public async Task<SkillResult> ExecuteAsync(
        string command,
        IReadOnlyList<string> baseArguments,
        SkillInvocationRequest request,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var argumentList = BuildArgumentList(baseArguments, request);

        var result = new SkillResult
        {
            Command = command,
            Arguments = string.Join(' ', argumentList),
            StartedAt = DateTimeOffset.UtcNow,
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(request.CancellationToken, cancellationToken);

        var psi = new ProcessStartInfo
        {
            FileName = command,
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory)
                ? Environment.CurrentDirectory
                : workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        foreach (var arg in argumentList)
        {
            psi.ArgumentList.Add(arg);
        }

        try
        {
            using var process = Process.Start(psi);
            if (process is null)
            {
                result.Error = "进程启动失败：返回空进程引用。";
                return result;
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(cts.Token);
            var errorTask = process.StandardError.ReadToEndAsync(cts.Token);
            var exitTask = process.WaitForExitAsync(cts.Token);

            var timeoutMs = ComputeTimeoutMilliseconds(request, cts);
            if (timeoutMs > 0 && timeoutMs != Timeout.Infinite)
            {
                var completed = await Task.WhenAny(exitTask, Task.Delay(timeoutMs, cts.Token));
                var endedByTimeout = completed != exitTask;
                if (endedByTimeout)
                {
                    result.IsTimedOut = !request.CancellationToken.IsCancellationRequested;
                    result.IsCanceled = request.CancellationToken.IsCancellationRequested;
                    TryKill(process);
                    cts.Cancel(); // 让未完成的任务尽早退出
                }
            }

            if (!result.IsTimedOut && !result.IsCanceled)
            {
                try
                {
                    await exitTask;
                    result.ExitCode = process.ExitCode;
                }
                catch (OperationCanceledException)
                {
                    result.IsCanceled = request.CancellationToken.IsCancellationRequested;
                    result.IsTimedOut = !result.IsCanceled;
                    TryKill(process);
                }
            }

            result.StandardOutput = await ReadSafe(outputTask);
            result.StandardError = await ReadSafe(errorTask);
        }
        catch (Win32Exception ex)
        {
            result.Error = $"无法启动进程：{ex.Message}";
        }
        catch (InvalidOperationException ex)
        {
            result.Error = ex.Message;
        }
        finally
        {
            result.FinishedAt = DateTimeOffset.UtcNow;
        }

        return result;
    }

    private int ComputeTimeoutMilliseconds(SkillInvocationRequest request, CancellationTokenSource cts)
    {
        var seconds = request.TimeoutSeconds > 0 ? request.TimeoutSeconds : DefaultTimeoutSeconds;
        if (seconds <= 0)
        {
            return -1;
        }

        var ms = seconds * 1000L;
        return ms > int.MaxValue ? int.MaxValue : (int)ms;
    }

    private static async Task<string> ReadSafe(Task<string> task)
    {
        try
        {
            return await task;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private static List<string> BuildArgumentList(
        IReadOnlyList<string> baseArguments,
        SkillInvocationRequest request)
    {
        var list = new List<string>(baseArguments.Count + request.Arguments.Count);
        foreach (var arg in baseArguments)
        {
            list.Add(arg.Replace(InputToken, request.Input));
        }
        list.AddRange(request.Arguments);
        return list;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(2000);
            }
        }
        catch (Exception)
        {
            // 忽略终止失败。
        }
    }
}