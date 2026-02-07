# Asaki ComboSystem 连招系统设计文档

## 目录

1. [概述](#1-概述)
2. [设计原则](#2-设计原则)
3. [系统架构](#3-系统架构)
4. [核心模块设计](#4-核心模块设计)
5. [数据结构设计](#5-数据结构设计)
6. [状态机设计](#6-状态机设计)
7. [动画事件集成](#7-动画事件集成)
8. [判定系统（仅管理）](#8-判定系统仅管理)
9. [接口与回调](#9-接口与回调)
10. [可视化编辑器（基于Graph）](#10-可视化编辑器基于graph)
11. [高级特性：自定义重置策略](#11-高级特性自定义重置策略)
12. [使用示例](#12-使用示例)

---

## 1. 概述

### 1.1 设计目标

Asaki ComboSystem 是一个专注于**连招表现**的轻量级插件，负责管理角色的连招动画流程和状态转换，**不涉及伤害计算、命中反馈等战斗逻辑**。

### 1.2 核心职责

| 属于ComboSystem | 不属于ComboSystem |
|-----------------|-------------------|
| 连招状态管理 | 伤害计算 |
| 动画播放控制 | 受击反馈 |
| 判定框激活/禁用 | 血量管理 |
| 连招窗口管理 | 特效播放 |
| 输入缓冲 | 音效播放 |

### 1.3 适用场景

- 动作游戏的角色连招系统
- 格斗游戏的招式组合系统
- RPG游戏的技能连携系统

---

## 2. 设计原则

### 2.1 单一职责原则

连招系统只负责"连招的表现"，通过**回调机制**通知外部系统，由外部决定如何处理。

```csharp
// ComboSystem只负责通知，不处理具体逻辑
public event Action<HitBoxInfo> OnHitBoxActivated;  // 判定框激活时通知
public event Action<string> OnComboStateChanged;     // 状态变化时通知

// 外部Combat系统订阅并处理
comboSystem.OnHitBoxActivated += (hitBox) => {
    // Combat系统处理命中检测和伤害计算
    combatManager.ProcessHitDetection(hitBox);
};
```

### 2.2 与Graph系统的关系

- **运行时**：使用轻量级状态机，不使用Graph运行时（避免 overhead）
- **编辑器**：使用Graph系统创建可视化连招编辑器，导出为ComboTree资产

### 2.3 无跨模块事件

连招系统与Controller深度耦合，使用C#事件/Action而非AsakiBroker。

---

## 3. 系统架构

### 3.1 架构概览

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         外部系统（使用者实现）                                │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐         │
│  │  PlayerInput    │    │  CombatSystem   │    │  EffectSystem   │         │
│  │  (输入系统)      │    │  (战斗系统)      │    │  (特效系统)      │         │
│  └────────┬────────┘    └────────┬────────┘    └────────┬────────┘         │
│           │                      │                      │                  │
│           │  TriggerAttack()     │  OnHitBoxActivated   │  OnMoveStarted   │
│           │  OnStateChanged      │  OnMoveCompleted     │                  │
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
│                                    │                                        │
│           ┌────────────────────────┼────────────────────────┐               │
│           ▼                        ▼                        ▼               │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐         │
│  │  ComboTree      │    │  HitBox         │    │ Animation       │         │
│  │  (Scriptable)   │    │  (Colliders)    │    │ Event Receiver  │         │
│  └─────────────────┘    └─────────────────┘    └─────────────────┘         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                         编辑器层（基于Graph系统）                            │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    ComboGraphEditor 可视化编辑器                     │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                  │   │
│  │  │ MoveNode    │  │ Transition  │  │ Condition   │                  │   │
│  │  │ (招式节点)   │  │ Edge (连接)  │  │ Node (条件)  │                  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘                  │   │
│  │                           │                                         │   │
│  │                           ▼                                         │   │
│  │                    ┌─────────────┐                                  │   │
│  │                    │ Export to   │                                  │   │
│  │                    │ ComboTree   │                                  │   │
│  │                    └─────────────┘                                  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.2 职责分离说明

```
┌─────────────────────────────────────────────────────────────────┐
│                     ComboSystem 职责边界                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ComboSystem 负责：                                            │
│   ┌─────────────────────────────────────────────────────────┐  │
│   │  1. 接收输入 → 判断是否可以进入下一招                      │  │
│   │  2. 播放动画 → 通过AnimatorBridge控制动画                  │  │
│   │  3. 激活判定框 → 通知外部"判定框已激活"                    │  │
│   │  4. 管理连招窗口 → 决定何时可以接受下一次输入               │  │
│   │  5. 状态转换 → Idle → Startup → Active → Recovery         │  │
│   └─────────────────────────────────────────────────────────┘  │
│                                                                 │
│   ComboSystem 不负责：                                          │
│   ┌─────────────────────────────────────────────────────────┐  │
│   │  ✗ 检测碰撞（只激活Collider，外部Physics系统处理）         │  │
│   │  ✗ 计算伤害（通过回调通知外部CombatSystem）                │  │
│   │  ✗ 播放特效（通过回调通知外部EffectSystem）                │  │
│   │  ✗ 播放音效（通过回调通知外部AudioSystem）                 │  │
│   │  ✗ 处理受击（外部CombatSystem处理）                       │  │
│   └─────────────────────────────────────────────────────────┘  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 4. 核心模块设计

### 4.1 AsakiComboController - 连招控制器

```csharp
namespace Asaki.Plungin.ComboSystem
{
    /// <summary>
    /// 连招系统核心控制器 - 仅负责连招表现，不处理战斗逻辑
    /// </summary>
    public class AsakiComboController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private ComboTree comboTree;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        [Header("Settings")]
        [SerializeField] private float inputBufferDuration = 0.3f;

        // 核心组件
        private ComboStateMachine _stateMachine;
        private ComboAnimatorBridge _animatorBridge;
        private HitBoxManager _hitBoxManager;
        private InputBuffer _inputBuffer;

        // 运行时状态
        private ComboMove _currentMove;
        private int _comboCount;
        private float _comboTimer;

        #region 回调事件（供外部订阅）

        /// <summary>连招开始</summary>
        public event Action OnComboStarted;

        /// <summary>连招中断</summary>
        public event Action<InterruptReason> OnComboInterrupted;

        /// <summary>连招完成（自然结束）</summary>
        public event Action OnComboCompleted;

        /// <summary>招式开始（动画开始播放）</summary>
        public event Action<ComboMove> OnMoveStarted;

        /// <summary>招式判定开始（可以命中敌人）</summary>
        public event Action<HitBoxInfo[]> OnHitBoxesActivated;

        /// <summary>招式判定结束</summary>
        public event Action OnHitBoxesDeactivated;

        /// <summary>招式完成（动画播放完毕）</summary>
        public event Action<ComboMove> OnMoveCompleted;

        /// <summary>连招窗口开启（可以输入下一招）</summary>
        public event Action<float> OnComboWindowOpened;

        /// <summary>连招窗口关闭</summary>
        public event Action OnComboWindowClosed;

        /// <summary>状态变化</summary>
        public event Action<ComboStateType, ComboStateType> OnStateChanged;

        #endregion

        void Awake()
        {
            InitializeComponents();
        }

        void Update()
        {
            _stateMachine.Update(Time.deltaTime);
            _inputBuffer.Update(Time.deltaTime);
            _comboTimer += Time.deltaTime;
        }

        /// <summary>
        /// 触发攻击指令 - 由外部输入系统调用
        /// </summary>
        public void TriggerAttack(AttackInputType inputType)
        {
            if (!CanAcceptInput())
            {
                // 缓冲输入
                _inputBuffer.PushInput(inputType);
                return;
            }

            ProcessAttackInput(inputType);
        }

        /// <summary>
        /// 中断当前连招 - 由外部系统调用（如受击时）
        /// </summary>
        public void InterruptCombo(InterruptReason reason)
        {
            if (_stateMachine.CurrentStateType == ComboStateType.Idle)
                return;

            _stateMachine.ChangeState<ComboInterruptedState>();
            OnComboInterrupted?.Invoke(reason);

            // 清理
            _hitBoxManager.DeactivateAllHitBoxes();
            _comboCount = 0;
        }

        /// <summary>
        /// 重置连招状态
        /// </summary>
        public void ResetCombo()
        {
            _stateMachine.ChangeState<ComboIdleState>();
            _hitBoxManager.DeactivateAllHitBoxes();
            _inputBuffer.Clear();
            _comboCount = 0;
            _comboTimer = 0f;
            OnComboCompleted?.Invoke();
        }

        /// <summary>
        /// 检查是否可以接受输入
        /// </summary>
        public bool CanAcceptInput()
        {
            var state = _stateMachine.CurrentStateType;
            return state == ComboStateType.Idle ||
                   state == ComboStateType.ComboWindow;
        }

        #region 内部方法 - 状态机回调

        /// <summary>
        /// 由状态机调用 - 开始新招式
        /// </summary>
        internal void StartMove(ComboMove move)
        {
            _currentMove = move;
            _comboCount++;

            // 播放动画
            _animatorBridge.PlayMoveAnimation(move);

            // 通知外部
            OnMoveStarted?.Invoke(move);

            if (_comboCount == 1)
                OnComboStarted?.Invoke();
        }

        /// <summary>
        /// 由状态机调用 - 激活判定框
        /// </summary>
        internal void ActivateHitBoxes()
        {
            if (_currentMove?.HitBoxes == null) return;

            // 激活Collider
            _hitBoxManager.ActivateHitBoxes(_currentMove.HitBoxes);

            // 通知外部"判定框已激活" - 外部CombatSystem处理命中检测
            var hitBoxInfos = _currentMove.HitBoxes.Select(h => new HitBoxInfo
            {
                HitBoxId = h.HitBoxId,
                Collider = _hitBoxManager.GetCollider(h.HitBoxId),
                Owner = gameObject,
                MoveData = _currentMove
            }).ToArray();

            OnHitBoxesActivated?.Invoke(hitBoxInfos);
        }

        /// <summary>
        /// 由状态机调用 - 禁用判定框
        /// </summary>
        internal void DeactivateHitBoxes()
        {
            _hitBoxManager.DeactivateAllHitBoxes();
            OnHitBoxesDeactivated?.Invoke();
        }

        /// <summary>
        /// 由状态机调用 - 招式完成
        /// </summary>
        internal void CompleteMove()
        {
            OnMoveCompleted?.Invoke(_currentMove);
        }

        /// <summary>
        /// 由状态机调用 - 开启连招窗口
        /// </summary>
        internal void OpenComboWindow(float duration)
        {
            OnComboWindowOpened?.Invoke(duration);
        }

        /// <summary>
        /// 由状态机调用 - 关闭连招窗口
        /// </summary>
        internal void CloseComboWindow()
        {
            OnComboWindowClosed?.Invoke();
        }

        /// <summary>
        /// 由状态机调用 - 状态变化
        /// </summary>
        internal void NotifyStateChanged(ComboStateType from, ComboStateType to)
        {
            OnStateChanged?.Invoke(from, to);
        }

        #endregion
    }
}
```

### 4.2 核心组件说明

| 组件 | 职责 | 说明 |
|------|------|------|
| `ComboStateMachine` | 管理连招状态转换 | 轻量级状态机，不使用Graph运行时 |
| `ComboAnimatorBridge` | 动画控制桥接 | 播放动画、接收AnimationEvent |
| `HitBoxManager` | 判定框管理 | 仅管理Collider的激活/禁用，不处理碰撞 |
| `InputBuffer` | 输入缓冲 | 缓存输入，支持连招衔接 |

---

## 5. 数据结构设计

### 5.1 ComboMove - 招式数据

```csharp
namespace Asaki.Plungin.ComboSystem
{
    /// <summary>
    /// 招式数据定义 - 纯数据，无逻辑
    /// </summary>
    [Serializable]
    public class ComboMove
    {
        [Header("Basic")]
        public string MoveId;
        public string MoveName;

        [Header("Animation")]
        public string AnimationStateName;
        public float AnimationSpeed = 1f;

        [Header("Timing")]
        public float StartupTime;       // 前摇时间（从动画开始到判定开始）
        public float ActiveDuration;    // 判定持续时间
        public float RecoveryTime;      // 后摇时间
        public float ComboWindowStart;  // 连招窗口开始时间（相对于动画开始）
        public float ComboWindowEnd;    // 连招窗口结束时间

        [Header("Hit Boxes")]
        public HitBoxDefinition[] HitBoxes;

        [Header("Requirements")]
        public int MinComboCount;       // 最小连击数要求
        public int MaxComboCount;       // 最大连击数限制
        public float Cooldown;          // 冷却时间

        // 运行时数据
        [NonSerialized] public float LastUsedTime = -999f;

        public bool IsOnCooldown(float currentTime) =>
            currentTime - LastUsedTime < Cooldown;
    }

    /// <summary>
    /// 判定框定义 - 纯数据
    /// </summary>
    [Serializable]
    public class HitBoxDefinition
    {
        public string HitBoxId;
        public HitBoxShape Shape;
        public Vector3 Offset;
        public Vector3 Size;        // Box用
        public float Radius;        // Sphere/Capsule用
        public float Height;        // Capsule用
        public string BoneName;     // 跟随的骨骼名称
    }

    public enum HitBoxShape { Box, Sphere, Capsule }
}
```

### 5.2 ComboTree - 连招树

```csharp
namespace Asaki.Plungin.ComboSystem
{
    /// <summary>
    /// 连招树 - 包含所有招式和转换关系
    /// </summary>
    [CreateAssetMenu(fileName = "ComboTree", menuName = "Asaki/ComboSystem/ComboTree")]
    public class ComboTree : ScriptableObject
    {
        [Header("Info")]
        public string TreeId;
        public string Description;

        [Header("Moves")]
        public ComboMove[] Moves;

        [Header("Transitions")]
        public ComboTransition[] Transitions;

        [Header("Settings")]
        public float InputBufferWindow = 0.3f;
        public float MaxComboDuration = 10f;
        public int MaxComboLength = 10;

        // 运行时查找表
        private Dictionary<string, ComboMove> _moveLookup;
        private Dictionary<string, List<ComboTransition>> _transitionLookup;

        void OnEnable()
        {
            BuildLookupTables();
        }

        void BuildLookupTables()
        {
            _moveLookup = Moves?.ToDictionary(m => m.MoveId) ?? new();

            _transitionLookup = new();
            if (Transitions != null)
            {
                foreach (var t in Transitions)
                {
                    if (!_transitionLookup.ContainsKey(t.FromMoveId))
                        _transitionLookup[t.FromMoveId] = new();
                    _transitionLookup[t.FromMoveId].Add(t);
                }
            }
        }

        public ComboMove GetMove(string moveId) =>
            _moveLookup?.GetValueOrDefault(moveId);

        public List<ComboTransition> GetTransitions(string fromMoveId) =>
            _transitionLookup?.GetValueOrDefault(fromMoveId) ?? new();

        public ComboMove FindNextMove(string currentMoveId, AttackInputType input)
        {
            var transitions = GetTransitions(currentMoveId);
            return transitions
                .Where(t => t.InputType == input && t.IsValid())
                .Select(t => GetMove(t.ToMoveId))
                .FirstOrDefault();
        }
    }

    [Serializable]
    public class ComboTransition
    {
        public string FromMoveId;
        public string ToMoveId;
        public AttackInputType InputType;
        public TransitionCondition[] Conditions;

        public bool IsValid() => !string.IsNullOrEmpty(FromMoveId) &&
                                  !string.IsNullOrEmpty(ToMoveId);
    }

    [Serializable]
    public class TransitionCondition
    {
        public ConditionType Type;
        public string Parameter;
        public float Value;
    }
}
```

### 5.3 HitBoxInfo - 判定框信息（传递给外部）

```csharp
namespace Asaki.Plungin.ComboSystem
{
    /// <summary>
    /// 判定框信息 - 通过回调传递给外部系统
    /// </summary>
    public struct HitBoxInfo
    {
        public string HitBoxId;
        public Collider Collider;       // 激活的Collider，外部可以用它做检测
        public GameObject Owner;        // 攻击者
        public ComboMove MoveData;      // 招式数据（外部可读取伤害等信息）

        // 方便外部访问的数据
        public int Damage => MoveData?.Damage ?? 0;
        public float HitStun => MoveData?.HitStun ?? 0f;
    }
}
```

---

## 6. 状态机设计

### 6.1 状态类型

```csharp
namespace Asaki.Plungin.ComboSystem
{
    public enum ComboStateType
    {
        Idle,           // 待机
        Startup,        // 前摇
        Active,         // 判定中
        Recovery,       // 后摇
        ComboWindow,    // 连招窗口
        Interrupted     // 中断
    }

    public enum InterruptReason
    {
        Damaged,
        Stunned,
        KnockedDown,
        Forced,
        UserCancel
    }
}
```

### 6.2 状态转换图

```
                              ┌─────────────┐
                              │    Idle     │
                              └──────┬──────┘
                                     │ TriggerAttack
                                     ▼
                              ┌─────────────┐
                              │   Startup   │
                              │   (前摇)    │
                              └──────┬──────┘
                                     │ StartupTime elapsed
                                     ▼
                              ┌─────────────┐
                              │   Active    │
                              │  (判定中)   │
                              └──────┬──────┘
                                     │ ActiveDuration elapsed
                                     ▼
                              ┌─────────────┐
                              │   Recovery  │
                              │   (后摇)    │
                              └──────┬──────┘
                                     │ RecoveryTime elapsed
                                     ▼
                         ┌───────────────────────┐
                         │     ComboWindow       │
                         │     (连招窗口)        │
                         └───────────┬───────────┘
                                     │
            ┌────────────────────────┼────────────────────────┐
            │                        │                        │
            │ TriggerAttack          │ Timeout                │ Interrupted
            ▼                        ▼                        ▼
     ┌─────────────┐          ┌─────────────┐          ┌─────────────┐
     │   Startup   │          │    Idle     │          │ Interrupted │
     │  (下一招)   │          │  (连招结束)  │          │  (中断恢复)  │
     └─────────────┘          └─────────────┘          └──────┬──────┘
                                                              │
                                                              ▼
                                                       ┌─────────────┐
                                                       │    Idle     │
                                                       └─────────────┘
```

### 6.3 核心状态实现

```csharp
namespace Asaki.Plungin.ComboSystem.States
{
    /// <summary>
    /// 攻击判定状态
    /// </summary>
    public class ComboActiveState : ComboStateBase
    {
        private float _timer;
        private ComboMove _move;

        public void SetMove(ComboMove move) => _move = move;

        public override void OnEnter()
        {
            _timer = 0f;

            // 激活判定框
            Controller.ActivateHitBoxes();
        }

        public override void OnUpdate(float deltaTime)
        {
            _timer += deltaTime;

            // 检查是否进入后摇
            if (_timer >= _move.ActiveDuration)
            {
                Machine.ChangeState<ComboRecoveryState>();
            }
        }

        public override void OnExit()
        {
            // 禁用判定框
            Controller.DeactivateHitBoxes();
        }
    }

    /// <summary>
    /// 连招窗口状态
    /// </summary>
    public class ComboWindowState : ComboStateBase
    {
        private float _timer;
        private float _duration;

        public void SetDuration(float duration) => _duration = duration;

        public override void OnEnter()
        {
            _timer = 0f;
            Controller.OpenComboWindow(_duration);
        }

        public override void OnUpdate(float deltaTime)
        {
            _timer += deltaTime;

            // 检查是否有缓冲的输入
            if (Controller.InputBuffer.TryGetInput(out var input))
            {
                if (Controller.TryContinueCombo(input))
                    return;
            }

            // 窗口超时
            if (_timer >= _duration)
            {
                Controller.ResetCombo();
            }
        }

        public override void OnExit()
        {
            Controller.CloseComboWindow();
        }
    }
}
```

---

## 7. 动画事件集成

### 7.1 动画事件接收器

```csharp
namespace Asaki.Plungin.ComboSystem
{
    /// <summary>
    /// 动画事件接收器 - 附加在Animator所在GameObject
    /// </summary>
    public class ComboAnimationEventReceiver : MonoBehaviour
    {
        private AsakiComboController _controller;

        public void Initialize(AsakiComboController controller)
        {
            _controller = controller;
        }

        // 由Animation Event调用
        void OnComboEvent(string eventName)
        {
            _controller?.OnAnimationEvent(eventName);
        }

        // 预定义事件
        void OnStartupEnd() => _controller?.OnAnimationEvent("StartupEnd");
        void OnActiveStart() => _controller?.OnAnimationEvent("ActiveStart");
        void OnActiveEnd() => _controller?.OnAnimationEvent("ActiveEnd");
        void OnRecoveryEnd() => _controller?.OnAnimationEvent("RecoveryEnd");
        void OnComboWindowOpen() => _controller?.OnAnimationEvent("ComboWindowOpen");
    }
}
```

---

## 8. 判定系统（仅管理）

### 8.1 HitBoxManager - 仅管理Collider

```csharp
namespace Asaki.Plungin.ComboSystem
{
    /// <summary>
    /// 判定框管理器 - 仅负责Collider的激活/禁用
    /// 不处理任何碰撞检测逻辑
    /// </summary>
    public class HitBoxManager : MonoBehaviour
    {
        [SerializeField] private Transform hitBoxRoot;

        private Dictionary<string, HitBox> _hitBoxes = new();
        private List<HitBox> _activeHitBoxes = new();

        void Awake()
        {
            // 预创建判定框对象池
            InitializeHitBoxes();
        }

        /// <summary>
        /// 激活判定框 - 由状态机调用
        /// </summary>
        public void ActivateHitBoxes(HitBoxDefinition[] definitions)
        {
            foreach (var def in definitions)
            {
                if (_hitBoxes.TryGetValue(def.HitBoxId, out var hitBox))
                {
                    hitBox.Activate(def);
                    _activeHitBoxes.Add(hitBox);
                }
            }
        }

        /// <summary>
        /// 禁用所有判定框
        /// </summary>
        public void DeactivateAllHitBoxes()
        {
            foreach (var hitBox in _activeHitBoxes)
            {
                hitBox.Deactivate();
            }
            _activeHitBoxes.Clear();
        }

        /// <summary>
        /// 获取Collider - 外部CombatSystem使用
        /// </summary>
        public Collider GetCollider(string hitBoxId)
        {
            return _hitBoxes.TryGetValue(hitBoxId, out var hitBox)
                ? hitBox.Collider
                : null;
        }
    }

    /// <summary>
    /// 判定框对象
    /// </summary>
    public class HitBox : MonoBehaviour
    {
        public Collider Collider { get; private set; }
        public string CurrentId { get; private set; }

        public void Activate(HitBoxDefinition def)
        {
            CurrentId = def.HitBoxId;

            // 设置形状
            SetupShape(def);

            // 设置位置
            var bone = transform.parent.Find(def.BoneName);
            if (bone != null)
            {
                transform.SetParent(bone);
                transform.localPosition = def.Offset;
            }

            // 激活
            gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            gameObject.SetActive(false);
            transform.SetParent(transform.root);
        }

        void SetupShape(HitBoxDefinition def)
        {
            // 根据Shape设置Collider...
        }
    }
}
```

---

## 9. 接口与回调

### 9.1 输入接口

```csharp
namespace Asaki.Plungin.ComboSystem
{
    /// <summary>
    /// 连招输入接口
    /// </summary>
    public interface IComboInput
    {
        void TriggerAttack(AttackInputType inputType);
        void InterruptCombo(InterruptReason reason);
        void ResetCombo();
        bool CanAcceptInput();
    }

    public enum AttackInputType
    {
        LightAttack,
        HeavyAttack,
        Skill1,
        Skill2,
        Skill3,
        Ultimate
    }
}
```

### 9.2 回调使用示例

```csharp
// 外部CombatSystem示例
public class CombatSystem : MonoBehaviour
{
    [SerializeField] private AsakiComboController comboController;

    void OnEnable()
    {
        // 订阅ComboSystem的回调
        comboController.OnHitBoxesActivated += OnHitBoxesActivated;
        comboController.OnHitBoxesDeactivated += OnHitBoxesDeactivated;
        comboController.OnMoveStarted += OnMoveStarted;
    }

    void OnDisable()
    {
        comboController.OnHitBoxesActivated -= OnHitBoxesActivated;
        comboController.OnHitBoxesDeactivated -= OnHitBoxesDeactivated;
        comboController.OnMoveStarted -= OnMoveStarted;
    }

    /// <summary>
    /// 判定框激活时 - 开始检测碰撞
    /// </summary>
    void OnHitBoxesActivated(HitBoxInfo[] hitBoxes)
    {
        foreach (var hitBox in hitBoxes)
        {
            // 使用Collider进行碰撞检测
            var collider = hitBox.Collider;
            var hits = Physics.OverlapBox(
                collider.bounds.center,
                collider.bounds.extents,
                collider.transform.rotation,
                enemyLayer
            );

            foreach (var hit in hits)
            {
                // 处理命中
                ProcessHit(hitBox, hit);
            }
        }
    }

    /// <summary>
    /// 判定框禁用时 - 停止检测
    /// </summary>
    void OnHitBoxesDeactivated()
    {
        // 清理检测状态
    }

    /// <summary>
    /// 招式开始时 - 播放音效/特效
    /// </summary>
    void OnMoveStarted(ComboMove move)
    {
        // 播放音效
        AudioManager.Play(move.SwingSound);

        // 播放特效
        EffectManager.Play(move.StartEffect, transform.position);
    }

    void ProcessHit(HitBoxInfo hitBox, Collider target)
    {
        // 计算伤害
        int damage = CalculateDamage(hitBox.Damage);

        // 应用伤害
        target.GetComponent<Health>()?.TakeDamage(damage);

        // 播放受击特效
        EffectManager.PlayHitEffect(hitBox.MoveData.HitEffect, target.transform.position);
    }
}
```

---

## 10. 可视化编辑器（基于Graph）

### 10.1 编辑器架构

```
┌─────────────────────────────────────────────────────────────────┐
│                    ComboGraphEditor                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────┐      ┌─────────────┐      ┌─────────────┐     │
│  │  MoveNode   │──────│ Transition  │──────│  MoveNode   │     │
│  │  (轻攻击1)   │      │   Edge      │      │  (轻攻击2)   │     │
│  └─────────────┘      └─────────────┘      └─────────────┘     │
│         │                                          │            │
│         │                                          │            │
│         └──────────────────┬───────────────────────┘            │
│                            │                                    │
│                            ▼                                    │
│                     ┌─────────────┐                             │
│                     │  Export to  │                             │
│                     │ ComboTree   │                             │
│                     └─────────────┘                             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

```
┌─────────────────────────────────────────────────────────────────┐
│                    ComboGraphEditor                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────┐      ┌─────────────┐      ┌─────────────┐     │
│  │  MoveNode   │──────│ Transition  │──────│  MoveNode   │     │
│  │  (轻攻击1)   │      │   Edge      │      │  (轻攻击2)   │     │
│  └─────────────┘      └─────────────┘      └─────────────┘     │
│         │                                          │            │
│         │                                          │            │
│         └──────────────────┬───────────────────────┘            │
│                            │                                    │
│                            ▼                                    │
│                     ┌─────────────┐                             │
│                     │  Export to  │                             │
│                     │ ComboTree   │                             │
│                     └─────────────┘                             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 10.2 Graph节点定义

```csharp
#if UNITY_EDITOR
namespace Asaki.Plungin.ComboSystem.Editor
{
    /// <summary>
    /// 招式节点 - 用于Graph编辑器
    /// </summary>
    [Serializable]
    public class MoveNode : AsakiNodeBase
    {
        public ComboMove MoveData;

        [AsakiNodeInput("In")]
        public AsakiFlowPort InputFlow;

        [AsakiNodeOutput("Out")]
        public AsakiFlowPort OutputFlow;

        public override string Title => MoveData?.MoveName ?? "Move";
    }

    /// <summary>
    /// 条件节点
    /// </summary>
    [Serializable]
    public class ConditionNode : AsakiNodeBase
    {
        public TransitionCondition Condition;

        [AsakiNodeInput("In")]
        public AsakiFlowPort InputFlow;

        [AsakiNodeOutput("True")]
        public AsakiFlowPort TrueOutput;

        [AsakiNodeOutput("False")]
        public AsakiFlowPort FalseOutput;

        public override string Title => "Condition";
    }
}
#endif
```

---

## 11. 高级特性：自定义重置策略

### 11.1 设计动机

传统连招系统在连招中断或超时时总是将连击数重置为0。但在某些游戏设计中（如怪物猎人的气刃连斩、某些格斗游戏的连击累积机制），需要更灵活的重置策略：

- **保持计数**：连招中断后保持当前连击数
- **递减衰减**：每次中断减少一定连击数，但不归零
- **条件重置**：根据特定条件决定是否重置
- **自定义计算**：使用函数计算重置后的值

### 11.2 重置策略接口

```csharp
namespace Asaki.Plungin.ComboSystem
{
    /// <summary>
    /// 连招重置策略接口
    /// </summary>
    public interface IComboResetStrategy
    {
        /// <summary>
        /// 计算重置后的连招计数
        /// </summary>
        /// <param name="currentCount">当前连击数</param>
        /// <param name="context">连招上下文</param>
        /// <returns>重置后的计数</returns>
        int CalculateResetCount(int currentCount, ComboContext context);

        /// <summary>
        /// 检查是否应该重置
        /// </summary>
        bool ShouldReset(ComboContext context);
    }

    /// <summary>
    /// 连招上下文 - 传递给重置策略的数据
    /// </summary>
    public class ComboContext
    {
        public AsakiComboController Controller;
        public ComboMove CurrentMove;
        public ComboMove PreviousMove;
        public int ComboCount;
        public float ComboTimer;
        public InterruptReason? InterruptReason;
        public Dictionary<string, object> Blackboard = new();

        public T GetData<T>(string key) =>
            Blackboard.TryGetValue(key, out var val) ? (T)val : default;
        public void SetData<T>(string key, T value) =>
            Blackboard[key] = value;
    }
}
```

### 11.3 内置重置策略

```csharp
namespace Asaki.Plungin.ComboSystem.Strategies
{
    /// <summary>
    /// 重置为0策略（默认）
    /// </summary>
    public class ResetToZeroStrategy : IComboResetStrategy
    {
        public int CalculateResetCount(int currentCount, ComboContext context) => 0;
        public bool ShouldReset(ComboContext context) => true;
    }

    /// <summary>
    /// 保持计数策略 - 连招中断后保持当前计数
    /// </summary>
    public class KeepCountStrategy : IComboResetStrategy
    {
        public int CalculateResetCount(int currentCount, ComboContext context) => currentCount;
        public bool ShouldReset(ComboContext context) => false; // 不真正"重置"，只是保持
    }

    /// <summary>
    /// 递减策略 - 每次中断减少固定值
    /// </summary>
    public class DecayCountStrategy : IComboResetStrategy
    {
        public int DecayAmount = 1;
        public int MinCount = 0;

        public int CalculateResetCount(int currentCount, ComboContext context)
        {
            return Mathf.Max(MinCount, currentCount - DecayAmount);
        }

        public bool ShouldReset(ComboContext context) => true;
    }

    /// <summary>
    /// 百分比递减策略
    /// </summary>
    public class PercentageDecayStrategy : IComboResetStrategy
    {
        [Range(0f, 1f)]
        public float DecayPercent = 0.5f;
        public int MinCount = 0;

        public int CalculateResetCount(int currentCount, ComboContext context)
        {
            int newCount = Mathf.RoundToInt(currentCount * (1f - DecayPercent));
            return Mathf.Max(MinCount, newCount);
        }

        public bool ShouldReset(ComboContext context) => true;
    }

    /// <summary>
    /// 设置特定值策略
    /// </summary>
    public class SetToSpecificStrategy : IComboResetStrategy
    {
        public int TargetCount = 0;

        public int CalculateResetCount(int currentCount, ComboContext context) => TargetCount;
        public bool ShouldReset(ComboContext context) => true;
    }

    /// <summary>
    /// 条件重置策略 - 根据条件决定是否重置
    /// </summary>
    public class ConditionalResetStrategy : IComboResetStrategy
    {
        public Func<ComboContext, bool> Condition;
        public IComboResetStrategy TrueStrategy = new ResetToZeroStrategy();
        public IComboResetStrategy FalseStrategy = new KeepCountStrategy();

        public int CalculateResetCount(int currentCount, ComboContext context)
        {
            var strategy = Condition?.Invoke(context) == true ? TrueStrategy : FalseStrategy;
            return strategy.CalculateResetCount(currentCount, context);
        }

        public bool ShouldReset(ComboContext context) => true;
    }

    /// <summary>
    /// 自定义函数策略 - 使用委托自定义逻辑
    /// </summary>
    public class CustomResetStrategy : IComboResetStrategy
    {
        public Func<int, ComboContext, int> ResetFunction;
        public Func<ComboContext, bool> ShouldResetFunction;

        public int CalculateResetCount(int currentCount, ComboContext context)
        {
            return ResetFunction?.Invoke(currentCount, context) ?? 0;
        }

        public bool ShouldReset(ComboContext context)
        {
            return ShouldResetFunction?.Invoke(context) ?? true;
        }
    }
}
```

### 11.4 在ComboTree中配置重置策略

```csharp
namespace Asaki.Plungin.ComboSystem
{
    /// <summary>
    /// 连招树 - 支持自定义重置策略
    /// </summary>
    [CreateAssetMenu(fileName = "ComboTree", menuName = "Asaki/ComboSystem/ComboTree")]
    public class ComboTree : ScriptableObject
    {
        // ... 其他字段 ...

        [Header("Reset Strategies")]
        public ResetStrategyDefinition[] ResetStrategies;

        [Header("Default Reset")]
        public ResetComboMode DefaultResetMode = ResetComboMode.ResetToZero;

        // 运行时策略缓存
        private Dictionary<string, IComboResetStrategy> _strategyCache;

        void OnEnable()
        {
            BuildLookupTables();
            BuildResetStrategies();
        }

        void BuildResetStrategies()
        {
            _strategyCache = new();
            if (ResetStrategies != null)
            {
                foreach (var def in ResetStrategies)
                {
                    _strategyCache[def.GroupName] = CreateStrategy(def);
                }
            }
        }

        IComboResetStrategy CreateStrategy(ResetStrategyDefinition def)
        {
            return def.Mode switch
            {
                ResetComboMode.ResetToZero => new ResetToZeroStrategy(),
                ResetComboMode.KeepCount => new KeepCountStrategy(),
                ResetComboMode.Decay => new DecayCountStrategy
                {
                    DecayAmount = def.DecayAmount,
                    MinCount = def.MinCount
                },
                ResetComboMode.PercentageDecay => new PercentageDecayStrategy
                {
                    DecayPercent = def.DecayPercent,
                    MinCount = def.MinCount
                },
                ResetComboMode.SetToSpecific => new SetToSpecificStrategy
                {
                    TargetCount = def.SpecificValue
                },
                ResetComboMode.CustomFunction => new CustomResetStrategy
                {
                    ResetFunction = def.CustomResetFunction,
                    ShouldResetFunction = def.CustomShouldResetFunction
                },
                _ => new ResetToZeroStrategy()
            };
        }

        /// <summary>
        /// 应用重置策略
        /// </summary>
        public int ApplyResetStrategy(string groupName, int currentCount, ComboContext context)
        {
            if (_strategyCache.TryGetValue(groupName, out var strategy))
            {
                if (strategy.ShouldReset(context))
                {
                    return strategy.CalculateResetCount(currentCount, context);
                }
                return currentCount;
            }

            // 默认重置为0
            return 0;
        }
    }

    /// <summary>
    /// 重置策略定义
    /// </summary>
    [Serializable]
    public class ResetStrategyDefinition
    {
        public string GroupName;
        public ResetComboMode Mode;

        // Decay模式参数
        public int DecayAmount = 1;
        public float DecayPercent = 0.5f;
        public int MinCount = 0;

        // SetToSpecific模式参数
        public int SpecificValue = 0;

        // CustomFunction模式参数
        [NonSerialized]
        public Func<int, ComboContext, int> CustomResetFunction;
        [NonSerialized]
        public Func<ComboContext, bool> CustomShouldResetFunction;
    }

    /// <summary>
    /// 重置模式枚举
    /// </summary>
    public enum ResetComboMode
    {
        ResetToZero,        // 重置为0
        KeepCount,          // 保持当前计数
        Decay,              // 固定值递减
        PercentageDecay,    // 百分比递减
        SetToSpecific,      // 设置为特定值
        CustomFunction      // 自定义函数
    }
}
```

### 11.5 在转换中指定重置策略

```csharp
[Serializable]
public class ComboTransition
{
    public string FromMoveId;
    public string ToMoveId;
    public AttackInputType InputType;
    public TransitionCondition[] Conditions;

    [Header("Reset")]
    public string ResetGroup = "default";  // 使用哪个重置策略组
}
```

### 11.6 使用示例

```csharp
// 示例1：怪物猎人气刃连斩 - 保持计数
[CreateAssetMenu]
public class SpiritBladeCombo : ComboTree
{
    void Reset()
    {
        TreeId = "spirit_blade";

        // 定义保持计数的重置策略
        ResetStrategies = new[]
        {
            new ResetStrategyDefinition
            {
                GroupName = "spirit_combo",
                Mode = ResetComboMode.KeepCount  // 连招窗口超时不重置
            },
            new ResetStrategyDefinition
            {
                GroupName = "on_damaged",
                Mode = ResetComboMode.SetToSpecific,
                SpecificValue = 0  // 受击时重置
            }
        };

        Transitions = new[]
        {
            new ComboTransition
            {
                FromMoveId = "spirit_blade_1",
                ToMoveId = "spirit_blade_2",
                InputType = AttackInputType.LightAttack,
                ResetGroup = "spirit_combo"  // 使用保持计数策略
            },
            new ComboTransition
            {
                FromMoveId = "spirit_blade_2",
                ToMoveId = "spirit_blade_3",
                InputType = AttackInputType.LightAttack,
                ResetGroup = "spirit_combo"
            }
        };
    }
}

// 示例2：递减衰减系统
[CreateAssetMenu]
public class DecayComboSystem : ComboTree
{
    void Reset()
    {
        ResetStrategies = new[]
        {
            new ResetStrategyDefinition
            {
                GroupName = "decay_on_interrupt",
                Mode = ResetComboMode.Decay,
                DecayAmount = 2,      // 每次中断减2
                MinCount = 0
            }
        };
    }
}

// 示例3：自定义函数 - 根据连击质量决定保留多少
[CreateAssetMenu]
public class QualityBasedCombo : ComboTree
{
    void Reset()
    {
        ResetStrategies = new[]
        {
            new ResetStrategyDefinition
            {
                GroupName = "quality_based",
                Mode = ResetComboMode.CustomFunction,
                CustomResetFunction = (currentCount, context) =>
                {
                    // 根据命中率决定保留多少连击
                    float hitRate = context.GetData<float>("HitRate");
                    if (hitRate > 0.8f) return currentCount;      // 高命中率保持
                    if (hitRate > 0.5f) return currentCount / 2;  // 中等保留一半
                    return 0;                                      // 低命中率重置
                },
                CustomShouldResetFunction = (context) =>
                {
                    // 只有在连招超时时才应用重置
                    return context.InterruptReason == null;
                }
            }
        };
    }
}

// 示例4：运行时动态设置策略
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private ComboTree comboTree;
    private AsakiComboController _comboSystem;

    void Start()
    {
        _comboSystem = GetComponent<AsakiComboController>();
        _comboSystem.Initialize(comboTree);

        // 订阅中断事件，动态决定重置策略
        _comboSystem.OnComboInterrupted += (reason) =>
        {
            var context = new ComboContext
            {
                Controller = _comboSystem,
                ComboCount = _comboSystem.CurrentComboCount,
                InterruptReason = reason
            };

            // 根据原因选择不同策略
            string strategyGroup = reason switch
            {
                InterruptReason.Damaged => "on_damaged",
                InterruptReason.UserCancel => "on_cancel",
                _ => "default"
            };

            int newCount = comboTree.ApplyResetStrategy(
                strategyGroup,
                _comboSystem.CurrentComboCount,
                context
            );

            _comboSystem.SetComboCount(newCount);
        };
    }
}
```

### 11.7 策略组合与链式调用

```csharp
/// <summary>
    /// 组合策略 - 多个策略链式执行
    /// </summary>
    public class CompositeResetStrategy : IComboResetStrategy
    {
        public List<IComboResetStrategy> Strategies = new();
        public CompositeMode Mode = CompositeMode.Sequential;

        public enum CompositeMode
        {
            Sequential,     // 顺序执行，前一个结果作为后一个输入
            Minimum,        // 取所有策略的最小值
            Maximum,        // 取所有策略的最大值
            Average         // 取平均值
        }

        public int CalculateResetCount(int currentCount, ComboContext context)
        {
            if (Strategies.Count == 0) return currentCount;

            switch (Mode)
            {
                case CompositeMode.Sequential:
                    int result = currentCount;
                    foreach (var strategy in Strategies)
                    {
                        result = strategy.CalculateResetCount(result, context);
                    }
                    return result;

                case CompositeMode.Minimum:
                    return Strategies.Min(s => s.CalculateResetCount(currentCount, context));

                case CompositeMode.Maximum:
                    return Strategies.Max(s => s.CalculateResetCount(currentCount, context));

                case CompositeMode.Average:
                    var results = Strategies.Select(s => s.CalculateResetCount(currentCount, context));
                    return Mathf.RoundToInt(results.Average());

                default:
                    return currentCount;
            }
        }

        public bool ShouldReset(ComboContext context)
        {
            // 任一策略认为应该重置，就重置
            return Strategies.Any(s => s.ShouldReset(context));
        }
    }
```

---

## 12. 使用示例

### 12.1 基础使用

```csharp
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private AsakiComboController combo;
    [SerializeField] private CombatSystem combat;

    void Start()
    {
        // CombatSystem订阅ComboSystem的回调
        combo.OnHitBoxesActivated += combat.OnComboHitBoxesActivated;
        combo.OnMoveStarted += combat.OnComboMoveStarted;
    }

    void Update()
    {
        // 输入传递给ComboSystem
        if (Input.GetButtonDown("Fire1"))
            combo.TriggerAttack(AttackInputType.LightAttack);

        if (Input.GetButtonDown("Fire2"))
            combo.TriggerAttack(AttackInputType.HeavyAttack);
    }

    // 受击时中断连招
    public void OnTakeDamage()
    {
        combo.InterruptCombo(InterruptReason.Damaged);
    }
}
```

### 12.2 创建连招数据

```csharp
[CreateAssetMenu(fileName = "PlayerCombo", menuName = "Asaki/ComboSystem/PlayerCombo")]
public class PlayerComboTree : ComboTree
{
    void Reset()
    {
        TreeId = "player_basic";
        Moves = new[]
        {
            new ComboMove
            {
                MoveId = "light_1",
                MoveName = "轻攻击1",
                AnimationStateName = "LightAttack1",
                StartupTime = 0.1f,
                ActiveDuration = 0.2f,
                RecoveryTime = 0.3f,
                ComboWindowStart = 0.15f,
                ComboWindowEnd = 0.4f,
                Damage = 10,
                HitStun = 0.3f,
                HitBoxes = new[]
                {
                    new HitBoxDefinition
                    {
                        HitBoxId = "hand_r",
                        Shape = HitBoxShape.Sphere,
                        Radius = 0.3f,
                        BoneName = "Hand_R"
                    }
                }
            },
            new ComboMove
            {
                MoveId = "light_2",
                MoveName = "轻攻击2",
                AnimationStateName = "LightAttack2",
                // ...
            }
        };

        Transitions = new[]
        {
            new ComboTransition
            {
                FromMoveId = "light_1",
                ToMoveId = "light_2",
                InputType = AttackInputType.LightAttack
            }
        };
    }
}
```

---

## 附录

### 文件结构

```
Assets/Asaki/Plungin/ComboSystem/
├── Runtime/
│   ├── Core/
│   │   ├── AsakiComboController.cs
│   │   ├── ComboStateMachine.cs
│   │   ├── ComboAnimatorBridge.cs
│   │   └── HitBoxManager.cs
│   ├── States/
│   │   ├── ComboStateBase.cs
│   │   ├── ComboIdleState.cs
│   │   ├── ComboActiveState.cs
│   │   └── ComboWindowState.cs
│   ├── Data/
│   │   ├── ComboTree.cs
│   │   ├── ComboMove.cs
│   │   └── HitBoxInfo.cs
│   └── Utils/
│       └── InputBuffer.cs
├── Editor/
│   ├── ComboGraphEditor.cs
│   ├── MoveNodeView.cs
│   └── ComboTreeExporter.cs
└── ComboSystem.asmdef
```

---

*文档版本: 2.0*
*最后更新: 2026-02-05*
*作者: Asaki Framework Team*
