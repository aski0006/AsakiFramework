# Asaki FSM System - 使用示例与API参考

本文档提供 Asaki FSM 系统的详细使用指南，包括推荐使用场景、完整API参考和实用代码示例。

---

## 第一部分：使用用途推荐

### 1. 游戏角色AI状态管理

**适用场景**：
- 玩家角色控制（Idle、Walk、Run、Jump、Attack等）
- NPC敌人AI（Patrol、Chase、Attack、Dead等）
- Boss战斗阶段（Phase1、Phase2、Phase3、Enrage等）

**推荐原因**：
- 状态职责清晰，每个状态独立处理特定行为
- 通过 `CheckTransition` 方法集中管理状态转换逻辑
- 状态复用机制避免频繁创建/销毁对象

```
Player State Machine:
┌─────────┐    移动输入    ┌─────────┐    停止移动    ┌─────────┐
│  Idle   │──────────────▶│  Walk   │──────────────▶│  Idle   │
└────┬────┘               └────┬────┘               └─────────┘
     │                         │
     │ 攻击键                   │ 攻击键
     ▼                         ▼
┌─────────┐               ┌─────────┐
│ Attack  │               │ Attack  │
└────┬────┘               └────┬────┘
     │                         │
     │ 攻击结束                 │ 攻击结束
     ▼                         ▼
┌─────────┐               ┌─────────┐
│  Idle   │◀──────────────│  Walk   │
└─────────┘               └─────────┘
```

### 2. 游戏流程控制

**适用场景**：
- 游戏整体流程（Menu、Loading、Playing、Paused、GameOver）
- 关卡流程（Intro、Gameplay、BossFight、Victory）
- 剧情演出（Dialogue、Cutscene、Choice、Transition）

**推荐原因**：
- 流程状态转换严格可控
- 每个状态可独立管理资源加载/释放
- 支持子状态机实现复杂流程

```
Game Flow State Machine:
┌─────────┐   开始游戏    ┌─────────┐   加载完成   ┌─────────┐
│  Menu   │─────────────▶│ Loading │─────────────▶│ Playing │
└─────────┘              └─────────┘              └────┬────┘
     ▲                                                 │
     │                                                 │ 暂停
     │                                                 ▼
     │                                            ┌─────────┐
     │                                            │ Paused  │
     │                                            └────┬────┘
     │                                                 │ 恢复
     │                                                 │
     │              ┌─────────┐   重新开始            │
     └──────────────│GameOver │◀──────────────────────┘
                    └─────────┘   游戏结束
```

### 3. UI状态管理

**适用场景**：
- 面板动画状态（Hidden、Showing、Visible、Hiding）
- 弹窗管理（Closed、Opening、Open、Closing）
- 复杂UI流程（FormStep1、FormStep2、FormStep3、Confirm）

**推荐原因**：
- 动画与逻辑分离
- 防止重复触发状态转换
- 支持异步操作（如资源加载）

### 4. 动画系统配合

**适用场景**：
- 与Unity Animator配合管理复杂动画状态
- 动画过渡控制
- 动画事件处理

**推荐原因**：
- 动画状态与游戏逻辑状态同步
- 支持动画完成回调
- 可处理动画打断逻辑

### 5. 网络状态管理

**适用场景**：
- 连接状态（Disconnected、Connecting、Connected、Reconnecting）
- 游戏房间状态（Lobby、Matchmaking、InRoom、Playing）
- 数据同步状态（Idle、Syncing、Synced、Error）

**推荐原因**：
- 网络状态转换严格可控
- 错误状态统一处理
- 支持重连逻辑

### 6. 物理对象状态

**适用场景**：
- 平台移动（Idle、Moving、Pausing、Stopped）
- 机关触发（Inactive、Activating、Active、Deactivating）
- 载具控制（Parked、Starting、Running、Stopping）

**推荐原因**：
- 与FixedUpdate生命周期集成
- 物理状态转换平滑
- 支持物理事件响应

---

## 第二部分：FSM系统API

### 2.1 AsakiStateMachine<TContext>

状态机管理器类，负责状态的切换和生命周期管理。

#### 构造函数

```csharp
public AsakiStateMachine(TContext context)
```

