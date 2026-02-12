# Asaki ComboSystem 使用指南与示例

本文档包含三个部分：使用场景推荐、完整 API 参考和详细使用示例。

---

## 第一部分：使用场景推荐

### 1.1 适用游戏类型

#### 动作游戏 (Action Game)
**推荐度：★★★★★**

- **典型应用**：鬼泣、猎天使魔女、战神等风格的连击系统
- **核心特性**：
  - 多段连击（3-5段基础连击）
  - 空中连击（Juggle 系统）
  - 武器切换连招
  - 闪避取消连招
- **配置建议**：
  - 输入缓冲：0.2-0.3秒
  - 连招窗口：0.3-0.5秒
  - 重置策略：受击时重置

#### 格斗游戏 (Fighting Game)
**推荐度：★★★★★**

- **典型应用**：街霸、拳皇、罪恶装备等 2D/3D 格斗
- **核心特性**：
  - 复杂输入指令（236A、623B 等）
  - 取消链（Cancel Chain）
  - 必杀技派生
  - 防御反击
- **配置建议**：
  - 输入缓冲：0.1-0.15秒（更严格）
  - 连招窗口：根据招式帧数精确配置
  - 重置策略：根据游戏设计选择保持或重置

#### 魂系游戏 (Souls-like)
**推荐度：★★★★☆**

- **典型应用**：黑暗之魂、艾尔登法环、只狼
- **核心特性**：
  - 轻重攻击组合
  - 蓄力攻击
  - 战技派生
  - 精力管理（与耐力系统结合）
- **配置建议**：
  - 输入缓冲：0.15-0.2秒
  - 连招窗口：较短（0.2-0.3秒）
  - 重置策略：受击或精力耗尽时重置

#### RPG 游戏
**推荐度：★★★★☆**

- **典型应用**：原神、崩坏：星穹铁道、最终幻想
- **核心特性**：
  - 技能连携
  - 元素反应触发
  - 角色切换连招
  - QTE 连击
- **配置建议**：
  - 输入缓冲：0.3-0.5秒（更宽松）
  - 连招窗口：0.5-1秒
  - 重置策略：角色切换时保持计数

#### 平台动作游戏 (Platformer)
**推荐度：★★★☆☆**

- **典型应用**：空洞骑士、奥日、死亡细胞
- **核心特性**：
  - 空中攻击连招
  - 下砸派生
  - 冲刺攻击
- **配置建议**：
  - 输入缓冲：0.2秒
  - 连招窗口：0.3秒
  - 重置策略：落地或受击时重置

### 1.2 使用模式推荐

#### 模式 A：标准连招系统
适合大多数动作游戏，提供流畅的连击体验。

```
轻攻击1 → 轻攻击2 → 轻攻击3 → 终结技
   ↑
重攻击（分支）
```

**配置参数**：
- 基础连击数：3-5段
- 每段伤害递增：1.0x → 1.2x → 1.5x → 2.0x
- 输入缓冲：0.25秒
- 连招窗口：0.4秒

#### 模式 B：派生连招系统
适合需要复杂操作的高级玩家。

```
轻攻击1 ─┬→ 轻攻击2 ─┬→ 轻攻击3
         │           └→ 重攻击（派生）
         └→ 重攻击（派生）
```

**配置参数**：
- 多分支路径：每段2-3个选择
- 输入缓冲：0.2秒
- 连招窗口：0.3秒
- 需要精确的输入时机

#### 模式 C：累积连招系统
适合格斗游戏或需要连击评分的系统。

```
连招进行中 → 连击数累积 → 伤害加成/评分提升
     ↓
 受击/超时 → 应用重置策略 → 保留部分计数
```

**配置参数**：
- 重置策略：Decay（递减）
- 递减量：2-3
- 最小保留：0
- 最大连击限制：99

#### 模式 D：武器切换连招
适合多武器系统的游戏。

```
剑 轻攻击 → 枪 射击 → 斧 重击 → 剑 终结技
```

**配置参数**：
- 每种武器独立连招树
- 武器切换保持连击数
- 切换后连招窗口延长

### 1.3 扩展应用

#### 与技能系统结合
```csharp
// 技能触发连招
public class SkillComboIntegration : MonoBehaviour
{
    [SerializeField] private AsakiComboController combo;
    [SerializeField] private SkillSystem skills;

    void Update()
    {
        // 技能可以触发连招
        if (skills.IsSkillReady("FireSlash"))
        {
            combo.TriggerAttack("FireSlash_Skill");
        }
    }
}
```

#### 与 QTE 系统结合
```csharp
// QTE 成功继续连招
public class QTECombo : MonoBehaviour
{
    [SerializeField] private AsakiComboController combo;
    [SerializeField] private QTESystem qte;

    void OnEnable()
    {
        combo.OnComboWindowOpened += OnWindowOpen;
    }

    void OnWindowOpen(float duration)
    {
        // 在连招窗口显示 QTE
        qte.StartQTE(duration, OnQTESuccess, OnQTEFail);
    }

    void OnQTESuccess()
    {
        combo.TriggerAttack("QTE_Finisher");
    }

    void OnQTEFail()
    {
        combo.ResetCombo();
    }
}
```

