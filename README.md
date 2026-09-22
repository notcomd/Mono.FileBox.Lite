# Mono.FileBox.Lite

一个以**对象生命周期状态机**为核心的**文件对象存储引擎**（.NET 类库。L0–L5 分层架构、单向依赖、内容寻址、去重、分块、索引、备份、集群）。

- 目标框架：核心类库 `.NET Standard 2.0`，示例/测试 `.NET 8/9+`
- 依赖：`Microsoft.Extensions.DependencyInjection`、`System.Text.Json`、`System.Memory`
- 许可：MIT

---

## 1. 它能做什么

| 能力 | 说明 |
|------|------|
| 对象存储引擎 | SHA-256 内容寻址 + 全局去重、流式落盘（内存 O(1)）、范围读取、物理清除 |
| 对象内固定分块 | 大对象按 8 MiB 切块 + 对象级 manifest（可选开关），跨块自动拼接读取 |
| 对象生命周期 | 每个存储对象是有限状态机，`Pending → Stored → Indexed → Audited → Available → … → Purged` |
| 对象索引 | 前缀 / 标签 / 层级 / 状态 / 时间 / 大小等多维查询、游标分页、一致性维护（Verify/Repair/Rebuild）|
| 本地索引持久化 | JSON 文档 + 热点缓存（高频条目入内存、`.hot` sidecar 冷启动预载，免重复解析索引文件）|
| 对象备份 | 共享块池 + 全量/增量、恢复、校验、目标支持本地目录（S3 兼容为占位）|
| 集群 / 扩容 | 一致哈希、拓扑、扩容/缩容、容量水位、分层 |
| 并发模型 | 单进程、异步多线程（线程池），不同对象并行插入/读取，无冲突 |

不在范围内（由上层负责）：租户/RBAC、认证授权、计费审计。

---

## 2. 快速开始

### 2.1 安装

发布到 NuGet 后（或以 `ProjectReference`/`dotnet add reference` 引用源码工程）：

```bash
dotnet add package Mono.FileBox.Lite.DependencyInjection
```

> 依赖关系：`Mono.FileBox.Lite.DependencyInjection` 一个包即自动依赖其余 9 个库包
> （Abstractions / StateMachine / Storage / Subsystems / Index / Backup / Cluster / Configuration / UseCases），因此**通常只需引用这一个包**。

### 2.2 最小示例

```csharp
using Microsoft.Extensions.DependencyInjection;
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Configuration;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.UseCases;
using Mono.FileBox.Lite.DependencyInjection;

// 1) 配置存储池（至少一个启用的磁盘池）
var options = new FileBoxOptions
{
    Cluster = { NodeId = "node-a" },
    Storage =
    {
        Pools =
        {
            new PoolOptions
            {
                PoolId   = "hot-1",
                RootPath = @"C:\filebox-data\hot",
                Tier     = StorageTier.Hot,
                Enabled  = true
            }
        }
    }
};

// 2) 组装 DI 容器
await using var provider = new ServiceCollection()
    .AddMonoFileBoxLite(options)          // 核心 + 状态机 + 默认子系统
    .AddMonoFileBoxLiteStorage()          // 内容寻址存储
    .AddMonoFileBoxLiteIndex()            // 结构索引 + 查询
    .AddMonoFileBoxLiteUseCases()         // 高层用例（Put/Get/Delete/Archive/Restore）
    .BuildServiceProvider();

// 3) 放入一个对象（流式写入，自动计算 SHA-256 并去重）
var put = provider.GetRequiredService<IPutObjectUseCase>();
var putResult = await put.ExecuteAsync(new PutObjectCommand
{
    NamespaceId = "ns1",
    ObjectKey   = "/uploads/report.pdf",
    Content     = File.OpenRead(@"C:\tmp\report.pdf"),
    ContentType = "application/pdf",
    Tags        = new Dictionary<string, string> { ["dept"] = "finance" }
});

Console.WriteLine(putResult.Succeeded
    ? $"stored, hash={putResult.ContentHash}, state={putResult.State}"
    : $"failed: {putResult.Error}");

// 4) 读取对象（按内容哈希寻址）
var get = provider.GetRequiredService<IGetObjectUseCase>();
var getResult = await get.ExecuteAsync(new GetObjectCommand
{
    ContentHash = putResult.ContentHash,
    NamespaceId = "ns1"
});

if (getResult.Content is not null)
{
    using var stream = getResult.Content;   // 读取返回的流即可
    // ... 保存到文件 / 返回给调用方
}
```

