# Asaki Framework 实体系统使用案例设计报告

## 目录

1. [可行性报告审核结论](#一可行性报告审核结论)
   - [技术可行性评估](#11-技术可行性评估)
   - [资源需求评估](#12-资源需求评估)
   - [潜在风险分析](#13-潜在风险分析)
2. [使用案例一：2D平台跳跃游戏](#二使用案例一2d平台跳跃游戏)
3. [使用案例二：3D动作冒险游戏](#三使用案例二3d动作冒险游戏)
4. [使用案例三：2D俯视角射击游戏](#四使用案例三2d俯视角射击游戏)
5. [总结与建议](#五总结与建议)

---

## 一、可行性报告审核结论

### 1.1 技术可行性评估

基于对《Asaki Framework 实体系统可行性分析报告》的深入审核，得出以下技术可行性结论：

#### 1.1.1 架构兼容性：✅ 高度可行

| 评估维度 | 评估结果 | 说明 |
|---------|---------|------|
| 与CQRS架构集成 | **优秀** | 实体系统作为Model层的补充，与现有Command/Query/Event机制天然契合 |
| 与Model-System分层 | **良好** | EntityModel可作为聚合根，System层负责实体逻辑处理，架构一致 |
| 与Blackboard系统 | **优秀** | Blackboard可作为组件数据存储后端，形成互补关系 |
| 与Unity生态 | **良好** | 推荐的轻量级EC模式与Unity传统组件系统开发习惯一致 |

**关键技术点验证：**

1. **接口设计合理性**：`IEntity`、`IEntityComponent`、`IEntityWorld` 三个核心接口职责清晰，符合单一职责原则
2. **生命周期管理**：组件的 `OnAttach`/`OnDetach`/`OnEnable`/`OnDisable` 回调机制完善
3. **查询系统设计**：支持单组件和多组件查询，满足常见业务场景
4. **Unity桥接方案**：`EntityBridge` 组件提供了与GameObject的双向绑定能力

#### 1.1.2 技术实现难度：中等

```
技术复杂度分析：
├── 核心接口实现：低复杂度
│   ├── IEntity / Entity：使用Dictionary存储组件
│   ├── IEntityWorld：实体容器管理
│   └── EntityId：int或long类型标识
│
├── 查询系统实现：中等复杂度
│   ├── 单组件查询：O(n)遍历
│   ├── 多组件查询：位掩码过滤
│   └── 缓存机制：观察者模式失效
│
└── Unity集成：中等复杂度
    ├── EntityBridge生命周期同步
    ├── 编辑器工具开发
    └── 性能调试工具
```

### 1.2 资源需求评估

#### 1.2.1 开发资源需求

| 阶段 | 预计工期 | 人力需求 | 关键产出 |
|-----|---------|---------|---------|
| **第一阶段：基础框架** | 2-3周 | 1名核心开发者 | 核心接口、基础组件、Architecture集成 |
| **第二阶段：查询系统** | 1-2周 | 1名核心开发者 | 查询API、缓存优化、迭代器 |
| **第三阶段：高级功能** | 2-3周 | 1名核心开发者 + 0.5名工具开发 | 原型系统、Unity桥接、调试工具 |
| **文档与示例** | 1周 | 0.5名技术写作 | API文档、使用指南、示例项目 |
| **总计** | **6-9周** | **约1.5人月** | **完整实体系统** |

#### 1.2.2 运行时资源开销

| 资源类型 | 预估开销 | 优化建议 |
|---------|---------|---------|
| **内存** | 每个实体约50-100字节（不含组件数据） | 使用对象池复用组件实例 |
| **CPU** | 查询操作O(n)，n为实体数量 | 缓存查询结果，使用位掩码加速 |
| **GC** | 组件列表可能产生GC Alloc | 使用数组或List池化 |

### 1.3 潜在风险分析

#### 1.3.1 风险矩阵

| 风险项 | 可能性 | 影响程度 | 风险等级 | 缓解措施 |
|-------|-------|---------|---------|---------|
| 与现有架构冲突 | 低 | 高 | 🟡 中 | 保持轻量级设计，作为可选功能 |
| 性能不达预期 | 中 | 中 | 🟡 中 | 提供性能指南，支持缓存优化 |
| 开发者学习成本 | 中 | 低 | 🟢 低 | 提供详细文档，保持Unity风格 |
| 与GameObject系统冲突 | 中 | 中 | 🟡 中 | 提供清晰的桥接方案和最佳实践 |
| 过度设计风险 | 中 | 高 | 🟡 中 | 坚持轻量级EC，不实现完整ECS |

#### 1.3.2 设计边界确认

**推荐的设计边界（应遵循）：**

| 应该做 ✅ | 不应该做 ❌ |
|---------|-----------|
| 轻量级Entity-Component模式 | 完整的ECS内存布局优化（Archetype Chunk） |
| 作为Model-System架构的补充 | 取代现有的Model-System架构 |
| 可选功能，渐进式采用 | 强制所有项目使用 |
| 与Unity开发习惯保持一致 | 引入全新的开发范式 |
| 支持Undo/Redo的命令封装 | 在组件中实现复杂业务逻辑 |

---

## 二、使用案例一：2D平台跳跃游戏

### 2.1 案例概述

**游戏类型**：2D平台跳跃（Platformer）
**核心玩法**：玩家控制角色在2D场景中跳跃、移动，收集道具，避开敌人
**技术特点**：物理驱动、精确碰撞、流畅的摄像机跟随

### 2.2 3C核心要素设计

#### 2.2.1 Character（角色）

**实体组件设计：**

```csharp
// ============================================
// 2D平台跳跃游戏 - 角色组件设计
// ============================================

/// <summary>
/// 玩家标签组件 - 标记玩家实体
/// </summary>
public class PlayerTagComponent : IEntityComponent
{
    public IEntity Entity { get; set; }
    public int PlayerId { get; set; } = 0;

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose() { }
}

/// <summary>
/// 2D变换组件 - 位置、旋转、缩放
/// </summary>
public class Transform2DComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    // 使用ReactiveProperty支持数据绑定
    public AsakiProperty<Vector2> Position { get; } = new(Vector2.zero);
    public AsakiProperty<float> Rotation { get; } = new(0f);
    public AsakiProperty<Vector2> Scale { get; } = new(Vector2.one);

    // 便捷属性
    public Vector2 Forward => new Vector2(
        Mathf.Cos(Rotation.Value * Mathf.Deg2Rad),
        Mathf.Sin(Rotation.Value * Mathf.Deg2Rad)
    );

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose()
    {
        Position?.Dispose();
        Rotation?.Dispose();
        Scale?.Dispose();
    }
}

/// <summary>
/// 2D物理组件 - 刚体物理属性
/// </summary>
public class Physics2DComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    // 物理属性
    public Vector2 Velocity { get; set; }
    public float GravityScale { get; set; } = 1f;
    public float Friction { get; set; } = 0.1f;
    public float MaxSpeed { get; set; } = 10f;

    // 状态标记
    public bool IsGrounded { get; set; }
    public bool IsTouchingWall { get; set; }

    // 碰撞体引用（与Unity物理系统桥接）
    public Rigidbody2D Rigidbody { get; set; }
    public Collider2D Collider { get; set; }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose() { }
}

/// <summary>
/// 角色属性组件 - 生命值、能量等
/// </summary>
public class CharacterStatsComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    // 使用ReactiveProperty实现UI自动更新
    public AsakiProperty<int> Health { get; } = new(100);
    public AsakiProperty<int> MaxHealth { get; } = new(100);
    public AsakiProperty<int> Energy { get; } = new(100);
    public AsakiProperty<bool> IsAlive { get; } = new(true);

    // 受伤无敌时间
    public float InvincibleTime { get; set; } = 0f;

    public void TakeDamage(int damage)
    {
        if (InvincibleTime > 0 || !IsAlive.Value) return;

        Health.Value = Mathf.Max(0, Health.Value - damage);
        InvincibleTime = 0.5f; // 0.5秒无敌

        if (Health.Value <= 0)
        {
            IsAlive.Value = false;
            AsakiBroker.Publish(new PlayerDeathEvent { EntityId = Entity.Id });
        }
        else
        {
            AsakiBroker.Publish(new PlayerDamagedEvent
            {
                EntityId = Entity.Id,
                Damage = damage,
                CurrentHealth = Health.Value
            });
        }
    }

    public void Heal(int amount)
    {
        if (!IsAlive.Value) return;
        Health.Value = Mathf.Min(MaxHealth.Value, Health.Value + amount);
    }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose()
    {
        Health?.Dispose();
        MaxHealth?.Dispose();
        Energy?.Dispose();
        IsAlive?.Dispose();
    }
}

/// <summary>
/// 跳跃能力组件
/// </summary>
public class JumpAbilityComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    // 跳跃参数
    public float JumpForce { get; set; } = 12f;
    public int MaxJumpCount { get; set; } = 2; // 支持二段跳
    public float CoyoteTime { get; set; } = 0.1f; // 土狼时间
    public float JumpBufferTime { get; set; } = 0.1f; // 跳跃缓冲

    // 运行时状态
    public int CurrentJumpCount { get; set; }
    public float CoyoteTimer { get; set; }
    public float JumpBufferTimer { get; set; }

    public bool CanJump => CurrentJumpCount < MaxJumpCount;

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose() { }
}

/// <summary>
/// 动画组件 - 管理角色动画状态
/// </summary>
public class AnimationComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    public Animator Animator { get; set; }

    // 动画参数哈希（性能优化）
    private readonly int _speedHash = Animator.StringToHash("Speed");
    private readonly int _isGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int _isJumpingHash = Animator.StringToHash("IsJumping");
    private readonly int _velocityYHash = Animator.StringToHash("VelocityY");

    public void UpdateAnimation(float speed, bool isGrounded, bool isJumping, float velocityY)
    {
        if (Animator == null) return;

        Animator.SetFloat(_speedHash, speed);
        Animator.SetBool(_isGroundedHash, isGrounded);
        Animator.SetBool(_isJumpingHash, isJumping);
        Animator.SetFloat(_velocityYHash, velocityY);
    }

    public void PlayAnimation(string stateName)
    {
        Animator?.Play(stateName);
    }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose() { }
}
```

**角色创建命令：**

```csharp
/// <summary>
/// 创建玩家实体命令
/// </summary>
public class CreatePlayerCommand : AsakiCommand<EntityId>
{
    private readonly Vector2 _spawnPosition;
    private readonly GameObject _playerPrefab;

    public CreatePlayerCommand(Vector2 spawnPosition, GameObject playerPrefab)
    {
        _spawnPosition = spawnPosition;
        _playerPrefab = playerPrefab;
    }

    public override EntityId Execute()
    {
        var world = GetModel<EntityModel>().World;
        var entity = world.CreateEntity();

        // 添加玩家标签
        entity.AddComponent<PlayerTagComponent>();

        // 添加变换组件
        var transform = entity.AddComponent<Transform2DComponent>();
        transform.Position.Value = _spawnPosition;

        // 添加物理组件
        var physics = entity.AddComponent<Physics2DComponent>();
        physics.GravityScale = 3f;
        physics.MaxSpeed = 8f;

        // 添加属性组件
        entity.AddComponent<CharacterStatsComponent>();

        // 添加跳跃能力
        entity.AddComponent<JumpAbilityComponent>();

        // 添加动画组件
        entity.AddComponent<AnimationComponent>();

        // 实例化Unity对象并建立桥接
        var go = Object.Instantiate(_playerPrefab, _spawnPosition, Quaternion.identity);
        var bridge = go.AddComponent<PlayerEntityBridge>();
        bridge.Initialize(entity.Id);

        // 设置物理引用
        physics.Rigidbody = go.GetComponent<Rigidbody2D>();
        physics.Collider = go.GetComponent<Collider2D>();

        // 设置动画引用
        var anim = entity.GetComponent<AnimationComponent>();
        anim.Animator = go.GetComponent<Animator>();

        return entity.Id;
    }
}
```

#### 2.2.2 Camera（摄像机）

**摄像机实体设计：**

```csharp
/// <summary>
/// 摄像机组件 - 2D平滑跟随
/// </summary>
public class Camera2DComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    // 目标实体（跟随目标）
    public IEntity TargetEntity { get; set; }

    // 跟随参数
    public float SmoothTime { get; set; } = 0.3f;
    public Vector2 Offset { get; set; } = new Vector2(0, 2);
    public float LookAheadDistance { get; set; } = 3f;

    // 边界限制
    public bool UseBounds { get; set; } = false;
    public Bounds CameraBounds { get; set; }

    // 运行时数据
    private Vector2 _velocity;

    public Vector2 CalculatePosition(Vector2 targetPosition, Vector2 targetVelocity, float deltaTime)
    {
        // 预测性跟随
        Vector2 lookAhead = targetVelocity.normalized * LookAheadDistance;
        Vector2 desiredPosition = targetPosition + Offset + lookAhead;

        // 平滑插值
        Vector2 smoothedPosition = Vector2.SmoothDamp(
            Entity.GetComponent<Transform2DComponent>().Position.Value,
            desiredPosition,
            ref _velocity,
            SmoothTime,
            float.MaxValue,
            deltaTime
        );

        // 边界限制
        if (UseBounds)
        {
            smoothedPosition = ClampToBounds(smoothedPosition);
        }

        return smoothedPosition;
    }

    private Vector2 ClampToBounds(Vector2 position)
    {
        float camHeight = Camera.main.orthographicSize * 2;
        float camWidth = camHeight * Camera.main.aspect;

        float minX = CameraBounds.min.x + camWidth / 2;
        float maxX = CameraBounds.max.x - camWidth / 2;
        float minY = CameraBounds.min.y + camHeight / 2;
        float maxY = CameraBounds.max.y - camHeight / 2;

        return new Vector2(
            Mathf.Clamp(position.x, minX, maxX),
            Mathf.Clamp(position.y, minY, maxY)
        );
    }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose() { }
}

/// <summary>
/// 摄像机震动组件
/// </summary>
public class CameraShakeComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    public float ShakeAmount { get; set; }
    public float ShakeDuration { get; set; }
    public float DecreaseFactor { get; set; } = 1f;

    public Vector2 GetShakeOffset()
    {
        if (ShakeDuration <= 0) return Vector2.zero;

        ShakeDuration -= Time.deltaTime;
        ShakeAmount = Mathf.Max(0, ShakeAmount - DecreaseFactor * Time.deltaTime);

        return Random.insideUnitCircle * ShakeAmount;
    }

    public void TriggerShake(float amount, float duration)
    {
        ShakeAmount = amount;
        ShakeDuration = duration;
    }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose() { }
}
```

**摄像机系统：**

```csharp
/// <summary>
/// 摄像机控制系统 - 处理所有摄像机实体的更新
/// </summary>
public class CameraControlSystem : IAsakiSystem, IAsakiTickable
{
    private EntityModel _entityModel;
    private Camera _mainCamera;

    public void Setup()
    {
        _mainCamera = Camera.main;
        _entityModel = AsakiContext.Get<EntityModel>();
    }

    public void Tick(float deltaTime)
    {
        // 查询所有摄像机实体
        foreach (var entity in _entityModel.World.Query<Camera2DComponent>())
        {
            var cameraComp = entity.GetComponent<Camera2DComponent>();
            var transform = entity.GetComponent<Transform2DComponent>();

            if (cameraComp.TargetEntity == null) continue;

            // 获取目标位置和速度
            var targetTransform = cameraComp.TargetEntity.GetComponent<Transform2DComponent>();
            var targetPhysics = cameraComp.TargetEntity.GetComponent<Physics2DComponent>();

            if (targetTransform == null) continue;

            Vector2 targetPos = targetTransform.Position.Value;
            Vector2 targetVel = targetPhysics?.Velocity ?? Vector2.zero;

            // 计算新位置
            Vector2 newPosition = cameraComp.CalculatePosition(targetPos, targetVel, deltaTime);

            // 应用震动
            var shakeComp = entity.GetComponent<CameraShakeComponent>();
            if (shakeComp != null)
            {
                newPosition += shakeComp.GetShakeOffset();
            }

            // 更新摄像机位置
            transform.Position.Value = newPosition;
            _mainCamera.transform.position = new Vector3(
                newPosition.x,
                newPosition.y,
                _mainCamera.transform.position.z
            );
        }
    }

    public void Dispose() { }
}
```

#### 2.2.3 Controller（控制器）

**输入处理系统：**

```csharp
/// <summary>
/// 玩家输入组件 - 存储输入状态
/// </summary>
public class PlayerInputComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    // 使用ReactiveProperty支持输入响应
    public AsakiProperty<Vector2> MoveInput { get; } = new(Vector2.zero);
    public AsakiProperty<bool> JumpPressed { get; } = new(false);
    public AsakiProperty<bool> JumpHeld { get; } = new(false);
    public AsakiProperty<bool> AttackPressed { get; } = new(false);

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose()
    {
        MoveInput?.Dispose();
        JumpPressed?.Dispose();
        JumpHeld?.Dispose();
        AttackPressed?.Dispose();
    }
}

/// <summary>
/// 玩家输入系统 - 读取输入并更新组件
/// </summary>
public class PlayerInputSystem : IAsakiSystem, IAsakiTickable
{
    private EntityModel _entityModel;

    public void Setup()
    {
        _entityModel = AsakiContext.Get<EntityModel>();
    }

    public void Tick(float deltaTime)
    {
        // 查询所有玩家实体
        foreach (var entity in _entityModel.World.Query<PlayerTagComponent>())
        {
            var input = entity.GetComponent<PlayerInputComponent>();
            if (input == null) continue;

            // 读取水平输入
            float horizontal = Input.GetAxisRaw("Horizontal");
            input.MoveInput.Value = new Vector2(horizontal, 0);

            // 读取跳跃输入
            input.JumpPressed.Value = Input.GetButtonDown("Jump");
            input.JumpHeld.Value = Input.GetButton("Jump");

            // 读取攻击输入
            input.AttackPressed.Value = Input.GetButtonDown("Fire1");
        }
    }

    public void Dispose() { }
}

/// <summary>
/// 玩家移动系统 - 处理玩家移动逻辑
/// </summary>
public class PlayerMovementSystem : IAsakiSystem, IAsakiTickable
{
    private EntityModel _entityModel;

    public void Setup()
    {
        _entityModel = AsakiContext.Get<EntityModel>();
    }

    public void Tick(float deltaTime)
    {
        foreach (var entity in _entityModel.World.Query<PlayerTagComponent>())
        {
            var input = entity.GetComponent<PlayerInputComponent>();
            var physics = entity.GetComponent<Physics2DComponent>();
            var transform = entity.GetComponent<Transform2DComponent>();
            var jumpAbility = entity.GetComponent<JumpAbilityComponent>();
            var stats = entity.GetComponent<CharacterStatsComponent>();

            if (input == null || physics == null || transform == null) continue;
            if (!stats.IsAlive.Value) continue;

            // 处理水平移动
            HandleMovement(input, physics, deltaTime);

            // 处理跳跃
            if (jumpAbility != null)
            {
                HandleJump(input, physics, jumpAbility, deltaTime);
            }

            // 应用物理
            ApplyPhysics(physics, transform, deltaTime);

            // 更新动画
            UpdateAnimation(entity, physics, input);
        }
    }

    private void HandleMovement(PlayerInputComponent input, Physics2DComponent physics, float deltaTime)
    {
        float targetSpeed = input.MoveInput.Value.x * physics.MaxSpeed;
        float speedDiff = targetSpeed - physics.Velocity.x;
        float acceleration = physics.IsGrounded ? 10f : 5f;

        physics.Velocity.x += speedDiff * acceleration * deltaTime;
    }

    private void HandleJump(
        PlayerInputComponent input,
        Physics2DComponent physics,
        JumpAbilityComponent jumpAbility,
        float deltaTime)
    {
        // 更新土狼时间
        if (physics.IsGrounded)
        {
            jumpAbility.CoyoteTimer = jumpAbility.CoyoteTime;
            jumpAbility.CurrentJumpCount = 0;
        }
        else
        {
            jumpAbility.CoyoteTimer -= deltaTime;
        }

        // 跳跃缓冲
        if (input.JumpPressed.Value)
        {
            jumpAbility.JumpBufferTimer = jumpAbility.JumpBufferTime;
        }
        else
        {
            jumpAbility.JumpBufferTimer -= deltaTime;
        }

        // 执行跳跃
        bool canCoyoteJump = jumpAbility.CoyoteTimer > 0 && jumpAbility.CurrentJumpCount == 0;
        bool canBufferedJump = jumpAbility.JumpBufferTimer > 0 && physics.IsGrounded;

        if ((canCoyoteJump || canBufferedJump || jumpAbility.CanJump) && jumpAbility.JumpBufferTimer > 0)
        {
            physics.Velocity.y = jumpAbility.JumpForce;
            jumpAbility.CurrentJumpCount++;
            jumpAbility.JumpBufferTimer = 0;
            jumpAbility.CoyoteTimer = 0;

            AsakiBroker.Publish(new PlayerJumpEvent { EntityId = physics.Entity.Id });
        }

        // 可变跳跃高度（按住跳得更高）
        if (!input.JumpHeld.Value && physics.Velocity.y > 0)
        {
            physics.Velocity.y *= 0.5f;
        }
    }

    private void ApplyPhysics(Physics2DComponent physics, Transform2DComponent transform, float deltaTime)
    {
        // 应用重力
        if (!physics.IsGrounded)
        {
            physics.Velocity.y += Physics2D.gravity.y * physics.GravityScale * deltaTime;
        }

        // 应用摩擦力
        if (physics.IsGrounded)
        {
            physics.Velocity.x *= (1 - physics.Friction);
        }

        // 更新位置
        Vector2 newPosition = transform.Position.Value + physics.Velocity * deltaTime;
        transform.Position.Value = newPosition;

        // 同步到Unity物理系统
        if (physics.Rigidbody != null)
        {
            physics.Rigidbody.velocity = physics.Velocity;
        }
    }

    private void UpdateAnimation(IEntity entity, Physics2DComponent physics, PlayerInputComponent input)
    {
        var anim = entity.GetComponent<AnimationComponent>();
        if (anim == null) return;

        float speed = Mathf.Abs(physics.Velocity.x);
        bool isJumping = !physics.IsGrounded && physics.Velocity.y > 0;

        anim.UpdateAnimation(speed, physics.IsGrounded, isJumping, physics.Velocity.y);
    }

    public void Dispose() { }
}
```

### 2.3 系统架构图

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     2D平台跳跃游戏 - 实体系统架构                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                          Entity World                               │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐  │   │
│  │  │  Player      │  │  Camera      │  │  Enemies/Items           │  │   │
│  │  │  Entity      │  │  Entity      │  │  Entities...             │  │   │
│  │  └──────┬───────┘  └──────┬───────┘  └───────────┬──────────────┘  │   │
│  │         │                 │                      │                 │   │
│  │  ┌──────▼───────┐  ┌──────▼───────┐  ┌───────────▼──────────┐     │   │
│  │  │ PlayerTag    │  │ Camera2D     │  │ EnemyTag/ItemTag     │     │   │
│  │  │ Transform2D  │  │ Transform2D  │  │ Transform2D          │     │   │
│  │  │ Physics2D    │  │ CameraShake  │  │ Physics2D            │     │   │
│  │  │ Character    │  └──────────────┘  │ AI/Collectible       │     │   │
│  │  │ JumpAbility  │                    └──────────────────────┘     │   │
│  │  │ Animation    │                                                │   │
│  │  │ PlayerInput  │                                                │   │
│  │  └──────────────┘                                                │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                        │
│                                    ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                          System 层                                   │   │
│  │  ┌─────────────────┐ ┌─────────────────┐ ┌───────────────────────┐ │   │
│  │  │ PlayerInput     │ │ PlayerMovement  │ │ CameraControl         │ │   │
│  │  │ System          │ │ System          │ │ System                │ │   │
│  │  │ (读取输入)       │ │ (移动/跳跃逻辑)  │ │ (摄像机跟随)          │ │   │
│  │  └─────────────────┘ └─────────────────┘ └───────────────────────┘ │   │
│  │  ┌─────────────────┐ ┌─────────────────┐                           │   │
│  │  │ PhysicsSync     │ │ Animation       │                           │   │
│  │  │ System          │ │ System          │                           │   │
│  │  │ (物理同步)       │ │ (动画更新)       │                           │   │
│  │  └─────────────────┘ └─────────────────┘                           │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                        │
│                                    ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                          Command 层                                  │   │
│  │  ┌─────────────────┐ ┌─────────────────┐ ┌───────────────────────┐ │   │
│  │  │ CreatePlayer    │ │ PlayerJump      │ │ PlayerTakeDamage      │ │   │
│  │  │ Command         │ │ Command         │ │ Command               │ │   │
│  │  └─────────────────┘ └─────────────────┘ └───────────────────────┘ │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2.4 关键交互流程

```
玩家按下跳跃键
      │
      ▼
┌─────────────────┐
│ PlayerInput     │
│ System          │
│ 读取JumpPressed │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ PlayerMovement  │
│ System          │
│ 检查跳跃条件     │
│ (土狼时间/二段跳) │
└────────┬────────┘
         │
         ▼
┌─────────────────┐     ┌─────────────────┐
│ Physics2D       │────▶│ PlayerJumpEvent │
│ Component       │     │ 发布事件         │
│ 更新Velocity    │     └────────┬────────┘
└────────┬────────┘              │
         │                       ▼
         │              ┌─────────────────┐
         │              │ CameraShake     │
         │              │ System          │
         │              │ 触发摄像机震动   │
         │              └─────────────────┘
         ▼
┌─────────────────┐
│ Unity Rigidbody │
│ 同步物理更新     │
└─────────────────┘
```

---

## 三、使用案例二：3D动作冒险游戏

### 3.1 案例概述

**游戏类型**：3D动作冒险（Action Adventure）
**核心玩法**：玩家在3D世界中探索、战斗、解谜，与NPC交互
**技术特点**：复杂动画系统、战斗连招、状态机驱动、对话系统

### 3.2 3C核心要素设计

#### 3.2.1 Character（角色）

```csharp
// ============================================
// 3D动作冒险游戏 - 角色组件设计
// ============================================

/// <summary>
/// 3D变换组件
/// </summary>
public class Transform3DComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    public AsakiProperty<Vector3> Position { get; } = new(Vector3.zero);
    public AsakiProperty<Quaternion> Rotation { get; } = new(Quaternion.identity);
    public AsakiProperty<Vector3> Scale { get; } = new(Vector3.one);

    public Vector3 Forward => Rotation.Value * Vector3.forward;
    public Vector3 Right => Rotation.Value * Vector3.right;
    public Vector3 Up => Rotation.Value * Vector3.up;

    public void LookAt(Vector3 target)
    {
        Vector3 direction = target - Position.Value;
        if (direction.sqrMagnitude > 0.001f)
        {
            Rotation.Value = Quaternion.LookRotation(direction);
        }
    }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose()
    {
        Position?.Dispose();
        Rotation?.Dispose();
        Scale?.Dispose();
    }
}

/// <summary>
/// 角色状态机组件 - 管理复杂角色状态
/// </summary>
public class CharacterStateMachineComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    private AsakiStateMachine<CharacterState> _stateMachine;

    public AsakiProperty<CharacterState> CurrentState { get; } = new(CharacterState.Idle);

    public void Initialize()
    {
        _stateMachine = new AsakiStateMachine<CharacterState>();

        // 配置状态转换
        _stateMachine.Configure(CharacterState.Idle)
            .Permit(CharacterTrigger.Move, CharacterState.Walk)
            .Permit(CharacterTrigger.Jump, CharacterState.Jump)
            .Permit(CharacterTrigger.Attack, CharacterState.Attack);

        _stateMachine.Configure(CharacterState.Walk)
            .Permit(CharacterTrigger.Stop, CharacterState.Idle)
            .Permit(CharacterTrigger.Run, CharacterState.Run)
            .Permit(CharacterTrigger.Jump, CharacterState.Jump)
            .Permit(CharacterTrigger.Attack, CharacterState.Attack);

        _stateMachine.Configure(CharacterState.Run)
            .Permit(CharacterTrigger.Stop, CharacterState.Idle)
            .Permit(CharacterTrigger.Walk, CharacterState.Walk)
            .Permit(CharacterTrigger.Jump, CharacterState.Jump)
            .Permit(CharacterTrigger.Attack, CharacterState.Attack);

        _stateMachine.Configure(CharacterState.Jump)
            .Permit(CharacterTrigger.Land, CharacterState.Idle)
            .Permit(CharacterTrigger.Attack, CharacterState.AirAttack);

        _stateMachine.Configure(CharacterState.Attack)
            .Permit(CharacterTrigger.AttackEnd, CharacterState.Idle)
            .Permit(CharacterTrigger.Combo, CharacterState.ComboAttack);

        _stateMachine.OnTransitioned += transition =>
        {
            CurrentState.Value = transition.Destination;
            AsakiBroker.Publish(new CharacterStateChangedEvent
            {
                EntityId = Entity.Id,
                PreviousState = transition.Source,
                CurrentState = transition.Destination
            });
        };
    }

    public void Fire(CharacterTrigger trigger)
    {
        _stateMachine?.Fire(trigger);
    }

    public bool CanFire(CharacterTrigger trigger)
    {
        return _stateMachine?.CanFire(trigger) ?? false;
    }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose()
    {
        CurrentState?.Dispose();
    }
}

public enum CharacterState
{
    Idle, Walk, Run, Jump, Fall, Attack, AirAttack, ComboAttack,
    Hit, Dodge, Block, Death, Interact
}

public enum CharacterTrigger
{
    Move, Stop, Run, Walk, Jump, Land, Attack, AttackEnd, Combo,
    GetHit, Dodge, Block, Die, Interact
}

/// <summary>
/// 战斗属性组件
/// </summary>
public class CombatStatsComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    // 基础属性
    public AsakiProperty<int> Level { get; } = new(1);
    public AsakiProperty<int> Health { get; } = new(100);
    public AsakiProperty<int> MaxHealth { get; } = new(100);
    public AsakiProperty<int> Stamina { get; } = new(100);
    public AsakiProperty<int> MaxStamina { get; } = new(100);

    // 战斗属性
    public int AttackPower { get; set; } = 10;
    public int Defense { get; set; } = 5;
    public float CriticalRate { get; set; } = 0.1f;
    public float CriticalDamage { get; set; } = 1.5f;

    // 战斗状态
    public bool IsInvincible { get; set; }
    public float InvincibleTimer { get; set; }

    public void TakeDamage(int damage, IEntity attacker)
    {
        if (IsInvincible || Health.Value <= 0) return;

        int actualDamage = Mathf.Max(1, damage - Defense);
        Health.Value = Mathf.Max(0, Health.Value - actualDamage);

        AsakiBroker.Publish(new DamageTakenEvent
        {
            EntityId = Entity.Id,
            AttackerId = attacker?.Id ?? -1,
            Damage = actualDamage,
            IsCritical = false
        });

        if (Health.Value <= 0)
        {
            AsakiBroker.Publish(new EntityDeathEvent { EntityId = Entity.Id });
        }
        else
        {
            // 触发受击无敌
            IsInvincible = true;
            InvincibleTimer = 0.5f;
        }
    }

    public bool ConsumeStamina(int amount)
    {
        if (Stamina.Value < amount) return false;
        Stamina.Value -= amount;
        return true;
    }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose()
    {
        Level?.Dispose();
        Health?.Dispose();
        MaxHealth?.Dispose();
        Stamina?.Dispose();
        MaxStamina?.Dispose();
    }
}

/// <summary>
/// 武器组件
/// </summary>
public class WeaponComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    public string WeaponId { get; set; }
    public int BaseDamage { get; set; } = 10;
    public float AttackSpeed { get; set; } = 1f;
    public float AttackRange { get; set; } = 2f;

    // 连招配置
    public List<AttackData> ComboChain { get; set; } = new();
    public int CurrentComboIndex { get; set; }
    public float ComboWindow { get; set; } = 0.5f;
    public float ComboTimer { get; set; }

    public bool CanAttack => ComboTimer <= 0 || CurrentComboIndex < ComboChain.Count;

    public AttackData GetCurrentAttack()
    {
        if (ComboChain.Count == 0) return null;
        return ComboChain[Mathf.Min(CurrentComboIndex, ComboChain.Count - 1)];
    }

    public void AdvanceCombo()
    {
        CurrentComboIndex++;
        ComboTimer = ComboWindow;

        if (CurrentComboIndex >= ComboChain.Count)
        {
            ResetCombo();
        }
    }

    public void ResetCombo()
    {
        CurrentComboIndex = 0;
        ComboTimer = 0;
    }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose() { }
}

public class AttackData
{
    public string AnimationName;
    public float DamageMultiplier = 1f;
    public float StaminaCost = 10f;
    public float Duration = 0.5f;
    public float HitBoxStartTime = 0.2f;
    public float HitBoxEndTime = 0.4f;
}

/// <summary>
/// 锁定目标组件 - 用于战斗锁定
/// </summary>
public class LockOnComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    public IEntity LockedTarget { get; set; }
    public float LockOnRange { get; set; } = 10f;
    public float LockOnAngle { get; set; } = 60f;

    public bool IsLockedOn => LockedTarget != null;

    public void FindTarget(List<IEntity> potentialTargets)
    {
        var transform = Entity.GetComponent<Transform3DComponent>();
        if (transform == null) return;

        IEntity bestTarget = null;
        float bestScore = float.MaxValue;

        foreach (var target in potentialTargets)
        {
            if (target.Id == Entity.Id) continue;

            var targetTransform = target.GetComponent<Transform3DComponent>();
            if (targetTransform == null) continue;

            Vector3 toTarget = targetTransform.Position.Value - transform.Position.Value;
            float distance = toTarget.magnitude;
            float angle = Vector3.Angle(transform.Forward, toTarget);

            if (distance > LockOnRange || angle > LockOnAngle) continue;

            float score = distance + angle * 0.1f;
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = target;
            }
        }

        LockedTarget = bestTarget;
    }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose() { }
}
```

#### 3.2.2 Camera（摄像机）

```csharp
/// <summary>
/// 3D动作摄像机组件 - 支持锁定和动态视角
/// </summary>
public class ActionCameraComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    // 基础跟随
    public IEntity TargetEntity { get; set; }
    public Vector3 Offset { get; set; } = new Vector3(0, 2, -5);
    public float FollowSpeed { get; set; } = 5f;

    // 视角控制
    public float RotationSpeed { get; set; } = 3f;
    public float MinVerticalAngle { get; set; } = -30f;
    public float MaxVerticalAngle { get; set; } = 60f;
    public float CurrentYaw { get; set; }
    public float CurrentPitch { get; set; }

    // 锁定模式
    public bool IsLockOnMode { get; set; }
    public IEntity LockOnTarget { get; set; }
    public float LockOnTransitionSpeed { get; set; } = 5f;

    // 碰撞检测
    public float CameraRadius { get; set; } = 0.3f;
    public LayerMask CollisionLayers { get; set; }

    public Vector3 CalculateDesiredPosition()
    {
        if (TargetEntity == null) return Vector3.zero;

        var targetTransform = TargetEntity.GetComponent<Transform3DComponent>();
        if (targetTransform == null) return Vector3.zero;

        Vector3 targetPos = targetTransform.Position.Value;

        if (IsLockOnMode && LockOnTarget != null)
        {
            // 锁定模式：摄像机位于目标和锁定对象之间
            return CalculateLockOnPosition(targetPos);
        }
        else
        {
            // 自由模式：基于输入旋转
            return CalculateFreePosition(targetPos);
        }
    }

    private Vector3 CalculateFreePosition(Vector3 targetPos)
    {
        Quaternion rotation = Quaternion.Euler(CurrentPitch, CurrentYaw, 0);
        Vector3 desiredPos = targetPos + rotation * Offset;
        return desiredPos;
    }

    private Vector3 CalculateLockOnPosition(Vector3 targetPos)
    {
        var lockOnTransform = LockOnTarget.GetComponent<Transform3DComponent>();
        if (lockOnTransform == null) return targetPos + Offset;

        Vector3 lockOnPos = lockOnTransform.Position.Value;
        Vector3 midPoint = (targetPos + lockOnPos) * 0.5f;

        // 计算垂直于目标连线的方向
        Vector3 toTarget = (targetPos - lockOnPos).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, toTarget).normalized;

        return midPoint + right * Offset.z + Vector3.up * Offset.y;
    }

    public Vector3 HandleCollision(Vector3 desiredPosition, Vector3 targetPosition)
    {
        Vector3 direction = desiredPosition - targetPosition;
        float distance = direction.magnitude;

        if (Physics.SphereCast(targetPosition, CameraRadius, direction.normalized,
            out RaycastHit hit, distance, CollisionLayers))
        {
            return targetPosition + direction.normalized * (hit.distance - 0.1f);
        }

        return desiredPosition;
    }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose() { }
}
```

#### 3.2.3 Controller（控制器）

```csharp
/// <summary>
/// 3D动作输入组件
/// </summary>
public class ActionInputComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    // 移动输入
    public AsakiProperty<Vector2> MoveInput { get; } = new(Vector2.zero);
    public AsakiProperty<bool> IsRunning { get; } = new(false);

    // 摄像机输入
    public AsakiProperty<Vector2> LookInput { get; } = new(Vector2.zero);

    // 动作输入
    public AsakiProperty<bool> JumpPressed { get; } = new(false);
    public AsakiProperty<bool> AttackPressed { get; } = new(false);
    public AsakiProperty<bool> DodgePressed { get; } = new(false);
    public AsakiProperty<bool> BlockPressed { get; } = new(false);
    public AsakiProperty<bool> LockOnPressed { get; } = new(false);
    public AsakiProperty<bool> InteractPressed { get; } = new(false);

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose()
    {
        MoveInput?.Dispose();
        IsRunning?.Dispose();
        LookInput?.Dispose();
        JumpPressed?.Dispose();
        AttackPressed?.Dispose();
        DodgePressed?.Dispose();
        BlockPressed?.Dispose();
        LockOnPressed?.Dispose();
        InteractPressed?.Dispose();
    }
}

/// <summary>
/// 3D角色控制系统
/// </summary>
public class ActionCharacterSystem : IAsakiSystem, IAsakiTickable
{
    private EntityModel _entityModel;

    public void Setup()
    {
        _entityModel = AsakiContext.Get<EntityModel>();
    }

    public void Tick(float deltaTime)
    {
        // 更新所有动作角色
        foreach (var entity in _entityModel.World.Query<ActionInputComponent>())
        {
            UpdateCharacter(entity, deltaTime);
        }
    }

    private void UpdateCharacter(IEntity entity, float deltaTime)
    {
        var input = entity.GetComponent<ActionInputComponent>();
        var stateMachine = entity.GetComponent<CharacterStateMachineComponent>();
        var combat = entity.GetComponent<CombatStatsComponent>();
        var transform = entity.GetComponent<Transform3DComponent>();

        if (input == null || stateMachine == null || combat == null || transform == null)
            return;

        // 更新无敌时间
        if (combat.IsInvincible)
        {
            combat.InvincibleTimer -= deltaTime;
            if (combat.InvincibleTimer <= 0)
                combat.IsInvincible = false;
        }

        // 处理输入
        HandleMovementInput(entity, input, stateMachine, transform);
        HandleCombatInput(entity, input, stateMachine, combat);
        HandleActionInput(entity, input, stateMachine);
    }

    private void HandleMovementInput(
        IEntity entity,
        ActionInputComponent input,
        CharacterStateMachineComponent stateMachine,
        Transform3DComponent transform)
    {
        Vector2 moveInput = input.MoveInput.Value;
        bool hasInput = moveInput.sqrMagnitude > 0.01f;

        // 根据输入触发状态转换
        if (hasInput)
        {
            if (input.IsRunning.Value && stateMachine.CanFire(CharacterTrigger.Run))
            {
                stateMachine.Fire(CharacterTrigger.Run);
            }
            else if (stateMachine.CanFire(CharacterTrigger.Move))
            {
                stateMachine.Fire(CharacterTrigger.Move);
            }

            // 计算移动方向（基于摄像机朝向）
            Vector3 camForward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized;
            Vector3 moveDirection = camForward * moveInput.y + camRight * moveInput.x;

            // 旋转角色朝向移动方向
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.Rotation.Value = Quaternion.Slerp(
                    transform.Rotation.Value,
                    targetRotation,
                    Time.deltaTime * 10f
                );
            }
        }
        else if (stateMachine.CanFire(CharacterTrigger.Stop))
        {
            stateMachine.Fire(CharacterTrigger.Stop);
        }
    }

    private void HandleCombatInput(
        IEntity entity,
        ActionInputComponent input,
        CharacterStateMachineComponent stateMachine,
        CombatStatsComponent combat)
    {
        // 攻击
        if (input.AttackPressed.Value && stateMachine.CanFire(CharacterTrigger.Attack))
        {
            var weapon = entity.GetComponent<WeaponComponent>();
            if (weapon != null && weapon.CanAttack)
            {
                var attack = weapon.GetCurrentAttack();
                if (attack != null && combat.ConsumeStamina((int)attack.StaminaCost))
                {
                    stateMachine.Fire(CharacterTrigger.Attack);
                    weapon.AdvanceCombo();
                }
            }
        }

        // 闪避
        if (input.DodgePressed.Value)
        {
            // 闪避逻辑
        }

        // 格挡
        if (input.BlockPressed.Value)
        {
            // 格挡逻辑
        }
    }

    private void HandleActionInput(
        IEntity entity,
        ActionInputComponent input,
        CharacterStateMachineComponent stateMachine)
    {
        // 跳跃
        if (input.JumpPressed.Value && stateMachine.CanFire(CharacterTrigger.Jump))
        {
            stateMachine.Fire(CharacterTrigger.Jump);
        }

        // 锁定
        if (input.LockOnPressed.Value)
        {
            var lockOn = entity.GetComponent<LockOnComponent>();
            if (lockOn != null)
            {
                // 查找敌人并锁定
                var enemies = _entityModel.World.Query<CombatStatsComponent>()
                    .Where(e => e.GetComponent<PlayerTagComponent>() == null)
                    .ToList();
                lockOn.FindTarget(enemies);
            }
        }
    }

    public void Dispose() { }
}

/// <summary>
/// 3D摄像机控制系统
/// </summary>
public class ActionCameraSystem : IAsakiSystem, IAsakiTickable
{
    private EntityModel _entityModel;
    private Camera _mainCamera;

    public void Setup()
    {
        _mainCamera = Camera.main;
        _entityModel = AsakiContext.Get<EntityModel>();
    }

    public void Tick(float deltaTime)
    {
        foreach (var entity in _entityModel.World.Query<ActionCameraComponent>())
        {
            var camera = entity.GetComponent<ActionCameraComponent>();
            var lockOn = camera.TargetEntity?.GetComponent<LockOnComponent>();

            // 更新锁定状态
            camera.IsLockOnMode = lockOn?.IsLockedOn ?? false;
            camera.LockOnTarget = lockOn?.LockedTarget;

            // 处理视角输入
            if (!camera.IsLockOnMode)
            {
                var input = camera.TargetEntity?.GetComponent<ActionInputComponent>();
                if (input != null)
                {
                    camera.CurrentYaw += input.LookInput.Value.x * camera.RotationSpeed;
                    camera.CurrentPitch -= input.LookInput.Value.y * camera.RotationSpeed;
                    camera.CurrentPitch = Mathf.Clamp(camera.CurrentPitch,
                        camera.MinVerticalAngle, camera.MaxVerticalAngle);
                }
            }
            else
            {
                // 锁定模式下自动调整视角
                UpdateLockOnRotation(camera, deltaTime);
            }

            // 计算并应用位置
            Vector3 desiredPos = camera.CalculateDesiredPosition();
            Vector3 targetPos = camera.TargetEntity.GetComponent<Transform3DComponent>().Position.Value;
            Vector3 finalPos = camera.HandleCollision(desiredPos, targetPos);

            _mainCamera.transform.position = Vector3.Lerp(
                _mainCamera.transform.position,
                finalPos,
                camera.FollowSpeed * deltaTime
            );

            // 更新朝向
            if (camera.IsLockOnMode && camera.LockOnTarget != null)
            {
                var lockOnTransform = camera.LockOnTarget.GetComponent<Transform3DComponent>();
                _mainCamera.transform.LookAt(lockOnTransform.Position.Value);
            }
            else
            {
                _mainCamera.transform.LookAt(targetPos + Vector3.up * 1.5f);
            }
        }
    }

    private void UpdateLockOnRotation(ActionCameraComponent camera, float deltaTime)
    {
        // 计算看向锁定目标所需的旋转
        var targetTransform = camera.TargetEntity.GetComponent<Transform3DComponent>();
        var lockOnTransform = camera.LockOnTarget.GetComponent<Transform3DComponent>();

        Vector3 midPoint = (targetTransform.Position.Value + lockOnTransform.Position.Value) * 0.5f;
        Vector3 toMidPoint = midPoint - targetTransform.Position.Value;

        float targetYaw = Mathf.Atan2(toMidPoint.x, toMidPoint.z) * Mathf.Rad2Deg;
        camera.CurrentYaw = Mathf.LerpAngle(camera.CurrentYaw, targetYaw,
            camera.LockOnTransitionSpeed * deltaTime);
    }

    public void Dispose() { }
}
```

### 3.3 系统架构图

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                   3D动作冒险游戏 - 实体系统架构                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                          Entity World                               │   │
│  │                                                                     │   │
│  │  ┌─────────────────────────────────────────────────────────────┐   │   │
│  │  │                    Player Entity                             │   │   │
│  │  │  ┌─────────────────────────────────────────────────────────┐ │   │   │
│  │  │  │ Components:                                             │ │   │   │
│  │  │  │ - PlayerTagComponent                                    │ │   │   │
│  │  │  │ - Transform3DComponent                                  │ │   │   │
│  │  │  │ - CharacterStateMachineComponent (状态机驱动)            │ │   │   │
│  │  │  │ - CombatStatsComponent (战斗属性)                        │ │   │   │
│  │  │  │ - WeaponComponent (武器/连招)                            │ │   │   │
│  │  │  │ - LockOnComponent (锁定系统)                             │ │   │   │
│  │  │  │ - ActionInputComponent                                   │ │   │   │
│  │  │  │ - AnimationComponent                                     │ │   │   │
│  │  │  └─────────────────────────────────────────────────────────┘ │   │   │
│  │  └─────────────────────────────────────────────────────────────┘   │   │
│  │                                                                     │   │
│  │  ┌─────────────────────────────────────────────────────────────┐   │   │
│  │  │                    Camera Entity                             │   │   │
│  │  │  - ActionCameraComponent (3D动作摄像机)                      │   │   │
│  │  │  - Transform3DComponent                                      │   │   │
│  │  │  - CameraShakeComponent                                      │   │   │
│  │  └─────────────────────────────────────────────────────────────┘   │   │
│  │                                                                     │   │
│  │  ┌─────────────────────────────────────────────────────────────┐   │   │
│  │  │                    Enemy Entities                            │   │   │
│  │  │  - EnemyTagComponent                                         │   │   │
│  │  │  - AIComponent (AI行为树)                                     │   │   │
│  │  │  - CombatStatsComponent                                      │   │   │
│  │  │  - WeaponComponent                                           │   │   │
│  │  └─────────────────────────────────────────────────────────────┘   │   │
│  │                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                        │
│                                    ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                          System 层                                   │   │
│  │  ┌─────────────────┐ ┌─────────────────┐ ┌───────────────────────┐ │   │
│  │  │ ActionInput     │ │ ActionCharacter │ │ ActionCamera          │ │   │
│  │  │ System          │ │ System          │ │ System                │ │   │
│  │  │ (输入处理)       │ │ (角色控制)       │ │ (摄像机控制)          │ │   │
│  │  └─────────────────┘ └─────────────────┘ └───────────────────────┘ │   │
│  │  ┌─────────────────┐ ┌─────────────────┐ ┌───────────────────────┐ │   │
│  │  │ Combat          │ │ AI              │ │ Animation             │ │   │
│  │  │ System          │ │ System          │ │ System                │ │   │
│  │  │ (伤害计算)       │ │ (敌人AI)         │ │ (动画状态同步)        │ │   │
│  │  └─────────────────┘ └─────────────────┘ └───────────────────────┘ │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 四、使用案例三：2D俯视角射击游戏

### 4.1 案例概述

**游戏类型**：2D俯视角射击（Top-Down Shooter）
**核心玩法**：玩家控制角色在2D地图上移动射击，对抗大量敌人
**技术特点**：大量实体管理、对象池优化、弹幕系统、波次管理

### 4.2 3C核心要素设计

#### 4.2.1 Character（角色）

```csharp
// ============================================
// 2D俯视角射击游戏 - 角色组件设计
// ============================================

/// <summary>
/// 俯视角变换组件
/// </summary>
public class TopDownTransformComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    public AsakiProperty<Vector2> Position { get; } = new(Vector2.zero);
    public AsakiProperty<float> Rotation { get; } = new(0f); // Z轴旋转
    public AsakiProperty<Vector2> Scale { get; } = new(Vector2.one);

    public Vector2 Forward => new Vector2(
        Mathf.Cos(Rotation.Value * Mathf.Deg2Rad),
        Mathf.Sin(Rotation.Value * Mathf.Deg2Rad)
    );

    public void LookAt(Vector2 target)
    {
        Vector2 direction = target - Position.Value;
        if (direction.sqrMagnitude > 0.001f)
        {
            Rotation.Value = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }
    }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose()
    {
        Position?.Dispose();
        Rotation?.Dispose();
        Scale?.Dispose();
    }
}

/// <summary>
/// 射击者组件 - 管理武器和射击
/// </summary>
public class ShooterComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    // 武器配置
    public string WeaponId { get; set; }
    public float FireRate { get; set; } = 0.2f; // 射击间隔
    public float ProjectileSpeed { get; set; } = 15f;
    public int ProjectileDamage { get; set; } = 10;
    public float SpreadAngle { get; set; } = 5f; // 散射角度
    public int ProjectileCount { get; set; } = 1; // 每次射击弹丸数

    // 运行时状态
    public float FireCooldown { get; set; }
    public bool IsFiring { get; set; }
    public int Ammo { get; set; } = 30;
    public int MaxAmmo { get; set; } = 30;
    public float ReloadTime { get; set; } = 1.5f;
    public bool IsReloading { get; set; }
    public float ReloadProgress { get; set; }

    // 枪口偏移
    public Vector2 MuzzleOffset { get; set; } = new Vector2(0.5f, 0);

    public bool CanFire => FireCooldown <= 0 && !IsReloading && Ammo > 0;

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose() { }
}

/// <summary>
/// 移动组件 - 俯视角移动
/// </summary>
public class TopDownMovementComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    public float MoveSpeed { get; set; } = 5f;
    public float Acceleration { get; set; } = 10f;
    public float Deceleration { get; set; } = 15f;

    public Vector2 CurrentVelocity { get; set; }
    public bool IsMoving { get; set; }

    // 冲刺
    public float DashSpeed { get; set; } = 15f;
    public float DashDuration { get; set; } = 0.2f;
    public float DashCooldown { get; set; } = 1f;
    public bool IsDashing { get; set; }
    public float DashTimer { get; set; }
    public float DashCooldownTimer { get; set; }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose() { }
}

/// <summary>
/// 生命值组件 - 简化版
/// </summary>
public class HealthComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    public AsakiProperty<int> CurrentHealth { get; } = new(100);
    public AsakiProperty<int> MaxHealth { get; } = new(100);

    // 受伤保护
    public float HitInvincibilityDuration { get; set; } = 0.1f;
    public float InvincibilityTimer { get; set; }
    public bool IsInvincible => InvincibilityTimer > 0;

    public void TakeDamage(int damage)
    {
        if (IsInvincible) return;

        CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - damage);
        InvincibilityTimer = HitInvincibilityDuration;

        AsakiBroker.Publish(new HealthChangedEvent
        {
            EntityId = Entity.Id,
            CurrentHealth = CurrentHealth.Value,
            MaxHealth = MaxHealth.Value
        });

        if (CurrentHealth.Value <= 0)
        {
            AsakiBroker.Publish(new EntityDestroyedEvent { EntityId = Entity.Id });
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

/// <summary>
/// 敌人AI组件
/// </summary>
public class EnemyAIComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    // AI类型
    public EnemyAIType AIType { get; set; } = EnemyAIType.Chase;

    // 检测范围
    public float DetectionRange { get; set; } = 10f;
    public float AttackRange { get; set; } = 5f;
    public float StopDistance { get; set; } = 2f;

    // 移动模式
    public float WanderRadius { get; set; } = 3f;
    public float WanderTimer { get; set; }
    public Vector2 WanderTarget { get; set; }

    // 状态
    public AIState CurrentState { get; set; } = AIState.Idle;
    public IEntity TargetEntity { get; set; }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose() { }
}

public enum EnemyAIType
{
    Chase,      // 追击型
    Ranged,     // 远程型
    Turret,     // 炮台型（固定位置）
    Swarm       // 群体型
}

public enum AIState
{
    Idle,       // 待机
    Wander,     // 游荡
    Chase,      // 追击
    Attack,     // 攻击
    Retreat     // 撤退
}

/// <summary>
/// 弹丸组件
/// </summary>
public class ProjectileComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    public Vector2 Direction { get; set; }
    public float Speed { get; set; }
    public int Damage { get; set; }
    public float Lifetime { get; set; }
    public float CurrentLifetime { get; set; }

    public IEntity Owner { get; set; } // 发射者
    public bool Pierce { get; set; } // 是否穿透
    public int PierceCount { get; set; }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose() { }
}
```

#### 4.2.2 Camera（摄像机）

```csharp
/// <summary>
/// 俯视角摄像机组件
/// </summary>
public class TopDownCameraComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    // 跟随目标
    public IEntity TargetEntity { get; set; }

    // 基础参数
    public float SmoothTime { get; set; } = 0.3f;
    public Vector3 Offset { get; set; } = new Vector3(0, 0, -10);

    // 动态缩放
    public float BaseSize { get; set; } = 8f;
    public float MinSize { get; set; } = 5f;
    public float MaxSize { get; set; } = 15f;

    // 多目标支持（用于显示所有敌人）
    public List<IEntity> AdditionalTargets { get; set; } = new();
    public float TargetsPadding { get; set; } = 2f;

    private Vector3 _velocity;

    public Vector3 CalculatePosition()
    {
        if (TargetEntity == null) return Offset;

        var targetTransform = TargetEntity.GetComponent<TopDownTransformComponent>();
        if (targetTransform == null) return Offset;

        Vector3 center = targetTransform.Position.Value;

        // 如果有额外目标，计算包围盒中心
        if (AdditionalTargets.Count > 0)
        {
            Bounds bounds = new Bounds(center, Vector3.zero);
            bounds.Encapsulate(center);

            foreach (var target in AdditionalTargets)
            {
                if (target == null) continue;
                var transform = target.GetComponent<TopDownTransformComponent>();
                if (transform != null)
                {
                    bounds.Encapsulate(transform.Position.Value);
                }
            }

            center = bounds.center;
        }

        return center + Offset;
    }

    public float CalculateSize()
    {
        if (AdditionalTargets.Count == 0 || TargetEntity == null)
            return BaseSize;

        var targetTransform = TargetEntity.GetComponent<TopDownTransformComponent>();
        if (targetTransform == null) return BaseSize;

        float maxDistance = 0;
        Vector2 center = targetTransform.Position.Value;

        foreach (var target in AdditionalTargets)
        {
            if (target == null) continue;
            var transform = target.GetComponent<TopDownTransformComponent>();
            if (transform != null)
            {
                float distance = Vector2.Distance(center, transform.Position.Value);
                maxDistance = Mathf.Max(maxDistance, distance);
            }
        }

        float targetSize = Mathf.Clamp(maxDistance + TargetsPadding, MinSize, MaxSize);
        return targetSize;
    }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose() { }
}
```

#### 4.2.3 Controller（控制器）

```csharp
/// <summary>
/// 俯视角输入组件
/// </summary>
public class TopDownInputComponent : IEntityComponent
{
    public IEntity Entity { get; set; }

    // 移动输入
    public AsakiProperty<Vector2> MoveInput { get; } = new(Vector2.zero);

    // 瞄准输入（鼠标位置或右摇杆）
    public AsakiProperty<Vector2> AimInput { get; } = new(Vector2.zero);
    public bool UseMouseAim { get; set; } = true;
    public Vector2 MouseWorldPosition { get; set; }

    // 动作输入
    public AsakiProperty<bool> FirePressed { get; } = new(false);
    public AsakiProperty<bool> FireHeld { get; } = new(false);
    public AsakiProperty<bool> DashPressed { get; } = new(false);
    public AsakiProperty<bool> ReloadPressed { get; } = new(false);

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose()
    {
        MoveInput?.Dispose();
        AimInput?.Dispose();
        FirePressed?.Dispose();
        FireHeld?.Dispose();
        DashPressed?.Dispose();
        ReloadPressed?.Dispose();
    }
}

/// <summary>
/// 俯视角玩家控制系统
/// </summary>
public class TopDownPlayerSystem : IAsakiSystem, IAsakiTickable
{
    private EntityModel _entityModel;
    private Camera _mainCamera;

    public void Setup()
    {
        _mainCamera = Camera.main;
        _entityModel = AsakiContext.Get<EntityModel>();
    }

    public void Tick(float deltaTime)
    {
        // 更新所有玩家实体
        foreach (var entity in _entityModel.World.Query<PlayerTagComponent>())
        {
            UpdatePlayer(entity, deltaTime);
        }
    }

    private void UpdatePlayer(IEntity entity, float deltaTime)
    {
        var input = entity.GetComponent<TopDownInputComponent>();
        var movement = entity.GetComponent<TopDownMovementComponent>();
        var transform = entity.GetComponent<TopDownTransformComponent>();
        var shooter = entity.GetComponent<ShooterComponent>();
        var health = entity.GetComponent<HealthComponent>();

        if (input == null || movement == null || transform == null) return;

        // 更新无敌时间
        if (health != null && health.InvincibilityTimer > 0)
        {
            health.InvincibilityTimer -= deltaTime;
        }

        // 处理移动
        HandleMovement(input, movement, transform, deltaTime);

        // 处理瞄准
        HandleAim(input, transform);

        // 处理射击
        if (shooter != null)
        {
            HandleShooting(entity, input, shooter, transform, deltaTime);
        }

        // 处理冲刺
        HandleDash(input, movement, transform, deltaTime);
    }

    private void HandleMovement(
        TopDownInputComponent input,
        TopDownMovementComponent movement,
        TopDownTransformComponent transform,
        float deltaTime)
    {
        if (movement.IsDashing)
        {
            // 冲刺中保持当前方向
            movement.DashTimer -= deltaTime;
            if (movement.DashTimer <= 0)
            {
                movement.IsDashing = false;
                movement.DashCooldownTimer = movement.DashCooldown;
            }

            // 应用冲刺速度
            Vector2 dashVelocity = transform.Forward * movement.DashSpeed;
            transform.Position.Value += dashVelocity * deltaTime;
            return;
        }

        // 更新冲刺冷却
        if (movement.DashCooldownTimer > 0)
        {
            movement.DashCooldownTimer -= deltaTime;
        }

        // 计算目标速度
        Vector2 targetVelocity = input.MoveInput.Value * movement.MoveSpeed;

        // 平滑加速/减速
        float accel = input.MoveInput.Value.sqrMagnitude > 0.01f
            ? movement.Acceleration
            : movement.Deceleration;

        movement.CurrentVelocity = Vector2.MoveTowards(
            movement.CurrentVelocity,
            targetVelocity,
            accel * deltaTime
        );

        // 应用移动
        transform.Position.Value += movement.CurrentVelocity * deltaTime;
        movement.IsMoving = movement.CurrentVelocity.sqrMagnitude > 0.01f;
    }

    private void HandleAim(TopDownInputComponent input, TopDownTransformComponent transform)
    {
        if (input.UseMouseAim)
        {
            // 鼠标瞄准
            Vector2 aimDirection = input.MouseWorldPosition - transform.Position.Value;
            if (aimDirection.sqrMagnitude > 0.001f)
            {
                transform.Rotation.Value = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            }
        }
        else
        {
            // 摇杆瞄准
            if (input.AimInput.Value.sqrMagnitude > 0.1f)
            {
                transform.Rotation.Value = Mathf.Atan2(
                    input.AimInput.Value.y,
                    input.AimInput.Value.x
                ) * Mathf.Rad2Deg;
            }
        }
    }

    private void HandleShooting(
        IEntity entity,
        TopDownInputComponent input,
        ShooterComponent shooter,
        TopDownTransformComponent transform,
        float deltaTime)
    {
        // 更新冷却
        if (shooter.FireCooldown > 0)
        {
            shooter.FireCooldown -= deltaTime;
        }

        // 处理换弹
        if (shooter.IsReloading)
        {
            shooter.ReloadProgress += deltaTime;
            if (shooter.ReloadProgress >= shooter.ReloadTime)
            {
                shooter.Ammo = shooter.MaxAmmo;
                shooter.IsReloading = false;
                shooter.ReloadProgress = 0;
            }
            return;
        }

        // 开始换弹
        if (input.ReloadPressed.Value && shooter.Ammo < shooter.MaxAmmo)
        {
            shooter.IsReloading = true;
            shooter.ReloadProgress = 0;
            return;
        }

        // 自动换弹
        if (shooter.Ammo <= 0 && !shooter.IsReloading)
        {
            shooter.IsReloading = true;
            shooter.ReloadProgress = 0;
            return;
        }

        // 射击
        if (input.FireHeld.Value && shooter.CanFire)
        {
            FireProjectile(entity, shooter, transform);
            shooter.FireCooldown = shooter.FireRate;
            shooter.Ammo--;
        }
    }

    private void FireProjectile(IEntity owner, ShooterComponent shooter, TopDownTransformComponent transform)
    {
        // 计算枪口位置
        Vector2 muzzlePosition = transform.Position.Value +
            (Vector2)(Quaternion.Euler(0, 0, transform.Rotation.Value) * shooter.MuzzleOffset);

        // 发射多个弹丸（散射）
        for (int i = 0; i < shooter.ProjectileCount; i++)
        {
            // 计算散射角度
            float spread = Random.Range(-shooter.SpreadAngle, shooter.SpreadAngle);
            float angle = transform.Rotation.Value + spread;
            Vector2 direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            // 创建弹丸实体
            AsakiBroker.Publish(new SpawnProjectileEvent
            {
                Position = muzzlePosition,
                Direction = direction,
                Speed = shooter.ProjectileSpeed,
                Damage = shooter.ProjectileDamage,
                Owner = owner
            });
        }
    }

    private void HandleDash(
        TopDownInputComponent input,
        TopDownMovementComponent movement,
        TopDownTransformComponent transform,
        float deltaTime)
    {
        if (input.DashPressed.Value &&
            movement.DashCooldownTimer <= 0 &&
            !movement.IsDashing &&
            movement.IsMoving)
        {
            movement.IsDashing = true;
            movement.DashTimer = movement.DashDuration;

            // 冲刺方向为当前移动方向
            if (movement.CurrentVelocity.sqrMagnitude > 0.001f)
            {
                Vector2 dashDir = movement.CurrentVelocity.normalized;
                transform.Rotation.Value = Mathf.Atan2(dashDir.y, dashDir.x) * Mathf.Rad2Deg;
            }

            AsakiBroker.Publish(new PlayerDashEvent { EntityId = movement.Entity.Id });
        }
    }

    public void Dispose() { }
}

/// <summary>
/// 敌人AI系统
/// </summary>
public class EnemyAISystem : IAsakiSystem, IAsakiTickable
{
    private EntityModel _entityModel;
    private IEntity _playerEntity;

    public void Setup()
    {
        _entityModel = AsakiContext.Get<EntityModel>();

        // 缓存玩家实体
        foreach (var entity in _entityModel.World.Query<PlayerTagComponent>())
        {
            _playerEntity = entity;
            break;
        }
    }

    public void Tick(float deltaTime)
    {
        if (_playerEntity == null) return;

        var playerTransform = _playerEntity.GetComponent<TopDownTransformComponent>();
        if (playerTransform == null) return;

        // 更新所有敌人
        foreach (var entity in _entityModel.World.Query<EnemyAIComponent>())
        {
            UpdateEnemyAI(entity, playerTransform.Position.Value, deltaTime);
        }
    }

    private void UpdateEnemyAI(IEntity entity, Vector2 playerPosition, float deltaTime)
    {
        var ai = entity.GetComponent<EnemyAIComponent>();
        var transform = entity.GetComponent<TopDownTransformComponent>();
        var movement = entity.GetComponent<TopDownMovementComponent>();
        var shooter = entity.GetComponent<ShooterComponent>();

        if (ai == null || transform == null || movement == null) return;

        Vector2 toPlayer = playerPosition - transform.Position.Value;
        float distanceToPlayer = toPlayer.magnitude;

        // 更新目标
        ai.TargetEntity = _playerEntity;

        // 状态机
        switch (ai.AIType)
        {
            case EnemyAIType.Chase:
                UpdateChaseAI(ai, transform, movement, toPlayer, distanceToPlayer, deltaTime);
                break;

            case EnemyAIType.Ranged:
                UpdateRangedAI(ai, transform, movement, shooter, toPlayer, distanceToPlayer, deltaTime);
                break;

            case EnemyAIType.Turret:
                UpdateTurretAI(ai, transform, shooter, toPlayer, distanceToPlayer, deltaTime);
                break;

            case EnemyAIType.Swarm:
                UpdateSwarmAI(ai, transform, movement, toPlayer, distanceToPlayer, deltaTime);
                break;
        }
    }

    private void UpdateChaseAI(
        EnemyAIComponent ai,
        TopDownTransformComponent transform,
        TopDownMovementComponent movement,
        Vector2 toPlayer,
        float distance,
        float deltaTime)
    {
        // 看向玩家
        transform.LookAt(transform.Position.Value + toPlayer);

        // 追击
        if (distance > ai.StopDistance)
        {
            Vector2 direction = toPlayer.normalized;
            movement.CurrentVelocity = direction * movement.MoveSpeed;
            transform.Position.Value += movement.CurrentVelocity * deltaTime;
        }
        else
        {
            movement.CurrentVelocity = Vector2.zero;
        }
    }

    private void UpdateRangedAI(
        EnemyAIComponent ai,
        TopDownTransformComponent transform,
        TopDownMovementComponent movement,
        ShooterComponent shooter,
        Vector2 toPlayer,
        float distance,
        float deltaTime)
    {
        // 看向玩家
        transform.LookAt(transform.Position.Value + toPlayer);

        // 保持攻击距离
        if (distance > ai.AttackRange)
        {
            // 靠近
            Vector2 direction = toPlayer.normalized;
            movement.CurrentVelocity = direction * movement.MoveSpeed;
            transform.Position.Value += movement.CurrentVelocity * deltaTime;
        }
        else if (distance < ai.StopDistance)
        {
            // 后退
            Vector2 direction = -toPlayer.normalized;
            movement.CurrentVelocity = direction * movement.MoveSpeed * 0.5f;
            transform.Position.Value += movement.CurrentVelocity * deltaTime;
        }
        else
        {
            movement.CurrentVelocity = Vector2.zero;

            // 射击
            if (shooter != null && shooter.CanFire)
            {
                FireProjectile(entity, shooter, transform);
                shooter.FireCooldown = shooter.FireRate;
            }
        }

        if (shooter != null && shooter.FireCooldown > 0)
        {
            shooter.FireCooldown -= deltaTime;
        }
    }

    private void UpdateTurretAI(
        EnemyAIComponent ai,
        TopDownTransformComponent transform,
        ShooterComponent shooter,
        Vector2 toPlayer,
        float distance,
        float deltaTime)
    {
        if (distance > ai.DetectionRange) return;

        // 看向玩家
        transform.LookAt(transform.Position.Value + toPlayer);

        // 射击
        if (shooter != null && shooter.CanFire && distance <= ai.AttackRange)
        {
            FireProjectile(entity, shooter, transform);
            shooter.FireCooldown = shooter.FireRate;
        }

        if (shooter != null && shooter.FireCooldown > 0)
        {
            shooter.FireCooldown -= deltaTime;
        }
    }

    private void UpdateSwarmAI(
        EnemyAIComponent ai,
        TopDownTransformComponent transform,
        TopDownMovementComponent movement,
        Vector2 toPlayer,
        float distance,
        float deltaTime)
    {
        // 简单的群体行为：向玩家移动，但保持一定的分离
        Vector2 separation = CalculateSeparation(entity, transform);
        Vector2 alignment = CalculateAlignment(entity);
        Vector2 cohesion = toPlayer.normalized;

        Vector2 finalDirection = (separation * 1.5f + alignment * 0.5f + cohesion * 1f).normalized;

        transform.LookAt(transform.Position.Value + finalDirection);
        movement.CurrentVelocity = finalDirection * movement.MoveSpeed;
        transform.Position.Value += movement.CurrentVelocity * deltaTime;
    }

    private Vector2 CalculateSeparation(IEntity entity, TopDownTransformComponent transform)
    {
        Vector2 separation = Vector2.zero;
        int count = 0;
        float separationRadius = 1.5f;

        foreach (var other in _entityModel.World.Query<EnemyAIComponent>())
        {
            if (other.Id == entity.Id) continue;

            var otherTransform = other.GetComponent<TopDownTransformComponent>();
            if (otherTransform == null) continue;

            Vector2 diff = transform.Position.Value - otherTransform.Position.Value;
            float dist = diff.magnitude;

            if (dist < separationRadius && dist > 0)
            {
                separation += diff.normalized / dist;
                count++;
            }
        }

        return count > 0 ? (separation / count).normalized : Vector2.zero;
    }

    private Vector2 CalculateAlignment(IEntity entity)
    {
        Vector2 avgVelocity = Vector2.zero;
        int count = 0;

        foreach (var other in _entityModel.World.Query<EnemyAIComponent>())
        {
            if (other.Id == entity.Id) continue;

            var otherMovement = other.GetComponent<TopDownMovementComponent>();
            if (otherMovement == null) continue;

            avgVelocity += otherMovement.CurrentVelocity;
            count++;
        }

        return count > 0 ? (avgVelocity / count).normalized : Vector2.zero;
    }

    private void FireProjectile(IEntity owner, ShooterComponent shooter, TopDownTransformComponent transform)
    {
        Vector2 muzzlePosition = transform.Position.Value +
            (Vector2)(Quaternion.Euler(0, 0, transform.Rotation.Value) * shooter.MuzzleOffset);

        AsakiBroker.Publish(new SpawnProjectileEvent
        {
            Position = muzzlePosition,
            Direction = transform.Forward,
            Speed = shooter.ProjectileSpeed,
            Damage = shooter.ProjectileDamage,
            Owner = owner
        });
    }

    public void Dispose() { }
}

/// <summary>
/// 弹丸管理系统
/// </summary>
public class ProjectileSystem : IAsakiSystem, IAsakiTickable
{
    private EntityModel _entityModel;

    public void Setup()
    {
        _entityModel = AsakiContext.Get<EntityModel>();

        // 订阅弹丸生成事件
        AsakiBroker.Subscribe<SpawnProjectileEvent>(OnSpawnProjectile);
    }

    private void OnSpawnProjectile(SpawnProjectileEvent e)
    {
        CreateProjectile(e);
    }

    private void CreateProjectile(SpawnProjectileEvent e)
    {
        var world = _entityModel.World;
        var entity = world.CreateEntity();

        // 添加弹丸组件
        var projectile = entity.AddComponent<ProjectileComponent>();
        projectile.Direction = e.Direction;
        projectile.Speed = e.Speed;
        projectile.Damage = e.Damage;
        projectile.Owner = e.Owner;
        projectile.Lifetime = 3f;

        // 添加变换组件
        var transform = entity.AddComponent<TopDownTransformComponent>();
        transform.Position.Value = e.Position;
        transform.Rotation.Value = Mathf.Atan2(e.Direction.y, e.Direction.x) * Mathf.Rad2Deg;

        // 添加碰撞组件（用于检测）
        // 这里可以添加ColliderComponent等
    }

    public void Tick(float deltaTime)
    {
        // 更新所有弹丸
        var projectiles = _entityModel.World.Query<ProjectileComponent>().ToList();

        foreach (var entity in projectiles)
        {
            var projectile = entity.GetComponent<ProjectileComponent>();
            var transform = entity.GetComponent<TopDownTransformComponent>();

            if (projectile == null || transform == null) continue;

            // 更新生命周期
            projectile.CurrentLifetime += deltaTime;
            if (projectile.CurrentLifetime >= projectile.Lifetime)
            {
                DestroyProjectile(entity);
                continue;
            }

            // 移动弹丸
            Vector2 newPosition = transform.Position.Value + projectile.Direction * projectile.Speed * deltaTime;
            transform.Position.Value = newPosition;

            // 碰撞检测（简化版）
            CheckProjectileCollision(entity, projectile, newPosition);
        }
    }

    private void CheckProjectileCollision(IEntity projectileEntity, ProjectileComponent projectile, Vector2 position)
    {
        // 检测与敌人的碰撞
        foreach (var entity in _entityModel.World.Query<HealthComponent>())
        {
            // 跳过发射者
            if (entity.Id == projectile.Owner?.Id) continue;

            var transform = entity.GetComponent<TopDownTransformComponent>();
            if (transform == null) continue;

            float distance = Vector2.Distance(position, transform.Position.Value);
            if (distance < 0.5f) // 碰撞半径
            {
                // 造成伤害
                var health = entity.GetComponent<HealthComponent>();
                health?.TakeDamage(projectile.Damage);

                // 处理弹丸
                if (!projectile.Pierce)
                {
                    DestroyProjectile(projectileEntity);
                    return;
                }
                else
                {
                    projectile.PierceCount--;
                    if (projectile.PierceCount <= 0)
                    {
                        DestroyProjectile(projectileEntity);
                        return;
                    }
                }
            }
        }
    }

    private void DestroyProjectile(IEntity entity)
    {
        var world = _entityModel.World;
        world.DestroyEntity(entity.Id);
    }

    public void Dispose()
    {
        AsakiBroker.Unsubscribe<SpawnProjectileEvent>(OnSpawnProjectile);
    }
}

// 事件定义
public struct SpawnProjectileEvent : IAsakiEvent
{
    public Vector2 Position;
    public Vector2 Direction;
    public float Speed;
    public int Damage;
    public IEntity Owner;
}

public struct HealthChangedEvent : IAsakiEvent
{
    public int EntityId;
    public int CurrentHealth;
    public int MaxHealth;
}

public struct EntityDestroyedEvent : IAsakiEvent
{
    public int EntityId;
}

public struct PlayerDashEvent : IAsakiEvent
{
    public int EntityId;
}
```

### 4.3 系统架构图

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                  2D俯视角射击游戏 - 实体系统架构                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                          Entity World                               │   │
│  │                                                                     │   │
│  │  ┌─────────────────────────────────────────────────────────────┐   │   │
│  │  │                    Player Entity                             │   │   │
│  │  │  - PlayerTagComponent                                        │   │   │
│  │  │  - TopDownTransformComponent                                 │   │   │
│  │  │  - TopDownMovementComponent (移动/冲刺)                       │   │   │
│  │  │  - ShooterComponent (武器系统)                               │   │   │
│  │  │  - HealthComponent                                           │   │   │
│  │  │  - TopDownInputComponent                                     │   │   │
│  │  └─────────────────────────────────────────────────────────────┘   │   │
│  │                                                                     │   │
│  │  ┌─────────────────────────────────────────────────────────────┐   │   │
│  │  │                    Enemy Entities (大量)                      │   │   │
│  │  │  - EnemyAIComponent (AI行为: 追击/远程/炮台/群体)              │   │   │
│  │  │  - TopDownTransformComponent                                 │   │   │
│  │  │  - TopDownMovementComponent                                  │   │   │
│  │  │  - ShooterComponent                                          │   │   │
│  │  │  - HealthComponent                                           │   │   │
│  │  └─────────────────────────────────────────────────────────────┘   │   │
│  │                                                                     │   │
│  │  ┌─────────────────────────────────────────────────────────────┐   │   │
│  │  │                    Projectile Entities (对象池)               │   │   │
│  │  │  - ProjectileComponent (弹丸数据)                            │   │   │
│  │  │  - TopDownTransformComponent                                 │   │   │
│  │  └─────────────────────────────────────────────────────────────┘   │   │
│  │                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                        │
│                                    ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                          System 层                                   │   │
│  │  ┌─────────────────┐ ┌─────────────────┐ ┌───────────────────────┐ │   │
│  │  │ TopDownPlayer   │ │ EnemyAI         │ │ Projectile            │ │   │
│  │  │ System          │ │ System          │ │ System                │ │   │
│  │  │ (玩家控制)       │ │ (敌人AI)         │ │ (弹丸管理)            │ │   │
│  │  └─────────────────┘ └─────────────────┘ └───────────────────────┘ │   │
│  │  ┌─────────────────┐ ┌─────────────────┐                           │   │
│  │  │ TopDownCamera   │ │ Wave            │                           │   │
│  │  │ System          │ │ System          │                           │   │
│  │  │ (摄像机控制)     │ │ (波次管理)       │                           │   │
│  │  └─────────────────┘ └─────────────────┘                           │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                        对象池优化                                    │   │
│  │  - 弹丸对象池 (Projectile Pool)                                     │   │
│  │  - 敌人对象池 (Enemy Pool)                                          │   │
│  │  - 特效对象池 (Effect Pool)                                         │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 五、总结与建议

### 5.1 三个使用案例对比

| 特性 | 2D平台跳跃 | 3D动作冒险 | 2D俯视角射击 |
|-----|-----------|-----------|-------------|
| **核心玩法** | 平台跳跃、精确操作 | 近战格斗、连招系统 | 射击、大量敌人 |
| **实体数量** | 中等（10-50） | 中等（20-100） | 大量（100-1000+） |
| **摄像机复杂度** | 中（平滑跟随） | 高（锁定/自由切换） | 低（固定俯视角） |
| **输入复杂度** | 中（移动+跳跃） | 高（多动作组合） | 中（移动+瞄准+射击） |
| **关键组件** | Physics2D, JumpAbility | StateMachine, Weapon, LockOn | Shooter, AI, Projectile |
| **性能关注点** | 物理同步 | 动画状态机 | 对象池、批量处理 |

### 5.2 实体系统实施建议

#### 5.2.1 分阶段实施路线图

```
第一阶段（基础框架）
├── 核心接口实现
│   ├── IEntity / Entity
│   ├── IEntityComponent
│   └── IEntityWorld / EntityWorld
├── 基础组件
│   ├── Transform2DComponent
│   └── Transform3DComponent
└── Architecture集成
    └── EntityModel

第二阶段（查询系统）
├── 查询API实现
│   ├── Query<T>()
│   └── Query<T1, T2>()
├── 缓存优化
└── Unity桥接
    └── EntityBridge

第三阶段（高级功能）
├── 编辑器工具
│   ├── 实体调试窗口
│   └── 组件可视化
└── 性能优化
    └── 对象池集成
```

#### 5.2.2 最佳实践建议

1. **组件设计原则**
   - 保持组件单一职责
   - 使用ReactiveProperty实现数据绑定
   - 避免在组件中包含复杂逻辑

2. **系统更新优化**
   - 按组件类型批量查询
   - 使用缓存避免重复查询
   - 考虑使用空间分区优化大量实体

3. **与Unity集成**
   - 使用EntityBridge同步生命周期
   - 物理更新与Unity物理系统同步
   - 动画组件与Animator解耦

4. **性能考虑**
   - 对频繁创建的实体使用对象池
   - 避免在Tick中进行LINQ查询
   - 使用位掩码加速组件查询

### 5.3 最终结论

基于对可行性报告的审核和三个详细使用案例的设计，得出以下结论：

1. **技术可行性**：✅ **强烈推荐实施**
   - 与现有CQRS架构高度兼容
   - 轻量级EC模式符合项目定位
   - 实施风险可控，资源需求合理

2. **预期收益**：
   - 统一的游戏对象抽象
   - 灵活的组件组合能力
   - 与现有架构形成互补
   - 支持多种游戏类型

3. **实施优先级**：
   - **高优先级**：核心接口、基础组件、Architecture集成
   - **中优先级**：查询系统、Unity桥接
   - **低优先级**：编辑器工具、高级优化

---

*文档生成时间：2026-02-03*
*版本：v1.0*
*基于：Asaki Framework 实体系统可行性分析报告 v1.0*