---

## 第二部分：API 参考

### 2.1 AsakiComboController

核心控制器类，管理整个连招系统。

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `CurrentComboCount` | `int` | 当前连击数 |
| `CurrentMove` | `ComboMove` | 当前招式 |
| `ComboTimer` | `float` | 连招计时器（从第一招开始计时） |
| `CurrentStateType` | `ComboStateType` | 当前状态类型 |
| `InputBuffer` | `InputBuffer` | 输入缓冲（内部使用） |
| `StateMachine` | `AsakiStateMachine<AsakiComboController>` | 状态机（内部使用） |

#### 方法

##### Initialize
```csharp
public void Initialize(ComboTree tree)
```
初始化连招树（运行时设置）。

**参数**：
- `tree` - 连招树数据资产

**示例**：
```csharp
comboController.Initialize(comboTree);
```

##### TriggerAttack
```csharp
public void TriggerAttack(string inputTypeId)
```
触发攻击指令。

**参数**：
- `inputTypeId` - 输入类型ID（如 "LightAttack", "HeavyAttack"）

**示例**：
```csharp
if (Input.GetButtonDown("Fire1"))
    comboController.TriggerAttack("LightAttack");
```

##### InterruptCombo
```csharp
public void InterruptCombo(InterruptReason reason)
```
中断当前连招。

**参数**：
- `reason` - 中断原因

**示例**：
```csharp
public void OnTakeDamage()
{
    comboController.InterruptCombo(InterruptReason.Damaged);
}
```

##### ResetCombo
```csharp
public void ResetCombo()
```
重置连招状态。

**示例**：
```csharp
// 角色死亡时重置
void OnDeath()
{
    comboController.ResetCombo();
}
```

##### CanAcceptInput
```csharp
public bool CanAcceptInput()
```
检查是否可以接受输入。

**返回**：`true` 如果在 Idle 或 ComboWindow 状态

**示例**：
```csharp
void Update()
{
    if (!comboController.CanAcceptInput()) return;

    if (Input.GetButtonDown("Fire1"))
        comboController.TriggerAttack("LightAttack");
}
```

##### SetComboCount
```csharp
public void SetComboCount(int count)
```
设置连击数（用于自定义重置策略）。

**参数**：
- `count` - 新的连击数

**示例**：
```csharp
// 应用重置策略后设置新计数
int newCount = comboTree.ApplyResetStrategy("on_damaged",
    comboController.CurrentComboCount, context);
comboController.SetComboCount(newCount);
```

#### 事件

##### OnComboStarted
```csharp
public event Action OnComboStarted;
```
连招开始时触发（第一招开始）。

##### OnComboInterrupted
```csharp
public event Action<InterruptReason> OnComboInterrupted;
```
连招中断时触发。

##### OnComboCompleted
```csharp
public event Action OnComboCompleted;
```
连招完成时触发（自然结束）。

##### OnMoveStarted
```csharp
public event Action<ComboMove> OnMoveStarted;
```
招式开始时触发。

##### OnHitBoxesActivated
```csharp
public event Action<HitBoxInfo[]> OnHitBoxesActivated;
```
判定框激活时触发。

##### OnHitBoxesDeactivated
```csharp
public event Action OnHitBoxesDeactivated;
```
判定框禁用时触发。

##### OnMoveCompleted
```csharp
public event Action<ComboMove> OnMoveCompleted;
```
招式完成时触发。

##### OnComboWindowOpened
```csharp
public event Action<float> OnComboWindowOpened;
```
连招窗口开启时触发。

**参数**：窗口持续时间

##### OnComboWindowClosed
```csharp
public event Action OnComboWindowClosed;
```
连招窗口关闭时触发。

##### OnStateChanged
```csharp
public event Action<ComboStateType, ComboStateType> OnStateChanged;
```
状态变化时触发。

**参数**：(fromState, toState)

### 2.2 ComboTree

连招树数据资产。

#### 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `TreeId` | `string` | 连招树ID |
| `Description` | `string` | 描述 |
| `Moves` | `ComboMove[]` | 所有招式 |
| `Transitions` | `ComboTransition[]` | 转换关系 |
| `InputBufferWindow` | `float` | 输入缓冲窗口（秒） |
| `MaxComboDuration` | `float` | 最大连招持续时间（秒） |
| `MaxComboLength` | `int` | 最大连招长度 |
| `ResetStrategies` | `ResetStrategyDefinition[]` | 重置策略定义 |
| `DefaultResetMode` | `ResetComboMode` | 默认重置模式 |

#### 方法

##### GetMove
```csharp
public ComboMove GetMove(string moveId)
```
根据ID获取招式。

##### GetTransitions
```csharp
public List<ComboTransition> GetTransitions(string fromMoveId)
```
获取从指定招式出发的所有转换。

##### FindNextMove
```csharp
public ComboMove FindNextMove(string currentMoveId, string inputTypeId)
```
根据输入类型查找下一个招式。

##### ApplyResetStrategy
```csharp
public int ApplyResetStrategy(string groupName, int currentCount, ComboContext context)
```
应用重置策略。

