# Asaki ComboSystem - 连招系统

[![Unity Version](https://img.shields.io/badge/Unity-2023.2%2B-blue)](https://unity.com)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Asaki ComboSystem 是一个专注于**连招表现**的轻量级 Unity 插件，负责管理角色的连招动画流程和状态转换。

## 核心特性

- **专注表现** - 只负责连招动画和状态管理，不涉及伤害计算等战斗逻辑
- **可视化编辑** - 基于 Graph 系统的可视化连招编辑器
- **灵活输入** - 可扩展的输入类型系统，支持任意自定义输入
- **状态机驱动** - 轻量级状态机管理连招流程
- **输入缓冲** - 支持输入缓冲，提升连招手感
- **自定义重置策略** - 支持多种连招重置策略（保持、递减、自定义函数等）
- **事件驱动** - 丰富的回调事件，便于与外部系统集成

## 系统架构

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         外部系统（使用者实现）                                │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐         │
│  │  PlayerInput    │    │  CombatSystem   │    │  EffectSystem   │         │
│  │  (输入系统)      │    │  (战斗系统)      │    │  (特效系统)      │         │
│  └────────┬────────┘    └────────┬────────┘    └────────┬────────┘         │
│           │                      │                      │                  │
│           ▼                      ▼                      ▼                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                         Asaki ComboSystem (本插件)                           │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    AsakiComboController 核心控制器                   │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │   │
│  │  │ ComboState  │  │ ComboTree   │  │ HitBoxMgr   │  │  Animator   │ │   │
│  │  │   Machine   │  │   (Data)    │  │ (Only Mgr)  │  │   Bridge    │ │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘ │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

## 职责边界

| ComboSystem 负责 | ComboSystem 不负责 |
|------------------|-------------------|
| 连招状态管理 | 伤害计算 |
| 动画播放控制 | 受击反馈 |
| 判定框激活/禁用 | 血量管理 |
| 连招窗口管理 | 特效播放 |
| 输入缓冲 | 音效播放 |

## 快速开始

### 1. 安装

将 `ComboSystem` 文件夹复制到项目的 `Assets/Asaki/Plungin/` 目录下。

### 2. 创建连招数据

通过菜单创建 ComboTree 资产：

```
Assets -> Create -> Asaki -> ComboSystem -> ComboTree
```

### 3. 配置角色

在角色 GameObject 上添加 `AsakiComboController` 组件：

```csharp
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private AsakiComboController comboController;
    [SerializeField] private ComboTree comboTree;

    void Start()
    {
        comboController.Initialize(comboTree);
        
        // 订阅事件
        comboController.OnHitBoxesActivated += OnHitBoxesActivated;
        comboController.OnMoveStarted += OnMoveStarted;
    }

    void Update()
    {
        // 传递输入
        if (Input.GetButtonDown("Fire1"))
            comboController.TriggerAttack("LightAttack");
    }

    void OnHitBoxesActivated(HitBoxInfo[] hitBoxes)
    {
        // 处理命中检测...
    }

    void OnMoveStarted(ComboMove move)
    {
        // 播放音效/特效...
    }
}
```

## 核心组件

### AsakiComboController

连招系统核心控制器，负责管理整个连招流程。

```csharp
// 触发攻击
comboController.TriggerAttack("LightAttack");

// 中断连招
comboController.InterruptCombo(InterruptReason.Damaged);

// 重置连招
comboController.ResetCombo();

// 检查是否可以接受输入
bool canInput = comboController.CanAcceptInput();
```

### ComboTree

连招树数据资产，包含所有招式和转换关系。

```csharp
[CreateAssetMenu(fileName = "ComboTree", menuName = "Asaki/ComboSystem/ComboTree")]
public class ComboTree : ScriptableObject
{
    public string TreeId;
    public ComboMove[] Moves;
    public ComboTransition[] Transitions;
    public ResetStrategyDefinition[] ResetStrategies;
}
```

### ComboMove

招式数据定义。

```csharp
[Serializable]
public class ComboMove
{
    public string MoveId;
    public string MoveName;
    public string AnimationStateName;
    public float AnimationSpeed = 1f;
    
    // 时间参数
    public float StartupTime;       // 前摇时间
    public float ActiveDuration;    // 判定持续时间
    public float RecoveryTime;      // 后摇时间
    public float ComboWindowStart;  // 连招窗口开始
    public float ComboWindowEnd;    // 连招窗口结束
    
    // 判定框
    public HitBoxDefinition[] HitBoxes;
    
    // 限制条件
    public int MinComboCount;
    public int MaxComboCount;
    public float Cooldown;
}
```

## 状态机

连招系统使用轻量级状态机管理连招流程：

```
Idle → Startup → Active → Recovery → ComboWindow ─┐
  ↑                                               │
  └───────────────────────────────────────────────┘
```

| 状态 | 说明 |
|------|------|
| `Idle` | 待机状态，等待输入 |
| `Startup` | 前摇状态，动画开始但判定未激活 |
| `Active` | 判定状态，可以命中敌人 |
| `Recovery` | 后摇状态，判定结束但动画未完成 |
| `ComboWindow` | 连招窗口，可以输入下一招 |
| `Interrupted` | 中断状态，连招被强制中断 |

## 回调事件

```csharp
// 连招开始
comboController.OnComboStarted += () => { };

// 连招中断
comboController.OnComboInterrupted += (reason) => { };

// 连招完成
comboController.OnComboCompleted += () => { };

// 招式开始
comboController.OnMoveStarted += (move) => { };

// 判定框激活
comboController.OnHitBoxesActivated += (hitBoxes) => { };

// 判定框禁用
comboController.OnHitBoxesDeactivated += () => { };

// 招式完成
comboController.OnMoveCompleted += (move) => { };

// 连招窗口开启
comboController.OnComboWindowOpened += (duration) => { };

// 连招窗口关闭
comboController.OnComboWindowClosed += () => { };

// 状态变化
comboController.OnStateChanged += (from, to) => { };
```

## 可视化编辑器

### 打开编辑器

1. 创建 ComboGraph 资产：`Assets -> Create -> Asaki -> ComboSystem -> ComboGraph`
2. 双击打开可视化编辑器

### 编辑器功能

- **招式节点** - 定义单个招式的数据
- **转换边** - 定义招式之间的转换关系
- **条件节点** - 定义转换的条件
- **导出** - 导出为 ComboTree 资产
- **验证** - 检查图表配置是否正确
- **自动布局** - 自动排列节点位置

### 输入类型管理

支持自定义输入类型，通过 `ComboInputTypeManagerWindow` 管理：

```
Window -> Asaki -> ComboSystem -> Input Type Manager
```

## 自定义重置策略

ComboSystem 支持灵活的重置策略，用于控制连招中断后的连击数处理。

### 内置策略

```csharp
// 重置为0（默认）
ResetComboMode.ResetToZero

// 保持当前计数
ResetComboMode.KeepCount

// 固定值递减
ResetComboMode.Decay

// 百分比递减
ResetComboMode.PercentageDecay

// 设置为特定值
ResetComboMode.SetToSpecific

// 自定义函数
ResetComboMode.CustomFunction
```

### 配置重置策略

在 ComboTree 中配置：

```csharp
[CreateAssetMenu]
public class MyComboTree : ComboTree
{
    void Reset()
    {
        ResetStrategies = new[]
        {
            new ResetStrategyDefinition
            {
                GroupName = "on_timeout",
                Mode = ResetComboMode.KeepCount  // 超时时保持计数
            },
            new ResetStrategyDefinition
            {
                GroupName = "on_damaged",
                Mode = ResetComboMode.ResetToZero  // 受击时重置
            }
        };
    }
}
```

### 自定义策略

实现 `IComboResetStrategy` 接口：

```csharp
public class MyResetStrategy : IComboResetStrategy
{
    public int CalculateResetCount(int currentCount, ComboContext context)
    {
        // 自定义重置逻辑
        return currentCount / 2;
    }

    public bool ShouldReset(ComboContext context)
    {
        // 自定义判断逻辑
        return context.InterruptReason != InterruptReason.UserCancel;
    }
}
```

## 文件结构

```
ComboSystem/
├── Runtime/
│   ├── Core/
│   │   ├── AsakiComboController.cs      # 核心控制器
│   │   ├── ComboAnimatorBridge.cs       # 动画桥接
│   │   ├── HitBoxManager.cs             # 判定框管理
│   │   └── ComboAnimationEventReceiver.cs
│   ├── States/
│   │   ├── ComboStateBase.cs
│   │   ├── ComboIdleState.cs
│   │   ├── ComboStartupState.cs
│   │   ├── ComboActiveState.cs
│   │   ├── ComboRecoveryState.cs
│   │   ├── ComboWindowState.cs
│   │   └── ComboInterruptedState.cs
│   ├── Data/
│   │   ├── ComboTree.cs                 # 连招树数据
│   │   ├── ComboMove.cs                 # 招式数据
│   │   ├── ComboContext.cs              # 连招上下文
│   │   ├── HitBoxInfo.cs                # 判定框信息
│   │   └── Enums.cs                     # 枚举定义
│   └── Utils/
│       ├── InputBuffer.cs               # 输入缓冲
│       └── ComboStateMachineExt.cs      # 状态机扩展
├── Editor/
│   ├── ComboGraphView.cs                # 图视图
│   ├── ComboGraphAsset.cs               # 图资产
│   ├── ComboGraphAssetInspector.cs      # 资产检查器
│   ├── ComboGraphController.cs          # 图控制器
│   ├── ComboNodeView.cs                 # 节点视图
│   ├── ComboNodeSearchWindow.cs         # 节点搜索窗口
│   ├── ComboTreeExporterWindow.cs       # 导出窗口
│   ├── ComboInputTypeRegistry.cs        # 输入类型注册表
│   ├── ComboInputTypeManagerWindow.cs   # 输入类型管理器
│   └── ComboInputTypeExtensionExample.cs # 扩展示例
└── DESIGN.md                            # 详细设计文档
```

## 最佳实践

1. **职责分离** - ComboSystem 只负责表现，战斗逻辑由外部系统处理
2. **事件订阅** - 使用事件机制与外部系统通信，避免直接耦合
3. **数据驱动** - 通过 ComboTree 配置连招，便于调整和扩展
4. **输入缓冲** - 启用输入缓冲以提升连招手感
5. **重置策略** - 根据游戏设计选择合适的重置策略

## 依赖

- Unity 2023.2 或更高版本
- Asaki.Core.FSM（状态机系统）
- Asaki.Core.Graphs（Graph 系统）

## 许可证

MIT License

---

*文档版本: 1.0*  
*最后更新: 2026-02-06*  
*作者: Asaki Framework Team*
