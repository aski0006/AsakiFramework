# Asaki 实体系统与 Unity OOP 实现需求对比文档

## 目录

1. [概述](#一概述)
2. [架构设计差异](#二架构设计差异)
3. [实现方式差异](#三实现方式差异)
4. [性能特征差异](#四性能特征差异)
5. [适用场景差异](#五适用场景差异)
6. [迁移指南](#六迁移指南)
7. [总结](#七总结)

---

## 一、概述

### 1.1 两种范式的本质区别

| 特性 | Unity OOP (传统方式) | Asaki 实体系统 (EC模式) |
|-----|---------------------|------------------------|
| **核心思想** | 继承层次结构 | 组合优于继承 |
| **对象定义** | GameObject + MonoBehaviour | Entity + Component |
| **数据与逻辑** | 封装在类中 | 数据在Component，逻辑在System |
| **生命周期** | Unity引擎控制 | Asaki Architecture控制 |
| **通信方式** | 直接引用、SendMessage、事件 | Broker事件总线、Command/Query |

### 1.2 设计哲学对比

```
Unity OOP 设计哲学:
┌─────────────────────────────────────────┐
│  "是一个" (IS-A) 关系                   │
│                                         │
│  Player : MonoBehaviour                 │
│    └── 继承所有MonoBehaviour特性        │
│    └── 在类内部实现所有逻辑             │
│                                         │
│  关注: 对象是什么                       │
└─────────────────────────────────────────┘

Asaki 实体系统 设计哲学:
┌─────────────────────────────────────────┐
│  "有一个" (HAS-A) 关系                  │
│                                         │
│  Entity                                 │
│    ├── TransformComponent               │
│    ├── PhysicsComponent                 │
│    └── HealthComponent                  │
│                                         │
│  关注: 对象能做什么                     │
└─────────────────────────────────────────┘
```

---

## 二、架构设计差异

### 2.1 整体架构对比

#### Unity OOP 架构

```
┌─────────────────────────────────────────────────────────────────┐
│                        Unity Scene                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐ │
│  │  Player     │  │  Enemy      │  │  GameManager            │ │
│  │  GameObject │  │  GameObject │  │  (Singleton)            │ │
│  │             │  │             │  │                         │ │
│  │ ┌─────────┐ │  │ ┌─────────┐ │  │  - 游戏状态管理          │ │
│  │ │Player   │ │  │ │EnemyAI  │ │  │  - 全局事件分发          │ │
│  │ │Controller│ │  │ │Script   │ │  │  - 对象引用管理          │ │
│  │ │(Mono)   │ │  │ │(Mono)   │ │  │                         │ │
│  │ └─────────┘ │  │ └─────────┘ │  └─────────────────────────┘ │
│  │ ┌─────────┐ │  │ ┌─────────┐ │                              │
│  │ │Player   │ │  │ │Health   │ │                              │
│  │ │Health   │ │  │ │(Mono)   │ │                              │
│  │ │(Mono)   │ │  │ └─────────┘ │                              │
│  │ └─────────┘ │  └─────────────┘                              │
│  └─────────────┘                                                │
│                                                                 │
│  特点:                                                          │
│  - 每个GameObject是独立的                                       │
│  - 通过Inspector配置引用                                        │
│  - 使用Find/Tag查找其他对象                                     │
│  - 单例模式管理全局状态                                         │
└─────────────────────────────────────────────────────────────────┘
```

#### Asaki 实体系统架构

```
┌─────────────────────────────────────────────────────────────────┐
│                    Asaki Architecture                           │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │                      Entity World                         │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐   │ │
│  │  │  Player     │  │  Enemy      │  │  Camera         │   │ │
│  │  │  Entity     │  │  Entity     │  │  Entity         │   │ │
│  │  │             │  │             │  │                 │   │ │
│  │  │ - Transform │  │ - Transform │  │ - Transform     │   │ │
│  │  │ - Physics   │  │ - AI        │  │ - Camera2D      │   │ │
│  │  │ - Health    │  │ - Health    │  │                 │   │ │
│  │  │ - Input     │  │ - Shooter   │  │                 │   │ │
│  │  └─────────────┘  └─────────────┘  └─────────────────┘   │ │
│  └───────────────────────────────────────────────────────────┘ │
│                              │                                  │
│                              ▼                                  │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │                      System 层                             │ │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────────┐  │ │
│  │  │ PlayerSystem │ │ EnemyAISystem│ │ CameraSystem     │  │ │
│  │  │ (处理所有玩家)│ │ (处理所有敌人)│ │ (处理所有摄像机)  │  │ │
│  │  └──────────────┘ └──────────────┘ └──────────────────┘  │ │
│  └───────────────────────────────────────────────────────────┘ │
│                              │                                  │
│                              ▼                                  │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │                      Command/Query                         │ │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────────┐  │ │
│  │  │ MoveCommand  │ │ AttackCommand│ │ GetEntitiesQuery │  │ │
│  │  │ (支持Undo)   │ │ (支持Undo)   │ │ (支持缓存)       │  │ │
│  │  └──────────────┘ └──────────────┘ └──────────────────┘  │ │
│  └───────────────────────────────────────────────────────────┘ │
│                              │                                  │
│                              ▼                                  │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │                      Event Bus                             │ │
│  │              AsakiBroker (发布-订阅模式)                    │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  特点:                                                          │
│  - 实体是轻量级ID                                               │
│  - 通过World统一管理                                            │
│  - 使用Query筛选实体                                            │
│  - System批量处理同类实体                                       │
│  - Command封装可撤销操作                                        │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 核心概念映射表

| Unity OOP 概念 | Asaki 实体系统概念 | 说明 |
|---------------|-------------------|------|
| `GameObject` | `IEntity` | 游戏对象的抽象表示 |
| `MonoBehaviour` | `IEntityComponent` | 组件的抽象接口 |
| `Transform` | `TransformComponent` | 位置/旋转/缩放数据 |
| `Awake/Start` | `OnAttach` | 组件初始化时机 |
| `OnEnable/OnDisable` | `OnEnable/OnDisable` | 激活状态变化 |
| `OnDestroy` | `OnDetach` + `Dispose` | 组件销毁时机 |
| `Update/FixedUpdate` | `IAsakiTickable.Tick` | 更新逻辑 |
| `GetComponent<T>()` | `entity.GetComponent<T>()` | 获取组件 |
| `AddComponent<T>()` | `entity.AddComponent<T>()` | 添加组件 |
| `GameObject.Find()` | `world.Query<T>()` | 查找对象 |
| `SendMessage` | `AsakiBroker.Publish()` | 事件通信 |
| `ScriptableObject` | `EntityArchetype` | 数据配置模板 |

---

## 三、实现方式差异

### 3.1 角色实现对比

#### Unity OOP 实现

```csharp
// ============================================
// Unity OOP - 玩家控制器实现
// ============================================

public class PlayerController : MonoBehaviour
{
    // 直接引用其他组件
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerHealth health;

    // 配置参数
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    // 内部状态
    private float horizontalInput;
    private bool isGrounded;
    private Vector2 velocity;

    // 在Inspector中手动配置引用
    private void Awake()
    {
        // 如果没有配置，尝试自动获取
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (health == null) health = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        // 读取输入
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // 处理跳跃
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }

        // 更新动画
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        // 物理更新
        Move();
    }

    private void Move()
    {
        velocity = rb.velocity;
        velocity.x = horizontalInput * moveSpeed;
        rb.velocity = velocity;
    }

    private void Jump()
    {
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        isGrounded = false;
    }

    private void UpdateAnimation()
    {
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
        animator.SetBool("IsGrounded", isGrounded);
    }

    // 碰撞检测
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    // 与其他对象交互
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // 直接调用其他组件的方法
            var enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(10);
            }

            // 或者发送消息
            other.SendMessage("OnPlayerAttack", 10, SendMessageOptions.DontRequireReceiver);
        }
    }
}

// 生命值组件 - 另一个MonoBehaviour
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    // 事件定义
    public event Action<int, int> OnHealthChanged;
    public event Action OnDeath;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
}
```

#### Asaki 实体系统实现

```csharp
// ============================================
// Asaki 实体系统 - 玩家实现
// ============================================

// 1. 定义组件（纯数据）
public class PlayerInputComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    // 使用ReactiveProperty支持数据绑定
    public AsakiProperty<Vector2> MoveInput { get; } = new(Vector2.zero);
    public AsakiProperty<bool> JumpPressed { get; } = new(false);
    public AsakiProperty<bool> IsGrounded { get; } = new(true);

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose()
    {
        MoveInput?.Dispose();
        JumpPressed?.Dispose();
        IsGrounded?.Dispose();
    }
}

public class Physics2DComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    public Vector2 Velocity { get; set; }
    public float MoveSpeed { get; set; } = 5f;
    public float JumpForce { get; set; } = 10f;
    public float GravityScale { get; set; } = 1f;

    // 与Unity物理系统桥接
    public Rigidbody2D Rigidbody { get; set; }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose() { }
}

