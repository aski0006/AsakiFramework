# ALog 架构修复：解决循环依赖

## 问题描述

**原始问题：**
```
ALog (Asaki.Core) ──► ALogUnityBridge (Asaki.Unity)
        ▲                      │
        └──────────────────────┘
        
Asaki.Unity 依赖 Asaki.Core，导致循环依赖！
```

## 解决方案：接口 + 注册模式

**修复后：**
```
┌─────────────────────────────────────────────────────────────┐
│                        Asaki.Core                           │
│  ┌──────────────┐        ┌──────────────────────┐          │
│  │    ALog      │◄───────│ ALogBridgeManager    │          │
│  └──────────────┘        └──────────────────────┘          │
│         │                          ▲                        │
│         │                  注册/获取                          │
│         │                          │                        │
│         │                  ┌───────┴───────┐                │
│         │                  │ IALogUnity    │                │
│         │                  │ Bridge (接口) │                │
│         │                  └───────┬───────┘                │
│         │                          │                        │
│         │          实现            │                        │
│         │                          │                        │
└─────────┼──────────────────────────┼────────────────────────┘
          │                          │
          │ 依赖（单向）              │
          ▼                          ▼
┌─────────────────────────────────────────────────────────────┐
│                       Asaki.Unity                           │
│  ┌──────────────────────────────────────────────────────┐  │
│  │            ALogUnityBridge                           │  │
│  │  - 实现 IALogUnityBridge                             │  │
│  │  - 在 InitializeOnLoadMethod 中自注册                 │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## 关键改动

### 1. Core 层新增接口和管理器

**IALogUnityBridge.cs** (新增)
```csharp
// 位于 Asaki.Core - 无外部依赖
public interface IALogUnityBridge { ... }

public static class ALogBridgeManager 
{
    public static void RegisterBridge(IALogUnityBridge bridge) { ... }
    internal static IALogUnityBridge GetBridge() { ... }
}
```

### 2. ALog 使用桥接管理器

**ALog.cs** (修改)
```csharp
// 移除：using Asaki.Unity.Logging;
// 改为使用 Core 层内部的管理器
ALogBridgeManager.GetBridge()?.ForwardToUnityConsole(...);
```

### 3. Unity 层实现并注册

**ALogUnityBridge.cs** (修改)
```csharp
// 实现 Core 层的接口
public class ALogUnityBridge : IALogUnityBridge { ... }

// 自注册
[InitializeOnLoadMethod]
private static void Initialize()
{
    _instance = new ALogUnityBridge();
    ALogBridgeManager.RegisterBridge(_instance);
}
```

## 依赖关系验证

| 文件 | 所在程序集 | 引用 | 状态 |
|------|----------|------|------|
| `IALogUnityBridge.cs` | Asaki.Core | 无外部依赖 | ✅ |
| `ALog.cs` | Asaki.Core | `ALogBridgeManager` (内部) | ✅ |
| `ALogUnityBridge.cs` | Asaki.Unity | `IALogUnityBridge`, `ALogBridgeManager` | ✅ |

**结果：** Asaki.Core 不再依赖 Asaki.Unity，循环依赖已解除！

## 初始化流程

```
Unity Editor 启动
    │
    ├── [InitializeOnLoadMethod] ALogUnityBridge.Initialize()
    │       │
    │       └── ALogBridgeManager.RegisterBridge(_instance)
    │
    └── 用户调用 ALog.Info()
            │
            ├── ALogBridgeManager.GetBridge()?.ForwardToUnityConsole()
            │       │
            │       └── (如果已注册) 输出到 Unity Console
            │
            └── 继续原有流程 (Aggregator → FileWriter)
```

## 优势

1. **无循环依赖** - Core 层完全独立
2. **可测试性** - 可为 IALogUnityBridge 创建 mock 实现
3. **可扩展性** - 可添加其他桥接实现（如自定义 Console 窗口）
4. **向后兼容** - 原有代码无需改动，未注册桥接器时静默跳过
