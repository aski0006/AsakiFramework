# Asaki Blackboard System - 黑板系统

## 概述

Asaki Blackboard System 是一个为 Unity 设计的**高性能、类型安全、可扩展**的数据共享系统。它采用**确定性哈希键**和**多态值类型**设计，适用于 AI 行为树、对话系统、任务系统等需要在多个组件间共享状态的场景。

## 核心特性

- **跨平台一致性**: 使用 FNV-1a 哈希算法替代 .NET 默认哈希，确保跨平台/跨进程的键一致性
- **类型安全**: 泛型 API 提供编译时类型检查，避免运行时转换错误
- **多态值系统**: 支持任意可序列化类型的变量存储（通过 `AsakiValueBase` 继承体系）
- **响应式编程**: 内置属性系统 (`AsakiProperty<T>`) 支持值变更通知
- **作用域继承**: 支持父子黑板作用域，实现数据分层管理
- **批量操作**: 支持批量写入模式，减少多次变更的通知开销
- **编辑器集成**: 完整的 Unity Editor 支持，包括可视化变量管理面板

## 架构设计

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Asaki Blackboard System                             │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────────────┐ │
│  │   Editor Layer  │    │   Runtime Layer │    │    Variable Types       │ │
│  ├─────────────────┤    ├─────────────────┤    ├─────────────────────────┤ │
│  │ GlobalBlackboard│    │ AsakiBlackboard │    │  ┌─────────────────┐    │ │
│  │     Window      │◄──►│   (Core Impl)   │◄──►│  │ AsakiValueBase  │    │ │
│  ├─────────────────┤    ├─────────────────┤    │  │   (Abstract)    │    │ │
│  │AsakiBlackboard  │    │ IAsakiBlackboard│    │  └────────┬────────┘    │ │
│  │    Provider     │    │   (Interface)   │    │           │             │ │
│  └─────────────────┘    └─────────────────┘    │  ┌────────▼────────┐    │ │
│           │                      │             │  │ AsakiValue<T>   │    │ │
│           ▼                      ▼             │  │  (Generic Base) │    │ │
│  ┌─────────────────┐    ┌─────────────────┐    │  └────────┬────────┘    │ │
│  │  Graph Editor   │    │ AsakiBlackboardKey    │           │             │ │
│  │   Integration   │    │  (FNV-1a Hash)  │    │  ┌────────▼────────┐    │ │
│  └─────────────────┘    └─────────────────┘    │  │ Concrete Types  │    │ │
│                                                │  │ • AsakiInt      │    │ │
│  ┌──────────────────────────────────────┐     │  │ • AsakiFloat    │    │ │
│  │      Global Blackboard Asset         │     │  │ • AsakiBool     │    │ │
│  │  (ScriptableObject - Shared Data)    │     │  │ • AsakiString   │    │ │
│  └──────────────────────────────────────┘     │  │ • AsakiVector3  │    │ │
│                                                │  │ • ...           │    │ │
│  ┌──────────────────────────────────────┐     │  └─────────────────┘    │ │
│  │        Graph Asset Variables         │     └─────────────────────────┘ │
│  │   (Per-Graph Local Variables)        │                                 │ │
│  └──────────────────────────────────────┘                                 │ │
└─────────────────────────────────────────────────────────────────────────────┘
```

## 核心组件

### 1. AsakiBlackboardKey - 确定性哈希键

`AsakiBlackboardKey` 是黑板系统的核心标识符，使用 **FNV-1a 32位哈希算法** 确保跨平台一致性。

```csharp
// 隐式转换：string -> AsakiBlackboardKey
AsakiBlackboardKey key = "PlayerHealth";

// 或使用显式构造
var key2 = new AsakiBlackboardKey("PlayerHealth");

// 支持整数哈希构造（用于反序列化）
var key3 = new AsakiBlackboardKey(123456789);
```

**设计要点**:
- 使用 FNV-1a 替代 `string.GetHashCode()`，避免不同 .NET 实现的哈希差异
- 结构体设计，无 GC 分配
- 支持隐式转换，使用便捷
- Editor 下保留 DebugName 便于调试

### 2. IAsakiBlackboard / AsakiBlackboard - 黑板接口与实现

黑板是数据存储的核心容器，提供类型安全的键值存储。

```csharp
// 创建黑板（可选父作用域）
var blackboard = new AsakiBlackboard(parentScope);

