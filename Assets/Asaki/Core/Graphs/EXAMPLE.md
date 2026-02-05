# Asaki Graph System 使用示例与API参考

本文档提供 Asaki Graph System 的详细使用指南，包含推荐使用场景、完整API参考及可运行的代码示例。

---

# 第一部分：使用用途推荐

## 1. 核心定位

Asaki Graph System 是一个**可视化编程框架**，专为 Unity 设计，用于构建可配置的流程逻辑。它最适合以下核心用途：

| 用途类型 | 契合度 | 核心优势 |
|---------|-------|---------|
| **AI 行为树** | ⭐⭐⭐⭐⭐ | 可视化编辑、运行时调试、黑板数据共享、支持异步操作 |
| **对话系统** | ⭐⭐⭐⭐⭐ | 分支流程可视化、变量驱动对话、条件判断节点 |
| **技能/法术系统** | ⭐⭐⭐⭐ | 技能流程编排、前摇/伤害/后摇阶段、冷却管理 |
| **任务系统** | ⭐⭐⭐⭐ | 任务流程可视化、条件检查、状态切换 |
| **事件系统** | ⭐⭐⭐⭐ | 事件响应链、异步操作支持、错误处理 |
| **过场动画** | ⭐⭐⭐ | 时间线控制、相机切换、角色动作序列 |

## 2. 推荐使用场景详解

### 2.1 AI 行为树（最经典用法）

**为什么适合：**
- AI 需要**可视化调试** - 运行时高亮显示正在执行的节点
- 需要**黑板数据共享** - 感知数据、目标信息跨节点传递
- 需要**异步等待** - 移动、攻击动画需要等待完成
- 需要**条件分支** - 根据环境选择不同行为

```csharp
// 行为树节点示例
[Serializable]
[AsakiGraphContext(typeof(BehaviorTreeGraph), "Action")]
public class ChasePlayerNode : AsakiAsyncNodeBase
{
    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;
    
    public float StopDistance = 2.0f;
    
    [AsakiNodeOutput("Success")]
    public AsakiFlowPort SuccessOutput;
    
    [AsakiNodeOutput("Failure")]
    public AsakiFlowPort FailureOutput;
    
    public override async Task<NodeExecutionResult> ExecuteAsync(
        AsakiGraphRuntimeContext context, 
        CancellationToken cancellationToken)
    {
        // 从黑板获取目标位置
        Vector3 targetPos = context.Blackboard.GetValue<Vector3>("TargetPosition");
        GameObject owner = context.Owner;
        
        while (Vector3.Distance(owner.transform.position, targetPos) > StopDistance)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // 移动逻辑
            owner.transform.position = Vector3.MoveTowards(
                owner.transform.position, 
                targetPos, 
                Time.deltaTime * 5f
            );
            
            await Task.Yield();
        }
        
        return NodeExecutionResult.Succeed("Success");
    }
}
```

### 2.2 对话系统

**为什么适合：**
- **分支可视化** - 对话选项以节点形式清晰展示
- **变量驱动** - 根据游戏状态显示不同对话
- **条件判断** - 检查任务完成状态、好感度等
- **异步等待** - 等待玩家选择、等待对话播放完成

```csharp
// 对话系统节点示例
[Serializable]
[AsakiGraphContext(typeof(DialogueGraph), "Dialogue")]
public class ShowDialogueNode : AsakiAsyncNodeBase
{
    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;
    
    [TextArea(3, 5)]
    public string DialogueText;
    public string SpeakerName;
    public float AutoSkipDelay = 0f; // 0 = 不自动跳过
    
    [AsakiNodeOutput("Out")]
    public AsakiFlowPort OutputFlow;
    
    public override async Task<NodeExecutionResult> ExecuteAsync(
        AsakiGraphRuntimeContext context,
        CancellationToken cancellationToken)
    {
        // 显示对话UI
        DialogueUI.Instance.Show(SpeakerName, DialogueText);
        
        if (AutoSkipDelay > 0)
        {
            await Task.Delay((int)(AutoSkipDelay * 1000), cancellationToken);
        }
        else
        {
            // 等待玩家点击
            await DialogueUI.Instance.WaitForClick(cancellationToken);
        }
        
        DialogueUI.Instance.Hide();
        return NodeExecutionResult.Succeed("Out");
    }
}

// 分支选择节点
[Serializable]
[AsakiGraphContext(typeof(DialogueGraph), "Dialogue")]
public class ChoiceNode : AsakiAsyncNodeBase
{
    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;
    
    public List<string> Options = new List<string> { "Option 1", "Option 2" };
    
    [AsakiNodeOutput("Option1")]
    public AsakiFlowPort Option1Output;
    
    [AsakiNodeOutput("Option2")]
    public AsakiFlowPort Option2Output;
    
    public override async Task<NodeExecutionResult> ExecuteAsync(
        AsakiGraphRuntimeContext context,
        CancellationToken cancellationToken)
    {
        int selectedIndex = await DialogueUI.Instance.ShowOptions(Options, cancellationToken);
        
        string outputPort = selectedIndex == 0 ? "Option1" : "Option2";
        return NodeExecutionResult.Succeed(outputPort);
    }
}
```