public class HealthComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    public AsakiProperty<int> CurrentHealth { get; } = new(100);
    public AsakiProperty<int> MaxHealth { get; } = new(100);

    public void TakeDamage(int damage)
    {
        CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - damage);

        // 通过事件总线通知
        AsakiBroker.Publish(new HealthChangedEvent
        {
            EntityId = Entity.Id,
            CurrentHealth = CurrentHealth.Value,
            MaxHealth = MaxHealth.Value
        });

        if (CurrentHealth.Value <= 0)
        {
            AsakiBroker.Publish(new EntityDeathEvent { EntityId = Entity.Id });
        }
    }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose()
    {
        CurrentHealth?.Dispose();
        MaxHealth?.Dispose();
    }
}

// 2. 定义系统（处理逻辑）
public class PlayerInputSystem : IAsakiSystem, IAsakiTickable
{
    private EntityModel _entityModel;

    public void Setup()
    {
        _entityModel = AsakiContext.Get<EntityModel>();
    }

    public void Tick(float deltaTime)
    {
        // 批量处理所有玩家实体的输入
        foreach (var entity in _entityModel.World.Query<PlayerInputComponent>())
        {
            var input = entity.GetComponent<PlayerInputComponent>();

            input.MoveInput.Value = new Vector2(Input.GetAxisRaw("Horizontal"), 0);
            input.JumpPressed.Value = Input.GetButtonDown("Jump");
        }
    }

