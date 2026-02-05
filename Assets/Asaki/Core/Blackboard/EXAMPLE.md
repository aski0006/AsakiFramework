# Asaki Blackboard System - 使用指南与示例

## 目录

1. [使用用途推荐](#一使用用途推荐)
2. [Blackboard 系统 API](#二blackboard-系统-api)
3. [使用示例](#三使用示例)

---

## 一、使用用途推荐

### 1.1 AI 行为树系统

黑板系统是 AI 行为树的核心数据共享机制，用于在行为树节点间传递感知信息和决策状态。

**典型应用场景**:
- **感知数据共享**: AI 视觉检测到的目标位置、类型存储在黑板，供选择节点使用
- **导航状态**: 当前导航目标点、路径状态
- **行为状态**: 当前行为阶段、冷却时间、优先级标记
- **记忆系统**: 敌人最后已知位置、调查点标记

```csharp
// AI 黑板键常量定义
public static class AIBlackboardKeys
{
    public const string Target = "Target";
    public const string TargetPosition = "TargetPosition";
    public const string IsAlerted = "IsAlerted";
    public const string PatrolIndex = "PatrolIndex";
    public const string AttackCooldown = "AttackCooldown";
}
```

### 1.2 任务/Quest 系统

管理任务状态、进度计数、条件检查等数据。

**典型应用场景**:
- **任务进度**: 收集物品数量、击杀敌人计数
- **任务状态**: 是否接受、是否完成、是否提交
- **条件检查**: 前置任务完成状态、等级要求
- **动态目标**: 随机生成的目标位置、NPC ID

```csharp
// 任务黑板键常量定义
public static class QuestBlackboardKeys
{
    public const string KillCount = "Quest_KillCount";
    public const string TargetCount = "Quest_TargetCount";
    public const string IsCompleted = "Quest_IsCompleted";
    public const string QuestGiverID = "Quest_GiverID";
}
```

### 1.3 对话系统

管理对话状态、选择结果、分支条件等。

**典型应用场景**:
- **对话进度**: 当前对话节点 ID
- **玩家选择**: 分支选择结果存储
- **条件判断**: 好感度检查、物品持有检查
- **动态文本**: 插入玩家名称、变量值

```csharp
// 对话黑板键常量定义
public static class DialogueBlackboardKeys
{
    public const string CurrentNodeID = "Dialogue_CurrentNode";
    public const string SelectedOption = "Dialogue_SelectedOption";
    public const string FactionReputation = "Dialogue_Reputation";
    public const string PlayerName = "Dialogue_PlayerName";
}
```

### 1.4 游戏状态管理

全局游戏状态、配置参数的集中管理。

**典型应用场景**:
- **游戏配置**: 难度级别、游戏速度、时间缩放
- **章节进度**: 当前章节、解锁状态
- **统计信息**: 总游戏时间、死亡次数、收集完成度
- **成就追踪**: 成就解锁状态、进度计数

```csharp
// 游戏状态黑板键常量定义
public static class GameBlackboardKeys
{
    public const string Difficulty = "Game_Difficulty";
    public const string CurrentChapter = "Game_Chapter";
    public const string PlayTime = "Game_PlayTime";
    public const string DeathCount = "Game_DeathCount";
}
```

### 1.5 动画状态机

在动画控制器和脚本间共享状态信息。

**典型应用场景**:
- **动画触发**: 触发攻击、受击、死亡动画
- **混合树参数**: 移动速度、方向、姿态
- **状态标记**: 是否在地面上、是否攀爬中
- **IK 目标**: 头部注视目标、手部抓取点

### 1.6 技能/能力系统

管理技能状态、冷却、消耗等数据。

**典型应用场景**:
- **技能冷却**: 各技能剩余冷却时间
- **资源消耗**: 当前法力值、怒气值、能量值
- **Buff/Debuff**: 持续时间和层数
- **连招状态**: 当前连击数、可接技能标记

---

## 二、Blackboard 系统 API

### 2.1 IAsakiBlackboard 接口

黑板系统的核心接口，定义了所有黑板必须实现的功能。

```csharp
public interface IAsakiBlackboard : IAsakiService, IDisposable
{
    // 获取值（键不存在时返回 default）
    T GetValue<T>(AsakiBlackboardKey key);
    
    // 设置值
    void SetValue<T>(AsakiBlackboardKey key, T value);
    
    // 获取响应式属性（支持变更通知）
    AsakiProperty<T> GetProperty<T>(AsakiBlackboardKey key);
    
    // 检查键是否存在
    bool HasKey(AsakiBlackboardKey key);
    
    // 移除键
    void Remove(AsakiBlackboardKey key);
    
    // 清空所有数据
    void Clear();
    
    // 开始批量操作模式
    IDisposable BeginBatch();
}
```

### 2.2 AsakiBlackboard 实现类

黑板的具体实现，支持父子作用域继承。

#### 构造函数

```csharp
// 创建独立黑板
var blackboard = new AsakiBlackboard();

// 创建带父作用域的黑板（支持数据继承）
var childBlackboard = new AsakiBlackboard(parentBlackboard);
```

#### 核心方法详解

##### GetValue<T>

```csharp
/// <summary>
/// 获取指定键的值。
/// </summary>
/// <typeparam name="T">值的类型</typeparam>
/// <param name="key">黑板键</param>
/// <returns>键对应的值，若不存在返回 default(T)</returns>
public T GetValue<T>(AsakiBlackboardKey key)

// 使用示例
int health = blackboard.GetValue<int>("Health");
Vector3 position = blackboard.GetValue<Vector3>("TargetPosition");
```

##### SetValue<T>

```csharp
/// <summary>
/// 设置指定键的值。
/// </summary>
/// <typeparam name="T">值的类型</typeparam>
/// <param name="key">黑板键</param>
/// <param name="value">要设置的值</param>
public void SetValue<T>(AsakiBlackboardKey key, T value)

// 使用示例
blackboard.SetValue("Health", 100);
blackboard.SetValue("TargetPosition", transform.position);
```

##### GetProperty<T>

```csharp
/// <summary>
/// 获取响应式属性，支持值变更通知。
/// </summary>
/// <typeparam name="T">属性值类型</typeparam>
/// <param name="key">黑板键</param>
/// <returns>响应式属性实例</returns>
public AsakiProperty<T> GetProperty<T>(AsakiBlackboardKey key)

// 使用示例
var healthProp = blackboard.GetProperty<int>("Health");
healthProp.OnValueChanged += (oldVal, newVal) => 
{
    Debug.Log($"Health: {oldVal} -> {newVal}");
};
```

##### BeginBatch

```csharp
/// <summary>
/// 开始批量操作模式，在 using 块结束时统一触发通知。
/// </summary>
/// <returns>批量操作作用域，使用 using 语句自动释放</returns>
public IDisposable BeginBatch()

// 使用示例
using (blackboard.BeginBatch())
{
    blackboard.SetValue("Health", 80);
    blackboard.SetValue("Mana", 50);
    blackboard.SetValue("Stamina", 30);
} // 统一触发所有变更通知
```

### 2.3 AsakiBlackboardKey 结构体

确定性哈希键，使用 FNV-1a 算法。

```csharp
public readonly struct AsakiBlackboardKey : IEquatable<AsakiBlackboardKey>
{
    public readonly int Hash;
    
#if UNITY_EDITOR
    public readonly string DebugName;
#endif
    
    // 从字符串构造
    public AsakiBlackboardKey(string keyName)
    
    // 从哈希值构造
    public AsakiBlackboardKey(int hash)
    
    // 隐式转换
    public static implicit operator AsakiBlackboardKey(string name)
    public static implicit operator AsakiBlackboardKey(int hash)
}
```

#### 使用方式

```csharp
// 方式1：隐式转换
AsakiBlackboardKey key1 = "PlayerHealth";

// 方式2：显式构造
var key2 = new AsakiBlackboardKey("PlayerHealth");

// 方式3：使用常量
public static class Keys
{
    public static readonly AsakiBlackboardKey Health = "PlayerHealth";
}
```

### 2.4 AsakiProperty<T> 响应式属性

支持值变更通知的属性包装器。

```csharp
public class AsakiProperty<T>
{
    // 当前值
    public T Value { get; set; }
    
    // 值变更事件
    public event Action<T, T> OnValueChanged;
    
    // 隐式转换为 T
    public static implicit operator T(AsakiProperty<T> property)
}
```

#### 使用方式

```csharp
// 获取属性
var healthProp = blackboard.GetProperty<int>("Health");

// 订阅变更事件
healthProp.OnValueChanged += (oldVal, newVal) =>
{
    Debug.Log($"Health changed from {oldVal} to {newVal}");
    if (newVal <= 0)
    {
        OnPlayerDeath();
    }
};

// 设置值（会触发事件）
healthProp.Value = 50;

// 隐式转换
int currentHealth = healthProp;
```

### 2.5 扩展方法

#### BlackboardExtensions

```csharp
public static class BlackboardExtensions
{
    /// <summary>
    /// 批量设置多个值（元组方式）
    /// </summary>
    public static void BatchSet(
        this IAsakiBlackboard blackboard,
        params (string key, object value)[] updates
    )
    
    /// <summary>
    /// 批量设置多个值（字典方式）
    /// </summary>
    public static void BatchSet(
        this IAsakiBlackboard blackboard,
        Dictionary<string, object> updates
    )
}
```

### 2.6 AsakiGlobalBlackboardAsset 全局黑板资产

跨图共享的全局变量存储。

```csharp
public class AsakiGlobalBlackboardAsset : ScriptableObject
{
    [SerializeReference]
    public List<AsakiVariableDef> GlobalVariables;
    
    /// <summary>
    /// 获取或创建全局变量
    /// </summary>
    public AsakiVariableDef GetOrCreateVariable(string name, Type valueType)
    
    /// <summary>
    /// 移除变量
    /// </summary>
    public bool RemoveVariable(string name)
}
```

#### 使用方式

```csharp
[SerializeField] private AsakiGlobalBlackboardAsset globalBlackboard;

void Start()
{
    // 获取或创建变量
    var scoreVar = globalBlackboard.GetOrCreateVariable("TotalScore", typeof(AsakiInt));
    
    // 访问值
    int score = ((AsakiInt)scoreVar.ValueData).Value;
}
```

### 2.7 变量约束 API

#### IVariableConstraint 接口

```csharp
public interface IVariableConstraint
{
    bool IsValid(object value);
    string GetErrorMessage(object value);
}
```

#### 内置约束实现

```csharp
// 范围约束（适用于数值类型）
[Serializable]
public class RangeConstraint : IVariableConstraint
{
    public float MinValue = float.MinValue;
    public float MaxValue = float.MaxValue;
}

// 非空约束
[Serializable]
public class NotNullConstraint : IVariableConstraint
{
}

// 正则约束（适用于字符串）
[Serializable]
public class RegexConstraint : IVariableConstraint
{
    public string Pattern;
}
```

### 2.8 调试工具 API

#### BlackboardProfiler

```csharp
#if UNITY_EDITOR
public static class BlackboardProfiler
{
    // 启用性能分析
    public static void Enable()
    
    // 禁用性能分析
    public static void Disable()
    
    // 打印访问报告
    public static void PrintReport()
    
    // 获取统计信息
    public static IReadOnlyDictionary<string, ProfileData> GetStats()
}
#endif
```

---

## 三、使用示例

### 3.1 基础使用示例

#### 示例 1：创建和使用黑板

```csharp
using Asaki.Core.Blackboard;
using UnityEngine;

public class BasicExample : MonoBehaviour
{
    private IAsakiBlackboard _blackboard;
    
    void Start()
    {
        // 创建黑板实例
        _blackboard = new AsakiBlackboard();
        
        // 设置值
        _blackboard.SetValue("PlayerName", "Hero");
        _blackboard.SetValue("Level", 1);
        _blackboard.SetValue("Health", 100f);
        
        // 获取值
        string playerName = _blackboard.GetValue<string>("PlayerName");
        int level = _blackboard.GetValue<int>("Level");
        float health = _blackboard.GetValue<float>("Health");
        
        Debug.Log($"Player: {playerName}, Level: {level}, Health: {health}");
    }
}
```

#### 示例 2：使用常量管理键名

```csharp
using Asaki.Core.Blackboard;
using UnityEngine;

public class BlackboardKeys
{
    // 使用静态只读字段定义键
    public static readonly AsakiBlackboardKey Health = "PlayerHealth";
    public static readonly AsakiBlackboardKey Mana = "PlayerMana";
    public static readonly AsakiBlackboardKey Position = "PlayerPosition";
    public static readonly AsakiBlackboardKey IsDead = "IsPlayerDead";
}

public class KeyConstantsExample : MonoBehaviour
{
    private IAsakiBlackboard _blackboard;
    
    void Start()
    {
        _blackboard = new AsakiBlackboard();
        
        // 使用常量设置值
        _blackboard.SetValue(BlackboardKeys.Health, 100);
        _blackboard.SetValue(BlackboardKeys.Mana, 50);
        _blackboard.SetValue(BlackboardKeys.IsDead, false);
        
        // 使用常量获取值
        int health = _blackboard.GetValue<int>(BlackboardKeys.Health);
        Debug.Log($"Current Health: {health}");
    }
}
```

### 3.2 响应式编程示例

#### 示例 3：生命值变更监听

```csharp
using Asaki.Core.Blackboard;
using Asaki.Core.Reactive;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    
    private IAsakiBlackboard _blackboard;
    private AsakiProperty<int> _healthProp;
    private AsakiProperty<bool> _isDeadProp;
    
    void Awake()
    {
        _blackboard = new AsakiBlackboard();
        
        // 获取或创建属性
        _healthProp = _blackboard.GetProperty<int>("Health");
        _isDeadProp = _blackboard.GetProperty<bool>("IsDead");
        
        // 订阅变更事件
        _healthProp.OnValueChanged += OnHealthChanged;
        
        // 初始化值
        _healthProp.Value = maxHealth;
        _isDeadProp.Value = false;
    }
    
    void OnDestroy()
    {
        // 取消订阅避免内存泄漏
        _healthProp.OnValueChanged -= OnHealthChanged;
        _blackboard.Dispose();
    }
    
    void OnHealthChanged(int oldHealth, int newHealth)
    {
        Debug.Log($"Health: {oldHealth} -> {newHealth}");
        
        // 生命值归零
        if (newHealth <= 0 && oldHealth > 0)
        {
            _isDeadProp.Value = true;
            OnDeath();
        }
        // 复活
        else if (newHealth > 0 && oldHealth <= 0)
        {
            _isDeadProp.Value = false;
            OnRevive();
        }
        
        // 更新 UI
        UpdateHealthUI(newHealth);
    }
    
    public void TakeDamage(int damage)
    {
        int currentHealth = _healthProp.Value;
        _healthProp.Value = Mathf.Max(0, currentHealth - damage);
    }
    
    public void Heal(int amount)
    {
        int currentHealth = _healthProp.Value;
        _healthProp.Value = Mathf.Min(maxHealth, currentHealth + amount);
    }
    
    void OnDeath()
    {
        Debug.Log("Player died!");
        // 播放死亡动画、游戏结束逻辑等
    }
    
    void OnRevive()
    {
        Debug.Log("Player revived!");
        // 播放复活动画等
    }
    
    void UpdateHealthUI(int health)
    {
        // 更新血条 UI
        float healthPercent = (float)health / maxHealth;
        // UIManager.Instance.SetHealthBar(healthPercent);
    }
}
```

#### 示例 4：多属性联动

```csharp
using Asaki.Core.Blackboard;
using Asaki.Core.Reactive;
using UnityEngine;

public class StatsSystem : MonoBehaviour
{
    private IAsakiBlackboard _blackboard;
    private AsakiProperty<int> _strengthProp;
    private AsakiProperty<int> _constitutionProp;
    private AsakiProperty<int> _maxHealthProp;
    private AsakiProperty<int> _attackPowerProp;
    
    void Awake()
    {
        _blackboard = new AsakiBlackboard();
        
        _strengthProp = _blackboard.GetProperty<int>("Strength");
        _constitutionProp = _blackboard.GetProperty<int>("Constitution");
        _maxHealthProp = _blackboard.GetProperty<int>("MaxHealth");
        _attackPowerProp = _blackboard.GetProperty<int>("AttackPower");
        
        // 力量影响攻击力
        _strengthProp.OnValueChanged += (oldVal, newVal) =>
        {
            _attackPowerProp.Value = newVal * 2; // 每点力量 = 2点攻击
        };
        
        // 体质影响最大生命值
        _constitutionProp.OnValueChanged += (oldVal, newVal) =>
        {
            _maxHealthProp.Value = newVal * 10; // 每点体质 = 10点生命
        };
        
        // 初始化
        _strengthProp.Value = 10;
        _constitutionProp.Value = 10;
    }
    
    public void AddStrength(int amount)
    {
        _strengthProp.Value += amount;
    }
    
    public void AddConstitution(int amount)
    {
        _constitutionProp.Value += amount;
    }
}
```

### 3.3 批量操作示例

#### 示例 5：批量设置值

```csharp
using Asaki.Core.Blackboard;
using UnityEngine;

public class BatchExample : MonoBehaviour
{
    private IAsakiBlackboard _blackboard;
    
    void Start()
    {
        _blackboard = new AsakiBlackboard();
        
        // 方式1：使用 BeginBatch
        using (_blackboard.BeginBatch())
        {
            _blackboard.SetValue("Health", 100);
            _blackboard.SetValue("Mana", 50);
            _blackboard.SetValue("Stamina", 75);
            _blackboard.SetValue("Level", 5);
            _blackboard.SetValue("Experience", 1250);
        } // 所有变更通知在此统一触发
        
        // 方式2：使用扩展方法（元组）
        _blackboard.BatchSet(
            ("Strength", 15),
            ("Agility", 12),
            ("Intelligence", 10),
            ("Wisdom", 8),
            ("Charisma", 14)
        );
        
        // 方式3：使用扩展方法（字典）
        var updates = new Dictionary<string, object>
        {
            { "Gold", 1000 },
            { "Silver", 500 },
            { "Copper", 250 },
            { "Gems", 10 }
        };
        _blackboard.BatchSet(updates);
    }
}
```

### 3.4 父子作用域示例

#### 示例 6：分层黑板系统

```csharp
using Asaki.Core.Blackboard;
using UnityEngine;

public class HierarchicalBlackboardExample : MonoBehaviour
{
    void Start()
    {
        // 创建全局黑板（父作用域）
        var globalBlackboard = new AsakiBlackboard();
        globalBlackboard.SetValue("GameDifficulty", "Normal");
        globalBlackboard.SetValue("MaxPlayers", 4);
        
        // 创建玩家1黑板（继承全局）
        var player1Blackboard = new AsakiBlackboard(globalBlackboard);
        player1Blackboard.SetValue("PlayerName", "Player1");
        player1Blackboard.SetValue("Health", 100);
        
        // 创建玩家2黑板（继承全局）
        var player2Blackboard = new AsakiBlackboard(globalBlackboard);
        player2Blackboard.SetValue("PlayerName", "Player2");
        player2Blackboard.SetValue("Health", 80);
        
        // 玩家1可以访问全局值
        string difficulty1 = player1Blackboard.GetValue<string>("GameDifficulty");
        Debug.Log($"Player1 sees difficulty: {difficulty1}"); // Normal
        
        // 玩家2也可以访问全局值
        string difficulty2 = player2Blackboard.GetValue<string>("GameDifficulty");
        Debug.Log($"Player2 sees difficulty: {difficulty2}"); // Normal
        
        // 玩家1覆盖全局值（仅影响自己）
        player1Blackboard.SetValue("GameDifficulty", "Hard");
        
        // 检查值
        Debug.Log($"Global: {globalBlackboard.GetValue<string>("GameDifficulty")}"); // Normal
        Debug.Log($"Player1: {player1Blackboard.GetValue<string>("GameDifficulty")}"); // Hard
        Debug.Log($"Player2: {player2Blackboard.GetValue<string>("GameDifficulty")}"); // Normal
    }
}
```

### 3.5 AI 行为树示例

#### 示例 7：AI 感知与行为

```csharp
using Asaki.Core.Blackboard;
using UnityEngine;

// AI 黑板键定义
public static class AIBlackboardKeys
{
    public static readonly AsakiBlackboardKey Self = "Self";
    public static readonly AsakiBlackboardKey Target = "Target";
    public static readonly AsakiBlackboardKey TargetPosition = "TargetPosition";
    public static readonly AsakiBlackboardKey IsAlerted = "IsAlerted";
    public static readonly AsakiBlackboardKey PatrolIndex = "PatrolIndex";
    public static readonly AsakiBlackboardKey AttackRange = "AttackRange";
    public static readonly AsakiBlackboardKey LastKnownTargetPosition = "LastKnownTargetPosition";
}

// AI 控制器
public class AIController : MonoBehaviour
{
    private IAsakiBlackboard _blackboard;
    
    void Awake()
    {
        _blackboard = new AsakiBlackboard();
        
        // 初始化 AI 数据
        _blackboard.SetValue(AIBlackboardKeys.Self, this);
        _blackboard.SetValue(AIBlackboardKeys.AttackRange, 2f);
        _blackboard.SetValue(AIBlackboardKeys.IsAlerted, false);
        _blackboard.SetValue(AIBlackboardKeys.PatrolIndex, 0);
    }
    
    // 视觉检测
    public void OnDetectTarget(GameObject target)
    {
        _blackboard.SetValue(AIBlackboardKeys.Target, target);
        _blackboard.SetValue(AIBlackboardKeys.TargetPosition, target.transform.position);
        _blackboard.SetValue(AIBlackboardKeys.IsAlerted, true);
        _blackboard.SetValue(AIBlackboardKeys.LastKnownTargetPosition, target.transform.position);
        
        Debug.Log($"AI detected target: {target.name}");
    }
    
    // 丢失目标
    public void OnLoseTarget()
    {
        var lastPos = _blackboard.GetValue<Vector3>(AIBlackboardKeys.TargetPosition);
        _blackboard.SetValue(AIBlackboardKeys.LastKnownTargetPosition, lastPos);
        _blackboard.SetValue(AIBlackboardKeys.Target, (GameObject)null);
        
        Debug.Log("AI lost target, investigating last known position");
    }
    
    // 检查是否在攻击范围内
    public bool IsTargetInAttackRange()
    {
        var target = _blackboard.GetValue<GameObject>(AIBlackboardKeys.Target);
        if (target == null) return false;
        
        float attackRange = _blackboard.GetValue<float>(AIBlackboardKeys.AttackRange);
        float distance = Vector3.Distance(transform.position, target.transform.position);
        
        return distance <= attackRange;
    }
    
    // 获取下一个巡逻点
    public Vector3 GetNextPatrolPoint()
    {
        int currentIndex = _blackboard.GetValue<int>(AIBlackboardKeys.PatrolIndex);
        // ... 获取巡逻点逻辑
        _blackboard.SetValue(AIBlackboardKeys.PatrolIndex, (currentIndex + 1) % 4);
        return Vector3.zero; // 返回实际巡逻点
    }
}

// AI 行为节点示例
public abstract class AINodeBase
{
    protected IAsakiBlackboard Blackboard { get; private set; }
    
    public void Initialize(IAsakiBlackboard blackboard)
    {
        Blackboard = blackboard;
    }
    
    public abstract NodeStatus Execute();
}

public enum NodeStatus { Success, Failure, Running }

// 检查是否有目标节点
public class HasTargetNode : AINodeBase
{
    public override NodeStatus Execute()
    {
        var target = Blackboard.GetValue<GameObject>(AIBlackboardKeys.Target);
        return target != null ? NodeStatus.Success : NodeStatus.Failure;
    }
}

// 移动到目标位置节点
public class MoveToTargetNode : AINodeBase
{
    public override NodeStatus Execute()
    {
        var targetPos = Blackboard.GetValue<Vector3>(AIBlackboardKeys.TargetPosition);
        // ... 移动逻辑
        return NodeStatus.Running;
    }
}
```

### 3.6 任务系统示例

#### 示例 8：任务进度管理

```csharp
using Asaki.Core.Blackboard;
using UnityEngine;
using System;

// 任务事件
public static class QuestEvents
{
    public static event Action<string, int> OnKillCountChanged;
    public static event Action<string> OnQuestCompleted;
    
    public static void RaiseKillCountChanged(string questId, int count)
    {
        OnKillCountChanged?.Invoke(questId, count);
    }
    
    public static void RaiseQuestCompleted(string questId)
    {
        OnQuestCompleted?.Invoke(questId);
    }
}

// 任务管理器
public class QuestManager : MonoBehaviour
{
    [SerializeField] private AsakiGlobalBlackboardAsset _globalBlackboard;
    
    private IAsakiBlackboard _questBlackboard;
    
    void Awake()
    {
        _questBlackboard = new AsakiBlackboard();
        InitializeQuest("Quest_KillWolves", 5);
        InitializeQuest("Quest_CollectHerbs", 10);
    }
    
    void InitializeQuest(string questId, int targetCount)
    {
        // 创建任务变量
        var killCountVar = _globalBlackboard.GetOrCreateVariable(
            $"{questId}_CurrentCount", 
            typeof(AsakiInt)
        );
        var targetCountVar = _globalBlackboard.GetOrCreateVariable(
            $"{questId}_TargetCount", 
            typeof(AsakiInt)
        );
        var isCompletedVar = _globalBlackboard.GetOrCreateVariable(
            $"{questId}_IsCompleted", 
            typeof(AsakiBool)
        );
        
        // 初始化值
        ((AsakiInt)killCountVar.ValueData).Value = 0;
        ((AsakiInt)targetCountVar.ValueData).Value = targetCount;
        ((AsakiBool)isCompletedVar.ValueData).Value = false;
    }
    
    // 增加击杀计数
    public void AddKill(string questId, int amount = 1)
    {
        var countVar = _globalBlackboard.GetOrCreateVariable(
            $"{questId}_CurrentCount", 
            typeof(AsakiInt)
        );
        var targetVar = _globalBlackboard.GetOrCreateVariable(
            $"{questId}_TargetCount", 
            typeof(AsakiInt)
        );
        var completedVar = _globalBlackboard.GetOrCreateVariable(
            $"{questId}_IsCompleted", 
            typeof(AsakiBool)
        );
        
        int currentCount = ((AsakiInt)countVar.ValueData).Value;
        int targetCount = ((AsakiInt)targetVar.ValueData).Value;
        
        currentCount += amount;
        ((AsakiInt)countVar.ValueData).Value = currentCount;
        
        QuestEvents.RaiseKillCountChanged(questId, currentCount);
        
        // 检查完成
        if (currentCount >= targetCount && !((AsakiBool)completedVar.ValueData).Value)
        {
            ((AsakiBool)completedVar.ValueData).Value = true;
            QuestEvents.RaiseQuestCompleted(questId);
            Debug.Log($"Quest {questId} completed!");
        }
    }
    
    // 获取任务进度
    public float GetQuestProgress(string questId)
    {
        var countVar = _globalBlackboard.GetOrCreateVariable(
            $"{questId}_CurrentCount", 
            typeof(AsakiInt)
        );
        var targetVar = _globalBlackboard.GetOrCreateVariable(
            $"{questId}_TargetCount", 
            typeof(AsakiInt)
        );
        
        int current = ((AsakiInt)countVar.ValueData).Value;
        int target = ((AsakiInt)targetVar.ValueData).Value;
        
        return target > 0 ? (float)current / target : 0f;
    }
}
```

### 3.7 对话系统示例

#### 示例 9：对话状态管理

```csharp
using Asaki.Core.Blackboard;
using UnityEngine;

// 对话黑板键
public static class DialogueBlackboardKeys
{
    public static readonly AsakiBlackboardKey CurrentNodeID = "Dialogue_CurrentNode";
    public static readonly AsakiBlackboardKey SelectedOption = "Dialogue_SelectedOption";
    public static readonly AsakiBlackboardKey NPCName = "Dialogue_NPCName";
    public static readonly AsakiBlackboardKey PlayerName = "Dialogue_PlayerName";
    public static readonly AsakiBlackboardKey FactionReputation = "Dialogue_Reputation";
    public static readonly AsakiBlackboardKey HasKeyItem = "Dialogue_HasKeyItem";
}

// 对话管理器
public class DialogueManager : MonoBehaviour
{
    private IAsakiBlackboard _dialogueBlackboard;
    
    void Awake()
    {
        _dialogueBlackboard = new AsakiBlackboard();
        
        // 初始化默认值
        _dialogueBlackboard.SetValue(DialogueBlackboardKeys.PlayerName, "Adventurer");
        _dialogueBlackboard.SetValue(DialogueBlackboardKeys.FactionReputation, 0);
        _dialogueBlackboard.SetValue(DialogueBlackboardKeys.HasKeyItem, false);
    }
    
    public void StartDialogue(string npcName, string startNodeID)
    {
        _dialogueBlackboard.SetValue(DialogueBlackboardKeys.NPCName, npcName);
        _dialogueBlackboard.SetValue(DialogueBlackboardKeys.CurrentNodeID, startNodeID);
        _dialogueBlackboard.SetValue(DialogueBlackboardKeys.SelectedOption, -1);
        
        Debug.Log($"Started dialogue with {npcName}");
    }
    
    public void SelectOption(int optionIndex)
    {
        _dialogueBlackboard.SetValue(DialogueBlackboardKeys.SelectedOption, optionIndex);
        
        // 根据选择更新状态
        ProcessDialogueChoice(optionIndex);
    }
    
    void ProcessDialogueChoice(int optionIndex)
    {
        string currentNode = _dialogueBlackboard.GetValue<string>(DialogueBlackboardKeys.CurrentNodeID);
        int reputation = _dialogueBlackboard.GetValue<int>(DialogueBlackboardKeys.FactionReputation);
        
        // 示例：根据节点选择更新声望
        switch (currentNode)
        {
            case "Quest_Accept":
                _dialogueBlackboard.SetValue(DialogueBlackboardKeys.FactionReputation, reputation + 10);
                Debug.Log("Reputation increased by 10");
                break;
            case "Quest_Refuse":
                _dialogueBlackboard.SetValue(DialogueBlackboardKeys.FactionReputation, reputation - 5);
                Debug.Log("Reputation decreased by 5");
                break;
        }
    }
    
    public void SetKeyItem(bool hasItem)
    {
        _dialogueBlackboard.SetValue(DialogueBlackboardKeys.HasKeyItem, hasItem);
    }
    
    // 检查对话条件
    public bool CheckCondition(string condition)
    {
        switch (condition)
        {
            case "HasKeyItem":
                return _dialogueBlackboard.GetValue<bool>(DialogueBlackboardKeys.HasKeyItem);
            case "HighReputation":
                return _dialogueBlackboard.GetValue<int>(DialogueBlackboardKeys.FactionReputation) >= 50;
            default:
                return false;
        }
    }
}
```

### 3.8 全局黑板资产示例

#### 示例 10：全局配置管理

```csharp
using Asaki.Core.Blackboard;
using Asaki.Core.Blackboard.Variables;
using Asaki.Core.Graphs;
using UnityEngine;

public class GlobalConfigManager : MonoBehaviour
{
    [SerializeField] private AsakiGlobalBlackboardAsset _globalConfig;
    
    void Awake()
    {
        InitializeGlobalConfig();
    }
    
    void InitializeGlobalConfig()
    {
        // 游戏难度
        var difficultyVar = _globalConfig.GetOrCreateVariable("GameDifficulty", typeof(AsakiString));
        if (string.IsNullOrEmpty(((AsakiString)difficultyVar.ValueData).Value))
        {
            ((AsakiString)difficultyVar.ValueData).Value = "Normal";
        }
        
        // 玩家初始生命值
        var playerHealthVar = _globalConfig.GetOrCreateVariable("PlayerBaseHealth", typeof(AsakiInt));
        if (((AsakiInt)playerHealthVar.ValueData).Value == 0)
        {
            ((AsakiInt)playerHealthVar.ValueData).Value = 100;
        }
        
        // 敌人伤害倍率
        var damageMultVar = _globalConfig.GetOrCreateVariable("EnemyDamageMultiplier", typeof(AsakiFloat));
        if (((AsakiFloat)damageMultVar.ValueData).Value == 0f)
        {
            ((AsakiFloat)damageMultVar.ValueData).Value = 1.0f;
        }
        
        // 游戏速度
        var gameSpeedVar = _globalConfig.GetOrCreateVariable("GameTimeScale", typeof(AsakiFloat));
        if (((AsakiFloat)gameSpeedVar.ValueData).Value == 0f)
        {
            ((AsakiFloat)gameSpeedVar.ValueData).Value = 1.0f;
        }
        
        // 是否开启教程
        var tutorialVar = _globalConfig.GetOrCreateVariable("ShowTutorial", typeof(AsakiBool));
        // 默认开启
    }
    
    public void SetDifficulty(string difficulty)
    {
        var var_def = _globalConfig.GetOrCreateVariable("GameDifficulty", typeof(AsakiString));
        ((AsakiString)var_def.ValueData).Value = difficulty;
        
        // 根据难度调整其他参数
        switch (difficulty)
        {
            case "Easy":
                SetDamageMultiplier(0.5f);
                SetBaseHealth(150);
                break;
            case "Normal":
                SetDamageMultiplier(1.0f);
                SetBaseHealth(100);
                break;
            case "Hard":
                SetDamageMultiplier(1.5f);
                SetBaseHealth(80);
                break;
        }
    }
    
    void SetDamageMultiplier(float mult)
    {
        var var_def = _globalConfig.GetOrCreateVariable("EnemyDamageMultiplier", typeof(AsakiFloat));
        ((AsakiFloat)var_def.ValueData).Value = mult;
    }
    
    void SetBaseHealth(int health)
    {
        var var_def = _globalConfig.GetOrCreateVariable("PlayerBaseHealth", typeof(AsakiInt));
        ((AsakiInt)var_def.ValueData).Value = health;
    }
    
    // 获取配置值
    public T GetConfig<T>(string key)
    {
        var var_def = _globalConfig.GlobalVariables.Find(v => v.Name == key);
        if (var_def?.ValueData is AsakiValue<T> value)
        {
            return value.Value;
        }
        return default;
    }
}
```

### 3.9 调试与性能分析示例

#### 示例 11：性能分析

```csharp
#if UNITY_EDITOR
using Asaki.Core.Blackboard;
using UnityEngine;

public class BlackboardProfilingExample : MonoBehaviour
{
    private IAsakiBlackboard _blackboard;
    
    void Start()
    {
        _blackboard = new AsakiBlackboard();
        
        // 启用性能分析
        BlackboardProfiler.Enable();
        
        // 执行大量操作
        RunPerformanceTest();
        
        // 打印报告
        Invoke(nameof(PrintReport), 5f);
    }
    
    void RunPerformanceTest()
    {
        // 模拟高频访问
        for (int i = 0; i < 10000; i++)
        {
            _blackboard.SetValue($"Key_{i % 100}", i);
            _blackboard.GetValue<int>($"Key_{i % 100}");
        }
    }
    
    void PrintReport()
    {
        BlackboardProfiler.PrintReport();
        
        // 获取详细统计
        var stats = BlackboardProfiler.GetStats();
        foreach (var kvp in stats)
        {
            Debug.Log($"Key: {kvp.Key}, Access Count: {kvp.Value.AccessCount}");
        }
        
        // 禁用分析
        BlackboardProfiler.Disable();
    }
}
#endif
```

### 3.10 完整游戏示例

#### 示例 12：RPG 游戏状态管理

```csharp
using Asaki.Core.Blackboard;
using Asaki.Core.Blackboard.Variables;
using Asaki.Core.Reactive;
using UnityEngine;

public class RPGGameManager : MonoBehaviour
{
    public static RPGGameManager Instance { get; private set; }
    
    [SerializeField] private AsakiGlobalBlackboardAsset _globalBlackboard;
    
    // 游戏状态黑板
    private IAsakiBlackboard _gameState;
    
    // 常用属性的快捷访问
    public AsakiProperty<int> PlayerLevel { get; private set; }
    public AsakiProperty<int> PlayerExp { get; private set; }
    public AsakiProperty<int> PlayerGold { get; private set; }
    public AsakiProperty<int> PlayerHealth { get; private set; }
    public AsakiProperty<int> PlayerMaxHealth { get; private set; }
    public AsakiProperty<bool> IsPaused { get; private set; }
    
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeGameState();
    }
    
    void InitializeGameState()
    {
        _gameState = new AsakiBlackboard();
        
        // 初始化属性
        PlayerLevel = _gameState.GetProperty<int>("PlayerLevel");
        PlayerExp = _gameState.GetProperty<int>("PlayerExp");
        PlayerGold = _gameState.GetProperty<int>("PlayerGold");
        PlayerHealth = _gameState.GetProperty<int>("PlayerHealth");
        PlayerMaxHealth = _gameState.GetProperty<int>("PlayerMaxHealth");
        IsPaused = _gameState.GetProperty<bool>("IsPaused");
        
        // 设置初始值
        PlayerLevel.Value = 1;
        PlayerExp.Value = 0;
        PlayerGold.Value = 0;
        PlayerMaxHealth.Value = 100;
        PlayerHealth.Value = 100;
        IsPaused.Value = false;
        
        // 订阅事件
        PlayerExp.OnValueChanged += OnExpChanged;
        PlayerHealth.OnValueChanged += OnHealthChanged;
    }
    
    void OnExpChanged(int oldExp, int newExp)
    {
        int requiredExp = GetRequiredExpForLevel(PlayerLevel.Value);
        
        if (newExp >= requiredExp)
        {
            LevelUp();
        }
    }
    
    void LevelUp()
    {
        PlayerLevel.Value++;
        PlayerExp.Value -= GetRequiredExpForLevel(PlayerLevel.Value - 1);
        
        // 升级奖励
        PlayerMaxHealth.Value += 10;
        PlayerHealth.Value = PlayerMaxHealth.Value;
        
        Debug.Log($"Level Up! Now Level {PlayerLevel.Value}");
    }
    
    void OnHealthChanged(int oldHealth, int newHealth)
    {
        if (newHealth <= 0)
        {
            GameOver();
        }
    }
    
    void GameOver()
    {
        Debug.Log("Game Over!");
        IsPaused.Value = true;
        // 显示游戏结束界面
    }
    
    int GetRequiredExpForLevel(int level)
    {
        return level * 100;
    }
    
    // 公共方法
    public void AddGold(int amount)
    {
        PlayerGold.Value += amount;
    }
    
    public void AddExp(int amount)
    {
        PlayerExp.Value += amount;
    }
    
    public void TakeDamage(int damage)
    {
        PlayerHealth.Value = Mathf.Max(0, PlayerHealth.Value - damage);
    }
    
    public void Heal(int amount)
    {
        PlayerHealth.Value = Mathf.Min(PlayerMaxHealth.Value, PlayerHealth.Value + amount);
    }
    
    public void SetPause(bool paused)
    {
        IsPaused.Value = paused;
        Time.timeScale = paused ? 0f : 1f;
    }
}
```

---

## 附录

### A. 常见错误与解决方案

| 错误 | 原因 | 解决方案 |
|------|------|----------|
| `InvalidCastException` | 类型不匹配 | 确保 `GetValue<T>` 的 T 与 `SetValue` 时的类型一致 |
| `KeyNotFoundException` | 键不存在且返回 null | 使用 `HasKey` 检查或使用默认值 |
| 内存泄漏 | 未取消事件订阅 | 在 `OnDestroy` 中取消 `OnValueChanged` 订阅 |
| 性能问题 | 频繁单个操作 | 使用 `BeginBatch` 批量操作 |

### B. 最佳实践清单

- [ ] 使用常量或静态只读字段管理键名
- [ ] 在 `OnDestroy` 中取消事件订阅
- [ ] 使用 `BeginBatch` 进行批量操作
- [ ] 合理使用父子作用域避免数据污染
- [ ] 对重要变量添加约束验证
- [ ] 使用泛型 API 避免装箱拆箱
- [ ] 在 Editor 下使用 `BlackboardProfiler` 分析性能

### C. 类型映射表

| C# 类型 | Asaki 类型 | 用途 |
|---------|-----------|------|
| `int` | `AsakiInt` | 整数计数、ID |
| `float` | `AsakiFloat` | 百分比、时间、坐标 |
| `bool` | `AsakiBool` | 状态标记、开关 |
| `string` | `AsakiString` | 名称、描述、ID |
| `Vector3` | `AsakiVector3` | 位置、方向 |
| `Vector2` | `AsakiVector2` | UI 坐标、2D 位置 |
| `Color` | `AsakiColor` | 颜色配置 |
| `GameObject` | `AsakiGameObject` | 对象引用 |
