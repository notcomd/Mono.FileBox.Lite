# Mono.FileBox.Lite 开发文档

> 版本：v1.0（实现随附文档定稿）
> 定位：以对象生命周期状态机为核心的**文件对象存储引擎**（.NET 库项目）
> 本文档面向继承与二次开发者，说明代码结构、模块职责、构建/运行/测试方式与已知限制。

---

## 1. 概述

Mono.FileBox.Lite 是一个包含 **L0–L5 分层** 的文件对象存储引擎类库。每个存储对象（blob）是一个生命周期状态机实例，从写入到清除走 `Pending → … → Purged` 的有限状态与受控转移。

核心能力：

| 能力 | 对应模块 |
|------|----------|
| 文件对象存储引擎 | 内容寻址（SHA-256）、去重、流式落盘、读取、清除 |
| 对象内固定分块 | 大对象切 8 MiB 块 + 对象级 manifest（可选开关） |
| 对象索引 | 多维查询（前缀/标签/层级/状态/时间/大小）、游标分页、一致性维护 |
| 对象备份 | 全量/增量共享块池、恢复、校验 |
| 集群 | 一致哈希、拓扑、扩容/缩容、容量监控、分层 |

不在范围内（由上层负责）：租户/RBAC、认证授权、计费审计。

## 2. 技术栈

- 核心库：.NET Standard 2.0（L0–L4 全部类库）
- 示例 / 测试：.NET 9.0
- 测试框架：xUnit
- 依赖：`Microsoft.Extensions.DependencyInjection`、`System.Text.Json`、`System.Memory`

## 3. 目录结构与分层

```
src/
├── Mono.FileBox.Lite.Abstractions/       # L0  纯接口与模型（零依赖）
├── Mono.FileBox.Lite.StateMachine/       # L3  状态机引擎
├── Mono.FileBox.Lite.Storage/            # L1  内容寻址存储（含分块）
├── Mono.FileBox.Lite.Subsystems/         # L2  默认/NoOp 实现、事件、生命周期、索引写入
├── Mono.FileBox.Lite.Index/              # L2  结构化索引与查询引擎
├── Mono.FileBox.Lite.Backup/             # L2  备份
├── Mono.FileBox.Lite.Cluster/            # L2  集群协调
├── Mono.FileBox.Lite.Configuration/      # L2  配置校验/通知
├── Mono.FileBox.Lite.UseCases/           # L4  用例编排
├── Mono.FileBox.Lite.DependencyInjection/# —   全部 AddMonoFileBoxLite* 装配扩展
samples/
└── Mono.FileBox.Lite.Sample/             # L5  大文件上传压测 + 资源采样
tests/
└── Mono.FileBox.Lite.Index.Tests/        #     索引/端到端 xUnit 测试
Mono.FileBox.Lite.slnx                    # 解决方案
```

依赖约束：**分层单向向下**（L4→L3→L2→L1→L0，L0 零依赖），采用 `ProjectReference` 严格实现。

## 4. 构建 / 运行 / 测试

```bash
# 编译整个解决方案
dotnet build Mono.FileBox.Lite.slnx -c Release

# 运行示例（大文件上传压测，资源采样：工作集/托管堆/CPU/磁盘吞吐）
# 可调体量：MONOFILEBOX_SAMPLE_SIZE_MB=2048
dotnet run --project samples/Mono.FileBox.Lite.Sample/Mono.FileBox.Lite.Sample.csproj -c Release

# 运行索引测试
dotnet test tests/Mono.FileBox.Lite.Index.Tests/Mono.FileBox.Lite.Index.Tests.csproj -c Release
```

> 注意：首次 `dotnet test` 的 testhost 启动较慢；若遇到卡住，先杀掉残留的
> `testhost.exe`/`vstest.console` 进程再重跑。单测在 51 ms 内通过。

### 4.1 分块开关

对象内分块默认**关闭**（每个对象 = 单个物理块）。开启：

```csharp
options.Storage.Chunking.Enabled = true;      // ChunkSizeBytes 默认 8 MiB
```

开启后，对象 > 一个 chunk 会被切成若干 chunk 块（按 chunk 哈希寻址）并写一个对象级 manifest；对象 ≤ 一个 chunk 仍走整对象单块。块为对象私有、不跨对象共享。

## 5. 核心设计

### 5.1 状态机（L3）

- `ObjectState` / `ObjectTrigger` 枚举；`TransitionRegistry.On(t).From(a).To(b).Guard<..>().Do<..>().Observe<..>()` 注册转移。
- `DefaultObjectStateMachine.FireAsync` 执行顺序：guard → 前向 observer → action → 提交状态 → 持久化 → 后向 observer。
- 非法 `(trigger, state)` 返回 `NotAllowed`，状态不变；guard 拒绝返回 `Denied`（默认不抛异常）。
- 角色实现约定：一个具体类型可同时实现「服务接口 + 角色接口」，例如 `NoOpDistributedLock : IDistributedLock, IGuard`，这是 `.Guard<IDistributedLock>()` 能工作的前提。

### 5.2 存储引擎（L1）

- `Sha256ObjectWriter`：流式 SHA-256（可寻流原位哈希、非可寻流暂存临时文件），全局内容寻址去重，落盘经 `IDiskSelector → IIOPipeline → IPhysicalDevice`。
- 内存行为：**有界缓冲（ArrayPool）+ 流式**，写入路径不再整块物化 payload；1 GiB 上传驻留内存 ~35–50 MiB（O(1)）。
- `StorageObjectReader`：支持 offset/length 范围读；分块对象跨块区间自动拼接。
- `StoragePhysicalEraser`：Purge 时删除物理块（分块对象则删各块 + manifest）。