| 参数 | 类型 | 说明 |
|------|------|------|
| context | TContext | 状态持有者上下文实例 |

**示例**：
```csharp
var stateMachine = new AsakiStateMachine<PlayerController>(this);
```

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| Context | TContext | 获取状态持有者上下文 |
| CurrentState | AsakiState<TContext> | 获取当前激活的状态实例 |

**示例**：
```csharp
// 获取上下文
var player = stateMachine.Context;

// 获取当前状态
var currentState = stateMachine.CurrentState;
if (currentState is AttackState)
{
    Debug.Log("当前正在攻击状态");
}
```

#### 方法

##### ChangeState<TState>()

```csharp
public void ChangeState<TState>() where TState : AsakiState<TContext>, new()
```

切换到指定类型的状态。

| 类型参数 | 约束 | 说明 |
|----------|------|------|
| TState | AsakiState<TContext>, new() | 目标状态类型 |

**执行流程**：
1. 调用当前状态的 `OnExit()` 方法
2. 从缓存获取或创建目标状态实例
3. 调用目标状态的 `OnEnter()` 方法

**示例**：
```csharp
// 切换到行走状态
stateMachine.ChangeState<WalkState>();

// 切换到攻击状态
stateMachine.ChangeState<AttackState>();
```

##### Update(float deltaTime)

```csharp
public void Update(float deltaTime)
```

驱动当前状态的每帧更新。

| 参数 | 类型 | 说明 |
|------|------|------|
| deltaTime | float | 自上一帧以来经过的时间（秒） |

**调用时机**：建议在 MonoBehaviour.Update 中调用

**示例**：
```csharp
private void Update()
{
    stateMachine.Update(Time.deltaTime);
}
```

##### FixedUpdate(float fixedDeltaTime)

```csharp
public void FixedUpdate(float fixedDeltaTime)
```

驱动当前状态的物理更新。

| 参数 | 类型 | 说明 |
|------|------|------|
| fixedDeltaTime | float | 固定时间步长（秒） |

**调用时机**：建议在 MonoBehaviour.FixedUpdate 中调用

**示例**：
```csharp
private void FixedUpdate()
{
    stateMachine.FixedUpdate(Time.fixedDeltaTime);
}
```

##### Stop()

```csharp
public void Stop()
```

停止状态机并清理所有资源。

**执行操作**：
1. 调用当前状态的 `OnExit()` 方法
2. 清空当前状态引用
3. 清空状态缓存池

**调用时机**：建议在对象销毁时调用

**示例**：
```csharp
private void OnDestroy()
{
    stateMachine.Stop();
}
```

---

### 2.2 AsakiState<TContext>

状态基类，所有具体状态类都应继承此类。

#### 属性

| 属性 | 类型 | 说明 | 访问级别 |
|------|------|------|----------|
| Context | TContext | 状态持有者上下文 | protected |
| Machine | AsakiStateMachine<TContext> | 所属状态机 | protected |

**示例**：
```csharp
public class WalkState : AsakiState<PlayerController>
{
    public override void OnUpdate(float deltaTime)
    {
        // 访问上下文
        Context.Move(Vector2.right);
        
        // 访问状态机
        if (Context.Health <= 0)
        {
            Machine.ChangeState<DeadState>();
        }
    }
}
```

#### 方法

##### Initialize(AsakiStateMachine<TContext> machine, TContext context)

```csharp
public virtual void Initialize(AsakiStateMachine<TContext> machine, TContext context)
```

初始化状态实例。此方法由状态机自动调用，无需手动调用。

| 参数 | 类型 | 说明 |
|------|------|------|
| machine | AsakiStateMachine<TContext> | 所属状态机 |
| context | TContext | 状态持有者上下文 |

**重写示例**：
```csharp
public override void Initialize(AsakiStateMachine<PlayerController> machine, PlayerController context)
{
    base.Initialize(machine, context);
    // 自定义初始化逻辑
    Debug.Log("WalkState 初始化完成");
}
```

##### OnEnter()

```csharp
public virtual void OnEnter()
```

当状态被激活时调用。

**用途**：
- 初始化状态数据
- 播放动画
- 播放音效
- 设置初始值

