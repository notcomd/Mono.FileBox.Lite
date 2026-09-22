# 架构与分层

Mono.FileBox.Lite 采用 **L0–L5 分层**架构，**严格单向依赖向下**（L4→L3→L2→L1→L0，L0 零依赖），通过 `ProjectReference` 实现。

---

## 分层与模块

```
Layer        Project                            职责
─────────────────────────────────────────────────────────────────────
L0 抽象     Mono.FileBox.Lite.Abstractions      纯接口与模型，零依赖
L1 存储     Mono.FileBox.Lite.Storage           内容寻址、分块、读写、清除、磁盘选择、IO 管线
L3 状态机   Mono.FileBox.Lite.StateMachine      对象生命周期状态机引擎
L2 子系统   Mono.FileBox.Lite.Subsystems        默认/NoOp 实现、事件、生命周期、索引写入
L2 索引     Mono.FileBox.Lite.Index             结构化索引与查询引擎、JSON 持久化
L2 备份     Mono.FileBox.Lite.Backup            备份
L2 集群     Mono.FileBox.Lite.Cluster           集群协调、扩容/缩容
L2 配置     Mono.FileBox.Lite.Configuration     配置校验/变更通知
L4 用例     Mono.FileBox.Lite.UseCases          用例编排（带回滚）
装配         Mon.FileBox.Lite.DependencyInjection 全部 AddMonoFileBoxLite* 扩展
─────────────────────────────────────────────────────────────────────
L5 示例     samples/Mono.FileBox.Lite.Sample    大文件上传压测 + 资源采样
测试        tests/…                             xUnit 测试
```

依赖方向：`UseCases(L4) → StateMachine(L3) → {Subsystems, Index, Backup, Cluster, Configuration}(L2) → Storage(L1) → Abstractions(L0)`。

---

## 每层职责

### L0 · Abstractions（零依赖）

纯接口与模型：`IObjectWriter/IObjectReader`、`IPhysicalDevice`、`IDiskSelector/IIOPipeline`、`IDistributedLock/ILifecyclePolicy`、`IIndexReader/IIndexWriter/IEntryStore`、`IBackupTarget`、`IHashRing/IClusterTopology`、用例 Command/Result、全部 `*Options` 配置模型、枚举（`ObjectState/ObjectTrigger/StorageTier/DeduplicationMode/…`）。

### L1 · Storage

- `Sha256ObjectWriter`：流式 SHA-256 + 全局去重，落盘经 `IDiskSelector → IIOPipeline → IPhysicalDevice`。
- `StorageObjectReader`：支持 offset/length 范围读；分块对象跨块区间自动拼接。
- 物理块寻址：`blocks/{hash[0:2]}/{hash[2:4]}/{hash}`。
- `LocalFileSystemDevice`：磁盘落地实现（路径即文件路径）。
- `BufferedIOPipeline`：有界缓冲（ArrayPool）+ 流式，内存 O(1)。

### L3 · StateMachine

`DefaultObjectStateMachine.FireAsync` 顺序：guard → 前向 observer → action → 提交状态 → 持久化 → 后向 observer。

### L2 · Subsystems（默认/NoOp 实现）

| 抽象 | 默认实现 |
|------|----------|
| `IDistributedLock` | `NoOpDistributedLock`（恒 true，兼作 IGuard）|
| `ILifecyclePolicy` | `NeverExpirePolicy`（恒 false，兼作 IGuard）|
| `IContentModerator` | `PassThroughModerator`（通过，兼作 ITransitionAction）|
| `IEventBus` | `NullEventBus`（丢弃，兼作 ITransitionObserver）|
| `ITransitionLogger` | `NullLogger`（丢弃，兼作 ITransitionObserver）|
| `ILeaderElection` | `SingleNodeLeaderElection`（恒本节点）|
| `IIndexWriter` | `DefaultIndexWriter` + `IndexStateSyncObserver` |

### L2 · Index

- `IIndexReader` 由 `QueryPlanner`（7 个 Provider 挑驱动扫描）+ `QueryExecutor`（过滤、稳定排序、游标分页）组成。
- `IndexMaintainer`：`VerifyAsync` 以物理块为权威检出缺失/孤儿；`RepairAsync` 清理；`RebuildAsync` 重建。
- 持久化：`JsonFileIndexStore` + `HotCachingIndexStore`，见 [索引](Index)。

### L2 · Backup / Cluster / Configuration

- 备份：共享块池 + 全量/增量、manifest、恢复、校验。
- 集群：`ConsistentHashRing`、`RegistryBackedTopology`、扩容/缩容、`ICapacityMonitor` 水位。
- 配置：`FileBoxOptions` 模型 + `FileBoxOptionsValidator` + `DefaultOptionsChangeNotifier` 热重载。

### L4 · UseCases（编排 + 回滚）

- `PutObjectUseCase` 走 `Put→Index→Audit→Publish`，失败逆序回滚。
- `GetObjectUseCase` 是纯查询，不触发转移。

### 装配 · DependencyInjection

`AddMonoFileBoxLite`（核心 + 完整转移表 + NoOp 默认）与 `AddMonoFileBoxLiteStorage/Index/Backup/Scaling/UseCases`，并暴露 Fluent Builder 便于替换默认实现。装配方式和 appsettings.json 配置见 [快速开始](Getting-Started)。

---

## NuGet 包映射

| 包 | 层 |
|------|------|
| `Mono.FileBox.Lite.Abstractions` | L0 |
| `Mono.FileBox.Lite.Storage` | L1 |
| `Mono.FileBox.Lite.StateMachine` | L3 |
| `Mono.FileBox.Lite.Subsystems` / `Index` / `Backup` / `Cluster` / `Configuration` | L2 |
| `Mono.FileBox.Lite.UseCases` | L4 |
| `Mono.FileBox.Lite.DependencyInjection` | 装配，依赖其余 9 包 |

**推荐**：只需引用 `Mono.FileBox.Lite.DependencyInjection`。

---

- 回到 [Home](Home) · [快速开始](Getting-Started)