# 索引

结构化索引与查询引擎：多维查询、游标分页、一致性维护、JSON 文档持久化 + 热点缓存。

---

## 索引构成

- **`IIndexWriter`**（Subsystems）：经 `IEntryStore` 持久化 `IndexEntry`。
- **`IIndexReader`**（Index）：`QueryPlanner`（7 个 Provider 挑驱动扫描）+ `QueryExecutor`（过滤、稳定排序、游标分页）。
- **`IndexStateSyncObserver`**：在收藏转移后同步 `State/Tier` 到索引。
- **`IndexMaintainer`**：`VerifyAsync`（以物理块为权威检出缺失/孤儿）、`RepairAsync`（清理）、`RebuildAsync`（重建）。

---

## 支持的查询维度

`IndexQuery` 支持按以下维度组合过滤：

- **前缀** `KeyPrefix`
- **标签** `Tags`
- **层级 / 命名空间** `NamespaceId`
- **状态** `State`
- **时间范围**（创建/修改时间）
- **大小范围**
- **排序**（稳定排序）
- **游标分页** `PageRequest`（`Size` + 游标）

### 查询示例

```csharp
var index = provider.GetRequiredService<IIndexReader>();
var page = await index.QueryAsync(new IndexQuery
{
    NamespaceId = "ns1",
    KeyPrefix = "/uploads/",
    Tags = new Dictionary<string, string> { ["dept"] = "finance" },
    Page = new PageRequest { Size = 50 }
});

foreach (var item in page.Items)
    Console.WriteLine($"{item.ObjectKey}  hash={item.ContentHash}  state={item.State}");
```

---

## 持久化：默认内存 vs JSON 文档

默认索引为**内存实现**（`InMemoryEntryStore`），掉电不保存。需要本地持久化时，用 `JsonFileIndexStore` + 热点缓存。

### 切换（DI）

```csharp
services.AddMonoFileBoxLiteIndex(builder =>
{
    builder.UseJsonFileEntryStore(@"C:\filebox-data\index.json");
});
```

### JsonFileIndexStore

- **单一 JSON 文档**为权威存储；改动整体**原子重写**（临时文件 + move）。
- 读取按需解析，非常适合低频/冷数据，内存极省。
- 保留了 `IEntryStore` DB 接口，未来可无缝替换为 SQLite 实现。
- 数值类型精确保留（`long` 不会被提升成 `double`）。

### HotCachingIndexStore（装饰器）

包在任一种 `IEntryStore` 之上：

- 按读取计数把高频条目**载入内存**（达到 `PromotionThreshold` 后提升）。
- 命中即返回，**不再解析索引文档**。
- 超过 `Capacity` 按 **LRU** 淘汰。
- 热点集合落到旁路 `.hot` 文件，冷启动直接预载，同样免解析。

**默认参数**：`Capacity = 512`，`PromotionThreshold = 3`，`PersistHot = true`。

### 精细配置

```csharp
builder.UseJsonFileEntryStore(@"C:\filebox-data\index.json",
    new HotIndexOptions { Capacity = 512, PromotionThreshold = 3, PersistHot = true });
```

---

## 一致性

- 默认 `Strong` 一致性（`IndexConsistencyMode.Strong`）。
- 收藏转移后由 `IndexStateSyncObserver` 同步 `State/Tier`，保证索引与对象状态一致。

---

## 索引相关配置

```jsonc
"Index": {
  "Consistency": "Strong",            // Strong|Eventual
  "DefaultPageSize": 100,
  "MaxPageSize": 1000,
  "Features": { "EnablePrefix": true, ... },
  "Sharding": { "Enabled": false, "ShardCount": 1 },
  "Store": { "Provider": "sqlite", "ConnectionString": null, "CacheSizeMb": 64, "SyncWrites": true }
}
```

---

- 回到 [Home](Home) · [快速开始](Getting-Started) · [存储引擎](Storage-Engine)