### 5.3 索引（L2）

- `IIndexWriter`（Subsystems）经 `IEntryStore` 持久化 `IndexEntry`；`IndexStateSyncObserver` 在收藏转移后同步 `State/Tier`。
- `IIndexReader`（Index）由 `QueryPlanner`（7 个 Provider 挑驱动扫描）＋`QueryExecutor`（过滤、稳定排序、游标分页）组成。
- `IndexMaintainer`：VerifyAsync 以物理块为权威检出缺失/孤儿；RepairAsync 清理；RebuildAsync 重建。

### 5.4 备份 / 集群 / 配置（L2）

- 备份：共享块池 + 全量/增量（增量 = 父清单 ∪ 新增）、manifest、恢复、Verify；目标支持本地目录与远端（S3 兼容占位）。
- 集群：`ConsistentHashRing`、`RegistryBackedTopology`、扩容/缩容、`ICapacityMonitor` 水位阈值。
- 配置：`FileBoxOptions` 模型 + `FileBoxOptionsValidator`（内置校验规则）、`DefaultOptionsChangeNotifier` 热重载。

### 5.5 用例层与装配（L4 / DI）

- 用例层把「转移序列编排 + 回滚」从状态机剥离：`PutObjectUseCase` 走 `Put→Index→Audit→Publish`，失败逆序回滚；`GetObjectUseCase` 是纯查询，不触发转移。
- DI 扩展：`AddMonoFileBoxLite`（核心 + 完整转移表 + NoOp 默认）与 `AddMonoFileBoxLiteStorage/Index/Backup/Scaling/UseCases`，并暴露 Fluent Builder（`UseXxx<T>()`）便于替换默认实现。

## 6. 默认实现一览

| 抽象 | 默认实现 |
|------|----------|
| `IDistributedLock` | `NoOpDistributedLock`（恒 true，兼作 IGuard） |
| `ILifecyclePolicy` | `NeverExpirePolicy`（恒 false，兼作 IGuard） |
| `IContentModerator` | `PassThroughModerator`（通过，兼作 ITransitionAction） |
| `IEventBus` | `NullEventBus`（丢弃，兼作 ITransitionObserver） |
| `ITransitionLogger` | `NullLogger`（丢弃，兼作 ITransitionObserver） |
| `ILeaderElection` | `SingleNodeLeaderElection`（恒本节点） |
| `IObjectWriter/Reader` | `Sha256ObjectWriter` / `StorageObjectReader` |
| `IDiskSelector/IOPipeline/Device` | `ConsistentHashDiskSelector` / `BufferedIOPipeline` / `LocalFileSystemDevice` |
| `IIndexWriter/Reader/Maintainer` | `DefaultIndexWriter` / `DefaultIndexReader` / `IndexMaintainer` |

## 7. 内存优化要点（物化 vs 流式）

- 峰值内存的量级由「是否物化整块数据」决定：物化 = O(文件大小)，流式/惰性 = O(1)。
- 本引擎写入路径已流式化（`Sha256ObjectWriter` + `WriteBlockStreamAsync` 64 KiB 池化缓冲）。
- 压测用「可回放生成流 `PatternStream`」而非 `new byte[size]`，从而证明引擎自身内存与文件大小无关。
- 实现层面受限：netstandard2.0 的 `Stream.Read`/`SHA256.TransformBlock` 仅接受 `byte[]`，故用 `ArrayPool` 复用来替代 Span 切片。

## 8. 测试现状

- `tests/Mono.FileBox.Lite.Index.Tests`：
  - `IndexQueryTests`：前缀/标签/层级/状态/时间/大小、排序、命名空间隔离、游标分页稳定性。
  - `IndexMaintainerTests`：Verify 检出缺失块、Repair 清理孤儿、Rebuild 清空。
  - `EndToEndIndexPipelineTests`：经状态机 Put 写入索引 → `IIndexReader` 查询 → 状态同步到 `Available`、分页全量无重复。
- 已验证：单测 51 ms 通过；解决方案 0 警告 0 错误。

## 9. 已知限制 / Roadmap

- 索引采用内存实现（`InMemoryEntryStore` / `InMemoryOrderedKeyValueStore`），未接 SQLite 持久化。
- 备份 GC（`IBackupGarbageCollector`）为 NoOp，块回收需扫描所有清单引用。
- 分块为「对象私有」，**未实现跨对象共享块**与块级引用计数（有意保留为后续增强）。
- `ConsistentHashRing` 虚拟节点、再均衡带宽整形、索引分片在线迁移、S3-backed `IBackupTarget` 等列为 Roadmap。

## 10. Git 历史与提交规约

按模块提交（每模块一次本地 commit，消息前置模块编号）：

```
34c324b 基线清理（移除旧 skills 项目）
5ae4262 module1: L0 Abstractions
16047ef module2: L3 StateMachine
2dcd73d module3: L1 Storage
e4c3ea5 module4: L2 Subsystems
aca3e20 module5: L2 Index
51b01b2 module6: L2 Backup
9288cdc module7: L2 Cluster
95c035a module8: L2 Configuration
c3f0a68 module9: L4 UseCases
5d7f5fd module10: DependencyInjection
18c9dd8 module11: L5 Sample（端到端验证）
72661e9 sample: 大文件上传资源采样压测
3eedd9f storage: 流式写入路径
3025a82 storage+sample: ArrayPool/生成流（内存 O(1)）
9695551 storage: 对象内固定分块
```

---

*文档结束。*