### 2.3 技能/法术系统

**为什么适合：**
- **阶段编排** - 前摇 → 伤害判定 → 后摇
- **条件检查** - 检查蓝量、冷却、距离
- **异步等待** - 等待动画播放、等待伤害判定时机
- **黑板数据** - 存储技能参数、目标信息

```csharp
// 技能系统节点示例
[Serializable]
[AsakiGraphContext(typeof(SkillGraph), "Skill")]
public class CastSkillNode : AsakiAsyncNodeBase
{
    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;
    
    public string SkillId;
    public float CastTime = 0.5f;
    
    [AsakiNodeOutput("Success")]
    public AsakiFlowPort SuccessOutput;
    
    [AsakiNodeOutput("Interrupted")]
    public AsakiFlowPort InterruptedOutput;
    
    public override async Task<NodeExecutionResult> ExecuteAsync(
        AsakiGraphRuntimeContext context,
        CancellationToken cancellationToken)
    {
        // 播放施法动画
        Animator animator = context.Owner.GetComponent<Animator>();
        animator.SetTrigger("Cast");
        
        try
        {
            // 等待施法时间
            await Task.Delay((int)(CastTime * 1000), cancellationToken);
            return NodeExecutionResult.Succeed("Success");
        }
        catch (OperationCanceledException)
        {
            // 施法被打断
            animator.SetTrigger("Interrupt");
            return NodeExecutionResult.Succeed("Interrupted");
        }
    }
}

// 伤害判定节点
[Serializable]
[AsakiGraphContext(typeof(SkillGraph), "Skill")]
public class ApplyDamageNode : AsakiNodeBase
{
    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;
    
    [AsakiNodeInput("Damage")]
    public float DamageValue;
    
    public float Radius = 3.0f;
    public LayerMask TargetLayer;
    
    [AsakiNodeOutput("Out")]
    public AsakiFlowPort OutputFlow;
    
    // 同步节点在 Runner 中处理
}
```

### 2.4 任务系统

**为什么适合：**
- **流程可视化** - 任务步骤以节点链形式展示
- **条件检查** - 检查任务完成条件
- **状态切换** - 任务开始、进行中、完成、失败
- **事件响应** - 监听游戏事件更新任务进度

```csharp
// 任务系统节点示例
[Serializable]
[AsakiGraphContext(typeof(QuestGraph), "Quest")]
public class CheckQuestConditionNode : AsakiNodeBase
{
    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;
    
    public string ConditionKey; // 如 "KillCount"
    public int RequiredValue = 5;
    
    [AsakiNodeOutput("Completed")]
    public AsakiFlowPort CompletedOutput;
    
    [AsakiNodeOutput("InProgress")]
    public AsakiFlowPort InProgressOutput;
    
    public override string Title => $"Check {ConditionKey} >= {RequiredValue}";
}

// 更新任务进度节点
[Serializable]
[AsakiGraphContext(typeof(QuestGraph), "Quest")]
public class UpdateQuestProgressNode : AsakiNodeBase
{
    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;
    
    public string ProgressKey;
    
    [AsakiNodeInput("Delta")]
    public int DeltaValue = 1;
    
    [AsakiNodeOutput("Out")]
    public AsakiFlowPort OutputFlow;
}
```

## 3. 谨慎使用/不适用场景

| 场景 | 原因 | 替代方案 |
|-----|------|---------|
| **每帧高频更新的逻辑** | 节点执行有开销，不适合 Update 高频调用 | 直接在 MonoBehaviour 中实现 |
| **复杂的数学计算** | 可视化节点会增加复杂度 | 使用专门的计算系统或 ECS |
| **需要精确帧控制的逻辑** | 异步节点可能跨越多个帧 | 使用协程或状态机 |
| **大量实体（1000+）的独立逻辑** | 每个实体一个图实例内存开销大 | 使用 ECS 或共享图实例 |

## 4. 性能建议速查