### 2.3 ComboMove

招式数据定义。

#### 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `MoveId` | `string` | 招式唯一ID |
| `MoveName` | `string` | 招式名称 |
| `AnimationStateName` | `string` | 动画状态名 |
| `AnimationSpeed` | `float` | 动画播放速度 |
| `StartupTime` | `float` | 前摇时间（秒） |
| `ActiveDuration` | `float` | 判定持续时间（秒） |
| `RecoveryTime` | `float` | 后摇时间（秒） |
| `ComboWindowStart` | `float` | 连招窗口开始时间（秒） |
| `ComboWindowEnd` | `float` | 连招窗口结束时间（秒） |
| `HitBoxes` | `HitBoxDefinition[]` | 判定框定义 |
| `MinComboCount` | `int` | 最小连击数要求 |
| `MaxComboCount` | `int` | 最大连击数限制 |
| `Cooldown` | `float` | 冷却时间（秒） |

#### 方法

##### IsOnCooldown
```csharp
public bool IsOnCooldown(float currentTime)
```
检查是否在冷却中。

### 2.4 ComboTransition

连招转换定义。

#### 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `FromMoveId` | `string` | 起始招式ID |
| `ToMoveId` | `string` | 目标招式ID |
| `InputType` | `string` | 输入类型ID |
| `Conditions` | `TransitionCondition[]` | 转换条件 |
| `ResetGroup` | `string` | 重置策略组名 |

### 2.5 HitBoxDefinition

判定框定义。

#### 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `HitBoxId` | `string` | 判定框ID |
| `Shape` | `HitBoxShape` | 形状（Box/Sphere/Capsule） |
| `Offset` | `Vector3` | 偏移位置 |
| `Size` | `Vector3` | 尺寸（Box用） |
| `Radius` | `float` | 半径（Sphere/Capsule用） |
| `Height` | `float` | 高度（Capsule用） |
| `BoneName` | `string` | 跟随骨骼名称 |

### 2.6 HitBoxInfo

判定框信息（传递给外部系统）。

#### 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `HitBoxId` | `string` | 判定框ID |
| `Collider` | `Collider` | 激活的碰撞器 |
| `Owner` | `GameObject` | 攻击者 |
| `MoveData` | `ComboMove` | 招式数据 |

### 2.7 ComboContext

连招上下文（用于重置策略）。

#### 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `Controller` | `AsakiComboController` | 控制器 |
| `CurrentMove` | `ComboMove` | 当前招式 |
| `PreviousMove` | `ComboMove` | 上一招式 |
| `ComboCount` | `int` | 连击数 |
| `ComboTimer` | `float` | 连招计时器 |
| `InterruptReason` | `InterruptReason?` | 中断原因（可为null） |
| `Blackboard` | `Dictionary<string, object>` | 自定义数据 |

#### 方法

##### GetData
```csharp
public T GetData<T>(string key)
```
获取自定义数据。

##### SetData
```csharp
public void SetData<T>(string key, T value)
```
设置自定义数据。

### 2.8 IComboResetStrategy

重置策略接口。

```csharp
public interface IComboResetStrategy
{
    int CalculateResetCount(int currentCount, ComboContext context);
    bool ShouldReset(ComboContext context);
}
```

### 2.9 枚举

#### ComboStateType
```csharp
public enum ComboStateType
{
    Idle,           // 待机
    Startup,        // 前摇
    Active,         // 判定中
    Recovery,       // 后摇
    ComboWindow,    // 连招窗口
    Interrupted     // 中断
}
```

#### InterruptReason
```csharp
public enum InterruptReason
{
    Damaged,        // 受到伤害
    Stunned,        // 被眩晕
    KnockedDown,    // 被击倒
    Forced,         // 强制中断
    UserCancel      // 用户取消
}
```

#### HitBoxShape
```csharp
public enum HitBoxShape
{
    Box,            // 盒子
    Sphere,         // 球体
    Capsule         // 胶囊体
}
```

#### ResetComboMode
```csharp
public enum ResetComboMode
{
    ResetToZero,        // 重置为0
    KeepCount,          // 保持当前计数
    Decay,              // 固定值递减
    PercentageDecay,    // 百分比递减
    SetToSpecific,      // 设置为特定值
    CustomFunction      // 自定义函数
}
```

---

## 第三部分：使用示例

### 3.1 基础示例：玩家战斗系统

