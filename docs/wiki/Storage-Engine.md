# 存储引擎

内容寻址、全局去重、对象内分块、流式内存占用、物理块布局。

---

## 内容寻址与去重

- 对象地址即其 **SHA-256 哈希**，物理块按下式存放：

  ```
  blocks/{hash[0:2]}/{hash[2:4]}/{hash}
  ```

- 相同内容只落盘一次（`DeduplicationMode.Global` 默认全局去重）。
- `Sha256ObjectWriter` 边算哈希边流式写入，不把 payload 整块驻留内存——**内存 O(1)**。

---

## 硬件路径

写入/读取/删除都走统一抽象链：

```
调用方 → IObjectWriter / IObjectReader / IPhysicalEraser
       → IDiskSelector        (选择磁盘池)
       → IIOPipeline          (有界缓冲 IO 管线)
       → IPhysicalDevice      (物理落盘, 如 LocalFileSystemDevice)
```

- `ConsistentHashDiskSelector`：一致哈希选择磁盘池。
- `BufferedIOPipeline`：`ArrayPool<byte>` 复用缓冲，流式处理大文件。
- `LocalFileSystemDevice`：路径即本地文件路径，创建目录、写入、读取、删除、存在性检查。

---

## 对象内固定分块（默认关闭）

> **默认关闭**：每个对象 = 单个物理块。要开启，必须显式配置。

开启后，`> 8 MiB` 的对象按固定大小切块（每块以块哈希寻址），并写一个**对象级 manifest** 记录块顺序；对象 ≤ 一个 chunk 仍走整对象单块。块为**对象私有**，不跨对象共享（有意保留为后续增强）。

### 代码开启

```csharp
options.Storage.Chunking.Enabled = true;   // ChunkSizeBytes 默认 8 MiB
```

### 配置文件开启（appsettings.json）

```jsonc
"Storage": { "Chunking": { "Enabled": true, "ChunkSizeBytes": 8388608 } }
```

### 读取

`StorageObjectReader` 支持 offset/length 范围读；分块对象跨块区间自动拼接——对调用方透明。

### 清除

`StoragePhysicalEraser`：Purge 时删除物理块（分块对象则删各块 + manifest）。

---

## 内存优化（物化 vs 流式）

- 峰值内存的量级由「是否物化整块数据」决定：物化 = **O(文件大小)**，流式/惰性 = **O(1)**。
- 写入路径已流式化：`Sha256ObjectWriter` + `WriteBlockStreamAsync`（64 KiB 池化缓冲）。
- 压测使用「可回放生成流 `PatternStream`」而非 `new byte[size]`，证明引擎内存与文件大小无关。
- 实现受限：netstandard2.0 的 `Stream.Read`/`SHA256.TransformBlock` 仅接受 `byte[]`，故用 `ArrayPool` 复用替代 Span 切片。

**实测**：1 GiB 上传（内存 O(1)），驻留内存约 35–50 MiB。

---

## 相关配置项

`FileBoxOptions.Storage`：

| 键（appsettings） | 说明 | 默认 |
|------|------|------|
| `HashAlgorithm` | 哈希算法 | `SHA-256` |
| `Deduplication` | `Global`/`NamespaceScoped`/`Disabled` | `Global` |
| `VerifyAfterWrite` | 写入后校验 | `false` |
| `Chunking.Enabled` | 分块开关 | `false` |
| `Chunking.ChunkSizeBytes` | 每块字节数 | `8388608` |
| `Pools` | 磁盘池列表（`PoolId/RootPath/Tier/CapacityBytes/Priority/Enabled`）| 空 |
| `DefaultWrite` | 默认写入目标（`Tier/PoolId/BandwidthLimit`）| — |

---

## 使用示例

```csharp
var put = provider.GetRequiredService<IPutObjectUseCase>();
var res = await put.ExecuteAsync(new PutObjectCommand
{
    NamespaceId = "ns1",
    ObjectKey = "/big/blob.bin",
    Content = contentStream,             // 流式, 非整块内存
    ContentType = "application/octet-stream",
    Tags = new Dictionary<string, string> { ["size"] = "1024" }
});
```

---

- 回到 [Home](Home) · [快速开始](Getting-Started) · [索引](Index)