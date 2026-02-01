# Issue #3: [Feature] Smart Pool Governance: Auto-Shrink & LRU Eviction (对象 池智能治理：自动收缩与LRU淘汰)

**状态**: OPEN  
**作者**: aski0006 (Asaki0019)  
**创建时间**: 2026-01-31  
**更新时间**: 2026-01-31  
**URL**: https://github.com/aski0006/AsakiFramework/issues/3

---

## 1. 背景与痛点 (Problem Statement)

目前的 `AsakiPoolService` 虽然实现了基础的 `Get/Return` 功能，且 `AsakiGenericPool` 内部已存在 `Shrink(int targetSize)` 方法，但缺乏**自动化的治理机制**。

* **内存风险**：在高负载场景（如战斗、特效密集）过后，对象池中可能残留大量不再使用的闲置对象（例如缓存了 500 个 弹孔特效）。这些对象长期占用内存，导致内存水位只增不减，存在 OOM (Out Of Memory) 风险。
* **缺少策略**：目前必须由开发者手动调用 `Clear` 或 `Dispose`，这违背了"自动化管理"的初衷。

**相关代码引用：**
* 池实现：`Assets/Asaki/Core/Pooling/AsakiGenericPool.cs` (已有 Shrink 方法)
* 服务实现：`Assets/Asaki/Core/Pooling/AsakiPoolService.cs` (缺乏 Tick 或治理入口)
* 配置类：`Assets/Asaki/Core/Pooling/AsakiPoolConfig.cs` (缺乏 TTL/收缩阈值参数)

---

## 2. 解决方案提案 (Proposed Solution)

在 Service 层和 Config 层引入**"对象池治理 (Pool Governance)"**机制，实现基于压力（Memory Pressure）和时间（TTL/LRU）的自动收缩。

**核心功能点：**

### 2.1 配置扩展 (Config Expansion)

在 `AsakiPoolConfig` 中新增治理参数：

```csharp
public bool EnableAutoShrink = true;  // 开关
public float CheckInterval = 30f;     // 检查间隔（秒）
public float IdleTimeout = 60f;       // 对象闲置多久视为"过期" (TTL)
public int KeepMinSize = 5;           // 收缩时的保底数量
public float ShrinkRatio = 0.5f;      // 每次收缩的比例（平滑释放）
```

### 2.2 LRU 淘汰策略 (LRU Eviction)
* 修改 `GenericPool` 的存储结构或逻辑。
* 给池内对象（Stack/List）增加元数据 `LastUsedTime`。
* 在执行 `Shrink` 时，优先销毁 `Time.time - LastUsedTime > IdleTimeout` 的对象。

### 2.3 自动触发机制 (Automatic Trigger)
* 让 `AsakiPoolService` 实现 `IAsakiTickable`（利用 Simulation 模块）。
* 定时轮询所有 Active Pools，触发各自的治理逻辑。
* **低内存响应**：监听 `Application.lowMemory` 事件，当系统内存告急时，强制触发所有池子的 `Shrink(KeepMinSize)`，进行紧 急"瘦身"。

---

## 3. 预期收益 (Benefits)

* **内存安全**：自动释放长期不用的资源，显著降低游戏长时间运行后的内存占用 (PSS)。
* **性能平滑**：通过分帧收缩或空闲时收缩，避免 GC 尖峰。
* **鲁棒性**：在低端机上通过响应内存警告，减少 Crash 率 。

---

## 4. 下一步计划 (Next Steps)

- [ ] 修改 `AsakiPoolConfig.cs`，增加 TTL 和收缩相关字段。
- [ ] 升级 `AsakiGenericPool.cs`，实现基于时间的淘汰判断逻辑。
- [ ] 更新 `AsakiPoolService.cs`，实现 `IAsakiTickable` 接口并接入 `Application.lowMemory`。
- [ ] (可选) 在 `AsakiSmartPoolDebuggerWindow` 中增加"强制收缩" 按钮以便测试。