**示例**：
```csharp
public override void OnEnter()
{
    // 播放动画
    Context.Animator.SetBool("IsWalking", true);
    
    // 播放音效
    Context.AudioSource.PlayOneShot(Context.WalkSound);
    
    // 初始化数据
    _walkTime = 0f;
}
```

##### OnUpdate(float deltaTime)

```csharp
public virtual void OnUpdate(float deltaTime)
```

每帧更新时调用。

| 参数 | 类型 | 说明 |
|------|------|------|
| deltaTime | float | 自上一帧以来经过的时间（秒） |

**默认行为**：调用 `CheckTransition(deltaTime)`

**重写示例**：
```csharp
public override void OnUpdate(float deltaTime)
{
    // 先执行状态逻辑
    HandleMovement();
    UpdateAnimation();
    
    // 再调用基类方法（会执行CheckTransition）
    base.OnUpdate(deltaTime);
}
```

##### OnFixedUpdate(float fixedDeltaTime)

```csharp
public virtual void OnFixedUpdate(float fixedDeltaTime)
```

固定时间步更新时调用。

| 参数 | 类型 | 说明 |
|------|------|------|
| fixedDeltaTime | float | 固定时间步长（秒） |

**用途**：处理物理相关逻辑

**示例**：
```csharp
public override void OnFixedUpdate(float fixedDeltaTime)
{
    // 物理移动
    Context.Rigidbody.MovePosition(
        Context.Transform.position + _velocity * fixedDeltaTime
    );
}
```

##### OnExit()

```csharp
public virtual void OnExit()
```

当状态被停用时调用。

**用途**：
- 清理状态数据
- 停止动画/音效
- 保存状态信息
- 释放资源

**示例**：
```csharp
public override void OnExit()
{
    // 停止动画
    Context.Animator.SetBool("IsWalking", false);
    
    // 停止音效
    Context.AudioSource.Stop();
    
    // 保存数据
    Context.LastWalkTime = Time.time;
}
```

##### CheckTransition(float deltaTime)

```csharp
protected virtual void CheckTransition(float deltaTime)
```

检查是否应该转换到其他状态。

| 参数 | 类型 | 说明 |
|------|------|------|
| deltaTime | float | 自上一帧以来经过的时间（秒） |

**用途**：集中管理状态转换条件

**示例**：
```csharp
protected override void CheckTransition(float deltaTime)
{
    // 检查死亡
    if (Context.Health <= 0)
    {
        Machine.ChangeState<DeadState>();
        return;
    }
    
    // 检查受伤
    if (Context.IsHurt)
    {
        Machine.ChangeState<HurtState>();
        return;
    }
    
    // 检查输入
    if (!Context.IsMoving)
    {
        Machine.ChangeState<IdleState>();
    }
}
```

---

### 2.3 IAsakiState 接口

```csharp
public interface IAsakiState
{
    void OnEnter();
    void OnUpdate(float deltaTime);
    void OnFixedUpdate(float fixedDeltaTime);
    void OnExit();
}
```

**用途**：
- 自定义状态实现
- 不继承 `AsakiState<TContext>` 时的备选方案
- 需要完全自定义状态行为时

**实现示例**：
```csharp
public class CustomState : IAsakiState
{
    private PlayerController _context;
    
    public CustomState(PlayerController context)
    {
        _context = context;
    }
    
    public void OnEnter()
    {
        Debug.Log("进入自定义状态");
    }
    
    public void OnUpdate(float deltaTime)
    {
        // 自定义更新逻辑
    }
    
    public void OnFixedUpdate(float fixedDeltaTime)
    {
        // 自定义物理更新
    }
    
    public void OnExit()
    {
        Debug.Log("退出自定义状态");
    }
}
```

---

## 第三部分：使用示例

### 示例1：玩家角色状态机

**场景**：第三人称动作游戏的玩家角色控制

#### 完整实现