    public void Dispose() { }
}

public class PlayerMovementSystem : IAsakiSystem, IAsakiTickable
{
    private EntityModel _entityModel;

    public void Setup()
    {
        _entityModel = AsakiContext.Get<EntityModel>();
    }

    public void Tick(float deltaTime)
    {
        // 批量处理所有玩家的移动
        foreach (var entity in _entityModel.World.Query<PlayerInputComponent>())
        {
            var input = entity.GetComponent<PlayerInputComponent>();
            var physics = entity.GetComponent<Physics2DComponent>();

            if (physics == null) continue;

            // 处理移动
            physics.Velocity.x = input.MoveInput.Value.x * physics.MoveSpeed;

            // 处理跳跃
            if (input.JumpPressed.Value && input.IsGrounded.Value)
            {
                physics.Velocity.y = physics.JumpForce;
                input.IsGrounded.Value = false;

                // 发布跳跃事件
                AsakiBroker.Publish(new PlayerJumpEvent { EntityId = entity.Id });
            }

            // 应用重力
            if (!input.IsGrounded.Value)
            {
                physics.Velocity.y += Physics2D.gravity.y * physics.GravityScale * deltaTime;
            }

            // 同步到Unity物理系统
            if (physics.Rigidbody != null)
            {
                physics.Rigidbody.velocity = physics.Velocity;
            }
        }
    }