```csharp
// ✅ 推荐：低频触发（事件驱动）
public void OnPlayerDamaged()
{
    _graphRunner.SetVariable("PlayerHealth", currentHealth);
    _graphRunner.ExecuteNode(_damageResponseNode);
}

// ⚠️ 谨慎：中频调用（配合批处理）
void Update()
{
    if (Time.frameCount % 30 == 0) // 每30帧执行一次
    {
        _graphRunner.BatchUpdateVariables(bb =>
        {
            bb.SetValue("PlayerPos", transform.position);
            bb.SetValue("EnemyCount", enemyCount);
        });
    }
}

// ❌ 避免：高频调用（每帧多次）
void Update()
{
    // 不要这样做！
    // _graphRunner.SetVariable("Position", transform.position);
}
```

---

# 第二部分：Graph系统API

## 1. AsakiGraphAsset（图资源基类）

### 1.1 核心属性

```csharp
public abstract class AsakiGraphAsset : ScriptableObject
{
    [SerializeReference]
    public List<AsakiNodeBase> Nodes;        // 节点列表
    
    public List<AsakiEdgeData> Edges;        // 边列表
    
    [SerializeReference]
    public List<AsakiVariableDef> Variables; // 图级变量列表
}
```

### 1.2 运行时初始化

```csharp
// 必须在执行前调用
public void InitializeRuntime()
```

### 1.3 节点导航API

| 方法 | 签名 | 返回值 | 说明 |
|-----|------|-------|------|
| `GetEntryNode<T>` | `T GetEntryNode<T>()` | 第一个节点 | 获取入口节点 |
| `GetNodeByGUID` | `AsakiNodeBase GetNodeByGUID(string guid)` | 节点或null | 通过GUID查找节点 |
| `GetNextNode` | `AsakiNodeBase GetNextNode(AsakiNodeBase current, string portName = "Out")` | 下一节点或null | 获取指定端口的下一个节点 |
| `GetNextNodes` | `List<AsakiNodeBase> GetNextNodes(AsakiNodeBase current, string portName = "Out")` | 节点列表 | 获取指定端口的所有连接节点 |
| `GetNextNode<T>` | `T GetNextNode<T>(AsakiNodeBase current, string portName = "Out")` | 类型转换后的节点 | 泛型版本 |
| `GetInputConnection` | `AsakiEdgeData GetInputConnection(AsakiNodeBase targetNode, string inputPortName)` | 边数据或null | 获取输入连接 |

### 1.4 克隆与复制

```csharp
// 深度克隆图资源
public virtual TGraph Clone<TGraph>() where TGraph : AsakiGraphAsset

// 复制到目标图
public virtual void CopyTo(AsakiGraphAsset target)
```

## 2. AsakiNodeBase（节点基类）

### 2.1 核心属性

```csharp
public abstract class AsakiNodeBase
{
    [HideInInspector]
    public Vector2 Position;        // 编辑器位置
    
    [HideInInspector]
    public string GUID;             // 唯一标识符
    
    [HideInInspector]
    public int ExecutionOrder;      // 执行顺序
    
    public virtual string Title => GetType().Name; // 节点标题
}
```

### 2.2 生命周期方法

```csharp
// 节点创建时调用（需手动调用）
public virtual void OnCreated() { }
```

## 3. AsakiGraphRunner<TGraph>（图执行器）

### 3.1 核心属性

```csharp
public abstract class AsakiGraphRunner<TGraph> : MonoBehaviour
    where TGraph : AsakiGraphAsset
{
    [Header("Graphs Data")]
    public TGraph GraphAsset;                    // 图资源引用
    
    [Header("Runtime Settings")]
    public bool UseInstancedGraph = true;        // 是否使用实例化副本
    
    // 运行时图实例（内部使用）
    protected TGraph _runtimeGraph;
    
    // 运行时上下文（内部使用）
    protected AsakiGraphRuntimeContext _context;
}
```

### 3.2 生命周期方法

```csharp
// 初始化完成后调用
protected virtual void OnGraphInitialized() { }

// 执行自定义节点（子类重写）
protected virtual void OnExecuteCustomNode(AsakiNodeBase node) { }
```

### 3.3 黑板变量操作

| 方法 | 签名 | 说明 |
|-----|------|------|
| `GetVariable<T>` | `T GetVariable<T>(string key)` | 获取变量值 |
| `SetVariable<T>` | `void SetVariable<T>(string key, T value)` | 设置变量值（自动验证） |
| `BatchUpdateVariables` | `void BatchUpdateVariables(Action<IAsakiBlackboard> updates)` | 批量更新变量 |
| `ResetVariable` | `void ResetVariable(string key)` | 重置单个变量到默认值 |
| `ResetAllVariablesToDefault` | `void ResetAllVariablesToDefault()` | 重置所有变量到默认值 |

### 3.4 节点执行