**PlayerController.cs** - 上下文类
```csharp
using Asaki.Core.FSM;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float rotationSpeed = 10f;
    
    [Header("跳跃设置")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = -20f;
    
    [Header("攻击设置")]
    [SerializeField] private float attackCooldown = 0.5f;
    
    // 组件引用
    public CharacterController CharacterController { get; private set; }
    public Animator Animator { get; private set; }
    public Transform MainCamera { get; private set; }
    
    // 状态数据
    public Vector3 InputDirection { get; private set; }
    public Vector3 Velocity { get; set; }
    public bool IsGrounded { get; private set; }
    public bool IsRunning { get; private set; }
    public float Health { get; set; } = 100f;
    public float LastAttackTime { get; set; } = -999f;
    
    // 状态机
    private AsakiStateMachine<PlayerController> _stateMachine;
    
    private void Awake()
    {
        CharacterController = GetComponent<CharacterController>();
        Animator = GetComponent<Animator>();
        MainCamera = Camera.main.transform;
        
        _stateMachine = new AsakiStateMachine<PlayerController>(this);
    }
    
    private void Start()
    {
        _stateMachine.ChangeState<IdleState>();
    }
    
    private void Update()
    {
        // 读取输入
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        IsRunning = Input.GetKey(KeyCode.LeftShift);
        
        // 计算输入方向（相对于相机）
        Vector3 forward = MainCamera.forward;
        Vector3 right = MainCamera.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        
        InputDirection = (forward * vertical + right * horizontal).normalized;
        
        // 更新状态机
        _stateMachine.Update(Time.deltaTime);
        
        // 应用重力
        if (IsGrounded && Velocity.y < 0)
        {
            Velocity.y = -0.5f;
        }
        Velocity.y += gravity * Time.deltaTime;
        
        // 移动角色
        CharacterController.Move(Velocity * Time.deltaTime);
        IsGrounded = CharacterController.isGrounded;
    }
    
    private void FixedUpdate()
    {
        _stateMachine.FixedUpdate(Time.fixedDeltaTime);
    }
    
    public void Move(Vector3 direction, float speed)
    {
        Velocity = new Vector3(direction.x * speed, Velocity.y, direction.z * speed);
        
        // 旋转角色
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                rotationSpeed * Time.deltaTime
            );
        }
    }
    
    public void TakeDamage(float damage)
    {
        Health -= damage;
        if (Health <= 0)
        {
            _stateMachine.ChangeState<DeadState>();
        }
        else
        {
            _stateMachine.ChangeState<HurtState>();
        }
    }
    
    public bool CanAttack()
    {
        return Time.time >= LastAttackTime + attackCooldown;
    }
    
    private void OnDestroy()
    {
        _stateMachine.Stop();
    }
}
```

**IdleState.cs** - 待机状态
```csharp
using Asaki.Core.FSM;
using UnityEngine;

public class IdleState : AsakiState<PlayerController>
{
    public override void OnEnter()
    {
        Context.Animator.SetFloat("Speed", 0f);
    }
    
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        
        // 处理输入
        if (Input.GetButtonDown("Jump") && Context.IsGrounded)
        {
            Machine.ChangeState<JumpState>();
            return;
        }
        
        if (Input.GetButtonDown("Fire1") && Context.CanAttack())
        {
            Machine.ChangeState<AttackState>();
            return;
        }
    }
    
    protected override void CheckTransition(float deltaTime)
    {
        if (Context.InputDirection.sqrMagnitude > 0.01f)
        {
            Machine.ChangeState<WalkState>();
        }
    }
}
```

**WalkState.cs** - 行走/跑步状态
```csharp
using Asaki.Core.FSM;
using UnityEngine;

public class WalkState : AsakiState<PlayerController>
{
    public override void OnEnter()
    {
        UpdateAnimation();
    }
    
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        
        // 处理输入
        if (Input.GetButtonDown("Jump") && Context.IsGrounded)
        {
            Machine.ChangeState<JumpState>();
            return;
        }
        
        if (Input.GetButtonDown("Fire1") && Context.CanAttack())
        {
            Machine.ChangeState<AttackState>();
            return;
        }
        
        // 更新动画
        UpdateAnimation();
    }
    
    protected override void CheckTransition(float deltaTime)
    {
        if (Context.InputDirection.sqrMagnitude < 0.01f)
        {
            Machine.ChangeState<IdleState>();
        }
    }
    
    private void UpdateAnimation()
    {
        float speed = Context.IsRunning ? 1f : 0.5f;
        Context.Animator.SetFloat("Speed", speed);
    }
    
    public override void OnFixedUpdate(float fixedDeltaTime)
    {
        float speed = Context.IsRunning ? Context.RunSpeed : Context.WalkSpeed;
        Context.Move(Context.InputDirection, speed);
    }
}
```