    public void Dispose() { }
}

// 3. 定义命令（封装可撤销操作）
public class PlayerTakeDamageCommand : AsakiCommand
{
    private readonly int _entityId;
    private readonly int _damage;
    private int _previousHealth;

    public PlayerTakeDamageCommand(int entityId, int damage)
    {
        _entityId = entityId;
        _damage = damage;
    }

    public override void Execute()
    {
        var world = GetModel<EntityModel>().World;
        var entity = world.GetEntity(new EntityId(_entityId));
        var health = entity.GetComponent<HealthComponent>();

        _previousHealth = health.CurrentHealth.Value;
        health.TakeDamage(_damage);
    }

    public override void Undo()
    {
        var world = GetModel<EntityModel>().World;
        var entity = world.GetEntity(new EntityId(_entityId));
        var health = entity.GetComponent<HealthComponent>();

        health.CurrentHealth.Value = _previousHealth;
    }
}

// 4. 创建玩家的命令
public class CreatePlayerCommand : AsakiCommand<EntityId>
{
    private readonly Vector2 _spawnPosition;
    private readonly GameObject _prefab;

    public CreatePlayerCommand(Vector2 spawnPosition, GameObject prefab)
    {
        _spawnPosition = spawnPosition;
        _prefab = prefab;
    }

    public override EntityId Execute()
    {
        var world = GetModel<EntityModel>().World;

        // 创建实体
        var entity = world.CreateEntity();

        // 添加组件
        var transform = entity.AddComponent<Transform2DComponent>();
        transform.Position.Value = _spawnPosition;

        entity.AddComponent<PlayerInputComponent>();
        entity.AddComponent<Physics2DComponent>();
        entity.AddComponent<HealthComponent>();

        // 实例化Unity对象并建立桥接
        var go = Object.Instantiate(_prefab, _spawnPosition, Quaternion.identity);
        var bridge = go.AddComponent<EntityBridge>();
        bridge.Initialize(entity.Id);

        return entity.Id;
    }
}
```

### 3.2 关键差异点

| 差异点 | Unity OOP | Asaki 实体系统 |
|-------|-----------|---------------|
| **组件获取** | `GetComponent<T>()` 运行时查找 | `entity.GetComponent<T>()` 直接访问 |
| **对象查找** | `GameObject.Find()` / `FindObjectOfType()` | `world.Query<T>()` 批量筛选 |
| **事件通信** | C# event / `SendMessage` / 直接调用 | `AsakiBroker.Publish/Subscribe` |
| **数据修改** | 直接修改字段 | 通过Command封装，支持Undo |
| **生命周期** | `Awake/Start/Update/OnDestroy` | `OnAttach/OnEnable/Tick/OnDetach` |
| **物理同步** | 直接操作Rigidbody | 在System中同步Component数据到Rigidbody |
| **配置方式** | Inspector拖拽配置 | 代码配置 + 原型(Archetype) |

### 3.3 通信机制对比

#### Unity OOP 通信方式

```csharp
// 方式1: 直接引用（紧耦合）
public class Player : MonoBehaviour
{
    public Enemy targetEnemy; // Inspector配置

    void Attack()
    {
        targetEnemy.TakeDamage(10); // 直接调用
    }
}

// 方式2: 查找（性能开销）
void FindAndAttack()
{
    var enemy = GameObject.FindWithTag("Enemy").GetComponent<Enemy>();
    enemy.TakeDamage(10);
}

// 方式3: SendMessage（松散但低效）
void AttackArea()
{
    var colliders = Physics.OverlapSphere(transform.position, 5f);
    foreach (var col in colliders)
    {
        col.SendMessage("TakeDamage", 10, SendMessageOptions.DontRequireReceiver);
    }
}

// 方式4: C# Event（需要预先建立引用）
public class GameManager : MonoBehaviour
{
    public static event Action<int> OnPlayerScoreChanged;