| 方法 | 签名 | 说明 |
|-----|------|------|
| `ExecuteNode` | `void ExecuteNode(AsakiNodeBase node)` | 同步执行节点 |
| `ExecuteNodeAsync` | `async Task ExecuteNodeAsync(AsakiNodeBase node, CancellationToken ct)` | 异步执行节点 |
| `StartGraphAsync` | `async Task StartGraphAsync()` | 异步启动图（从入口节点开始） |
| `StopToken` | `void StopToken()` | 取消异步执行 |

### 3.5 输入值解析

```csharp
// 获取节点输入端口的值
protected T GetInputValue<T>(AsakiNodeBase currentNode, string inputPortName, T fallback = default(T))

// 解析节点输出值（子类可重写）
protected virtual T ResolveNodeValue<T>(AsakiNodeBase node, string outputPortName)
```

### 3.6 调试事件

```csharp
// 节点执行事件（Editor Only）
public event Action<AsakiNodeBase> OnNodeExecuted;
```

## 4. AsakiAsyncNodeBase（异步节点基类）

### 4.1 核心方法

```csharp
public abstract class AsakiAsyncNodeBase : AsakiNodeBase
{
    // 异步执行（必须实现）
    public abstract Task<NodeExecutionResult> ExecuteAsync(
        AsakiGraphRuntimeContext context,
        CancellationToken cancellationToken = default(CancellationToken));
    
    // 取消回调（可选重写）
    public virtual void OnCancelled()
    {
        Debug.Log($"[{GetType().Name}] Execution cancelled");
    }
}
```

### 4.2 NodeExecutionResult 结构

```csharp
public struct NodeExecutionResult
{
    public bool Success;           // 是否成功
    public string ErrorMessage;    // 错误信息
    public string OutputPortName;  // 输出端口名
    
    // 静态工厂方法
    public static NodeExecutionResult Succeed(string outputPort = "Out")
    public static NodeExecutionResult Fail(string error)
}
```

## 5. AsakiGraphRuntimeContext（运行时上下文）

```csharp
public class AsakiGraphRuntimeContext : IDisposable
{
    public IAsakiBlackboard Blackboard;  // 运行时黑板
    public GameObject Owner;             // 拥有者GameObject
    
    public void Dispose() { }
}
```

## 6. 变量节点

### 6.1 AsakiGetVariableNode（读取变量）

```csharp
[AsakiGraphContext(typeof(AsakiGraphAsset), "Variable/Get")]
public class AsakiGetVariableNode : AsakiNodeBase
{
    public string VariableName;         // 变量名
    public string VariableTypeName;     // 变量类型名
    public bool IsGlobalVariable;       // 是否全局变量
    
    [AsakiNodeOutput("Value")]
    public object Value;                // 输出值
    
    public override string Title => $"Get {VariableName}";
}
```

### 6.2 AsakiSetVariableNode（写入变量）

```csharp
[AsakiGraphContext(typeof(AsakiGraphAsset), "Variable/Set")]
public class AsakiSetVariableNode : AsakiNodeBase
{
    public string VariableName;         // 变量名
    public string VariableTypeName;     // 变量类型名
    
    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;     // 输入流
    
    [AsakiNodeInput("Value")]
    public object NewValue;             // 输入值
    
    [AsakiNodeOutput("Out")]
    public AsakiFlowPort OutputFlow;    // 输出流
    
    public override string Title => $"Set {VariableName}";
}
```

## 7. 特性标记

### 7.1 AsakiGraphContextAttribute（图上下文）

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class AsakiGraphContextAttribute : Attribute
{
    public Type GraphType { get; }      // 适用的图类型
    public string Path { get; }         // 菜单路径（如 "Action/Combat"）
    
    public AsakiGraphContextAttribute(Type graphType, string path = "")
}
```

### 7.2 AsakiNodeInputAttribute（输入端口）

```csharp
[AttributeUsage(AttributeTargets.Field)]
public class AsakiNodeInputAttribute : Attribute
{
    public string PortName { get; }     // 端口名称
    public bool Multiple { get; }       // 是否允许多个连接
    
    public AsakiNodeInputAttribute(string portName, bool multiple = false)
}
```

### 7.3 AsakiNodeOutputAttribute（输出端口）

```csharp
[AttributeUsage(AttributeTargets.Field)]
public class AsakiNodeOutputAttribute : Attribute
{
    public string PortName { get; }     // 端口名称
    public bool Multiple { get; }       // 是否允许多个连接
    
    public AsakiNodeOutputAttribute(string portName, bool multiple = false)
}
```

---

# 第三部分：使用示例

## 示例 1：基础图系统设置

### 1.1 创建图资源类型

```csharp
using Asaki.Core.Graphs;
using UnityEngine;