**JumpState.cs** - 跳跃状态
```csharp
using Asaki.Core.FSM;
using UnityEngine;

public class JumpState : AsakiState<PlayerController>
{
    private bool _hasJumped;
    
    public override void OnEnter()
    {
        _hasJumped = false;
        Context.Animator.SetTrigger("Jump");
    }
    
    public override void OnFixedUpdate(float fixedDeltaTime)
    {
        if (!_hasJumped)
        {
            Context.Velocity = new Vector3(
                Context.Velocity.x, 
                Context.JumpForce, 
                Context.Velocity.z
            );
            _hasJumped = true;
        }
        
        // 空中移动控制（减弱）
        Vector3 airDirection = Context.InputDirection * 0.5f;
        Context.Move(airDirection, Context.WalkSpeed);
    }
    
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        
        if (Context.IsGrounded && _hasJumped)
        {
            if (Context.InputDirection.sqrMagnitude > 0.01f)
            {
                Machine.ChangeState<WalkState>();
            }
            else
            {
                Machine.ChangeState<IdleState>();
            }
        }
    }
}
```

**AttackState.cs** - 攻击状态
```csharp
using Asaki.Core.FSM;
using UnityEngine;

public class AttackState : AsakiState<PlayerController>
{
    private float _attackDuration = 0.6f;
    private float _timer;
    private bool _hasDealtDamage;
    
    public override void OnEnter()
    {
        _timer = 0f;
        _hasDealtDamage = false;
        Context.LastAttackTime = Time.time;
        Context.Animator.SetTrigger("Attack");
    }
    
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        
        _timer += deltaTime;
        
        // 在攻击中段造成伤害
        if (_timer > 0.3f && !_hasDealtDamage)
        {
            PerformAttack();
            _hasDealtDamage = true;
        }
        
        // 攻击结束
        if (_timer >= _attackDuration)
        {
            if (Context.InputDirection.sqrMagnitude > 0.01f)
            {
                Machine.ChangeState<WalkState>();
            }
            else
            {
                Machine.ChangeState<IdleState>();
            }
        }
    }
    
    private void PerformAttack()
    {
        // 检测攻击范围内的敌人
        Collider[] hits = Physics.OverlapSphere(
            Context.transform.position + Context.transform.forward,
            1.5f,
            LayerMask.GetMask("Enemy")
        );
        
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Enemy>(out var enemy))
            {
                enemy.TakeDamage(20f);
            }
        }
    }
}
```

**HurtState.cs** - 受击状态
```csharp
using Asaki.Core.FSM;
using UnityEngine;

public class HurtState : AsakiState<PlayerController>
{
    private float _hurtDuration = 0.5f;
    private float _timer;
    private Vector3 _knockbackDirection;
    
    public override void OnEnter()
    {
        _timer = 0f;
        _knockbackDirection = -Context.transform.forward;
        Context.Animator.SetTrigger("Hurt");
    }
    
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        
        _timer += deltaTime;
        
        if (_timer >= _hurtDuration)
        {
            if (Context.InputDirection.sqrMagnitude > 0.01f)
            {
                Machine.ChangeState<WalkState>();
            }
            else
            {
                Machine.ChangeState<IdleState>();
            }
        }
    }
    
    public override void OnFixedUpdate(float fixedDeltaTime)
    {
        // 击退效果
        float knockbackForce = Mathf.Lerp(5f, 0f, _timer / _hurtDuration);
        Context.Move(_knockbackDirection, knockbackForce);
    }
}
```

**DeadState.cs** - 死亡状态
```csharp
using Asaki.Core.FSM;
using UnityEngine;

public class DeadState : AsakiState<PlayerController>
{
    public override void OnEnter()
    {
        Context.Animator.SetTrigger("Dead");
        Context.CharacterController.enabled = false;
        
        // 触发死亡事件
        GameEvents.PlayerDied?.Invoke();
    }
    
    public override void OnExit()
    {
        // 复活时重新启用
        Context.CharacterController.enabled = true;
    }
}
```