```csharp
using UnityEngine;
using Asaki.Plungin.ComboSystem;

public class PlayerCombat : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private AsakiComboController comboController;
    [SerializeField] private ComboTree comboTree;
    [SerializeField] private Animator animator;

    [Header("Settings")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float hitStopDuration = 0.1f;

    // 运行时数据
    private int _totalDamage;
    private int _hitCount;

    void Start()
    {
        InitializeComboSystem();
    }

    void InitializeComboSystem()
    {
        if (comboTree != null)
        {
            comboController.Initialize(comboTree);
        }

        // 订阅事件
        comboController.OnComboStarted += OnComboStarted;
        comboController.OnComboInterrupted += OnComboInterrupted;
        comboController.OnComboCompleted += OnComboCompleted;
        comboController.OnMoveStarted += OnMoveStarted;
        comboController.OnHitBoxesActivated += OnHitBoxesActivated;
        comboController.OnHitBoxesDeactivated += OnHitBoxesDeactivated;
        comboController.OnMoveCompleted += OnMoveCompleted;
        comboController.OnStateChanged += OnStateChanged;
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        // 轻攻击
        if (Input.GetButtonDown("Fire1"))
        {
            comboController.TriggerAttack("LightAttack");
        }

        // 重攻击
        if (Input.GetButtonDown("Fire2"))
        {
            comboController.TriggerAttack("HeavyAttack");
        }

        // 技能1
        if (Input.GetKeyDown(KeyCode.Q))
        {
            comboController.TriggerAttack("Skill1");
        }

        // 技能2
        if (Input.GetKeyDown(KeyCode.E))
        {
            comboController.TriggerAttack("Skill2");
        }
    }

    #region Combo Events

    void OnComboStarted()
    {
        _totalDamage = 0;
        _hitCount = 0;
        Debug.Log("[Combat] 连招开始");
    }

    void OnComboInterrupted(InterruptReason reason)
    {
        Debug.Log($"[Combat] 连招中断: {reason}");

        // 根据原因播放不同动画
        switch (reason)
        {
            case InterruptReason.Damaged:
                animator.SetTrigger("HitReaction");
                break;
            case InterruptReason.Stunned:
                animator.SetTrigger("Stunned");
                break;
            case InterruptReason.KnockedDown:
                animator.SetTrigger("Knockdown");
                break;
        }
    }

    void OnComboCompleted()
    {
        Debug.Log($"[Combat] 连招完成 - 总伤害: {_totalDamage}, 命中次数: {_hitCount}");

        // 显示连击评分
        ShowComboRating(_hitCount);
    }

    void OnMoveStarted(ComboMove move)
    {
        Debug.Log($"[Combat] 招式开始: {move.MoveName}");

        // 播放音效
        PlayAttackSound(move.MoveId);

        // 播放特效
        SpawnAttackEffect(move.MoveId);
    }

    void OnHitBoxesActivated(HitBoxInfo[] hitBoxes)
    {
        // 检测命中
        foreach (var hitBox in hitBoxes)
        {
            DetectHits(hitBox);
        }
    }

    void OnHitBoxesDeactivated()
    {
        // 清理命中记录
    }

    void OnMoveCompleted(ComboMove move)
    {
        Debug.Log($"[Combat] 招式完成: {move.MoveName}");
    }

    void OnStateChanged(ComboStateType from, ComboStateType to)
    {
        Debug.Log($"[Combat] 状态变化: {from} -> {to}");
    }

    #endregion

    #region Combat Logic

    void DetectHits(HitBoxInfo hitBox)
    {
        var collider = hitBox.Collider;
        if (collider == null) return;

        // 使用 Overlap 检测
        Collider[] hits = Physics.OverlapBox(
            collider.bounds.center,
            collider.bounds.extents,
            collider.transform.rotation,
            enemyLayer
        );

        foreach (var hit in hits)
        {
            ProcessHit(hitBox, hit);
        }
    }

    void ProcessHit(HitBoxInfo hitBox, Collider target)
    {
        // 获取伤害数据
        int damage = CalculateDamage(hitBox.MoveData);

        // 应用伤害
        var health = target.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);
            _totalDamage += damage;
            _hitCount++;

            // 命中停顿
            StartCoroutine(HitStop(hitStopDuration));

            // 播放受击特效
            SpawnHitEffect(target.transform.position);

            // 相机震动
            CameraShake.Instance.Shake(0.2f, 0.1f);
        }
    }

    int CalculateDamage(ComboMove move)
    {
        // 基础伤害 + 连击加成
        float comboMultiplier = 1f + (comboController.CurrentComboCount * 0.1f);
        return Mathf.RoundToInt(100 * comboMultiplier); // 示例数值
    }

    System.Collections.IEnumerator HitStop(float duration)
    {
        Time.timeScale = 0.1f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    #endregion

    #region External Triggers

    // 被外部调用：受到伤害
    public void OnTakeDamage(int damage)
    {
        comboController.InterruptCombo(InterruptReason.Damaged);
    }

    // 被外部调用：被眩晕
    public void OnStunned()
    {
        comboController.InterruptCombo(InterruptReason.Stunned);
    }

    // 被外部调用：被击倒
    public void OnKnockedDown()
    {
        comboController.InterruptCombo(InterruptReason.KnockedDown);
    }

    #endregion

    #region Effects & Audio

    void PlayAttackSound(string moveId)
    {
        // 实现音效播放
    }

    void SpawnAttackEffect(string moveId)
    {
        // 实现特效生成
    }

    void SpawnHitEffect(Vector3 position)
    {
        // 实现受击特效
    }

    void ShowComboRating(int hitCount)
    {
        string rating = hitCount switch
        {
            >= 20 => "SSS",
            >= 15 => "SS",
            >= 10 => "S",
            >= 5 => "A",
            _ => "B"
        };

        Debug.Log($"[Combat] 连击评级: {rating}");
    }

    #endregion

    void OnDestroy()
    {
        // 取消订阅
        if (comboController != null)
        {
            comboController.OnComboStarted -= OnComboStarted;
            comboController.OnComboInterrupted -= OnComboInterrupted;
            comboController.OnComboCompleted -= OnComboCompleted;
            comboController.OnMoveStarted -= OnMoveStarted;
            comboController.OnHitBoxesActivated -= OnHitBoxesActivated;
            comboController.OnHitBoxesDeactivated -= OnHitBoxesDeactivated;
            comboController.OnMoveCompleted -= OnMoveCompleted;
            comboController.OnStateChanged -= OnStateChanged;
        }
    }
}
```