---

## 3. NuGet 包与分层

| 包 | 层 | 职责 |
|----|----|------|
| `Mono.FileBox.Lite.Abstractions` | L0 | 纯接口/模型（零依赖）|
| `Mono.FileBox.Lite.Storage` | L1 | 内容寻址存储、分块、读写、清除、磁盘选择、IO 管线 |
| `Mono.FileBox.Lite.StateMachine` | L3 | 对象生命周期状态机引擎 |
| `Mono.FileBox.Lite.Subsystems` | L2 | 默认/NoOp 实现、事件、生命周期、索引写入 |
| `Mono.FileBox.Lite.Index` | L2 | 结构化索引与查询引擎（含 JSON 持久化 + 热点缓存）|
| `Mono.FileBox.Lite.Backup` | L2 | 备份 |
| `Mono.FileBox.Lite.Cluster` | L2 | 集群协调、扩容/缩容 |
| `Mono.FileBox.Lite.Configuration` | L2 | 配置校验/变更通知 |
| `Mono.FileBox.Lite.UseCases` | L4 | 用例编排（带回滚）|
| `Mono.FileBox.Lite.DependencyInjection` | — | 全部 `AddMonoFileBoxLite*` 装配扩展，**推荐唯一入口** |

依赖约束：**分层单向向下**（L4→L3→L2→L1→L0，L0 零依赖），用 `ProjectReference` 严格实现。

---

## 4. 装配（DependencyInjection）

按需分别添加模块扩展，每个都接受可选的 `configure` 回调用于替换默认实现：

```csharp
services
    .AddMonoFileBoxLite(options, machine: regs => { /* 可选：自定义转移表 */ })
    .AddMonoFileBoxLiteStorage(builder => { builder.UseIO<MyIOSink>(); })
    .AddMonoFileBoxLiteIndex(builder => { /* 见下：切换 JSON 索引 */ })
    .AddMonoFileBoxLiteBackup(builder => { /* 注册远端目标 */ })
    .AddMonoFileBoxLiteScaling(builder => { /* 扩容/容量阈值 */ })
    .AddMonoFileBoxLiteUseCases();
```

> 最小可行仅需 `AddMonoFileBoxLite` + `AddMonoFileBoxLiteStorage` + `AddMonoFileBoxLiteIndex` + `AddMonoFileBoxLiteUseCases`。

### 4.1 切换本地 JSON 索引（含热点缓存）

默认索引为内存实现（`InMemoryEntryStore`）。需要**持久化**时，一行切换为 JSON 文档存储，并自动套上热点缓存：

```csharp
services.AddMonoFileBoxLiteIndex(builder =>
{
    // path：JSON 文档路径；热点条目存到 path+".hot" 用于冷启动预载
    builder.UseJsonFileEntryStore(@"C:\filebox-data\index.json");
    // 可选精细参数：
    //   builder.UseJsonFileEntryStore(@"C:\filebox-data\index.json",
    //       new HotIndexOptions { Capacity = 512, PromotionThreshold = 3, PersistHot = true });
});
```

> 保留了 `IEntryStore` DB 接口，未来可无缝替换为 SQLite 等实现。

### 4.2 用 appsettings.json 配置文件来配置引擎

在 ASP.NET Core / 任何使用 `Microsoft.Extensions.Configuration` 的宿主里，可用 `appsettings.json` 声明引擎配置，再绑定到 `FileBoxOptions` 并装配。下面是一份「文件存储 API 服务」的完整 `appsettings.json` 示例，`FileBox:` 段对应 `FileBoxOptions` 的字段（含默认值标注，可只写需要覆盖的键）：