---

### 示例2：敌人AI状态机

**场景**：巡逻敌人的AI行为

**EnemyAI.cs** - 敌人类
```csharp
using Asaki.Core.FSM;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    [Header("巡逻设置")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float waitTimeAtPoint = 2f;
    
    [Header("检测设置")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float fieldOfView = 120f;
    
    [Header("战斗设置")]
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float health = 50f;
    
    // 组件引用
    public NavMeshAgent Agent { get; private set; }
    public Animator Animator { get; private set; }
    public Transform Player { get; private set; }
    
    // 状态数据
    public int CurrentPatrolIndex { get; set; }
    public float WaitTimer { get; set; }
    public float LastAttackTime { get; set; } = -999f;
    public float Health { get; set; }
    
    // 状态机
    private AsakiStateMachine<EnemyAI> _stateMachine;
    
    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Animator = GetComponent<Animator>();
        Player = GameObject.FindGameObjectWithTag("Player")?.transform;
        Health = health;
        
        _stateMachine = new AsakiStateMachine<EnemyAI>(this);
    }
    
    private void Start()
    {
        _stateMachine.ChangeState<PatrolState>();
    }
    
    private void Update()
    {
        _stateMachine.Update(Time.deltaTime);
    }
    
    public bool CanSeePlayer()
    {
        if (Player == null) return false;
        
        float distanceToPlayer = Vector3.Distance(transform.position, Player.position);
        if (distanceToPlayer > detectionRange) return false;
        
        Vector3 directionToPlayer = (Player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        
        if (angleToPlayer > fieldOfView * 0.5f) return false;
        
        // 视线检测
        if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, 
            out RaycastHit hit, detectionRange))
        {
            return hit.collider.CompareTag("Player");
        }
        
        return false;
    }
    
    public bool IsPlayerInAttackRange()
    {
        if (Player == null) return false;
        return Vector3.Distance(transform.position, Player.position) <= attackRange;
    }
    
    public void TakeDamage(float damage)
    {
        Health -= damage;
        if (Health <= 0)
        {
            _stateMachine.ChangeState<DeadState>();
        }
        else
        {
            _stateMachine.ChangeState<HurtState>();
        }
    }
    
    public bool CanAttack()
    {
        return Time.time >= LastAttackTime + attackCooldown;
    }
    
    private void OnDestroy()
    {
        _stateMachine?.Stop();
    }
}
```

**PatrolState.cs** - 巡逻状态
```csharp
using Asaki.Core.FSM;
using UnityEngine;

public class PatrolState : AsakiState<EnemyAI>
{
    public override void OnEnter()
    {
        Context.Animator.SetBool("IsWalking", true);
        MoveToNextPatrolPoint();
    }
    
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        
        // 检查是否到达巡逻点
        if (!Context.Agent.pathPending && 
            Context.Agent.remainingDistance < 0.5f)
        {
            Machine.ChangeState<WaitState>();
        }
    }
    
    protected override void CheckTransition(float deltaTime)
    {
        if (Context.CanSeePlayer())
        {
            Machine.ChangeState<ChaseState>();
        }
    }
    
    private void MoveToNextPatrolPoint()
    {
        if (Context.patrolPoints.Length == 0) return;
        
        Context.Agent.destination = Context.patrolPoints[Context.CurrentPatrolIndex].position;
        Context.CurrentPatrolIndex = (Context.CurrentPatrolIndex + 1) % Context.patrolPoints.Length;
    }
}
```

**WaitState.cs** - 等待状态
```csharp
using Asaki.Core.FSM;
using UnityEngine;

public class WaitState : AsakiState<EnemyAI>
{
    public override void OnEnter()
    {
        Context.Animator.SetBool("IsWalking", false);
        Context.WaitTimer = Context.waitTimeAtPoint;
    }
    
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        
        Context.WaitTimer -= deltaTime;
        
        if (Context.WaitTimer <= 0)
        {
            Machine.ChangeState<PatrolState>();
        }
    }
    
    protected override void CheckTransition(float deltaTime)
    {
        if (Context.CanSeePlayer())
        {
            Machine.ChangeState<ChaseState>();
        }
    }
}
```