### 3.2 进阶示例：自定义连招树

```csharp
using UnityEngine;
using Asaki.Plungin.ComboSystem;

[CreateAssetMenu(fileName = "WarriorCombo", menuName = "Asaki/ComboSystem/WarriorCombo")]
public class WarriorComboTree : ComboTree
{
    void Reset()
    {
        TreeId = "warrior_basic";
        Description = "战士基础连招";

        // 输入缓冲设置
        InputBufferWindow = 0.25f;
        MaxComboDuration = 10f;
        MaxComboLength = 10;

        // 定义招式
        Moves = new[]
        {
            // 轻攻击1
            new ComboMove
            {
                MoveId = "light_1",
                MoveName = "轻攻击1",
                AnimationStateName = "LightAttack1",
                AnimationSpeed = 1.2f,
                StartupTime = 0.1f,
                ActiveDuration = 0.15f,
                RecoveryTime = 0.2f,
                ComboWindowStart = 0.15f,
                ComboWindowEnd = 0.35f,
                MinComboCount = 0,
                MaxComboCount = 0,
                Cooldown = 0f,
                HitBoxes = new[]
                {
                    new HitBoxDefinition
                    {
                        HitBoxId = "sword_blade",
                        Shape = HitBoxShape.Box,
                        Size = new Vector3(0.5f, 0.2f, 1.5f),
                        Offset = new Vector3(0, 0, 0.75f),
                        BoneName = "Weapon_R"
                    }
                }
            },

            // 轻攻击2
            new ComboMove
            {
                MoveId = "light_2",
                MoveName = "轻攻击2",
                AnimationStateName = "LightAttack2",
                AnimationSpeed = 1.2f,
                StartupTime = 0.08f,
                ActiveDuration = 0.12f,
                RecoveryTime = 0.18f,
                ComboWindowStart = 0.12f,
                ComboWindowEnd = 0.3f,
                MinComboCount = 1,
                HitBoxes = new[]
                {
                    new HitBoxDefinition
                    {
                        HitBoxId = "sword_blade",
                        Shape = HitBoxShape.Box,
                        Size = new Vector3(0.5f, 0.2f, 1.5f),
                        Offset = new Vector3(0, 0, 0.75f),
                        BoneName = "Weapon_R"
                    }
                }
            },

            // 轻攻击3（终结技）
            new ComboMove
            {
                MoveId = "light_3",
                MoveName = "轻攻击3",
                AnimationStateName = "LightAttack3",
                AnimationSpeed = 1f,
                StartupTime = 0.15f,
                ActiveDuration = 0.25f,
                RecoveryTime = 0.4f,
                ComboWindowStart = 0.3f,
                ComboWindowEnd = 0.5f,
                MinComboCount = 2,
                MaxComboCount = 5,
                HitBoxes = new[]
                {
                    new HitBoxDefinition
                    {
                        HitBoxId = "sword_blade",
                        Shape = HitBoxShape.Box,
                        Size = new Vector3(0.6f, 0.3f, 1.8f),
                        Offset = new Vector3(0, 0, 0.9f),
                        BoneName = "Weapon_R"
                    },
                    new HitBoxDefinition
                    {
                        HitBoxId = "sword_tip",
                        Shape = HitBoxShape.Sphere,
                        Radius = 0.4f,
                        Offset = new Vector3(0, 0, 1.8f),
                        BoneName = "Weapon_R"
                    }
                }
            },

            // 重攻击
            new ComboMove
            {
                MoveId = "heavy_1",
                MoveName = "重攻击",
                AnimationStateName = "HeavyAttack1",
                AnimationSpeed = 0.9f,
                StartupTime = 0.3f,
                ActiveDuration = 0.3f,
                RecoveryTime = 0.5f,
                ComboWindowStart = 0.4f,
                ComboWindowEnd = 0.7f,
                MinComboCount = 0,
                Cooldown = 1f,
                HitBoxes = new[]
                {
                    new HitBoxDefinition
                    {
                        HitBoxId = "heavy_swing",
                        Shape = HitBoxShape.Capsule,
                        Radius = 0.5f,
                        Height = 2f,
                        Offset = new Vector3(0, 0, 1f),
                        BoneName = "Weapon_R"
                    }
                }
            },

            // 旋风斩（技能）
            new ComboMove
            {
                MoveId = "skill_spin",
                MoveName = "旋风斩",
                AnimationStateName = "SkillSpin",
                AnimationSpeed = 1.5f,
                StartupTime = 0.2f,
                ActiveDuration = 1f,
                RecoveryTime = 0.3f,
                ComboWindowStart = 0.8f,
                ComboWindowEnd = 1f,
                MinComboCount = 3,
                Cooldown = 5f,
                HitBoxes = new[]
                {
                    new HitBoxDefinition
                    {
                        HitBoxId = "spin_aoe",
                        Shape = HitBoxShape.Sphere,
                        Radius = 2.5f,
                        Offset = Vector3.zero,
                        BoneName = "Root"
                    }
                }
            }
        };

        // 定义转换关系
        Transitions = new[]
        {
            // 轻攻击连段
            new ComboTransition
            {
                FromMoveId = "light_1",
                ToMoveId = "light_2",
                InputType = "LightAttack",
                ResetGroup = "default"
            },
            new ComboTransition
            {
                FromMoveId = "light_2",
                ToMoveId = "light_3",
                InputType = "LightAttack",
                ResetGroup = "default"
            },
            new ComboTransition
            {
                FromMoveId = "light_3",
                ToMoveId = "light_1",
                InputType = "LightAttack",
                ResetGroup = "default"
            },

            // 重攻击分支
            new ComboTransition
            {
                FromMoveId = "light_1",
                ToMoveId = "heavy_1",
                InputType = "HeavyAttack",
                ResetGroup = "default"
            },
            new ComboTransition
            {
                FromMoveId = "light_2",
                ToMoveId = "heavy_1",
                InputType = "HeavyAttack",
                ResetGroup = "default"
            },

            // 技能派生
            new ComboTransition
            {
                FromMoveId = "light_3",
                ToMoveId = "skill_spin",
                InputType = "Skill1",
                ResetGroup = "default"
            },
            new ComboTransition
            {
                FromMoveId = "heavy_1",
                ToMoveId = "skill_spin",
                InputType = "Skill1",
                ResetGroup = "default"
            }
        };

        // 重置策略
        ResetStrategies = new[]
        {
            new ResetStrategyDefinition
            {
                GroupName = "default",
                Mode = ResetComboMode.ResetToZero
            },
            new ResetStrategyDefinition
            {
                GroupName = "on_damaged",
                Mode = ResetComboMode.ResetToZero
            },
            new ResetStrategyDefinition
            {
                GroupName = "keep_combo",
                Mode = ResetComboMode.KeepCount
            }
        };
    }
}
```