[CreateAssetMenu(menuName = "Asaki/Demo Graph")]
public class DemoGraph : AsakiGraphAsset { }
```

### 1.2 创建自定义节点

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

### 1.3 创建图执行器

```csharp
using Asaki.Core.Graphs;
using UnityEngine;

public class DemoGraphRunner : AsakiGraphRunner<DemoGraph>
{
    protected override void OnGraphInitialized()
    {
        Debug.Log("[DemoGraph] Graph initialized!");
        
        var entryNode = GetRuntimeGraph().GetEntryNode<AsakiNodeBase>();
        if (entryNode != null)
        {
            ExecuteNode(entryNode);
        }
    }
    
    protected override void OnExecuteCustomNode(AsakiNodeBase node)
    {
        if (node is LogMessageNode logNode)
        {
            Debug.Log($"[LogNode] {logNode.Message}");
            
            // 继续执行下一个节点
            var nextNode = GetRuntimeGraph().GetNextNode(node, "Out");
            if (nextNode != null)
            {
                ExecuteNode(nextNode);
            }
        }
    }
}
```

### 1.4 使用步骤

```csharp
// 步骤1：在 Project 窗口右键 → Create → Asaki → Demo Graph
// 步骤2：双击打开图编辑器
// 步骤3：按空格键呼出节点搜索窗口
// 步骤4：添加 LogMessageNode 节点并连接
// 步骤5：在场景中创建空物体，添加 DemoGraphRunner 组件
// 步骤6：将图资源拖到 Runner 的 GraphAsset 字段
// 步骤7：运行游戏
```

## 示例 2：AI行为树

### 2.1 行为树图类型

```csharp
[CreateAssetMenu(menuName = "Asaki/Behavior Tree")]
public class BehaviorTreeGraph : AsakiGraphAsset { }
```

### 2.2 复合节点 - 顺序执行

```csharp
[Serializable]
[AsakiGraphContext(typeof(BehaviorTreeGraph), "Composite")]
public class SequenceNode : AsakiNodeBase
{
    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;
    
    [AsakiNodeOutput("Out")]
    public AsakiFlowPort OutputFlow;
    
    public override string Title => "Sequence";
}
```

### 2.3 装饰器节点 - 条件检查

```csharp
[Serializable]
[AsakiGraphContext(typeof(BehaviorTreeGraph), "Decorator")]
public class CheckDistanceNode : AsakiNodeBase
{
    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;
    
    public float MaxDistance = 10f;
    public string TargetKey = "Target";
    
    [AsakiNodeOutput("True")]
    public AsakiFlowPort TrueOutput;
    
    [AsakiNodeOutput("False")]
    public AsakiFlowPort FalseOutput;
    
    public override string Title => $"Distance <= {MaxDistance}";
}
```

### 2.4 动作节点 - 追逐目标

```csharp
[Serializable]
[AsakiGraphContext(typeof(BehaviorTreeGraph), "Action")]
public class ChaseTargetNode : AsakiAsyncNodeBase
{
    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;
    
    public string TargetKey = "Target";
    public float StopDistance = 2f;
    public float Speed = 5f;
    
    [AsakiNodeOutput("Success")]
    public AsakiFlowPort SuccessOutput;
    
    [AsakiNodeOutput("Failure")]
    public AsakiFlowPort FailureOutput;
    
    public override async Task<NodeExecutionResult> ExecuteAsync(
        AsakiGraphRuntimeContext context,
        CancellationToken cancellationToken)
    {
        GameObject target = context.Blackboard.GetValue<GameObject>(TargetKey);
        GameObject owner = context.Owner;
        
        if (target == null)
            return NodeExecutionResult.Succeed("Failure");
        
        while (Vector3.Distance(owner.transform.position, target.transform.position) > StopDistance)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            Vector3 direction = (target.transform.position - owner.transform.position).normalized;
            owner.transform.position += direction * Speed * Time.deltaTime;
            
            await Task.Yield();
        }
        
        return NodeExecutionResult.Succeed("Success");
    }
}
```

### 2.5 行为树执行器

```csharp
public class BehaviorTreeRunner : AsakiGraphRunner<BehaviorTreeGraph>
{
    [Header("AI Settings")]
    public float UpdateInterval = 0.1f;
    
    private float _lastUpdateTime;
    
    protected override void OnGraphInitialized()
    {
        // 行为树持续运行
        StartCoroutine(UpdateBehaviorTree());
    }
    
    private IEnumerator UpdateBehaviorTree()
    {
        while (true)
        {
            if (Time.time - _lastUpdateTime >= UpdateInterval)
            {
                _lastUpdateTime = Time.time;
                
                var root = GetRuntimeGraph().GetEntryNode<AsakiNodeBase>();
                if (root != null)
                {
                    ExecuteNode(root);
                }
            }
            yield return null;
        }
    }
    
