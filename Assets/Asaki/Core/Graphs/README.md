# Asaki Graph System

[![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity.com)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Asaki Graph System 是一个为 Unity 设计的可视化图系统框架，提供完整的节点编辑器、运行时执行引擎和黑板数据管理系统。适用于行为树、对话系统、技能流程、任务流程等需要可视化编程的场景。

## 📋 目录

- [概述](#概述)
- [功能特性](#功能特性)
- [架构设计](#架构设计)
- [核心组件](#核心组件)
- [快速开始](#快速开始)
- [创建自定义节点](#创建自定义节点)
- [运行时执行](#运行时执行)
- [编辑器扩展](#编辑器扩展)
- [最佳实践](#最佳实践)

## 🎯 概述

Asaki Graph System 基于 Unity 的 GraphView 和 UI Toolkit 构建，提供以下核心能力：

- **可视化节点编辑器**：基于 Unity GraphView 的专业级节点编辑体验
- **运行时执行引擎**：支持同步/异步节点执行，带取消令牌支持
- **黑板数据系统**：图级和全局变量管理，支持数据流连接
- **高性能架构**：O(1) 节点查找、类型缓存、对象池复用
- **可扩展设计**：通过特性标记和接口实现自定义节点和图类型

### 适用场景

| 场景 | 说明 |
|------|------|
| **AI 行为树** | 可视化编辑 NPC 行为逻辑，支持顺序、选择、并行等复合节点 |
| **对话系统** | 创建分支对话流程，支持条件判断和变量驱动 |
| **技能流程** | 定义技能释放流程，包含前摇、伤害判定、后摇等阶段 |
| **任务系统** | 可视化任务流程，支持条件检查和状态切换 |
| **事件系统** | 创建事件响应链，支持异步操作和错误处理 |

## ✨ 功能特性

### 核心功能

- ✅ **可视化节点编辑器**：拖拽创建节点，可视化连接端口
- ✅ **多态节点系统**：支持任意继承 `AsakiNodeBase` 的自定义节点
- ✅ **黑板变量系统**：图级和全局变量，支持拖拽生成 Get/Set 节点
- ✅ **数据流连接**：支持值端口（数据传递）和 Flow 端口（执行控制）
- ✅ **异步执行**：支持 `async/await` 模式的异步节点
- ✅ **运行时调试**：编辑器中实时高亮显示正在执行的节点
- ✅ **撤销/重做**：完整的 Undo 系统支持
- ✅ **类型安全**：编译时类型检查，避免运行时错误

### 编辑器特性

| 特性 | 说明 |
|------|------|
| 节点搜索窗口 | 空格键呼出，按图类型过滤节点 |
| 变量黑板面板 | 显示局部和全局变量，支持拖拽创建节点 |
| 端口类型检查 | 自动验证连接端口的类型兼容性 |
| 实时调试高亮 | 运行时高亮显示正在执行的节点 |
| 全局黑板编辑器 | 独立窗口管理跨图共享变量 |

## 🏗️ 架构设计

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Asaki Graph System                                │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                        Editor Layer                                 │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌───────────┐  │   │
│  │  │ GraphWindow │  │ GraphView   │  │ NodeView    │  │ Blackboard│  │   │
│  │  │ (编辑器窗口) │  │ (画布)      │  │ (节点视图)   │  │ (变量面板)│  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └───────────┘  │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                  │   │
│  │  │ SearchWindow│  │ PortView    │  │ Debugger    │                  │   │
│  │  │ (节点搜索)  │  │ (端口视图)   │  │ (调试器)    │                  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘                  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                      Runtime Layer                                  │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌───────────┐  │   │
│  │  │ GraphAsset  │  │ GraphRunner │  │ RuntimeCtx  │  │ Blackboard│  │   │
│  │  │ (图资源)    │  │ (执行器)    │  │ (运行时上下文)│  │ (黑板)    │  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └───────────┘  │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                  │   │
│  │  │ NodeBase    │  │ AsyncNode   │  │ VariableNode│                  │   │
│  │  │ (节点基类)  │  │ (异步节点)   │  │ (变量节点)  │                  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘                  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                      Data Layer                                     │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌───────────┐  │   │
│  │  │ Nodes       │  │ Edges       │  │ Variables   │  │ GlobalBB  │  │   │
│  │  │ (节点列表)  │  │ (边列表)    │  │ (变量列表)  │  │ (全局黑板)│  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └───────────┘  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 数据流架构

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              数据流示例                                      │
└─────────────────────────────────────────────────────────────────────────────┘

   ┌──────────────┐         ┌──────────────┐         ┌──────────────┐
   │ GetVariable  │────────▶│   MathAdd    │────────▶│ SetVariable  │
   │  (PlayerHP)  │  Value  │  (HP + 10)   │  Result │ (PlayerHP)   │
   └──────────────┘         └──────────────┘         └──────────────┘
          │                                               │
          │                                               ▼
          │                                         ┌──────────────┐
          │                                         │   NextNode   │
          │                                         └──────────────┘
          ▼
   ┌──────────────┐
   │  Blackboard  │
   │  [PlayerHP]  │
   └──────────────┘
```

## 🔧 核心组件

### 1. AsakiGraphAsset（图资源基类）

```csharp
// 创建自定义图类型
[CreateAssetMenu(menuName = "Asaki/Behavior Tree")]
public class BehaviorTreeGraph : AsakiGraphAsset
{
    // 行为树特定数据
    public float GlobalCooldown = 1.0f;
}
```

**核心功能：**
- 存储节点列表 (`Nodes`) 和边列表 (`Edges`)
- 管理图级变量 (`Variables`)
- 构建运行时拓扑缓存（O(1) 查询）
- 提供节点导航 API（`GetNextNode`、`GetNodeByGUID` 等）

### 2. AsakiNodeBase（节点基类）

```csharp
[Serializable]
public class LogNode : AsakiNodeBase
{
    public string Message = "Hello World";

    public override string Title => $"Log: {Message}";

    public override void OnCreated()
    {
        if (string.IsNullOrEmpty(GUID))
            GUID = System.Guid.NewGuid().ToString();
    }
}
```

**核心属性：**

| 属性 | 说明 |
|------|------|
| `Position` | 节点在编辑器中的位置 |
| `GUID` | 全局唯一标识符，用于序列化引用 |
| `ExecutionOrder` | 执行顺序（用于并行节点） |
| `Title` | 节点标题（可重写为动态标题） |

### 3. AsakiGraphRunner（图执行器）

```csharp
public class BehaviorTreeRunner : AsakiGraphRunner<BehaviorTreeGraph>
{
    protected override void OnGraphInitialized()
    {
        // 图初始化完成后的自定义逻辑
        var rootNode = GetRuntimeGraph().GetEntryNode<RootNode>();
        ExecuteNode(rootNode);
    }

    protected override void OnExecuteCustomNode(AsakiNodeBase node)
    {
        // 处理自定义节点类型
        if (node is SequenceNode sequence)
        {
            ExecuteSequence(sequence);
        }
    }
}
```

**核心 API：**

| 方法 | 说明 |
|------|------|
| `GetVariable<T>` | 获取黑板变量值 |
| `SetVariable<T>` | 设置黑板变量值（自动验证） |
| `ExecuteNode` | 执行指定节点 |
| `StartGraphAsync` | 异步启动图执行 |
| `StopToken` | 取消异步执行 |

### 4. 异步节点支持

```csharp
[Serializable]
public class WaitNode : AsakiAsyncNodeBase
{
    public float Duration = 1.0f;

    public override async Task<NodeExecutionResult> ExecuteAsync(
        AsakiGraphRuntimeContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay((int)(Duration * 1000), cancellationToken);
            return NodeExecutionResult.Succeed("Out");
        }
        catch (OperationCanceledException)
        {
            return NodeExecutionResult.Fail("Cancelled");
        }
    }

    public override void OnCancelled()
    {
        Debug.Log("[WaitNode] Wait cancelled");
    }
}
```

### 5. 变量节点系统

```csharp
// Get 节点 - 从黑板读取值
[AsakiGraphContext(typeof(MyGraph), "Variable/Get")]
public class AsakiGetVariableNode : AsakiNodeBase
{
    public string VariableName;
    public string VariableTypeName;
    public bool IsGlobalVariable;

    [AsakiNodeOutput("Value")]
    public object Value;

    public override string Title => $"Get {VariableName}";
}

// Set 节点 - 向黑板写入值
[AsakiGraphContext(typeof(MyGraph), "Variable/Set")]
public class AsakiSetVariableNode : AsakiNodeBase
{
    public string VariableName;
    public string VariableTypeName;

    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;

    [AsakiNodeInput("Value")]
    public object NewValue;

    [AsakiNodeOutput("Out")]
    public AsakiFlowPort OutputFlow;

    public override string Title => $"Set {VariableName}";
}
```

## 🚀 快速开始

### 1. 创建自定义图类型

```csharp
using Asaki.Core.Graphs;
using UnityEngine;

[CreateAssetMenu(menuName = "Asaki/Demo Graph")]
public class DemoGraph : AsakiGraphAsset { }
```

### 2. 创建自定义节点

```csharp
using System;
using Asaki.Core.Attributes;
using Asaki.Core.Graphs;
using UnityEngine;

[Serializable]
[AsakiGraphContext(typeof(DemoGraph), "Action")]
public class LogMessageNode : AsakiNodeBase
{
    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;

    public string Message = "Hello Asaki!";

    [AsakiNodeOutput("Out")]
    public AsakiFlowPort OutputFlow;

    public override string Title => $"Log: {Message}";

    public override void OnCreated()
    {
        if (string.IsNullOrEmpty(GUID))
            GUID = System.Guid.NewGuid().ToString();
    }
}
```

### 3. 创建图执行器

```csharp
using Asaki.Core.Graphs;
using UnityEngine;

public class DemoGraphRunner : AsakiGraphRunner<DemoGraph>
{
    protected override void OnGraphInitialized()
    {
        var entryNode = GetRuntimeGraph().GetEntryNode<AsakiNodeBase>();
        if (entryNode != null)
        {
            ExecuteNode(entryNode);
        }
    }
}
```

### 4. 在场景中使用

1. 在 Project 窗口右键 → Create → Asaki → Demo Graph
2. 双击打开图编辑器
3. 按空格键呼出节点搜索窗口
4. 添加节点并连接
5. 在场景中创建空物体，添加 `DemoGraphRunner` 组件
6. 将图资源拖到 Runner 的 `GraphAsset` 字段
7. 运行游戏

## 📝 创建自定义节点

### 基础节点

```csharp
[Serializable]
[AsakiGraphContext(typeof(MyGraph), "Action")]
public class PrintNode : AsakiNodeBase
{
    // Flow 端口 - 控制执行顺序
    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;

    // 值端口 - 接收输入数据
    [AsakiNodeInput("Message")]
    public string Message;

    // 输出 Flow
    [AsakiNodeOutput("Out")]
    public AsakiFlowPort OutputFlow;

    public override string Title => "Print Message";
}
```

### 带多输出的节点

```csharp
[Serializable]
[AsakiGraphContext(typeof(MyGraph), "Logic")]
public class BranchNode : AsakiNodeBase
{
    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;

    [AsakiNodeInput("Condition")]
    public bool Condition;

    [AsakiNodeOutput("True")]
    public AsakiFlowPort TrueOutput;

    [AsakiNodeOutput("False")]
    public AsakiFlowPort FalseOutput;

    public override string Title => "Branch";
}
```

### 异步节点

```csharp
[Serializable]
[AsakiGraphContext(typeof(MyGraph), "Async")]
public class DelayNode : AsakiAsyncNodeBase
{
    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;

    public float Seconds = 1.0f;

    [AsakiNodeOutput("Out")]
    public AsakiFlowPort OutputFlow;

    [AsakiNodeOutput("Error")]
    public AsakiFlowPort ErrorOutput;

    public override async Task<NodeExecutionResult> ExecuteAsync(
        AsakiGraphRuntimeContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay((int)(Seconds * 1000), cancellationToken);
            return NodeExecutionResult.Succeed("Out");
        }
        catch (OperationCanceledException)
        {
            return NodeExecutionResult.Fail("Cancelled");
        }
    }
}
```

## ⚡ 运行时执行

### 同步执行

```csharp
public class MyRunner : AsakiGraphRunner<MyGraph>
{
    protected override void OnGraphInitialized()
    {
        var entry = GetRuntimeGraph().GetEntryNode<AsakiNodeBase>();
        ExecuteNode(entry);
    }
}
```

### 异步执行

```csharp
public class MyRunner : AsakiGraphRunner<MyGraph>
{
    private async void Start()
    {
        base.Start();
        await StartGraphAsync();
    }

    private void OnDestroy()
    {
        StopToken(); // 取消正在执行的异步操作
        base.OnDestroy();
    }
}
```

### 黑板变量操作

```csharp
// 获取变量
int health = GetVariable<int>("PlayerHealth");

// 设置变量（自动验证约束）
SetVariable("PlayerHealth", 100);

// 批量更新
BatchUpdateVariables(bb =>
{
    bb.SetValue("Health", 100);
    bb.SetValue("Mana", 50);
    bb.SetValue("Stamina", 75);
});

// 重置变量
ResetVariable("PlayerHealth");
ResetAllVariablesToDefault();
```

## 🛠️ 编辑器扩展

### 注册自定义图编辑器

```csharp
public class MyGraphController : IAsakiGraphViewController
{
    private readonly MyGraph _graph;
    private AsakiGraphView _graphView;

    public MyGraphController(MyGraph graph)
    {
        _graph = graph;
    }

    public VisualElement CreateGraphView()
    {
        _graphView = new AsakiGraphView(_graph);
        return _graphView;
    }

    public void Update() { }
    public void Save() { }
    public void Dispose() { }
}

// 注册（使用代码生成器或手动注册）
[InitializeOnLoad]
public static class MyGraphRegistration
{
    static MyGraphRegistration()
    {
        AsakiGraphWindow.Register<MyGraph>(asset => new MyGraphController(asset));
    }
}
```

### 自定义节点视图

```csharp
public class CustomNodeView : AsakiNodeView
{
    public CustomNodeView(AsakiNodeBase data, SerializedObject graphSO)
        : base(data, graphSO)
    {
        // 自定义样式
        style.backgroundColor = new StyleColor(new Color(0.2f, 0.3f, 0.4f));

        // 添加自定义 UI 元素
        var customLabel = new Label("Custom");
        extensionContainer.Add(customLabel);
    }
}
```

## 📚 最佳实践

### 1. 节点命名规范

```csharp
// ✅ 推荐：使用清晰的分类路径
[AsakiGraphContext(typeof(MyGraph), "Action/Combat")]
[AsakiGraphContext(typeof(MyGraph), "Logic/Flow Control")]
[AsakiGraphContext(typeof(MyGraph), "Variable/Get")]

// ❌ 避免：过于简单或模糊的名称
[AsakiGraphContext(typeof(MyGraph), "Node1")]
```

### 2. 端口命名规范

```csharp
// Flow 端口
[AsakiNodeInput("In")]
[AsakiNodeOutput("Out")]

// 条件端口
[AsakiNodeOutput("True")]
[AsakiNodeOutput("False")]

// 值端口
[AsakiNodeInput("Value")]
[AsakiNodeOutput("Result")]
```

### 3. GUID 生成

```csharp
public override void OnCreated()
{
    // ✅ 始终生成 GUID
    if (string.IsNullOrEmpty(GUID))
        GUID = System.Guid.NewGuid().ToString();
}
```

### 4. 异步节点异常处理

```csharp
public override async Task<NodeExecutionResult> ExecuteAsync(
    AsakiGraphRuntimeContext context,
    CancellationToken cancellationToken)
{
    try
    {
        // 异步操作
        await SomeAsyncOperation(cancellationToken);
        return NodeExecutionResult.Succeed("Out");
    }
    catch (OperationCanceledException)
    {
        // 处理取消
        return NodeExecutionResult.Fail("Cancelled");
    }
    catch (Exception e)
    {
        // 处理其他异常
        Debug.LogError($"[MyNode] Execution failed: {e}");
        return NodeExecutionResult.Fail(e.Message);
    }
}
```

### 5. 黑板变量访问

```csharp
// ✅ 推荐：使用常量定义键名
public static class BlackboardKeys
{
    public const string PlayerHealth = "Player/Health";
    public const string PlayerPosition = "Player/Position";
}

// 使用
SetVariable(BlackboardKeys.PlayerHealth, 100);
```

### 6. 性能优化

- **缓存节点引用**：避免频繁调用 `GetNodeByGUID`
- **使用批处理**：批量更新黑板变量
- **异步操作**：长时间操作使用异步节点
- **对象池**：复用节点执行上下文

## 🔗 依赖关系

- **Unity Engine**: 2021.3+ (GraphView, UI Toolkit)
- **Asaki.Core.Blackboard**: 黑板数据系统
- **Asaki.Core.Attributes**: 特性定义
- **Asaki.Core.Logging**: 日志系统 (ALog)

## 📄 许可证

MIT License

---

**作者**: Asaki Framework Team
**版本**: 1.0.0
**最后更新**: 2026-02-05