```jsonc
{
  "FileBox": {
    "StateMachine": {
      "ThrowOnGuardDenied": false,   // bool，默认 false
      "FailFastOnObserverError": false, // bool，默认 false
      "TransitionTimeout": "00:00:30",  // TimeSpan，默认 00:00:30
      "AuditDeniedTransitions": true,   // bool，默认 true
      "MaxOptimisticRetries": 3,        // int，默认 3
      "RetryBackoff": "00:00:00.050"    // TimeSpan，默认 50ms
    },
    "Storage": {
      "HashAlgorithm": "SHA-256",       // string，默认 "SHA-256"
      "Deduplication": "Global",        // DeduplicationMode：Global|NamespaceScoped|Disabled
      "VerifyAfterWrite": false,        // bool，默认 false
      "Chunking": {
        "Enabled": true,                // bool，默认 false（分块默认关闭，需显式开启）
        "ChunkSizeBytes": 8388608       // long，默认 8 MiB
      },
      "Pools": [
        {
          "PoolId": "hot-1",
          "RootPath": "C:\\filebox-data\\hot",
          "Tier": "Hot",                // StorageTier：Hot|Warm|Cold|Archive
          "CapacityBytes": null,        // long?，null 表示无上限
          "Priority": 0,                // int，默认 0
          "Enabled": true               // bool，默认 true
        },
        {
          "PoolId": "cold-1",
          "RootPath": "C:\\filebox-data\\cold",
          "Tier": "Cold",
          "Enabled": true
        }
      ],
      "DefaultWrite": {                 // WriteOptions
        "Tier": "Hot",
        "PoolId": null,                 // string?，留空按策略挑选
        "BandwidthLimit": null          // long?，null 表示不限
      }
    },
    "Index": {
      "Consistency": "Strong",          // IndexConsistencyMode：Strong|Eventual
      "DefaultPageSize": 100,           // int，默认 100
      "MaxPageSize": 1000,              // int，默认 1000
      "Features": {
        "EnablePrefix": true, "EnableTagInverted": true, "EnableAttribute": true,
        "EnableTierBitmap": true, "EnableStateBitmap": true,
        "EnableTimeIndex": true, "EnableSizeIndex": true
      },
      "Sharding": {
        "Enabled": false,               // bool，默认 false
        "ShardCount": 1                 // int，默认 1
      },
      "Store": {                        // OrderedKvOptions
        "Provider": "sqlite",           // string，默认 "sqlite"
        "ConnectionString": null,       // string?，默认用内置路径
        "CacheSizeMb": 64,              // int，默认 64
        "SyncWrites": true              // bool，默认 true
      }
    },
    "Backup": {
      "MaxConcurrency": 4,              // int，默认 4
      "BandwidthLimit": 0,              // long，0 表示不限
      "MaxRetries": 3,                  // int，默认 3
      "Targets": [
        {
          "TargetId": "local-primary",
          "Type": "local",              // 目标类型（如 "local"）
          "RootPath": "C:\\filebox-backups",
          "Enabled": true
        }
      ],
      "Schedules": [
        {
          "ScheduleId": "daily",
          "Cron": "0 2 * * *",          // Cron 表达式
          "Kind": "Incremental",        // BackupKind：Full|Incremental|Differential
          "TargetId": "local-primary",
          "Enabled": true
        }
      ]
    },
    "Cluster": {
      "Role": "Hybrid",                 // NodeRole：Coordinator|Storage|Index|Backup|Hybrid
      "NodeId": "node-a",
      "AdvertiseAddress": "localhost:0",
      "SeedNodes": [ "node-b:9000", "node-c:9000" ],
      "HeartbeatInterval": "00:00:05",  // TimeSpan，默认 5s
      "NodeTimeout": "00:00:30",        // TimeSpan，默认 30s
      "Thresholds": { "Warning": 0.70, "Critical": 0.85, "Full": 0.95 } // double 水位
    },
    "Lifecycle": {
      "ScanInterval": "00:05:00",       // TimeSpan，默认 5min
      "ScanBatchSize": 1000,            // int，默认 1000
      "ArchiveAfter": null,             // TimeSpan?，null 表示不启用
      "DeleteAfter": null               // TimeSpan?，null 表示不启用
    },
    "Observability": {
      "Logging": { "MinimumLevel": "Information" }, // LogLevel：Trace..Critical
      "Metrics": { "Enabled": true, "Port": 9090 },   // MetricsOptions
      "Tracing": { "Enabled": false }                // TracingOptions
    }
  }
}
```

