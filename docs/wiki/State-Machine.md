# 生命周期状态机

每个存储对象是一条**有限状态机**实例，状态转移受控、可编排、可回滚。

---

## 状态与触发

- **状态** `ObjectState`：`Pending, Stored, Indexed, Audited, Available, Archived, Expired, Deleted, Purged`
- **触发** `ObjectTrigger`：`Put, Index, Audit, Publish, Archive, Restore, Expire, Delete, Purge`

---

## 转移表

```
Pending ─Put→ Stored ─Index→ Indexed ─Audit→ Audited ─Publish→ Available

Available ─Archive→ Archived ─Restore→ Available
Available ─Expire→   Expired
Available ─Delete→   Deleted     Archived ─Delete→ Deleted
Deleted  ──/Expired ─Purge→ Purged
```

完整默认转移表（`AddDefaultObjectTransitions`）：

| 触发 | 源状态 | 目标状态 | guard / action / observer |
|------|--------|----------|---------------------------|
| Put | Pending | Stored | Guard:`IDistributedLock`; Do:`IObjectWriter`; Observe:`IEventBus` |
| Index | Stored | Indexed | Do:`IIndexWriter`; Observe:`IEventBus` |
| Audit | Indexed | Audited | Do:`IContentModerator`; Observe:`IndexStateSyncObserver` |
| Publish | Audited | Available | Observe:`ITransitionLogger`,`IndexStateSyncObserver` |
| Archive | Available | Archived | Guard:`ILifecyclePolicy`; Observe:`IndexStateSyncObserver` |
| Restore | Archived | Available | Observe:`IndexStateSyncObserver` |
| Expire | Available | Expired | Guard:`ILifecyclePolicy`; Observe:`IndexStateSyncObserver` |
| Delete | Available | Deleted | Observe:`IndexStateSyncObserver` |
| Delete | Archived | Deleted | Observe:`IndexStateSyncObserver` |
| Purge | Deleted | Purged | Do:`IPhysicalEraser`; Observe:`IEventBus` |
| Purge | Expired | Purged | Do:`IPhysicalEraser`; Observe:`IEventBus` |

---

## 执行语义

`DefaultObjectStateMachine.FireAsync` 执行顺序：

1. **guard**（前置条件，如分布式锁、生命周期策略）
2. **前向 observer**（如审计、状态同步）
3. **action**（副作用，如写对象、写索引、物理清除）
4. **提交状态** 并 **持久化**
5. **后向 observer**

结果语义：

- 非法 `(trigger, state)` → 返回 `NotAllowed`，状态不变。
- guard 拒绝 → 返回 `Denied`（默认不抛异常）。
- action/observer 异常 → 由 `FailFastOnObserverError` 决定是否快速失败。

---

## 自定义

### 自定义转移表

```csharp
var options = new FileBoxOptions(...);
services.AddMonoFileBoxLite(options, machine: regs =>
{
    // 在默认表基础上追加或覆盖
    // regs.On(ObjectTrigger.X).From(ObjectState.Y).To(ObjectState.Z)...
});
```

或用 `AddDefaultObjectTransitions(registry)` 打底后追加：

```csharp
var registry = new TransitionRegistry();
Mono.FileBox.Lite.DependencyInjection.FileBoxServiceCollectionExtensions
    .AddDefaultObjectTransitions(registry);
// 追加自定义转移
services.AddSingleton(registry);   // 再装配
```

---

## 角色实现约定

一个具体类型可同时实现「服务接口 + 角色接口」，例如：

- `NoOpDistributedLock : IDistributedLock, IGuard` —— 这是 `.Guard<IDistributedLock>()` 能工作的前提。
- `PassThroughModerator : IContentModerator, ITransitionAction`
- `NullLogger : ITransitionLogger, ITransitionObserver`

---

## 默认实现一览

| 抽象 | 默认实现 |
|------|----------|
| `IDistributedLock` | `NoOpDistributedLock`（恒 true）|
| `ILifecyclePolicy` | `NeverExpirePolicy`（恒 false）|
| `IContentModerator` | `PassThroughModerator`（放行）|
| `IEventBus` | `NullEventBus`（丢弃）|
| `ITransitionLogger` | `NullLogger`（丢弃）|
| `ILeaderElection` | `SingleNodeLeaderElection`（恒本节点）|

默认均为**单节点友好**实现；分布式场景通过 Builder 替换为真实实现。

---

- 回到 [Home](Home) · [架构与分层](Architecture) · [存储引擎](Storage-Engine)