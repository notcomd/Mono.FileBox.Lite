# Mono.FileBox.Lite

一个以**对象生命周期状态机**为核心的**文件对象存储引擎**（.NET 类库）。

**技术栈**：.NET Standard 2.0（核心库）· ASP.NET Core 可宿主（示例 .NET 8/9+）· 依赖 `Microsoft.Extensions.DependencyInjection` / `System.Text.Json` / `System.Memory`
**许可**：MIT

---

## 它是什么

Mono.FileBox.Lite 是一个 **L0–L5 分层**、**单向依赖** 的对象存储引擎类库。每个存储对象（blob）是一个生命周期状态机实例，从写入到清除走 `Pending → … → Purged` 的受控转移。

- **内容寻址**：对象以 SHA-256 哈希寻址，全局去重，流式落盘（内存 O(1)）。
- **对象内分块**：大对象按 8 MiB 切块 + 对象级 manifest（可选开关，默认关闭）。
- **生命周期状态机**：`Put/Index/Audit/Publish/Archive/Restore/Expire/Delete/Purge` 全流程可控转移。
- **多维索引**：前缀/标签/层级/状态/时间/大小查询、游标分页、一致性维护；JSON 文档持久化 + 热点缓存。
- **备份 / 集群 / 扩容**：共享块池全量/增量备份；一致哈希、拓扑、扩容/缩容、容量水位。
- **并发友好**：单进程异步多线程设计，对象级并行，无全局串行锁。

---

## 快速上手

```csharp
var services = new ServiceCollection();
services.AddMonoFileBoxLite(new FileBoxOptions
{
    Cluster = { NodeId = "node-a" },
    Storage = { Pools = { new PoolOptions { PoolId = "hot-1", RootPath = @"C:\data\hot", Tier = StorageTier.Hot, Enabled = true } } }
});
services.AddMonoFileBoxLiteStorage();
services.AddMonoFileBoxLiteIndex();
services.AddMonoFileBoxLiteUseCases();
var provider = services.BuildServiceProvider();

var put = provider.GetRequiredService<IPutObjectUseCase>();
var r = await put.ExecuteAsync(new PutObjectCommand { NamespaceId = "ns1", ObjectKey = "/a.bin", Content = File.OpenRead(@"C:\a.bin") });
Console.WriteLine(r.Succeeded ? r.ContentHash : r.Error);
```

> 完整示例与配置见 [快速开始](Getting-Started)。

---

## Wiki 导航

| 页面 | 内容 |
|------|------|
| [快速开始](Getting-Started) | 安装、最小示例、DI 装配、appsettings.json 文件配置 |
| [架构与分层](Architecture) | L0–L5 分层、模块职责、包结构 |
| [生命周期状态机](State-Machine) | 对象状态、触发、转移表、默认实现 |
| [存储引擎](Storage-Engine) | 内容寻址、去重、分块、内存优化、配置示例 |
| [索引](Index) | 查询模型、性能、JSON 持久化、热点缓存 |

---

## 仓库文档

- 详细开发/架构文档：[`docs/DEVELOPMENT.md`](https://github.com/notcomd/Mono.FileBox.Lite/blob/master/docs/DEVELOPMENT.md)
- 使用手册（根）：[`README.md`](https://github.com/notcomd/Mono.FileBox.Lite/blob/master/README.md)