    void AddScore(int points)
    {
        OnPlayerScoreChanged?.Invoke(points);
    }
}
```

#### Asaki 实体系统通信方式

```csharp
// 方式1: 事件总线（推荐）
public class PlayerAttackSystem : IAsakiSystem, IAsakiTickable
{
    public void Tick(float deltaTime)
    {
        // 发布事件
        AsakiBroker.Publish(new PlayerAttackEvent
        {
            AttackerId = playerEntity.Id,
            Damage = 10,
            Position = playerTransform.Position.Value
        });
    }
}

// 在其他地方订阅
public class EnemyHealthSystem : IAsakiSystem
{
    public void Setup()
    {
        AsakiBroker.Subscribe<PlayerAttackEvent>(OnPlayerAttack);
    }

    void OnPlayerAttack(PlayerAttackEvent e)
    {
        // 查询范围内的敌人
        foreach (var enemy in _entityModel.World.Query<HealthComponent>())
        {
            var transform = enemy.GetComponent<Transform2DComponent>();
            if (Vector2.Distance(transform.Position.Value, e.Position) < 5f)
            {
                var health = enemy.GetComponent<HealthComponent>();
                health.TakeDamage(e.Damage);
            }
        }
    }
}

// 方式2: Command（支持Undo）
public class DealDamageCommand : AsakiCommand
{
    public override void Execute()
    {
        // 执行伤害逻辑
        health.TakeDamage(_damage);

        // 同时触发事件
        AsakiBroker.Publish(new DamageDealtEvent { ... });
    }
}

// 方式3: Query（批量处理）
public void HealAllPlayers()
{
    // 一次性获取所有带HealthComponent的玩家
    foreach (var entity in _world.Query<HealthComponent, PlayerTag>())
    {
        var health = entity.GetComponent<HealthComponent>();
        health.Heal(50);
    }
}
```

---

## 四、性能特征差异

### 4.1 内存布局对比

#### Unity OOP 内存布局

```
内存分布（分散）:
┌─────────────────────────────────────────────────────────────┐
│  Heap Memory                                                │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐  │
│  │ Player       │    │ Enemy        │    │ Player       │  │
│  │ Controller   │    │ AI           │    │ Health       │  │
│  │ @ 0x1000     │    │ @ 0x5000     │    │ @ 0x9000     │  │
│  │              │    │              │    │              │  │
│  │ - rb         │    │ - target     │    │ - maxHealth  │  │
│  │ - animator   │    │ - waypoints  │    │ - current    │  │
│  │ - health     │    │              │    │ - OnChanged  │  │
│  └──────────────┘    └──────────────┘    └──────────────┘  │
│                                                             │
│  问题:                                                      │
│  - 对象分布在堆的各个位置                                    │
│  - CPU缓存不友好                                            │
│  - 缓存未命中率高                                           │
└─────────────────────────────────────────────────────────────┘
```

#### Asaki 实体系统内存布局

```
内存分布（集中）:
┌─────────────────────────────────────────────────────────────┐
│  Component Storage                                          │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ TransformComponent[]                                  │   │
│  │ [Entity0] [Entity1] [Entity2] [Entity3] ...          │   │
│  │  连续内存存储                                          │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ HealthComponent[]                                     │   │
│  │ [Entity0] [Entity1] [Entity2] [Entity3] ...          │   │
│  │  连续内存存储                                          │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Physics2DComponent[]                                  │   │
│  │ [Entity0] [Entity1] [Entity2] [Entity3] ...          │   │
│  │  连续内存存储                                          │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  优势:                                                      │
│  - 同类型组件连续存储                                       │
│  - CPU缓存友好                                              │
│  - 批量处理效率高                                           │
└─────────────────────────────────────────────────────────────┘
```

### 4.2 性能对比表

| 性能指标 | Unity OOP | Asaki 实体系统 | 说明 |
|---------|-----------|---------------|------|
| **内存开销** | 高 | 低 | MonoBehaviour有额外开销 |
| **缓存命中率** | 低 | 高 | 组件连续存储 |
| **批量处理** | 困难 | 容易 | Query系统支持 |
| **对象创建** | 慢 | 快 | 实体是轻量级ID |
| **GC压力** | 高 | 低 | 可配合对象池使用 |
| **跨对象通信** | 低效 | 高效 | 事件总线设计 |
| **运行时修改** | 受限 | 灵活 | 可动态添加/移除组件 |

### 4.3 批量处理性能对比

```csharp
// ============================================
// 场景: 更新1000个敌人的位置
// ============================================