// 基础操作
blackboard.SetValue("Health", 100);
int health = blackboard.GetValue<int>("Health");

// 响应式属性
var healthProp = blackboard.GetProperty<int>("Health");
healthProp.OnValueChanged += (oldVal, newVal) => 
    Debug.Log($"Health changed: {oldVal} -> {newVal}");

// 批量操作（减少通知次数）
using (blackboard.BeginBatch())
{
    blackboard.SetValue("Health", 80);
    blackboard.SetValue("Mana", 50);
    blackboard.SetValue("Stamina", 30);
    // 退出 using 时统一触发通知
}
```

**API 参考**:

| 方法 | 描述 |
|------|------|
| `SetValue<T>(key, value)` | 设置键值 |
| `GetValue<T>(key)` | 获取键值（不存在返回 default）|
| `GetProperty<T>(key)` | 获取响应式属性 |
| `HasKey(key)` | 检查键是否存在 |
| `Remove(key)` | 移除键 |
| `Clear()` | 清空所有数据 |
| `BeginBatch()` | 开始批量模式 |

### 3. AsakiValueBase / AsakiValue<T> - 多态值系统

支持多态序列化的值类型体系，允许在运行时处理不同类型的变量。

```csharp
// 内置类型
[Serializable]
public class AsakiInt : AsakiValue<int> 
{
    public AsakiInt() : base(() => new AsakiInt()) { }
}

// 自定义类型
[Serializable]
[AsakiBlackboardValueSchema] // 标记为黑板值模式
public class AsakiCustomData : AsakiValue<CustomData>
{
    public AsakiCustomData() : base(() => new AsakiCustomData()) { }
}
```

**内置类型清单**:

| 类型 | 描述 |
|------|------|
| `AsakiInt` | 整型 |
| `AsakiFloat` | 浮点型 |
| `AsakiBool` | 布尔型 |
| `AsakiString` | 字符串 |
| `AsakiVector3` | Unity Vector3 |
| `AsakiVector2` | Unity Vector2 |
| `AsakiVector2Int` | Unity Vector2Int |
| `AsakiVector3Int` | Unity Vector3Int |
| `AsakiColor` | Unity Color |
| `AsakiGameObject` | Unity GameObject 引用 |

### 4. AsakiVariableDef - 变量定义

变量定义是黑板变量的元数据容器，支持多态值存储和约束验证。

```csharp
public class AsakiVariableDef
{
    public string Name;                    // 变量名
    public AsakiValueBase ValueData;       // 当前值（多态）
    public AsakiValueBase DefaultValue;    // 默认值（多态）
    public bool IsExposed;                 // 是否暴露
    public IVariableConstraint Constraint; // 约束条件
}
```

### 5. IVariableConstraint - 变量约束

支持对变量值进行验证的约束系统。

```csharp
// 范围约束
var rangeConstraint = new RangeConstraint 
{ 
    MinValue = 0, 
    MaxValue = 100 
};

// 非空约束
var notNullConstraint = new NotNullConstraint();

// 正则约束
var regexConstraint = new RegexConstraint 
{ 
    Pattern = @"^[A-Z][a-z]+$" 
};
```

## 编辑器工具

### Global Blackboard Window

全局黑板编辑器窗口，用于管理跨图共享的变量。

**打开方式**: `Asaki/Window/Global Blackboard`

**功能**:
- 添加/删除全局变量
- 修改变量值
- 支持所有继承自 `AsakiValueBase` 的类型

### AsakiBlackboardProvider

GraphView 的黑板面板提供器，集成到节点编辑器中。

**功能**:
- 显示全局变量（只读）和局部变量
- 变量拖拽到图中创建节点
- 右键菜单：提升为全局变量
- 键盘删除支持

## 使用示例

### 基础使用

```csharp
using Asaki.Core.Blackboard;
using Asaki.Core.Blackboard.Variables;

public class Example : MonoBehaviour
{
    private IAsakiBlackboard _blackboard;
    