    protected override void OnExecuteCustomNode(AsakiNodeBase node)
    {
        switch (node)
        {
            case SequenceNode sequence:
                ExecuteSequence(sequence);
                break;
            case CheckDistanceNode check:
                ExecuteCheckDistance(check);
                break;
        }
    }
    
    private void ExecuteSequence(SequenceNode sequence)
    {
        // 顺序执行所有子节点
        var children = GetRuntimeGraph().GetNextNodes(sequence, "Out");
        foreach (var child in children)
        {
            ExecuteNode(child);
        }
    }
    
    private void ExecuteCheckDistance(CheckDistanceNode check)
    {
        GameObject target = GetVariable<GameObject>(check.TargetKey);
        GameObject owner = gameObject;
        
        if (target != null)
        {
            float distance = Vector3.Distance(owner.transform.position, target.transform.position);
            string outputPort = distance <= check.MaxDistance ? "True" : "False";
            
            var nextNode = GetRuntimeGraph().GetNextNode(check, outputPort);
            if (nextNode != null)
            {
                ExecuteNode(nextNode);
            }
        }
    }
}
```

## 示例 3：对话系统

### 3.1 对话图类型

```csharp
[CreateAssetMenu(menuName = "Asaki/Dialogue Graph")]
public class DialogueGraph : AsakiGraphAsset { }
```

### 3.2 对话节点

```csharp
[Serializable]
[AsakiGraphContext(typeof(DialogueGraph), "Dialogue")]
public class DialogueLineNode : AsakiAsyncNodeBase
{
    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;
    
    public string SpeakerName;
    
    [TextArea(3, 5)]
    public string DialogueText;
    
    public AudioClip VoiceClip;
    public float AutoSkipDelay = 0f;
    
    [AsakiNodeOutput("Out")]
    public AsakiFlowPort OutputFlow;
    
    public override string Title => $"{SpeakerName}: {DialogueText.Substring(0, Mathf.Min(20, DialogueText.Length))}...";
    
    public override async Task<NodeExecutionResult> ExecuteAsync(
        AsakiGraphRuntimeContext context,
        CancellationToken cancellationToken)
    {
        // 显示对话UI
        DialogueUIManager.Instance.ShowDialogue(SpeakerName, DialogueText, VoiceClip);
        
        if (AutoSkipDelay > 0)
        {
            await Task.Delay((int)(AutoSkipDelay * 1000), cancellationToken);
        }
        else
        {
            // 等待玩家点击
            await DialogueUIManager.Instance.WaitForPlayerInput(cancellationToken);
        }
        
        DialogueUIManager.Instance.HideDialogue();
        return NodeExecutionResult.Succeed("Out");
    }
}
```

### 3.3 选择分支节点

```csharp
[Serializable]
[AsakiGraphContext(typeof(DialogueGraph), "Dialogue")]
public class DialogueChoiceNode : AsakiAsyncNodeBase
{
    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;
    
    public List<string> Options = new List<string>();
    
    [AsakiNodeOutput("Option1")]
    public AsakiFlowPort Option1Output;
    
    [AsakiNodeOutput("Option2")]
    public AsakiFlowPort Option2Output;
    
    [AsakiNodeOutput("Option3")]
    public AsakiFlowPort Option3Output;
    
    public override async Task<NodeExecutionResult> ExecuteAsync(
        AsakiGraphRuntimeContext context,
        CancellationToken cancellationToken)
    {
        int selectedIndex = await DialogueUIManager.Instance.ShowOptions(Options, cancellationToken);
        
        string outputPort = $"Option{selectedIndex + 1}";
        return NodeExecutionResult.Succeed(outputPort);
    }
}
```

### 3.4 条件检查节点

```csharp
[Serializable]
[AsakiGraphContext(typeof(DialogueGraph), "Logic")]
public class CheckVariableNode : AsakiNodeBase
{
    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;
    
    public string VariableName;
    public int CompareValue;
    public ComparisonType Comparison;
    
    [AsakiNodeOutput("True")]
    public AsakiFlowPort TrueOutput;
    
    [AsakiNodeOutput("False")]
    public AsakiFlowPort FalseOutput;
    
    public override string Title => $"{VariableName} {GetComparisonSymbol()} {CompareValue}";
    
    private string GetComparisonSymbol()
    {
        return Comparison switch
        {
            ComparisonType.Equal => "==",
            ComparisonType.NotEqual => "!=",
            ComparisonType.Greater => ">",
            ComparisonType.Less => "<",
            ComparisonType.GreaterOrEqual => ">=",
            ComparisonType.LessOrEqual => "<=",
            _ => "=="
        };
    }
}