**ChaseState.cs** - 追击状态
```csharp
using Asaki.Core.FSM;
using UnityEngine;

public class ChaseState : AsakiState<EnemyAI>
{
    public override void OnEnter()
    {
        Context.Animator.SetBool("IsRunning", true);
    }
    
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        
        if (Context.Player != null)
        {
            Context.Agent.destination = Context.Player.position;
        }
    }
    
    protected override void CheckTransition(float deltaTime)
    {
        if (Context.IsPlayerInAttackRange() && Context.CanAttack())
        {
            Machine.ChangeState<AttackState>();
        }
        else if (!Context.CanSeePlayer() && Context.Agent.remainingDistance < 0.5f)
        {
            // 丢失目标，返回巡逻
            Machine.ChangeState<PatrolState>();
        }
    }
    
    public override void OnExit()
    {
        Context.Animator.SetBool("IsRunning", false);
    }
}
```

**AttackState.cs** - 攻击状态
```csharp
using Asaki.Core.FSM;
using UnityEngine;

public class AttackState : AsakiState<EnemyAI>
{
    private float _attackDuration = 1f;
    private float _timer;
    private bool _hasAttacked;
    
    public override void OnEnter()
    {
        _timer = 0f;
        _hasAttacked = false;
        Context.LastAttackTime = Time.time;
        Context.Animator.SetTrigger("Attack");
        Context.Agent.isStopped = true;
    }
    
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        
        _timer += deltaTime;
        
        // 执行攻击
        if (_timer > 0.4f && !_hasAttacked)
        {
            PerformAttack();
            _hasAttacked = true;
        }
        
        // 攻击结束
        if (_timer >= _attackDuration)
        {
            Context.Agent.isStopped = false;
            
            if (Context.IsPlayerInAttackRange())
            {
                Machine.ChangeState<ChaseState>();
            }
            else
            {
                Machine.ChangeState<ChaseState>();
            }
        }
    }
    
    private void PerformAttack()
    {
        if (Context.Player != null && 
            Vector3.Distance(Context.transform.position, Context.Player.position) <= Context.attackRange)
        {
            // 对玩家造成伤害
            if (Context.Player.TryGetComponent<PlayerController>(out var player))
            {
                player.TakeDamage(10f);
            }
        }
    }
}
```

---

### 示例3：游戏流程状态机

**场景**：游戏整体流程控制

**GameManager.cs**
```csharp
using Asaki.Core.FSM;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("游戏设置")]
    [SerializeField] private string gameplaySceneName = "Gameplay";
    [SerializeField] private float loadDelay = 1f;
    
    // 游戏数据
    public int CurrentLevel { get; set; }
    public int PlayerScore { get; set; }
    public int PlayerLives { get; set; } = 3;
    
    // 状态机
    private AsakiStateMachine<GameManager> _stateMachine;
    
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        _stateMachine = new AsakiStateMachine<GameManager>(this);
    }
    
    private void Start()
    {
        _stateMachine.ChangeState<MenuState>();
    }
    
    private void Update()
    {
        _stateMachine.Update(Time.deltaTime);
    }
    
    public void StartGame()
    {
        if (_stateMachine.CurrentState is MenuState)
        {
            _stateMachine.ChangeState<LoadingState>();
        }
    }
    
    public void PauseGame()
    {
        if (_stateMachine.CurrentState is PlayingState)
        {
            _stateMachine.ChangeState<PausedState>();
        }
    }
    
    public void ResumeGame()
    {
        if (_stateMachine.CurrentState is PausedState)
        {
            _stateMachine.ChangeState<PlayingState>();
        }
    }
    
    public void GameOver()
    {
        _stateMachine.ChangeState<GameOverState>();
    }
    
    public void ReturnToMenu()
    {
        _stateMachine.ChangeState<MenuState>();
    }
    
    private void OnDestroy()
    {
        _stateMachine?.Stop();
    }
}
```