### 3.3 高级示例：自定义重置策略

```csharp
using UnityEngine;
using Asaki.Plungin.ComboSystem;

/// <summary>
/// 怪物猎人气刃风格的重置系统
/// </summary>
public class SpiritBladeResetSystem : MonoBehaviour
{
    [SerializeField] private AsakiComboController comboController;
    [SerializeField] private ComboTree comboTree;

    // 气刃等级
    private int _spiritLevel;
    private float _lastHitTime;
    private float _comboTimeout = 5f;

    void Start()
    {
        comboController.Initialize(comboTree);

        // 订阅事件
        comboController.OnHitBoxesActivated += OnHitBoxesActivated;
        comboController.OnComboInterrupted += OnComboInterrupted;
        comboController.OnComboWindowOpened += OnComboWindowOpened;
    }

    void Update()
    {
        // 检查气刃衰减
        CheckSpiritDecay();
    }

    void OnHitBoxesActivated(HitBoxInfo[] hitBoxes)
    {
        _lastHitTime = Time.time;
    }

    void OnComboWindowOpened(float duration)
    {
        // 在连招窗口期间保持气刃
        // 不重置计数
    }

    void OnComboInterrupted(InterruptReason reason)
    {
        var context = new ComboContext
        {
            Controller = comboController,
            ComboCount = comboController.CurrentComboCount,
            InterruptReason = reason
        };

        // 根据中断原因应用不同策略
        int newCount = reason switch
        {
            InterruptReason.Damaged => ApplyDamagedReset(context),
            InterruptReason.UserCancel => ApplyCancelReset(context),
            _ => ApplyDefaultReset(context)
        };

        comboController.SetComboCount(newCount);

        // 更新气刃等级
        UpdateSpiritLevel(newCount);
    }

    int ApplyDamagedReset(ComboContext context)
    {
        // 受击时大幅降低
        int current = context.ComboCount;

        if (_spiritLevel >= 3)
        {
            // 红刃时保留一半
            return Mathf.Max(3, current / 2);
        }
        else if (_spiritLevel >= 2)
        {
            // 黄刃时保留1/3
            return Mathf.Max(1, current / 3);
        }
        else
        {
            // 无气刃或白刃时重置
            return 0;
        }
    }

    int ApplyCancelReset(ComboContext context)
    {
        // 主动取消时保持大部分
        return Mathf.Max(0, context.ComboCount - 1);
    }

    int ApplyDefaultReset(ComboContext context)
    {
        // 默认递减
        return Mathf.Max(0, context.ComboCount - 2);
    }

    void CheckSpiritDecay()
    {
        // 长时间未命中时气刃衰减
        if (Time.time - _lastHitTime > _comboTimeout)
        {
            if (_spiritLevel > 0)
            {
                _spiritLevel--;
                _lastHitTime = Time.time;

                Debug.Log($"[Spirit] 气刃等级下降: {_spiritLevel}");
            }
        }
    }

    void UpdateSpiritLevel(int comboCount)
    {
        int newLevel = comboCount switch
        {
            >= 15 => 3, // 红刃
            >= 10 => 2, // 黄刃
            >= 5 => 1,  // 白刃
            _ => 0      // 无
        };

        if (newLevel != _spiritLevel)
        {
            _spiritLevel = newLevel;
            OnSpiritLevelChanged(_spiritLevel);
        }
    }

    void OnSpiritLevelChanged(int level)
    {
        string levelName = level switch
        {
            3 => "红刃",
            2 => "黄刃",
            1 => "白刃",
            _ => "无"
        };

        Debug.Log($"[Spirit] 气刃等级变化: {levelName}");

        // 应用伤害加成
        float damageMultiplier = 1f + (level * 0.2f);
        // 通知伤害系统更新倍率
    }
}
```