在 `Program.cs`/`Startup` 里加载 + 绑定 + 装配：

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Configuration;
using Mono.FileBox.Lite.DependencyInjection;

// 1) 读取配置（ASP.NET Core 环境下 Configuration 已由 WebApplicationBuilder 注入）
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// 2) 绑定 "FileBox" 段到 FileBoxOptions（子对象均带默认值，省略的键用默认）
var fileBox = configuration.GetSection("FileBox").Get<FileBoxOptions>() ?? new FileBoxOptions();

// 3) 装配 DI（与代码方式完全一致）
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddMonoFileBoxLite(fileBox);
services.AddMonoFileBoxLiteStorage();
services.AddMonoFileBoxLiteIndex(builder => builder.UseJsonFileEntryStore(@"C:\filebox-data\index.json"));
services.AddMonoFileBoxLiteUseCases();
var provider = services.BuildServiceProvider();
```

要点：

- `FileBoxOptions` 及其子选项均为可变更的 POCO，所有子对象**默认即带实例**，`appsettings.json` 里未出现的键会保留类型默认值——因此示例可只写需要覆盖的字段。
- 枚举（`Tier`/`Deduplication`/`Consistency`/`Role`/`Kind`/`MinimumLevel`）以**字符串名称**匹配；`TimeSpan` 一律用**字符串**格式（如 `"00:05:00"`），不能写数字秒。
- 需要 `Microsoft.Extensions.Configuration.Binder`（绑定 `Get<T>()`）与 `Microsoft.Extensions.Configuration.Json`（`AddJsonFile`）。
- `Storage.Chunking.Enabled` 默认 `false`；想在配置层面开启分块，就在 `FileBox:Storage:Chunking:Enabled` 设为 `true`。
- 若要配置日志/指标重启后即时生效，可配合 `IOptionsChangeNotifier`（仓库内置热重载通知）监听变更。

---

## 5. 核心概念

### 5.1 内容寻址与去重

- 对象地址即其 **SHA-256 哈希**。物理块按下式存放：`blocks/{hash[0:2]}/{hash[2:4]}/{hash}`。
- 相同内容只落盘一次；`Sha256ObjectWriter` 边算哈希边流式写入，不把 payload 整块驻留内存（**内存 O(1)**，1 GiB 上传驻留约 35–50 MiB）。

### 5.2 生命周期状态机

每个对象是一条状态机（`ObjectState × ObjectTrigger`）：

```
Pending ─Put→ Stored ─Index→ Indexed ─Audit→ Audited ─Publish→ Available
Available ─Archive→ Archived ─Restore→ Available
Available ─Expire→ Expired      Available/Archived ─Delete→ Deleted ─Purge→ Purged
```

- 非法转移返回 `NotAllowed`，状态不变；guard 拒绝返回 `Denied`。
- 转移可挂 guard / action / observer（如 `IndexStateSyncObserver` 在 `Available` 时同步索引）。
- 默认实现为单节点友好（`NoOpDistributedLock`、`NullEventBus`、`NeverExpirePolicy` 等），可通过 Builder 替换为真实实现。

### 5.3 分块（默认关闭）

大对象（`>8 MiB`）切多块 + 对象级 manifest；小对象（`≤8 MiB`）整对象单块。**默认关闭**，需显式开启：

```csharp
options.Storage.Chunking.Enabled = true;   // ChunkSizeBytes 默认 8 MiB
```

### 5.4 用例层（带回滚）

高层 `UseCase` 把「转移序列 + 回滚」从状态机剥离：

| 用例 | 动作 |
|------|------|
| `IPutObjectUseCase` | `Put→Index→Audit→Publish`，任一步失败逆序回滚 |
| `IGetObjectUseCase` | 纯查询，不触发转移；覆盖分块对象的跨块读取 |
| `IDeleteObjectUseCase` | `Delete→Purge` 物理清除 + 索引移除 |
| `IArchiveObjectUseCase` / `IRestoreObjectUseCase` | 归档 / 恢复 |

---

## 6. 索引使用示例

```csharp
using Mono.FileBox.Lite.Abstractions.Index;