// Unity OOP 方式
public class EnemyManager : MonoBehaviour
{
    private List<EnemyController> enemies = new();

    void Update()
    {
        // 方式1: 每个Enemy自己Update（分散处理）
        // 每个EnemyController有自己的Update方法
        // Unity会逐个调用，缓存不友好

        // 方式2: 集中管理（需要维护列表）
        foreach (var enemy in enemies)
        {
            enemy.Move(); // 虚函数调用，可能有开销
        }
    }
}

// Asaki 实体系统方式
public class EnemyMovementSystem : IAsakiSystem, IAsakiTickable
{
    private EntityModel _entityModel;

    public void Tick(float deltaTime)
    {
        // 批量查询所有带Transform和Movement的敌人
        // 数据连续存储，缓存友好
        foreach (var entity in _entityModel.World.Query<Transform2DComponent, MovementComponent>())
        {
            var transform = entity.GetComponent<Transform2DComponent>();
            var movement = entity.GetComponent<MovementComponent>();

            // 直接内存访问，无虚函数开销
            transform.Position.Value += movement.Velocity * deltaTime;
        }
    }
}
```

---

## 五、适用场景差异

### 5.1 场景选择指南

```
┌─────────────────────────────────────────────────────────────────┐
│                      场景选择决策树                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1. 项目规模？                                                  │
│     ├── 小型原型/快速验证 → Unity OOP 更合适                    │
│     └── 中大型项目/长期维护 → Asaki 实体系统 更合适              │
│                                                                 │
│  2. 实体数量？                                                  │
│     ├── < 100 个 → Unity OOP 足够                               │
│     └── > 100 个 → Asaki 实体系统 性能更好                       │
│                                                                 │
│  3. 是否需要Undo/Redo？                                         │
│     ├── 不需要 → 两者皆可                                       │
│     └── 需要 → Asaki Command系统 原生支持                       │
│                                                                 │
│  4. 是否需要复杂架构？                                          │
│     ├── 简单直接 → Unity OOP                                    │
│     └── 需要CQRS/分层 → Asaki 实体系统                          │
│                                                                 │
│  5. 团队经验？                                                  │
│     ├── 熟悉Unity传统开发 → Unity OOP 上手快                    │
│     └── 愿意学习新架构 → Asaki 实体系统 长期收益大               │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 5.2 适用场景对比表

| 场景 | Unity OOP | Asaki 实体系统 | 推荐选择 |
|-----|-----------|---------------|---------|
| **快速原型开发** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | Unity OOP |
| **大型开放世界** | ⭐⭐ | ⭐⭐⭐⭐⭐ | Asaki |
| **需要Undo/Redo** | ⭐⭐ | ⭐⭐⭐⭐⭐ | Asaki |
| **大量同类型对象** | ⭐⭐ | ⭐⭐⭐⭐⭐ | Asaki |
| **复杂状态管理** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | Asaki |
| **简单2D小游戏** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | Unity OOP |
| **多人联机游戏** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | Asaki |
| **需要热更新** | ⭐⭐⭐ | ⭐⭐⭐⭐ | Asaki |

### 5.3 具体游戏类型推荐