### 3.4 进阶示例：AI 敌人连招系统

```csharp
using UnityEngine;
using Asaki.Plungin.ComboSystem;
using System.Collections;

public class EnemyAICombat : MonoBehaviour
{
    [SerializeField] private AsakiComboController comboController;
    [SerializeField] private ComboTree comboTree;
    [SerializeField] private Transform player;

    [Header("AI Settings")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float reactionTime = 0.3f;

    private float _lastAttackTime;
    private bool _isAttacking;

    // 连招模式权重
    private readonly (string pattern, float weight)[] _attackPatterns =
    {
        ("Light-Light-Light", 0.4f),
        ("Light-Light-Heavy", 0.3f),
        ("Heavy", 0.2f),
        ("Light-Skill", 0.1f)
    };

    void Start()
    {
        comboController.Initialize(comboTree);
        comboController.OnComboCompleted += OnComboCompleted;
        comboController.OnComboInterrupted += OnComboInterrupted;
    }

    void Update()
    {
        if (_isAttacking) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange && Time.time > _lastAttackTime + attackCooldown)
        {
            StartCoroutine(ExecuteAttackPattern());
        }
    }

    IEnumerator ExecuteAttackPattern()
    {
        _isAttacking = true;
        _lastAttackTime = Time.time;

        // 选择攻击模式
        string pattern = SelectAttackPattern();
        string[] attacks = pattern.Split('-');

        foreach (var attack in attacks)
        {
            // 等待反应时间
            yield return new WaitForSeconds(reactionTime);

            // 检查是否还能攻击
            if (!comboController.CanAcceptInput())
                break;

            // 检查玩家是否还在范围内
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance > attackRange * 1.5f)
                break;

            // 执行攻击
            string inputType = attack switch
            {
                "Light" => "LightAttack",
                "Heavy" => "HeavyAttack",
                "Skill" => "Skill1",
                _ => "LightAttack"
            };

            comboController.TriggerAttack(inputType);

            // 等待招式完成
            yield return new WaitUntil(() => comboController.CurrentStateType == ComboStateType.ComboWindow
                || comboController.CurrentStateType == ComboStateType.Idle);
        }

        _isAttacking = false;
    }

    string SelectAttackPattern()
    {
        float totalWeight = 0f;
        foreach (var (_, weight) in _attackPatterns)
        {
            totalWeight += weight;
        }

        float random = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var (pattern, weight) in _attackPatterns)
        {
            currentWeight += weight;
            if (random <= currentWeight)
            {
                return pattern;
            }
        }

        return _attackPatterns[0].pattern;
    }

    void OnComboCompleted()
    {
        // 连招完成后的处理
        Debug.Log("[EnemyAI] 连招完成");
    }

    void OnComboInterrupted(InterruptReason reason)
    {
        _isAttacking = false;

        // 被玩家打断时的反应
        if (reason == InterruptReason.Damaged)
        {
            // 可能被眩晕或反击
            if (Random.value < 0.3f)
            {
                // 30% 概率进入硬直
                StartCoroutine(Stagger());
            }
        }
    }

    IEnumerator Stagger()
    {
        Debug.Log("[EnemyAI] 进入硬直");
        yield return new WaitForSeconds(1f);
        Debug.Log("[EnemyAI] 硬直结束");
    }

    // 被玩家攻击时调用
    public void OnPlayerAttack()
    {
        // 根据当前状态决定是否格挡或闪避
        if (comboController.CurrentStateType == ComboStateType.Startup)
        {
            // 前摇期间可以被打断
            comboController.InterruptCombo(InterruptReason.Damaged);
        }
        else if (comboController.CurrentStateType == ComboStateType.Active)
        {
            // 判定期间可能霸体
            if (Random.value < 0.5f)
            {
                // 50% 概率霸体
                Debug.Log("[EnemyAI] 霸体!");
            }
            else
            {
                comboController.InterruptCombo(InterruptReason.Damaged);
            }
        }
    }
}
```