**MenuState.cs**
```csharp
using Asaki.Core.FSM;
using UnityEngine;

public class MenuState : AsakiState<GameManager>
{
    public override void OnEnter()
    {
        Time.timeScale = 1f;
        
        // 加载主菜单场景
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            SceneManager.LoadScene("MainMenu");
        }
        
        // 显示主菜单UI
        UIManager.Instance?.ShowMainMenu();
        
        Debug.Log("进入主菜单状态");
    }
    
    protected override void CheckTransition(float deltaTime)
    {
        // 状态转换由按钮触发
    }
}
```

**LoadingState.cs**
```csharp
using Asaki.Core.FSM;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingState : AsakiState<GameManager>
{
    private float _loadTimer;
    private AsyncOperation _loadOperation;
    
    public override void OnEnter()
    {
        _loadTimer = 0f;
        
        // 显示加载画面
        UIManager.Instance?.ShowLoadingScreen();
        
        // 开始异步加载
        _loadOperation = SceneManager.LoadSceneAsync(Context.gameplaySceneName);
        _loadOperation.allowSceneActivation = false;
        
        Debug.Log("开始加载游戏场景");
    }
    
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        
        _loadTimer += deltaTime;
        
        // 更新加载进度UI
        float progress = Mathf.Clamp01(_loadOperation.progress / 0.9f);
        UIManager.Instance?.UpdateLoadingProgress(progress);
        
        // 加载完成并等待延迟
        if (_loadOperation.progress >= 0.9f && _loadTimer >= Context.loadDelay)
        {
            _loadOperation.allowSceneActivation = true;
        }
    }
    
    protected override void CheckTransition(float deltaTime)
    {
        if (_loadOperation.isDone)
        {
            Machine.ChangeState<PlayingState>();
        }
    }
}
```

**PlayingState.cs**
```csharp
using Asaki.Core.FSM;
using UnityEngine;

public class PlayingState : AsakiState<GameManager>
{
    public override void OnEnter()
    {
        Time.timeScale = 1f;
        
        // 显示游戏HUD
        UIManager.Instance?.ShowGameHUD();
        
        Debug.Log("游戏开始");
    }
    
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        
        // 检测暂停输入
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Context.PauseGame();
        }
    }
    
    protected override void CheckTransition(float deltaTime)
    {
        if (Context.PlayerLives <= 0)
        {
            Machine.ChangeState<GameOverState>();
        }
    }
}
```

**PausedState.cs**
```csharp
using Asaki.Core.FSM;
using UnityEngine;

public class PausedState : AsakiState<GameManager>
{
    public override void OnEnter()
    {
        Time.timeScale = 0f;
        
        // 显示暂停菜单
        UIManager.Instance?.ShowPauseMenu();
        
        Debug.Log("游戏暂停");
    }
    
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        
        // 检测恢复输入
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Context.ResumeGame();
        }
    }
    
    public override void OnExit()
    {
        Time.timeScale = 1f;
        UIManager.Instance?.HidePauseMenu();
    }
}
```

**GameOverState.cs**
```csharp
using Asaki.Core.FSM;
using UnityEngine;

public class GameOverState : AsakiState<GameManager>
{
    public override void OnEnter()
    {
        Time.timeScale = 0.5f;
        
        // 显示游戏结束画面
        UIManager.Instance?.ShowGameOverScreen(Context.PlayerScore);
        
        Debug.Log("游戏结束");
    }
    
    protected override void CheckTransition(float deltaTime)
    {
        // 等待玩家选择重新开始或返回菜单
    }
}
```

---

## 总结

本文档提供了 Asaki FSM 系统的完整使用指南：

1. **使用场景**：角色AI、游戏流程、UI状态、动画系统、网络状态、物理对象
2. **完整API**：AsakiStateMachine<TContext>、AsakiState<TContext>、IAsakiState 的详细说明
3. **实用示例**：
   - 玩家角色状态机（Idle、Walk、Jump、Attack、Hurt、Dead）
   - 敌人AI状态机（Patrol、Wait、Chase、Attack）
   - 游戏流程状态机（Menu、Loading、Playing、Paused、GameOver）

通过这些示例，您可以快速掌握 Asaki FSM 的使用方法，并将其应用到您的项目中。