| 游戏类型 | 推荐方案 | 理由 |
|---------|---------|------|
| **2D平台跳跃** | 两者皆可 | 实体数量少，OOP简单直接 |
| **弹幕射击** | Asaki | 大量子弹实体，需要高性能 |
| **动作RPG** | Asaki | 复杂装备/技能系统，需要灵活组合 |
| **策略战棋** | Asaki | 需要Undo/Redo，状态管理复杂 |
| **休闲益智** | Unity OOP | 简单逻辑，快速开发 |
| **生存建造** | Asaki | 大量可交互对象，需要持久化 |
| **卡牌游戏** | Asaki | Command模式适合卡牌操作 |
| **视觉小说** | Unity OOP | 以叙事为主，逻辑简单 |

---

## 六、迁移指南

### 6.1 从Unity OOP迁移到Asaki实体系统

#### 迁移步骤

```
迁移路线图:
├── 第一阶段：准备工作
│   ├── 分析现有代码结构
│   ├── 识别核心实体类型
│   └── 设计组件拆分方案
│
├── 第二阶段：基础设施
│   ├── 实现EntityModel
│   ├── 创建基础组件
│   └── 建立Unity桥接
│
├── 第三阶段：逐步迁移
│   ├── 从独立功能开始（如Camera）
│   ├── 迁移玩家控制
│   ├── 迁移敌人AI
│   └── 迁移其他系统
│
└── 第四阶段：优化完善
    ├── 性能调优
    ├── 添加调试工具
    └── 完善文档
```

#### 代码迁移示例

```csharp
// ============================================
// 迁移示例: 从MonoBehaviour到Asaki组件
// ============================================

// 步骤1: 识别原有MonoBehaviour的数据和逻辑
// 原有代码
public class PlayerController : MonoBehaviour
{
    // 数据部分
    public float speed = 5f;
    public int health = 100;
    private Vector2 velocity;

    // 逻辑部分
    void Update() { ... }
    void TakeDamage(int damage) { ... }
}

// 步骤2: 拆分为组件（数据）
public class PlayerMovementComponent : IEntityComponent
{
    public IEntity Entity { get; set; }
    public float Speed { get; set; } = 5f;
    public Vector2 Velocity { get; set; }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose() { }
}

public class HealthComponent : IEntityComponent
{
    public IEntity Entity { get; set; }
    public AsakiProperty<int> Health { get; } = new(100);

    public void TakeDamage(int damage)
    {
        Health.Value = Mathf.Max(0, Health.Value - damage);
    }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose() { Health?.Dispose(); }
}

// 步骤3: 创建System（逻辑）
public class PlayerMovementSystem : IAsakiSystem, IAsakiTickable
{
    private EntityModel _entityModel;

    public void Setup()
    {
        _entityModel = AsakiContext.Get<EntityModel>();
    }

    public void Tick(float deltaTime)
    {
        foreach (var entity in _entityModel.World.Query<PlayerMovementComponent>())
        {
            var movement = entity.GetComponent<PlayerMovementComponent>();
            var input = entity.GetComponent<PlayerInputComponent>();
            var transform = entity.GetComponent<Transform2DComponent>();

            // 迁移原有的Update逻辑
            movement.Velocity = input.MoveInput.Value * movement.Speed;
            transform.Position.Value += movement.Velocity * deltaTime;
        }
    }

    public void Dispose() { }
}

// 步骤4: 创建Command（可撤销操作）
public class PlayerTakeDamageCommand : AsakiCommand
{
    private readonly int _entityId;
    private readonly int _damage;
    private int _previousHealth;

    public PlayerTakeDamageCommand(int entityId, int damage)
    {
        _entityId = entityId;
        _damage = damage;
    }

    public override void Execute()
    {
        var entity = GetModel<EntityModel>().World.GetEntity(new EntityId(_entityId));
        var health = entity.GetComponent<HealthComponent>();

        _previousHealth = health.Health.Value;
        health.TakeDamage(_damage);
    }

    public override void Undo()
    {
        var entity = GetModel<EntityModel>().World.GetEntity(new EntityId(_entityId));
        var health = entity.GetComponent<HealthComponent>();
        health.Health.Value = _previousHealth;
    }
}

// 步骤5: 创建桥接组件（与Unity GameObject连接）
public class PlayerEntityBridge : MonoBehaviour
{
    [SerializeField] private EntityId entityId;
    private IEntity _entity;
    private IEntityWorld _world;

    public void Initialize(EntityId id)
    {
        entityId = id;
        _world = AsakiContext.Get<EntityModel>().World;
        _entity = _world.GetEntity(entityId);

        // 同步Unity组件引用
        var physics = _entity.GetComponent<Physics2DComponent>();
        if (physics != null)
        {
            physics.Rigidbody = GetComponent<Rigidbody2D>();
        }
    }

    // 将Unity事件转发到Entity组件
    private void OnCollisionEnter2D(Collision2D collision)
    {
        var input = _entity.GetComponent<PlayerInputComponent>();
        if (input != null && collision.gameObject.CompareTag("Ground"))
        {
            input.IsGrounded.Value = true;
        }
    }
}
```