### 3.5 进阶示例：输入系统扩展

```csharp
using UnityEngine;
using Asaki.Plungin.ComboSystem;

/// <summary>
/// 扩展输入类型支持方向指令（格斗游戏风格）
/// </summary>
public class DirectionalInputSystem : MonoBehaviour
{
    [SerializeField] private AsakiComboController comboController;

    // 输入历史（用于检测指令）
    private readonly (Vector2 direction, float time)[] _inputHistory = new (Vector2, float)[10];
    private int _inputIndex;

    // 指令窗口时间
    [SerializeField] private float commandWindow = 0.3f;

    void Update()
    {
        RecordInput();

        // 检测特殊指令
        if (DetectCommand("236")) // 下前
        {
            comboController.TriggerAttack("Hadoken");
        }
        else if (DetectCommand("623")) // 前下前
        {
            comboController.TriggerAttack("Shoryuken");
        }
        else if (DetectCommand("214")) // 下后
        {
            comboController.TriggerAttack("Tatsumaki");
        }
        else if (Input.GetButtonDown("Fire1"))
        {
            comboController.TriggerAttack("LightAttack");
        }
        else if (Input.GetButtonDown("Fire2"))
        {
            comboController.TriggerAttack("HeavyAttack");
        }
    }

    void RecordInput()
    {
        Vector2 input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        // 只记录有效输入
        if (input.magnitude > 0.5f)
        {
            // 方向量化（8方向）
            Vector2 quantized = QuantizeDirection(input);

            // 检查是否与上一个输入不同
            if (_inputIndex == 0 || _inputHistory[_inputIndex - 1].direction != quantized)
            {
                _inputHistory[_inputIndex] = (quantized, Time.time);
                _inputIndex = (_inputIndex + 1) % _inputHistory.Length;
            }
        }
    }

    Vector2 QuantizeDirection(Vector2 input)
    {
        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
        angle = (angle + 360) % 360;

        // 转换为数字小键盘方向
        // 6 = 右, 8 = 上, 4 = 左, 2 = 下
        return angle switch
        {
            >= 337.5f or < 22.5f => new Vector2(1, 0),   // 6
            >= 22.5f and < 67.5f => new Vector2(1, 1),   // 9
            >= 67.5f and < 112.5f => new Vector2(0, 1),  // 8
            >= 112.5f and < 157.5f => new Vector2(-1, 1), // 7
            >= 157.5f and < 202.5f => new Vector2(-1, 0), // 4
            >= 202.5f and < 247.5f => new Vector2(-1, -1),// 1
            >= 247.5f and < 292.5f => new Vector2(0, -1), // 2
            >= 292.5f and < 337.5f => new Vector2(1, -1), // 3
            _ => Vector2.zero
        };
    }

    bool DetectCommand(string command)
    {
        // 解析指令字符串
        Vector2[] requiredDirections = new Vector2[command.Length];
        for (int i = 0; i < command.Length; i++)
        {
            requiredDirections[i] = command[i] switch
            {
                '6' => new Vector2(1, 0),
                '4' => new Vector2(-1, 0),
                '8' => new Vector2(0, 1),
                '2' => new Vector2(0, -1),
                '9' => new Vector2(1, 1),
                '7' => new Vector2(-1, 1),
                '3' => new Vector2(1, -1),
                '1' => new Vector2(-1, -1),
                _ => Vector2.zero
            };
        }

        // 在历史输入中查找匹配
        int matchIndex = 0;
        float lastMatchTime = 0;

        for (int i = _inputIndex - 1; i >= 0; i--)
        {
            var input = _inputHistory[i];

            // 检查时间窗口
            if (Time.time - input.time > commandWindow)
                break;

            if (input.direction == requiredDirections[matchIndex])
            {
                if (matchIndex == 0 || input.time > lastMatchTime)
                {
                    matchIndex++;
                    lastMatchTime = input.time;

                    if (matchIndex >= requiredDirections.Length)
                    {
                        // 清除已使用的输入
                        ClearInputHistory();
                        return true;
                    }
                }
            }
        }

        return false;
    }

    void ClearInputHistory()
    {
        for (int i = 0; i < _inputHistory.Length; i++)
        {
            _inputHistory[i] = (Vector2.zero, 0);
        }
        _inputIndex = 0;
    }
}
```

---

## 附录：常见问题

### Q: 如何调整连招的输入宽容度？
**A**: 修改 `ComboTree.InputBufferWindow` 和 `ComboMove.ComboWindowStart/End` 参数。

### Q: 如何实现空中连招？
**A**: 在 `ComboMove` 中配置不同的动画，并在地面检测逻辑中控制何时可以触发空中招式。

### Q: 如何与动画事件系统配合？
**A**: 使用 `ComboAnimationEventReceiver` 组件，在动画中触发事件来精确控制状态转换。

### Q: 如何实现完美闪避后的时间减缓？
**A**: 订阅 `OnComboWindowOpened` 事件，在窗口期间检测到闪避输入时触发时间减缓效果。

---

*文档版本: 1.0*
*最后更新: 2026-02-06*
*作者: Asaki Framework Team*
