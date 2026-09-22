# 快速开始

本文带你完成：安装 → 最小读写对象 → DI 装配 → 用 `appsettings.json` 配置。

---

## 1. 安装

从 NuGet 引入 **一个** 包即可（它自动依赖其余 9 个库包）：

```bash
dotnet add package Mono.FileBox.Lite.DependencyInjection
```

> 若尚未发布到 NuGet，可用 `dotnet add reference` 指向 `src/Mono.FileBox.Lite.DependencyInjection/Mono.FileBox.Lite.DependencyInjection.csproj`。

| 需要的能力 | 额外包 |
|------|------|
| 仅对象读写 + 索引 | `Mono.FileBox.Lite.DependencyInjection`（含全部）|
| 需要的扩展模块 | 随 DI 包一起引入 |

---

## 2. 最小读写示例

```csharp
using Microsoft.Extensions.DependencyInjection;
using Mono.FileBox.Lite.Abstractions;
using Mono.FileBox.Lite.Abstractions.Configuration;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.UseCases;
using Mono.FileBox.Lite.DependencyInjection;

var options = new FileBoxOptions
{
    Cluster = { NodeId = "node-a" },
    Storage =
    {
        Pools =
        {
            new PoolOptions
            {
                PoolId = "hot-1",
                RootPath = @"C:\filebox-data\hot",
                Tier = StorageTier.Hot,
                Enabled = true
            }
        }
    }
};

await using var provider = new ServiceCollection()
    .AddMonoFileBoxLite(options)
    .AddMonoFileBoxLiteStorage()
    .AddMonoFileBoxLiteIndex()
    .AddMonoFileBoxLiteUseCases()
    .BuildServiceProvider();

// 放入对象
var put = provider.GetRequiredService<IPutObjectUseCase>();
var putResult = await put.ExecuteAsync(new PutObjectCommand
{
    NamespaceId = "ns1",
    ObjectKey = "/uploads/report.pdf",
    Content = File.OpenRead(@"C:\tmp\report.pdf"),
    ContentType = "application/pdf",
    Tags = new Dictionary<string, string> { ["dept"] = "finance" }
});
Console.WriteLine(putResult.Succeeded
    ? $"stored, hash={putResult.ContentHash}, state={putResult.State}"
    : $"failed: {putResult.Error}");

// 按内容哈希读取
var get = provider.GetRequiredService<IGetObjectUseCase>();
var getResult = await get.ExecuteAsync(new GetObjectCommand
{
    ContentHash = putResult.ContentHash,
    NamespaceId = "ns1"
});
if (getResult.Content is not null)
{
    using var stream = getResult.Content;
    // 使用 stream ...
}
```

---

## 3. DI 装配（模块扩展）

按需组合，每个扩展接受可选的 `configure` 回调用于替换默认实现：

```csharp
services
    .AddMonoFileBoxLite(options, machine: regs => { /* 可选：自定义转移表 */ })
    .AddMonoFileBoxLiteStorage(builder => { /* 自定义 IO/磁盘选择 */ })
    .AddMonoFileBoxLiteIndex(builder => { /* 见下：切换 JSON 索引 */ })
    .AddMonoFileBoxLiteBackup(builder => { /* 注册备份目标 */ })
    .AddMonoFileBoxLiteScaling(builder => { /* 扩容/容量阈值 */ })
    .AddMonoFileBoxLiteUseCases();
```

**最小可行**：`AddMonoFileBoxLite` + `AddMonoFileBoxLiteStorage` + `AddMonoFileBoxLiteIndex` + `AddMonoFileBoxLiteUseCases`。

### 切换本地 JSON 索引（含热点缓存）

默认索引为内存实现。需要**持久化**时一行切换：

```csharp
services.AddMonoFileBoxLiteIndex(builder =>
{
    return builder.UseJsonFileEntryStore(@"C:\filebox-data\index.json");
});
```

- 数据写入 `index.json`（原子重写，临时文件 + move）
- 高频条目提升进内存，超过容量按 LRU 淘汰
- 热点集合落在 `index.json.hot`，冷启动预载，减少索引文件解析

---

## 4. 用 appsettings.json 配置文件配置

在使用 `Microsoft.Extensions.Configuration` 的宿主里（如 ASP.NET Core Web API 文件存储服务），用 `appsettings.json` 声明引擎配置并绑定到 `FileBoxOptions`。

> 完整无删减的 `appsettings.json` 示例（含默认值与类型标注）见本页底部。以下为**只写需要覆盖字段**的简版：

```jsonc
{
  "FileBox": {
    "StateMachine": { "ThrowOnGuardDenied": false, "TransitionTimeout": "00:00:30" },
    "Storage": {
      "Chunking": { "Enabled": true, "ChunkSizeBytes": 8388608 },
      "Pools": [
        { "PoolId": "hot-1", "RootPath": "C:\\filebox-data\\hot", "Tier": "Hot", "Enabled": true }
      ]
    },
    "Index": { "Consistency": "Strong", "DefaultPageSize": 100 },
    "Cluster": { "NodeId": "node-a", "Role": "Hybrid" }
  }
}
```

