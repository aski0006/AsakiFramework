# Asaki FSM System

[![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity.com)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Asaki FSM System 是一个为 Unity 设计的轻量级有限状态机框架，提供类型安全、零GC分配的状态管理方案。适用于角色AI、游戏流程控制、UI状态管理等场景。

## 📋 目录

- [概述](#概述)
- [功能特性](#功能特性)
- [架构设计](#架构设计)
- [核心组件](#核心组件)
- [快速开始](#快速开始)
- [创建自定义状态](#创建自定义状态)
- [状态转换](#状态转换)
- [最佳实践](#最佳实践)

## 🎯 概述

Asaki FSM 基于泛型设计，提供类型安全的状态管理。核心设计理念：

- **懒加载缓存**：状态仅在首次使用时创建，之后复用
- **零运行时GC**：通过对象池复用避免频繁的内存分配
- **泛型驱动**：编译时类型检查，避免运行时类型错误
- **Unity集成**：与 MonoBehaviour 生命周期无缝配合

### 适用场景

| 场景 | 说明 |
|------|------|
| **角色AI** | 管理角色的Idle、Walk、Attack、Dead等状态 |
| **游戏流程** | 控制Menu、Playing、Paused、GameOver等游戏状态 |
| **UI状态** | 管理面板的显示、隐藏、动画过渡状态 |
| **动画系统** | 与Animator配合，管理复杂的动画状态逻辑 |

## ✨ 功能特性

### 核心功能

- ✅ **类型安全**：泛型约束确保状态类型正确
- ✅ **零GC分配**：状态缓存池避免运行时内存分配
- ✅ **生命周期管理**：完整的OnEnter、OnUpdate、OnFixedUpdate、OnExit回调
- ✅ **状态复用**：状态实例缓存，支持频繁切换
- ✅ **上下文访问**：状态可直接访问持有者上下文
- ✅ **Unity集成**：与Update/FixedUpdate生命周期同步

### 性能特性

| 特性 | 说明 |
|------|------|
| 懒加载 | 状态首次使用时创建，避免初始化开销 |
| 对象缓存 | 状态实例复用，无GC压力 |
| 字典查找 | O(1)状态获取性能 |
| 零分配切换 | 状态切换不产生临时对象 |

## 🏗️ 架构设计

```
┌─────────────────────────────────────────────────────────────────┐
│                     Asaki FSM System                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                 AsakiStateMachine<TContext>             │   │
│  │                      (状态机管理器)                      │   │
│  │                                                         │   │
│  │  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐ │   │
│  │  │  StateCache │───▶│  State A    │───▶│  State B    │ │   │
│  │  │  (状态缓存)  │    │  (状态A)    │    │  (状态B)    │ │   │
│  │  └─────────────┘    └─────────────┘    └─────────────┘ │   │
│  │         │                                            │   │
│  │         ▼                                            │   │
│  │  ┌─────────────┐                                     │   │
│  │  │  Current    │                                     │   │
│  │  │  State      │                                     │   │
│  │  └─────────────┘                                     │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              │                                  │
│                              ▼                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                  AsakiState<TContext>                   │   │
│  │                     (状态基类)                          │   │
│  │                                                         │   │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌────────┐  │   │
│  │  │ OnEnter  │  │ OnUpdate │  │OnFixedUpd│  │ OnExit │  │   │
│  │  │ (进入)   │  │ (更新)   │  │ate(物理) │  │ (退出) │  │   │
│  │  └──────────┘  └──────────┘  └──────────┘  └────────┘  │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              │                                  │
│                              ▼                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                    TContext                             │   │
│  │                  (上下文持有者)                         │   │
│  │              (PlayerController/GameManager等)           │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 状态生命周期

```
┌─────────────────────────────────────────────────────────────────┐
│                        状态生命周期                              │
└─────────────────────────────────────────────────────────────────┘

    ChangeState<A>()              ChangeState<B>()
         │                             │
         ▼                             ▼
    ┌─────────┐                   ┌─────────┐
    │ State A │                   │ State B │
    └────┬────┘                   └────┬────┘
         │                             │
    OnEnter()                    OnEnter()
         │                             │
         ▼                             ▼
    ┌─────────┐                   ┌─────────┐
    │ Running │ ── OnExit() ───▶  │ Running │
    │ (运行中) │                   │ (运行中) │
    └─────────┘                   └─────────┘
         │                             │
    OnUpdate()                     OnUpdate()
    OnFixedUpdate()              OnFixedUpdate()
         │                             │
         ▼                             ▼
    [CheckTransition]             [CheckTransition]
```

## 🔧 核心组件

### 1. AsakiStateMachine<TContext>（状态机管理器）

```csharp
// 创建状态机
public class PlayerController : MonoBehaviour
{
    private AsakiStateMachine<PlayerController> _stateMachine;
    
    private void Awake()
    {
        // 传入上下文（通常是自身）
        _stateMachine = new AsakiStateMachine<PlayerController>(this);
    }
    
    private void Start()
    {
        // 切换到初始状态
        _stateMachine.ChangeState<IdleState>();
    }
    
    private void Update()
    {
        // 驱动状态更新
        _stateMachine.Update(Time.deltaTime);
    }
    
    private void FixedUpdate()
    {
        // 驱动物理更新
        _stateMachine.FixedUpdate(Time.fixedDeltaTime);
    }
    
    private void OnDestroy()
    {
        // 清理状态机
        _stateMachine.Stop();
    }
}
```

**核心API：**

| 方法 | 说明 |
|------|------|
| `ChangeState<TState>()` | 切换到指定状态类型 |
| `Update(deltaTime)` | 驱动状态每帧更新 |
| `FixedUpdate(fixedDeltaTime)` | 驱动状态物理更新 |
| `Stop()` | 停止状态机并清理资源 |
| `CurrentState` | 获取当前状态实例 |

### 2. AsakiState<TContext>（状态基类）

```csharp
public class IdleState : AsakiState<PlayerController>
{
    private float _idleTimer;
    
    public override void OnEnter()
    {
        // 进入状态时的初始化
        _idleTimer = 0f;
        Context.PlayAnimation("Idle");
    }
    
    public override void OnUpdate(float deltaTime)
    {
        // 每帧更新
        base.OnUpdate(deltaTime);
        
        _idleTimer += deltaTime;
        
        // 可以在这里处理输入等逻辑
        if (Context.Input.Movement.sqrMagnitude > 0.01f)
        {
            Machine.ChangeState<WalkState>();
        }
    }
    
    protected override void CheckTransition(float deltaTime)
    {
        // 检查状态转换条件
        if (_idleTimer > 5f)
        {
            Machine.ChangeState<SleepState>();
        }
    }
    
    public override void OnExit()
    {
        // 退出状态时的清理
        Debug.Log("Exit Idle State");
    }
}
```

**生命周期方法：**

| 方法 | 调用时机 | 用途 |
|------|----------|------|
| `OnEnter()` | 进入状态时 | 初始化状态数据、播放动画 |
| `OnUpdate()` | 每帧 | 输入处理、逻辑更新 |
| `OnFixedUpdate()` | 固定时间步 | 物理相关更新 |
| `OnExit()` | 退出状态时 | 清理资源、保存状态 |
| `CheckTransition()` | OnUpdate中 | 检查状态转换条件 |

**可访问属性：**

| 属性 | 说明 |
|------|------|
| `Context` | 状态持有者上下文 |
| `Machine` | 所属状态机实例 |

### 3. IAsakiState（状态接口）

```csharp
// 如需自定义状态实现，可实现此接口
public interface IAsakiState
{
    void OnEnter();
    void OnUpdate(float deltaTime);
    void OnFixedUpdate(float fixedDeltaTime);
    void OnExit();
}
```

## 🚀 快速开始

### 1. 定义上下文类

```csharp
using Asaki.Core.FSM;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Animator animator;
    
    public Vector2 InputDirection { get; private set; }
    public Animator Animator => animator;
    
    private AsakiStateMachine<PlayerController> _stateMachine;
    
    private void Awake()
    {
        _stateMachine = new AsakiStateMachine<PlayerController>(this);
    }
    
    private void Start()
    {
        _stateMachine.ChangeState<IdleState>();
    }
    
    private void Update()
    {
        InputDirection = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;
        
        _stateMachine.Update(Time.deltaTime);
    }
    
    private void FixedUpdate()
    {
        _stateMachine.FixedUpdate(Time.fixedDeltaTime);
    }
    
    public void Move(Vector2 direction)
    {
        transform.position += (Vector3)direction * moveSpeed * Time.deltaTime;
    }
}
```

### 2. 创建状态类

```csharp
using Asaki.Core.FSM;
using UnityEngine;

public class IdleState : AsakiState<PlayerController>
{
    public override void OnEnter()
    {
        Context.Animator.SetBool("IsMoving", false);
    }
    
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        
        if (Context.InputDirection.sqrMagnitude > 0.01f)
        {
            Machine.ChangeState<WalkState>();
        }
    }
}

public class WalkState : AsakiState<PlayerController>
{
    public override void OnEnter()
    {
        Context.Animator.SetBool("IsMoving", true);
    }
    
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        
        if (Context.InputDirection.sqrMagnitude < 0.01f)
        {
            Machine.ChangeState<IdleState>();
            return;
        }
        
        Context.Move(Context.InputDirection);
    }
}
```

### 3. 运行测试

1. 创建Player游戏对象
2. 添加PlayerController组件
3. 配置Animator引用
4. 运行游戏，使用方向键控制角色

## 📝 创建自定义状态

### 基础状态

```csharp
public class AttackState : AsakiState<PlayerController>
{
    private float _attackDuration = 0.5f;
    private float _timer;
    
    public override void OnEnter()
    {
        _timer = 0f;
        Context.Animator.SetTrigger("Attack");
        Context.PerformAttack();
    }
    
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        
        _timer += deltaTime;
        if (_timer >= _attackDuration)
        {
            Machine.ChangeState<IdleState>();
        }
    }
}
```

### 带参数的状态

```csharp
public class JumpState : AsakiState<PlayerController>
{
    private float _jumpForce;
    private bool _hasJumped;
    
    // 可以通过上下文传递参数
    public override void OnEnter()
    {
        _hasJumped = false;
        _jumpForce = Context.GetJumpForce();
        Context.Animator.SetTrigger("Jump");
    }
    
    public override void OnFixedUpdate(float fixedDeltaTime)
    {
        if (!_hasJumped)
        {
            Context.ApplyJumpForce(_jumpForce);
            _hasJumped = true;
        }
        
        // 检测落地
        if (Context.IsGrounded)
        {
            Machine.ChangeState<IdleState>();
        }
    }
}
```

### 复杂状态示例（带子状态）

```csharp
public class CombatState : AsakiState<PlayerController>
{
    private enum CombatSubState
    {
        Ready,
        Attacking,
        Defending,
        Cooldown
    }
    
    private CombatSubState _subState;
    private float _cooldownTimer;
    
    public override void OnEnter()
    {
        _subState = CombatSubState.Ready;
        Context.EnterCombatStance();
    }
    
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        
        switch (_subState)
        {
            case CombatSubState.Ready:
                HandleReadyState();
                break;
            case CombatSubState.Attacking:
                HandleAttackingState(deltaTime);
                break;
            case CombatSubState.Defending:
                HandleDefendingState();
                break;
            case CombatSubState.Cooldown:
                HandleCooldownState(deltaTime);
                break;
        }
    }
    
    private void HandleReadyState()
    {
        if (Input.GetButtonDown("Attack"))
        {
            _subState = CombatSubState.Attacking;
            Context.PerformAttack();
        }
        else if (Input.GetButton("Defend"))
        {
            _subState = CombatSubState.Defending;
            Context.StartDefending();
        }
    }
    
    private void HandleAttackingState(float deltaTime)
    {
        if (!Context.IsAttacking)
        {
            _subState = CombatSubState.Cooldown;
            _cooldownTimer = 0.3f;
        }
    }
    
    private void HandleDefendingState()
    {
        if (!Input.GetButton("Defend"))
        {
            Context.StopDefending();
            _subState = CombatSubState.Ready;
        }
    }
    
    private void HandleCooldownState(float deltaTime)
    {
        _cooldownTimer -= deltaTime;
        if (_cooldownTimer <= 0)
        {
            _subState = CombatSubState.Ready;
        }
    }
    
    protected override void CheckTransition(float deltaTime)
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Machine.ChangeState<IdleState>();
        }
    }
    
    public override void OnExit()
    {
        Context.ExitCombatStance();
    }
}
```

## 🔄 状态转换

### 基础转换

```csharp
// 在状态内部切换到其他状态
Machine.ChangeState<WalkState>();
```

### 条件转换

```csharp
protected override void CheckTransition(float deltaTime)
{
    // 检查健康值
    if (Context.Health <= 0)
    {
        Machine.ChangeState<DeadState>();
        return;
    }
    
    // 检查被击晕
    if (Context.IsStunned)
    {
        Machine.ChangeState<StunState>();
        return;
    }
    
    // 检查输入
    if (Input.GetButtonDown("Jump") && Context.IsGrounded)
    {
        Machine.ChangeState<JumpState>();
    }
}
```

### 外部触发转换

```csharp
public class PlayerController : MonoBehaviour
{
    private AsakiStateMachine<PlayerController> _stateMachine;
    
    // 外部事件触发状态转换
    public void TakeDamage(float damage)
    {
        Health -= damage;
        
        if (Health <= 0 && !(_stateMachine.CurrentState is DeadState))
        {
            _stateMachine.ChangeState<DeadState>();
        }
        else if (!(_stateMachine.CurrentState is HurtState))
        {
            _stateMachine.ChangeState<HurtState>();
        }
    }
    
    public void Interact()
    {
        if (_stateMachine.CurrentState is IdleState ||
            _stateMachine.CurrentState is WalkState)
        {
            _stateMachine.ChangeState<InteractState>();
        }
    }
}
```

## 📚 最佳实践

### 1. 状态命名规范

```csharp
// ✅ 推荐：使用描述性状态名
public class IdleState : AsakiState<PlayerController> { }
public class WalkState : AsakiState<PlayerController> { }
public class AttackState : AsakiState<PlayerController> { }

// ❌ 避免：过于简单或模糊的名称
public class State1 : AsakiState<PlayerController> { }
public class S1 : AsakiState<PlayerController> { }
```

### 2. 状态职责单一

```csharp
// ✅ 推荐：每个状态只负责一种行为
public class WalkState : AsakiState<PlayerController>
{
    public override void OnUpdate(float deltaTime)
    {
        // 只处理移动逻辑
        Context.Move(Context.InputDirection);
    }
}

// ❌ 避免：一个状态处理过多逻辑
public class PlayerState : AsakiState<PlayerController>
{
    public override void OnUpdate(float deltaTime)
    {
        // 同时处理移动、攻击、跳跃...
        if (Context.IsAttacking) { /* ... */ }
        else if (Context.IsJumping) { /* ... */ }
        else { /* 移动逻辑 */ }
    }
}
```

### 3. 合理使用CheckTransition

```csharp
public class PatrolState : AsakiState<EnemyController>
{
    protected override void CheckTransition(float deltaTime)
    {
        // ✅ 只检查转换条件，不执行状态逻辑
        if (Context.CanSeePlayer())
        {
            Machine.ChangeState<ChaseState>();
        }
        else if (Context.IsPatrolComplete())
        {
            Machine.ChangeState<IdleState>();
        }
    }
    
    public override void OnUpdate(float deltaTime)
    {
        // 先执行状态逻辑
        Context.Patrol();
        
        // 再调用基类方法（会执行CheckTransition）
        base.OnUpdate(deltaTime);
    }
}
```

### 4. 状态数据管理

```csharp
public class ChargeAttackState : AsakiState<PlayerController>
{
    // ✅ 状态内部数据
    private float _chargeTime;
    private const float MAX_CHARGE_TIME = 2f;
    
    // ✅ 通过上下文访问共享数据
    public override void OnEnter()
    {
        _chargeTime = 0f;
        Context.StartChargingVFX();
    }
    
    public override void OnUpdate(float deltaTime)
    {
        _chargeTime += deltaTime;
        
        // 更新UI显示
        Context.UpdateChargeUI(_chargeTime / MAX_CHARGE_TIME);
        
        base.OnUpdate(deltaTime);
    }
    
    public override void OnExit()
    {
        // 计算最终伤害
        float damage = Context.BaseDamage * (1 + _chargeTime / MAX_CHARGE_TIME);
        Context.ExecuteChargedAttack(damage);
        Context.StopChargingVFX();
    }
}
```

### 5. 错误处理

```csharp
public class ComplexState : AsakiState<PlayerController>
{
    public override void OnEnter()
    {
        try
        {
            Context.InitializeComplexSystem();
        }
        catch (Exception e)
        {
            Debug.LogError($"[ComplexState] 初始化失败: {e}");
            // 切换到安全状态
            Machine.ChangeState<IdleState>();
        }
    }
    
    public override void OnExit()
    {
        try
        {
            Context.CleanupComplexSystem();
        }
        catch (Exception e)
        {
            Debug.LogError($"[ComplexState] 清理失败: {e}");
        }
    }
}
```

### 6. 性能优化

```csharp
public class OptimizedState : AsakiState<PlayerController>
{
    // ✅ 缓存引用，避免每帧查找
    private Transform _target;
    private float _sqrDetectionRange;
    
    public override void OnEnter()
    {
        _target = Context.FindTarget();
        _sqrDetectionRange = Context.DetectionRange * Context.DetectionRange;
    }
    
    protected override void CheckTransition(float deltaTime)
    {
        // ✅ 使用平方距离比较，避免开方运算
        float sqrDistance = (_target.position - Context.Transform.position).sqrMagnitude;
        if (sqrDistance < _sqrDetectionRange)
        {
            Machine.ChangeState<AttackState>();
        }
    }
}
```

## 🔗 依赖关系

- **Unity Engine**: 2021.3+ (MonoBehaviour生命周期)
- **System.Collections.Generic**: Dictionary用于状态缓存

## 📄 许可证

MIT License

---

**作者**: Asaki Framework Team  
**版本**: 1.0.0  
**最后更新**: 2026-02-05
