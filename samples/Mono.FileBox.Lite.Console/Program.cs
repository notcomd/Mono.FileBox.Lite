using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Configuration;
using Mono.FileBox.Lite.Skills;

namespace Mono.FileBox.Lite.Console;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // 1. 配置：技能根目录指向随样例一起复制的 skills 目录。
        var skillsRoot = Path.Combine(AppContext.BaseDirectory, "skills");
        var options = new SkillOptions
        {
            SkillRootDirectory = skillsRoot,
            DefaultTimeoutSeconds = 30,
            SearchSubdirectories = true,
        };

        // 2. 组装执行器与管理器。
        var executor = new LocalSkillExecutor { DefaultTimeoutSeconds = options.DefaultTimeoutSeconds };
        var manager = new SkillManager(options, executor);

        System.Console.WriteLine($"技能根目录: {skillsRoot}");
        System.Console.WriteLine($"已发现 {manager.Skills.Count} 个本地技能：\n");

        foreach (var skill in manager.Skills)
        {
            System.Console.WriteLine($"  - [{skill.Id}] {skill.Name}: {skill.Description}");
        }

        if (manager.Skills.Count == 0)
        {
            System.Console.WriteLine("（未发现任何技能，退出。）");
            return 1;
        }

        // 3. 演示调用 hello 技能（演示 {input} 令牌替换）。
        System.Console.WriteLine("\n== 调用 hello 技能 ==");
        await InvokeAsync(manager, "hello", new SkillInvocationRequest { Input = "Mono.FileBox.Lite" });

        // 4. 演示调用 list-files 技能（无输入参数）。
        System.Console.WriteLine("\n== 调用 list-files 技能 ==");
        await InvokeAsync(manager, "list-files", new SkillInvocationRequest());

        // 5. 演示调用不存在的技能，验证容错。
        System.Console.WriteLine("\n== 调用不存在的技能(should-be-missing) ==");
        await InvokeAsync(manager, "should-be-missing", new SkillInvocationRequest());

        System.Console.WriteLine("\n演示结束。");
        return 0;
    }

    private static async Task InvokeAsync(SkillManager manager, string id, SkillInvocationRequest request)
    {
        var result = await manager.ExecuteAsync(id, request);
        System.Console.WriteLine($"  命令    : {result.Command} {result.Arguments}");
        if (!string.IsNullOrEmpty(result.Error))
        {
            System.Console.WriteLine($"  错误    : {result.Error}");
        }
        else
        {
            System.Console.WriteLine($"  退出码  : {result.ExitCode}");
            System.Console.WriteLine($"  成功    : {result.IsSuccess}");
            System.Console.WriteLine($"  耗时    : {result.DurationMilliseconds} ms");
            if (!string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                System.Console.WriteLine($"  输出    : {result.StandardOutput.Trim()}");
            }
            if (!string.IsNullOrWhiteSpace(result.StandardError))
            {
                System.Console.WriteLine($"  错误输出: {result.StandardError.Trim()}");
            }
        }
    }
}