public enum ComparisonType { Equal, NotEqual, Greater, Less, GreaterOrEqual, LessOrEqual }
```

### 3.5 对话执行器

```csharp
public class DialogueRunner : AsakiGraphRunner<DialogueGraph>
{
    public bool IsDialogueActive { get; private set; }
    
    public async void StartDialogue()
    {
        if (IsDialogueActive) return;
        
        IsDialogueActive = true;
        await StartGraphAsync();
        IsDialogueActive = false;
    }
    
    public void SkipDialogue()
    {
        StopToken();
    }
    
    protected override void OnExecuteCustomNode(AsakiNodeBase node)
    {
        if (node is CheckVariableNode checkNode)
        {
            ExecuteCheckVariable(checkNode);
        }
    }
    
    private void ExecuteCheckVariable(CheckVariableNode checkNode)
    {
        int value = GetVariable<int>(checkNode.VariableName);
        bool result = checkNode.Comparison switch
        {
            ComparisonType.Equal => value == checkNode.CompareValue,
            ComparisonType.NotEqual => value != checkNode.CompareValue,
            ComparisonType.Greater => value > checkNode.CompareValue,
            ComparisonType.Less => value < checkNode.CompareValue,
            ComparisonType.GreaterOrEqual => value >= checkNode.CompareValue,
            ComparisonType.LessOrEqual => value <= checkNode.CompareValue,
            _ => false
        };
        
        string outputPort = result ? "True" : "False";
        var nextNode = GetRuntimeGraph().GetNextNode(checkNode, outputPort);
        if (nextNode != null)
        {
            ExecuteNode(nextNode);
        }
    }
}
```

## 示例 4：黑板变量操作

### 4.1 基础变量操作

```csharp
public class VariableExample : MonoBehaviour
{
    [SerializeField] private DemoGraph GraphAsset;
    
    private DemoGraphRunner _runner;
    
    void Start()
    {
        _runner = gameObject.AddComponent<DemoGraphRunner>();
        _runner.GraphAsset = GraphAsset;
    }
    
    void Update()
    {
        // 设置变量
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _runner.SetVariable("JumpCount", GetVariable<int>("JumpCount") + 1);
        }
        
        // 获取变量
        int jumpCount = _runner.GetVariable<int>("JumpCount");
        Debug.Log($"Jump Count: {jumpCount}");
    }
}
```

### 4.2 批量更新变量

```csharp
public void OnPlayerLevelUp(int newLevel)
{
    _runner.BatchUpdateVariables(bb =>
    {
        bb.SetValue("PlayerLevel", newLevel);
        bb.SetValue("MaxHealth", 100 + newLevel * 10);
        bb.SetValue("MaxMana", 50 + newLevel * 5);
        bb.SetValue("SkillPoints", GetVariable<int>("SkillPoints") + 3);
    });
}
```

### 4.3 变量约束验证

```csharp
// 在图资源中配置变量约束
// 在 Inspector 中设置：
// - Name: "PlayerHealth"
// - ValueData: AsakiFloat (100)
// - Constraint: RangeConstraint (Min: 0, Max: 100)

public void TakeDamage(float damage)
{
    float currentHealth = _runner.GetVariable<float>("PlayerHealth");
    float newHealth = currentHealth - damage;
    
    // 自动验证约束，超出范围会警告并使用边界值
    _runner.SetVariable("PlayerHealth", newHealth);
}
```

### 4.4 变量重置

```csharp
public void ResetGame()
{
    // 重置单个变量
    _runner.ResetVariable("PlayerHealth");
    
    // 重置所有变量到默认值
    _runner.ResetAllVariablesToDefault();
}
```

## 示例 5：异步节点高级用法

### 5.1 带超时的异步操作

```csharp
[Serializable]
[AsakiGraphContext(typeof(MyGraph), "Async")]
public class WaitWithTimeoutNode : AsakiAsyncNodeBase
{
    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;
    
    public float Timeout = 5f;
    public string EventKey = "EventTriggered";
    
    [AsakiNodeOutput("Success")]
    public AsakiFlowPort SuccessOutput;
    
    [AsakiNodeOutput("Timeout")]
    public AsakiFlowPort TimeoutOutput;
    
