# ALog & LogDashboard 改进方案

## 改进概述

本次改进解决了以下核心问题：

1. ✅ **Unity 控制台集成** - ALog 日志现在同时输出到 Unity 控制台，支持双击跳转
2. ✅ **实时刷新** - Dashboard 刷新间隔可配置，最高支持 60fps
3. ✅ **完整堆栈** - Unity 控制台显示完整调用链，不再局限于聚合后的信息
4. ✅ **灵活配置** - 可通过配置或菜单快速开关控制台输出

---

## 新增文件

```
Assets/Asaki/Unity/Logging/
├── ALogUnityBridge.cs              # Unity 控制台桥接器

Assets/Asaki/Editor/Debugging/
├── ALogUnityBridgeToggle.cs        # 菜单控制
```

## 修改文件

```
Assets/Asaki/Core/Logging/
├── ALog.cs                         # 添加控制台桥接调用

Assets/Asaki/Core/Configs/
├── AsakiLogConfig.cs               # 添加编辑器配置项

Assets/Asaki/Editor/Debugging/
├── AsakiLogDashboard.cs            # 优化刷新逻辑
```

---

## 使用指南

### 1. 基本使用（无需改动）

```csharp
// 原有代码无需任何改动
ALog.Info("玩家登录", new { UserId = 123 });
ALog.Warn("配置缺失", configName);
ALog.Error("网络超时", exception);
```

### 2. Unity 控制台跳转

```csharp
// 现在在 Unity 控制台中：
// 1. 可以看到 [ALog] 前缀的日志
// 2. 双击日志行即可跳转到源代码
// 3. 完整堆栈跟踪自动显示在控制台详情中
```

### 3. 配置选项

在 `AsakiConfig` 的 `LogConfig` 中：

```csharp
var config = new AsakiLogConfig
{
    // 原有配置...
    MinLogLevel = AsakiLogLevel.Debug,
    MaxFileSizeKB = 2048,
    
    // 新增配置：
    OutputToUnityConsole = true,       // 是否输出到 Unity 控制台
    DashboardRefreshInterval = 0.05f   // Dashboard 刷新间隔（秒）
};
```

### 4. 菜单快捷操作

```
Asaki/
├── Open Log Dashboard (Ctrl+Alt+L)    # 快速打开 Dashboard
├── ALog Output to Unity Console       # 开关控制台输出
└── Log Dashboard V3                   # 原 Dashboard 入口
```

---

## 架构对比

### 改进前

```
User Code → ALog → Aggregator ─┬─► FileWriter (异步写入文件)
                               └─► Dashboard (LateUpdate刷新，有延迟)
                                
❌ Unity Console 完全隔离
❌ 无法双击跳转
❌ 堆栈信息在聚合后丢失
```

### 改进后

```
User Code ─┬─► ALogUnityBridge ──► Unity Console (实时，双击跳转)
           │                              ▲
           │                              │ 完整堆栈
           │
           └─► ALog ──► Aggregator ─┬─► FileWriter (异步写入)
                                     └─► Dashboard (可配置刷新率)

✅ Unity Console 实时同步
✅ 原生双击跳转支持
✅ Dashboard 保持独立作为历史分析工具
```

---

## 性能影响

| 场景 | 开销 | 说明 |
|------|------|------|
| Editor 下日志 | +0.1ms | 额外的 Unity Console 输出 |
| Release 构建 | 0 | 所有桥接代码被 `UNITY_EDITOR` 剔除 |
| Dashboard 刷新 | 可调 | 默认 20fps，可降至 1fps |

---

## 注意事项

### 关于堆栈信息

- **Unity 控制台**：显示完整调用堆栈，双击可跳转到源代码
- **LogDashboard**：显示聚合后的简化堆栈（用于重复日志计数）
- **文件日志**：保存完整堆栈信息供事后分析

### 关于刷新延迟

- **Unity 控制台**：实时（调用 Debug.Log 立即显示）
- **LogDashboard**：可配置（默认 0.05s = 20fps）

### 建议工作流

1. **开发阶段**：启用 Unity 控制台输出，关闭 Dashboard
2. **问题排查**：开启 Dashboard，设置较低刷新率观察趋势
3. **发布后**：Dashboard 自动不可用，依赖文件日志

---

## 回滚方案

如需恢复旧版本行为，在 `AsakiLogConfig` 中设置：

```csharp
OutputToUnityConsole = false
```

或在菜单中取消勾选 `Asaki/ALog Output to Unity Console`。

---

## 后续建议

1. **考虑移除 Dashboard？** - 不建议，它仍有用作历史日志分析工具的价值
2. **聚合是否必要？** - 对于文件写入是的，对于控制台输出不需要
3. **进一步优化**：考虑使用 Unity 的 `ILogHandler` 接口替代直接调用 Debug.Log