### 6.2 常见陷阱与解决方案

| 陷阱 | 问题描述 | 解决方案 |
|-----|---------|---------|
| **过度组件化** | 将每个字段都拆分为组件 | 按功能聚合，如Transform包含Position/Rotation |
| **System过于庞大** | 一个System处理所有逻辑 | 按功能拆分，如MovementSystem、CombatSystem |
| **忽视生命周期** | 不理解OnAttach/OnDetach时机 | 参考Unity的Awake/OnDestroy对应关系 |
| **直接修改数据** | 绕过Command直接修改组件 | 养成使用Command的习惯，支持Undo |
| **过度使用事件** | 所有通信都用Broker | 简单查询使用Query，必要时用事件 |
| **忽略性能** | 每帧查询所有实体 | 缓存查询结果，使用缓存的实体列表 |

---

## 七、总结

### 7.1 核心差异总结

| 维度 | Unity OOP | Asaki 实体系统 |
|-----|-----------|---------------|
| **设计范式** | 面向对象、继承 | 数据驱动、组合 |
| **架构模式** | 分散管理 | 集中管理（World） |
| **数据与逻辑** | 封装在一起 | 分离（Component vs System） |
| **通信方式** | 直接引用、事件 | 事件总线、Command/Query |
| **可维护性** | 随规模下降 | 随规模保持稳定 |
| **学习曲线** | 平缓 | 较陡峭 |
| **开发效率** | 前期快 | 后期快 |

### 7.2 选择建议

**选择 Unity OOP 当：**
- 项目规模小，实体数量少
- 需要快速原型验证
- 团队熟悉传统Unity开发
- 项目生命周期短

**选择 Asaki 实体系统 当：**
- 项目规模大，实体数量多
- 需要复杂的Undo/Redo系统
- 需要高性能批量处理
- 采用CQRS等现代架构
- 团队愿意投入学习成本

### 7.3 混合使用策略

在实际项目中，可以考虑混合使用两种范式：

```
混合架构示例:
┌─────────────────────────────────────────────────────────────┐
│                      Game Scene                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Asaki 实体系统层                                    │   │
│  │  - 玩家、敌人、可交互对象                             │   │
│  │  - 使用Entity+Component管理                          │   │
│  └─────────────────────────────────────────────────────┘   │
│                              │                              │
│                              ▼                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Unity OOP 层                                        │   │
│  │  - UI系统（UGUI）                                    │   │
│  │  - 特效管理                                          │   │
│  │  - 场景管理                                          │   │
│  │  - 简单的环境对象                                    │   │
│  └─────────────────────────────────────────────────────┘   │
│                              │                              │
│                              ▼                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  桥接层 (EntityBridge)                               │   │
│  │  - 同步Entity数据到GameObject                        │   │
│  │  - 转发Unity事件到Entity系统                         │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

这种混合策略可以发挥两种范式的优势，在保持核心游戏逻辑清晰的同时，充分利用Unity生态系统的便利性。

---

*文档生成时间：2026-02-03*
*版本：v1.0*