    public override async Task<NodeExecutionResult> ExecuteAsync(
        AsakiGraphRuntimeContext context,
        CancellationToken cancellationToken)
    {
        var timeoutCts = new CancellationTokenSource();
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Timeout));
        
        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token))
        {
            try
            {
                // 等待事件触发
                await WaitForEvent(context, EventKey, linkedCts.Token);
                return NodeExecutionResult.Succeed("Success");
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                return NodeExecutionResult.Succeed("Timeout");
            }
        }
    }
    
    private async Task WaitForEvent(AsakiGraphRuntimeContext context, string key, CancellationToken ct)
    {
        while (!context.Blackboard.GetValue<bool>(key))
        {
            ct.ThrowIfCancellationRequested();
            await Task.Yield();
        }
        // 重置事件标志
        context.Blackboard.SetValue(key, false);
    }
}
```

### 5.2 并行执行节点

```csharp
[Serializable]
[AsakiGraphContext(typeof(MyGraph), "Async")]
public class ParallelNode : AsakiAsyncNodeBase
{
    [AsakiNodeInput("In")]
    public AsakiFlowPort InputFlow;
    
    public ParallelMode Mode = ParallelMode.All; // All, Any, Race
    
    [AsakiNodeOutput("Out")]
    public AsakiFlowPort OutputFlow;
    
    [AsakiNodeOutput("Error")]
    public AsakiFlowPort ErrorOutput;
    
    public override async Task<NodeExecutionResult> ExecuteAsync(
        AsakiGraphRuntimeContext context,
        CancellationToken cancellationToken)
    {
        // 获取所有子节点
        var children = new List<AsakiNodeBase>(); // 从图中获取
        
        var tasks = children.Select(child => ExecuteChildAsync(child, context, cancellationToken));
        
        try
        {
            switch (Mode)
            {
                case ParallelMode.All:
                    await Task.WhenAll(tasks);
                    return NodeExecutionResult.Succeed("Out");
                    
                case ParallelMode.Any:
                    await Task.WhenAny(tasks);
                    return NodeExecutionResult.Succeed("Out");
                    
                case ParallelMode.Race:
                    var firstCompleted = await Task.WhenAny(tasks);
                    // 取消其他任务
                    return NodeExecutionResult.Succeed("Out");
            }
        }
        catch (Exception e)
        {
            return NodeExecutionResult.Fail(e.Message);
        }
        
        return NodeExecutionResult.Succeed("Out");
    }
    
    private async Task ExecuteChildAsync(AsakiNodeBase child, AsakiGraphRuntimeContext context, CancellationToken ct)
    {
        // 执行子节点逻辑
        await Task.Yield();
    }
}

public enum ParallelMode { All, Any, Race }
```

## 示例 6：自定义图编辑器

### 6.1 自定义图控制器

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
        
        // 添加自定义工具栏
        var toolbar = new VisualElement();
        toolbar.style.flexDirection = FlexDirection.Row;
        toolbar.style.height = 25;
        
        var validateButton = new Button(() => ValidateGraph())
        {
            text = "Validate"
        };
        toolbar.Add(validateButton);
        
        _graphView.Add(toolbar);
        
        return _graphView;
    }
    
    public void Update() { }
    public void Save() { }
    public void Dispose() { }
    
    private void ValidateGraph()
    {
        // 自定义验证逻辑
        Debug.Log("[MyGraph] Validation passed!");
    }
}
```

### 6.2 注册图编辑器

```csharp
[InitializeOnLoad]
public static class MyGraphEditorRegistration
{
    static MyGraphEditorRegistration()
    {
        AsakiGraphWindow.Register<MyGraph>(asset => new MyGraphController(asset));
    }
}
```

## 示例 7：运行时调试

### 7.1 监听节点执行事件

```csharp
public class GraphDebugger : MonoBehaviour
{
    [SerializeField] private DemoGraphRunner _runner;
    
    void OnEnable()
    {
#if UNITY_EDITOR
        _runner.OnNodeExecuted += OnNodeExecuted;
#endif
    }
    
    void OnDisable()
    {
#if UNITY_EDITOR
        _runner.OnNodeExecuted -= OnNodeExecuted;
#endif
    }
    
    private void OnNodeExecuted(AsakiNodeBase node)
    {
        Debug.Log($"[GraphDebug] Executed: {node.Title} ({node.GUID})");
    }
}
```

### 7.2 可视化黑板状态

```csharp
public class BlackboardDebugger : MonoBehaviour
{
    [SerializeField] private DemoGraphRunner _runner;
    
    void OnGUI()
    {
        if (_runner == null) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 400), "Blackboard", "box");
        
        var graph = _runner.GetRuntimeGraph();
        if (graph != null)
        {
            GUILayout.Label("Variables:");
            foreach (var variable in graph.Variables)
            {
                GUILayout.Label($"  {variable.Name}: {variable.ValueData}");
            }
        }
        
        GUILayout.EndArea();
    }
}
```

---

**作者**: Asaki Framework Team  
**版本**: 1.0.0  
**最后更新**: 2026-02-05