var index = provider.GetRequiredService<IIndexReader>();

// 按标签 + 前缀分页查询
var page = await index.QueryAsync(new IndexQuery
{
    NamespaceId = "ns1",
    KeyPrefix   = "/uploads/",
    Tags        = new Dictionary<string, string> { ["dept"] = "finance" },
    Page        = new PageRequest { Size = 50 }
});

foreach (var item in page.Items)
    Console.WriteLine($"{item.ObjectKey}  hash={item.ContentHash}  state={item.State}");
```

多维查询支持：前缀、标签、层级、状态、时间范围、大小范围、稳定排序、游标分页、命名空间隔离。

索引维护（以物理块为权威检出缺失/孤儿）：`IIndexMaintainer.VerifyAsync / RepairAsync / RebuildAsync`。

---

## 7. 高级用法

### 7.1 属性元数据 / 写入选项

```csharp
var res = await put.ExecuteAsync(new PutObjectCommand
{
    NamespaceId = "ns1",
    Content     = stream,
    Attributes  = new Dictionary<string, object> { ["owner"] = "alice", ["reviews"] = 3L },
    Write       = new WriteOptions { /* 覆盖默认 IO 参数 */ }
});
```

> 注意：JSON 索引持久化会保留数值原始类型（`long` 不会被提升成 `double`，见 `JsonFileIndexStore`）。

### 7.2 替换默认实现

各 `*Builder` 暴露扩展点，例如：

```csharp
services.AddMonoFileBoxLiteStorage(builder => builder.UseIO<MyCipherIO>()); // 自定义 IO 管线
services.AddMonoFileBoxLite()            // 内部可通过 machine 回调自定义状态机
```

### 7.3 并发

引擎为**单进程 + 异步多线程**设计：不同对象并行 Put/Get，无全局串行锁、无同 hash 冲突；适合线程池高并发调用（官方压测：20 并发上传/下载，SHA-256 全量校验通过）。

---

## 8. 常见问题 / 注意事项

- **分块默认关闭**：想对 `>8 MiB` 对象分块，务必先置 `options.Storage.Chunking.Enabled = true`。
- **自定义转移表**：传给 `AddMonoFileBoxLite(options, machine: ...)`，或用 `AddDefaultObjectTransitions(registry)` 打底后追加。
- **索引持久化**：默认内存实现不掉电保存；需要持久化请用 `builder.UseJsonFileEntryStore(path)`。
- **并发安全**：不同对象并行安全；同一哈希内容去重后并发读取均正确。

---

## 9. 构建 / 测试 / 打包

```bash
# 编译（要求 0 警告 0 错误）
dotnet build Mono.FileBox.Lite.slnx -c Release

# 运行示例（1 GiB 大文件上传压测，采样内存/CPU/磁盘）
dotnet run --project samples/Mono.FileBox.Lite.Sample -c Release
#   可调：MONOFILEBOX_SAMPLE_SIZE_MB=2048 , MONOFILEBOX_CONCURRENT_FILES=200

# 运行测试
dotnet test tests/Mono.FileBox.Lite.Index.Tests -c Release
dotnet test tests/Mono.FileBox.Lite.Functional.Tests -c Release

# 打包 NuGet（输出 artifacts/packages，含 .nupkg + .snupkg 符号包）
dotnet pack Mono.FileBox.Lite.slnx -c Release -o artifacts/packages
```

---

## 10. 文档

- 详细开发/架构文档见 [`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md)（分层说明、分块开关、索引持久化/热点机制、打包命令、已知限制与 Roadmap）。