    void Start()
    {
        _blackboard = new AsakiBlackboard();
        
        // 设置值
        _blackboard.SetValue("Score", 0);
        _blackboard.SetValue("PlayerName", "Hero");
        
        // 获取值
        int score = _blackboard.GetValue<int>("Score");
        string name = _blackboard.GetValue<string>("PlayerName");
    }
}
```

### 响应式编程

```csharp
public class HealthSystem : MonoBehaviour
{
    [SerializeField] private AsakiGlobalBlackboardAsset _globalBlackboard;
    
    private AsakiProperty<int> _healthProp;
    
    void Start()
    {
        var bb = new AsakiBlackboard();
        _healthProp = bb.GetProperty<int>("Health");
        _healthProp.OnValueChanged += OnHealthChanged;
    }
    
    void OnHealthChanged(int oldHealth, int newHealth)
    {
        if (newHealth <= 0)
        {
            Debug.Log("Player died!");
        }
    }
}
```

### 全局黑板使用

```csharp
// 在编辑器中创建全局黑板资产
// 1. Project 窗口右键 -> Create -> Asaki -> Global Blackboard
// 2. 配置变量

// 运行时访问
public class GameManager : MonoBehaviour
{
    [SerializeField] private AsakiGlobalBlackboardAsset _globalBlackboard;
    
    void Start()
    {
        // 获取或创建变量
        var scoreVar = _globalBlackboard.GetOrCreateVariable(
            "TotalScore", 
            typeof(AsakiInt)
        );
        
        // 修改变量值
        ((AsakiInt)scoreVar.ValueData).Value = 1000;
    }
}
```

### 批量操作

```csharp
// 方式1：使用 BeginBatch
using (blackboard.BeginBatch())
{
    blackboard.SetValue("A", 1);
    blackboard.SetValue("B", 2);
    blackboard.SetValue("C", 3);
} // 统一触发通知

// 方式2：使用扩展方法
blackboard.BatchSet(
    ("A", 1),
    ("B", 2),
    ("C", 3)
);
```

## 文件结构

```
Assets/Asaki/Core/Blackboard/
├── AsakiBlackboard.cs              # 黑板核心实现
├── AsakiBlackboardKey.cs           # 确定性哈希键
├── AsakiBlackboardPropertyType.cs  # 变量定义
├── AsakiVariableDef.cs             # 变量定义（别名）
├── BlackboardExtensions.cs         # 扩展方法
├── BlackboardProfiler.cs           # 性能分析工具
├── IAsakiBlackboard.cs             # 黑板接口
├── IVariableConstraint.cs          # 变量约束接口
└── Variables/
    ├── AsakiTypeBridge.cs          # 类型桥接
    ├── AsakiValueBase.cs           # 值基类
    └── Primitives.cs               # 基础类型实现

Assets/Asaki/Core/Graphs/
├── AsakiGlobalBlackboardAsset.cs   # 全局黑板资产
└── AsakiGraphAsset.cs              # 图资产（含局部变量）

Assets/Asaki/Editor/GraphEditors/
├── AsakiBlackboardProvider.cs      # 黑板面板提供器
├── DragVariableData.cs             # 拖拽数据
└── GlobalBlackboardWindow.cs       # 全局黑板窗口
```

## 性能优化

1. **哈希缓存**: `AsakiBlackboardKey` 使用 FNV-1a 预计算哈希，避免运行时字符串比较
2. **批量模式**: `BeginBatch()` 减少多次变更的通知开销
3. **延迟初始化**: 图资产缓存采用 Lazy Load 策略
4. **结构体设计**: Key 使用结构体避免 GC 分配
5. **类型桥接**: `AsakiTypeBridge` 提供快速类型分发

## 调试工具

```csharp
// 启用性能分析
BlackboardProfiler.Enable();

// ... 运行代码 ...

// 打印报告
BlackboardProfiler.PrintReport();
```

## 最佳实践

1. **键命名**: 使用常量或枚举管理键名，避免魔法字符串
2. **类型安全**: 优先使用泛型 API，避免装箱拆箱
3. **批量操作**: 多个相关变更使用 `BeginBatch()`
4. **作用域管理**: 合理使用父子作用域，避免数据污染
5. **约束验证**: 对重要变量添加约束条件

## 依赖

- Unity 2021.3 或更高版本
- Unity UI Toolkit (com.unity.ui)

## 许可证

MIT License - 详见项目根目录 LICENSE 文件