绑定 + 装配：

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mono.FileBox.Lite.Abstractions.Configuration;
using Mono.FileBox.Lite.DependencyInjection;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var fileBox = configuration.GetSection("FileBox").Get<FileBoxOptions>() ?? new FileBoxOptions();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddMonoFileBoxLite(fileBox);
services.AddMonoFileBoxLiteStorage();
services.AddMonoFileBoxLiteIndex(builder =>
    builder.UseJsonFileEntryStore(@"C:\filebox-data\index.json"));
services.AddMonoFileBoxLiteUseCases();
var provider = services.BuildServiceProvider();
```

**配置要点**

- `FileBoxOptions` 及其子选项均为可变 POCO，子对象**默认即带实例**——省略的键保留类型默认值。
- 枚举（`Tier`/`Deduplication`/`Consistency`/`Role`/`Kind`/`MinimumLevel`）以**字符串名称**匹配。
- `TimeSpan` 一律用**字符串**格式（如 `"00:05:00"`），不能写数字秒。
- 需要 `Microsoft.Extensions.Configuration.Binder` 与 `Microsoft.Extensions.Configuration.Json`。
- `Storage.Chunking.Enabled` 默认 `false`，分块需显式开启。

---

## 5. appsettings.json 完整参考示例

```jsonc
{
  "FileBox": {
    "StateMachine": {
      "ThrowOnGuardDenied": false,          // bool, 默认 false
      "FailFastOnObserverError": false,     // bool, 默认 false
      "TransitionTimeout": "00:00:30",      // TimeSpan, 默认 30s
      "AuditDeniedTransitions": true,       // bool, 默认 true
      "MaxOptimisticRetries": 3,            // int, 默认 3
      "RetryBackoff": "00:00:00.050"        // TimeSpan, 默认 50ms
    },
    "Storage": {
      "HashAlgorithm": "SHA-256",
      "Deduplication": "Global",            // Global|NamespaceScoped|Disabled
      "VerifyAfterWrite": false,
      "Chunking": { "Enabled": true, "ChunkSizeBytes": 8388608 },
      "Pools": [
        { "PoolId": "hot-1", "RootPath": "C:\\filebox-data\\hot", "Tier": "Hot", "Enabled": true },
        { "PoolId": "cold-1", "RootPath": "C:\\filebox-data\\cold", "Tier": "Cold", "Enabled": true }
      ],
      "DefaultWrite": { "Tier": "Hot", "PoolId": null, "BandwidthLimit": null }
    },
    "Index": {
      "Consistency": "Strong",              // Strong|Eventual
      "DefaultPageSize": 100,
      "MaxPageSize": 1000,
      "Features": {
        "EnablePrefix": true, "EnableTagInverted": true, "EnableAttribute": true,
        "EnableTierBitmap": true, "EnableStateBitmap": true,
        "EnableTimeIndex": true, "EnableSizeIndex": true
      },
      "Sharding": { "Enabled": false, "ShardCount": 1 },
      "Store": { "Provider": "sqlite", "ConnectionString": null, "CacheSizeMb": 64, "SyncWrites": true }
    },
    "Backup": {
      "MaxConcurrency": 4, "BandwidthLimit": 0, "MaxRetries": 3,
      "Targets": [
        { "TargetId": "local-primary", "Type": "local", "RootPath": "C:\\filebox-backups", "Enabled": true }
      ],
      "Schedules": [
        { "ScheduleId": "daily", "Cron": "0 2 * * *", "Kind": "Incremental", "TargetId": "local-primary", "Enabled": true }
      ]
    },
    "Cluster": {
      "Role": "Hybrid",                     // Coordinator|Storage|Index|Backup|Hybrid
      "NodeId": "node-a",
      "AdvertiseAddress": "localhost:0",
      "SeedNodes": [ "node-b:9000", "node-c:9000" ],
      "HeartbeatInterval": "00:00:05",
      "NodeTimeout": "00:00:30",
      "Thresholds": { "Warning": 0.70, "Critical": 0.85, "Full": 0.95 }
    },
    "Lifecycle": {
      "ScanInterval": "00:05:00", "ScanBatchSize": 1000,
      "ArchiveAfter": null, "DeleteAfter": null
    },
    "Observability": {
      "Logging": { "MinimumLevel": "Information" },
      "Metrics": { "Enabled": true, "Port": 9090 },
      "Tracing": { "Enabled": false }
    }
  }
}
```

---

## 6. 更进一步

- 了解分层与模块职责 → [架构与分层](Architecture)
- 对象生命周期转移详解 → [生命周期状态机](State-Machine)
- 存储 / 分块 / 内存优化 → [存储引擎](Storage-Engine)
- 索引查询与持久化 → [索引](Index)
- 回到 [Home](Home)