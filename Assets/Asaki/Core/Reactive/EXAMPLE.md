# Asaki Reactive 使用指南与示例

本文档提供 Asaki Reactive 系统的详细使用指南，包括推荐使用场景、完整 API 参考以及丰富的代码示例。

---

## 第一部分：使用用途推荐

### 1.1 适用场景

Asaki Reactive 特别适合以下开发场景：

#### UI 数据绑定
游戏开发中最常见的使用场景，将游戏数据与 UI 元素自动同步。

```csharp
// ViewModel
public class PlayerViewModel
{
    public AsakiProperty<int> Health { get; } = new(100);
    public AsakiProperty<int> MaxHealth { get; } = new(100);
    public AsakiProperty<string> PlayerName { get; } = new("Player");
}

// View (MonoBehaviour)
public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Slider healthSlider;

    void Start()
    {
        var vm = GameState.Instance.PlayerVM;

        // 自动绑定，无需手动取消订阅
        vm.Health.Subscribe(this, value => UpdateHealth(value, vm.MaxHealth.Value));
        vm.MaxHealth.Subscribe(this, value => UpdateHealth(vm.Health.Value, value));
        vm.PlayerName.Subscribe(this, value => nameText.text = value);
    }

    void UpdateHealth(int health, int maxHealth)
    {
        healthText.text = $"{health}/{maxHealth}";
        healthSlider.value = (float)health / maxHealth;
    }
}
```

#### 游戏状态管理
集中管理游戏状态，多个系统可以订阅状态变化。

```csharp
public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    // 游戏状态
    public AsakiProperty<GamePhase> CurrentPhase { get; } = new(GamePhase.Menu);
    public AsakiProperty<bool> IsPaused { get; } = new(false);
    public AsakiProperty<int> CurrentLevel { get; } = new(1);

    void Awake()
    {
        Instance = this;

        // 状态变化时执行相应逻辑
        CurrentPhase.Subscribe(this, OnPhaseChanged);
        IsPaused.Subscribe(this, OnPauseChanged);
    }

    void OnPhaseChanged(GamePhase phase)
    {
        switch (phase)
        {
            case GamePhase.Playing:
                Time.timeScale = 1f;
                break;
            case GamePhase.Paused:
                Time.timeScale = 0f;
                break;
            case GamePhase.GameOver:
                ShowGameOverScreen();
                break;
        }
    }
}
```

#### 事件系统替代
替代传统的 C# 事件系统，提供更清晰的订阅管理机制。

```csharp
// 传统事件方式
public class PlayerOld
{
    public event Action<int> OnHealthChanged;
    public event Action OnDeath;
}

// Reactive 方式
public class PlayerNew
{
    public AsakiProperty<int> Health { get; } = new(100);
    public AsakiProperty<bool> IsDead { get; } = new(false);
}

// 使用对比
public class Example : MonoBehaviour
{
    void UseOldWay(PlayerOld player)
    {
        // 需要手动取消订阅，容易忘记
        player.OnHealthChanged += OnHealthChanged;
        player.OnDeath += OnDeath;
    }

    void OnDestroy()
    {
        // 必须手动取消订阅，否则内存泄漏
        // player.OnHealthChanged -= OnHealthChanged;
        // player.OnDeath -= OnDeath;
    }

    void UseNewWay(PlayerNew player)
    {
        // 自动生命周期管理，无需手动取消订阅
        player.Health.Subscribe(this, value => Debug.Log($"Health: {value}"));
        player.IsDead.Subscribe(this, dead =>
        {
            if (dead) Debug.Log("Player died!");
        });
    }
    // 无需 OnDestroy，自动清理
}
```

#### 配置和热更新
游戏配置数据的动态更新。

```csharp
public class GameConfig
{
    public AsakiProperty<float> MasterVolume { get; } = new(1f);
    public AsakiProperty<float> MusicVolume { get; } = new(0.8f);
    public AsakiProperty<float> SFXVolume { get; } = new(0.8f);
}

public class AudioManager : MonoBehaviour
{
    void Start()
    {
        var config = GameConfig.Instance;

        // 音量变化时自动应用
        config.MasterVolume.Subscribe(this, ApplyMasterVolume);
        config.MusicVolume.Subscribe(this, ApplyMusicVolume);
        config.SFXVolume.Subscribe(this, ApplySFXVolume);
    }

    void ApplyMasterVolume(float volume)
    {
        AudioListener.volume = volume;
    }
}
```

### 1.2 不适用场景

以下场景不建议使用 Asaki Reactive：

| 场景 | 原因 | 替代方案 |
|------|------|----------|
| 高频更新数据（每帧变化） | 通知开销较大 | 直接访问字段或属性 |
| 临时一次性数据 | 增加不必要的复杂度 | 普通字段或属性 |
| 大量只读数据 | 不需要变化通知 | 普通属性或只读字段 |
| 跨线程频繁通信 | 非线程安全设计 | 使用 Unity 的 MainThreadDispatcher |
| 作为字典键 | 可变对象，哈希码会变化 | 使用不可变类型作为键 |

---

## 第二部分：Reactive 系统 API

### 2.1 AsakiProperty&lt;T&gt;

泛型可观察属性类，包装任意类型的值并提供变化通知机制。

#### 命名空间
```csharp
using Asaki.Core.Reactive;
```

#### 继承关系
```
System.Object
  └── AsakiProperty<T>
        └── IEquatable<AsakiProperty<T>>
        └── IAsakiPropertyBase
```

#### 构造函数

```csharp
// 使用默认值初始化
var prop1 = new AsakiProperty<int>();

// 使用指定值初始化
var prop2 = new AsakiProperty<string>("Hello");
var prop3 = new AsakiProperty<Vector3>(Vector3.zero);
```

#### 属性

| 属性 | 类型 | 访问 | 说明 |
|------|------|------|------|
| `Value` | `T` | get; set; | 获取或设置属性值，设置时自动通知订阅者 |
| `ValueType` | `Type` | get; | 获取值的类型（来自 IAsakiPropertyBase） |

#### 方法

##### Subscribe(Action&lt;T&gt; action)
订阅值变化事件。

```csharp
public IDisposable Subscribe(Action<T> action)
```

**参数：**
- `action`: 值变化时调用的委托

**返回值：**
- `IDisposable`: 订阅凭证，Dispose 时取消订阅

**特性：**
- 订阅时立即用当前值调用一次 action
- 返回的 IDisposable 可用于取消订阅

**示例：**
```csharp
var health = new AsakiProperty<int>(100);

// 基础订阅
var subscription = health.Subscribe(value =>
{
    Debug.Log($"Health: {value}");
});

// 取消订阅
subscription.Dispose();

// 或使用 using
using (health.Subscribe(value => Debug.Log(value)))
{
    // 订阅生效范围
}
```

##### Subscribe(MonoBehaviour owner, Action&lt;T&gt; action)
订阅并绑定到 MonoBehaviour 生命周期。

```csharp
public IDisposable Subscribe(MonoBehaviour owner, Action<T> action)
```

**参数：**
- `owner`: 订阅者所属的 MonoBehaviour
- `action`: 值变化时调用的委托

**特性：**
- 当 owner 被销毁时自动取消订阅
- 推荐在 MonoBehaviour 中使用

**示例：**
```csharp
public class PlayerUI : MonoBehaviour
{
    void Start()
    {
        // 自动生命周期管理
        GameState.Instance.Health.Subscribe(this, UpdateHealthBar);
    }

    void UpdateHealthBar(int health)
    {
        healthBar.value = health;
    }
    // 无需手动取消订阅
}
```

##### Unsubscribe(Action&lt;T&gt; action)
取消订阅值变化事件。

```csharp
public void Unsubscribe(Action<T> action)
```

**参数：**
- `action`: 要取消的委托

**示例：**
```csharp
Action<int> callback = value => Debug.Log(value);

health.Subscribe(callback);
health.Value = 80;  // 输出

health.Unsubscribe(callback);
health.Value = 50;  // 不输出
```

##### Bind(IAsakiObserver&lt;T&gt; observer)
绑定观察者接口。

```csharp
public IDisposable Bind(IAsakiObserver<T> observer)
```

**参数：**
- `observer`: 实现了 IAsakiObserver<T> 的观察者

**返回值：**
- `IDisposable`: 绑定凭证，Dispose 时解除绑定

**特性：**
- 绑定前检查是否已存在，避免重复
- 绑定时立即用当前值调用一次 OnValueChange

**示例：**
```csharp
public class HealthObserver : IAsakiObserver<int>
{
    public void OnValueChange(int value)
    {
        Debug.Log($"Health changed to: {value}");
    }
}

var observer = new HealthObserver();
var binding = health.Bind(observer);

// 解除绑定
binding.Dispose();
```

##### Unbind(IAsakiObserver&lt;T&gt; observer)
解除观察者绑定。

```csharp
public void Unbind(IAsakiObserver<T> observer)
```

**参数：**
- `observer`: 要解除绑定的观察者

##### Dispose()
释放所有订阅和绑定。

```csharp
public void Dispose()
```

**说明：**
- 清除所有委托订阅
- 清除所有观察者绑定
- 实现 IDisposable 接口

**示例：**
```csharp
var property = new AsakiProperty<int>(100);
property.Subscribe(value => Debug.Log(value));
property.Bind(new MyObserver());

// 释放所有
property.Dispose();
```

##### InvokeCallback(object value)
从基接口调用的回调方法。

```csharp
public void InvokeCallback(object value)
```

**说明：**
- 主要用于内部和反射场景
- 更新内部值并通知所有订阅者

#### 运算符重载

##### 隐式转换
```csharp
public static implicit operator T(AsakiProperty<T> property)
```

**示例：**
```csharp
var prop = new AsakiProperty<int>(10);
int value = prop;  // 隐式转换，value = 10
```

##### 相等性比较
```csharp
public static bool operator ==(AsakiProperty<T> left, AsakiProperty<T> right)
public static bool operator !=(AsakiProperty<T> left, AsakiProperty<T> right)
public static bool operator ==(T left, AsakiProperty<T> right)
public static bool operator !=(T left, AsakiProperty<T> right)
public static bool operator ==(AsakiProperty<T> left, T right)
public static bool operator !=(AsakiProperty<T> left, T right)
```

**示例：**
```csharp
var prop1 = new AsakiProperty<int>(10);
var prop2 = new AsakiProperty<int>(10);
var prop3 = new AsakiProperty<int>(20);

// 属性间比较
bool same = prop1 == prop2;  // true
bool diff = prop1 != prop3;  // true

// 与值比较
bool equal = prop1 == 10;    // true
bool notEqual = 20 != prop1; // true
```

#### 其他方法

##### Equals
```csharp
public bool Equals(AsakiProperty<T> other)
public override bool Equals(object obj)
```

##### ToString
```csharp
public override string ToString()
```

**示例：**
```csharp
var prop = new AsakiProperty<int>(42);
string str = prop.ToString();  // "42"
```

##### GetHashCode
```csharp
public override int GetHashCode()
```

**注意：** 始终抛出 NotSupportedException，因为可变对象不适合作为字典键。

### 2.2 IAsakiObserver&lt;T&gt;

观察者接口，用于接收属性值变化通知。

#### 定义
```csharp
public interface IAsakiObserver<T>
{
    void OnValueChange(T value);
}
```

#### 方法

##### OnValueChange(T value)
当观察的值发生变化时调用。

**参数：**
- `value`: 变化后的新值

**示例：**
```csharp
public class ScoreObserver : IAsakiObserver<int>
{
    private int _highScore;

    public void OnValueChange(int value)
    {
        if (value > _highScore)
        {
            _highScore = value;
            Debug.Log($"New high score: {_highScore}");
        }
    }
}
```

### 2.3 IAsakiPropertyBase

属性基接口，提供非泛型的属性访问能力。

#### 定义
```csharp
public interface IAsakiPropertyBase : IDisposable
{
    void InvokeCallback(object value);
    Type ValueType { get; }
}
```

#### 用途
- 类型擦除场景
- 通用属性容器
- 反射操作

**示例：**
```csharp
public class PropertyContainer
{
    private List<IAsakiPropertyBase> _properties = new();

    public void AddProperty<T>(AsakiProperty<T> property)
    {
        _properties.Add(property);
    }

    public void ResetAll()
    {
        foreach (var prop in _properties)
        {
            prop.Dispose();
        }
    }
}
```

### 2.4 AsakiBindingTracker

绑定生命周期追踪器，自动管理订阅的生命周期。

#### 定义
```csharp
[AddComponentMenu("")]
[DisallowMultipleComponent]
public class AsakiBindingTracker : MonoBehaviour
```

#### 方法

##### Track(IDisposable subscription)
追踪一个订阅。

```csharp
public void Track(IDisposable subscription)
```

**参数：**
- `subscription`: 要追踪的订阅凭证

**说明：**
- 当 GameObject 被销毁时自动释放
- 如果 Tracker 已被销毁，立即释放订阅

##### ReleaseAll()
立即释放所有追踪的订阅。

```csharp
public void ReleaseAll()
```

**说明：**
- 遍历所有订阅并调用 Dispose
- 捕获并记录异常，不影响其他订阅
- 清空订阅列表

#### 生命周期
```csharp
private void OnDestroy()
{
    _isDestroyed = true;
    ReleaseAll();
}
```

---

## 第三部分：使用示例

### 3.1 基础示例

#### 示例 1：简单的计数器
```csharp
using Asaki.Core.Reactive;
using UnityEngine;

public class CounterExample : MonoBehaviour
{
    private AsakiProperty<int> _counter = new(0);

    void Start()
    {
        // 订阅计数器变化
        _counter.Subscribe(this, value =>
        {
            Debug.Log($"Counter: {value}");
        });
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _counter.Value++;
        }
    }
}
```

#### 示例 2：玩家生命值系统
```csharp
using Asaki.Core.Reactive;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public AsakiProperty<int> Health { get; } = new(100);
    public AsakiProperty<int> MaxHealth { get; } = new(100);
    public AsakiProperty<bool> IsDead { get; } = new(false);

    void Start()
    {
        // 生命值变化时检查死亡
        Health.Subscribe(this, CheckDeath);

        // 死亡状态变化时执行逻辑
        IsDead.Subscribe(this, OnDeathStateChanged);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead.Value) return;

        Health.Value = Mathf.Max(0, Health.Value - damage);
    }

    public void Heal(int amount)
    {
        if (IsDead.Value) return;

        Health.Value = Mathf.Min(MaxHealth.Value, Health.Value + amount);
    }

    void CheckDeath(int health)
    {
        if (health <= 0 && !IsDead.Value)
        {
            IsDead.Value = true;
        }
    }

    void OnDeathStateChanged(bool dead)
    {
        if (dead)
        {
            Debug.Log("Player has died!");
            // 播放死亡动画、显示游戏结束界面等
        }
    }
}
```

### 3.2 UI 绑定示例

#### 示例 3：完整的 HUD 系统
```csharp
using Asaki.Core.Reactive;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ViewModel
public class PlayerData
{
    public AsakiProperty<string> PlayerName { get; } = new("Player 1");
    public AsakiProperty<int> Level { get; } = new(1);
    public AsakiProperty<int> Experience { get; } = new(0);
    public AsakiProperty<int> ExperienceToNext { get; } = new(100);
    public AsakiProperty<int> Health { get; } = new(100);
    public AsakiProperty<int> MaxHealth { get; } = new(100);
    public AsakiProperty<int> Mana { get; } = new(50);
    public AsakiProperty<int> MaxMana { get; } = new(50);
    public AsakiProperty<int> Gold { get; } = new(0);
}

// View
public class PlayerHUD : MonoBehaviour
{
    [Header("Info")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Health")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Mana")]
    [SerializeField] private Slider manaSlider;
    [SerializeField] private TextMeshProUGUI manaText;

    [Header("Experience")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI expText;

    [Header("Currency")]
    [SerializeField] private TextMeshProUGUI goldText;

    void Start()
    {
        var player = GameManager.Instance.PlayerData;

        // 基本信息
        player.PlayerName.Subscribe(this, value => nameText.text = value);
        player.Level.Subscribe(this, value => levelText.text = $"Lv.{value}");

        // 生命值
        player.Health.Subscribe(this, _ => UpdateHealth(player));
        player.MaxHealth.Subscribe(this, _ => UpdateHealth(player));

        // 法力值
        player.Mana.Subscribe(this, _ => UpdateMana(player));
        player.MaxMana.Subscribe(this, _ => UpdateMana(player));

        // 经验值
        player.Experience.Subscribe(this, _ => UpdateExp(player));
        player.ExperienceToNext.Subscribe(this, _ => UpdateExp(player));

        // 金币
        player.Gold.Subscribe(this, value => goldText.text = $"{value:N0}");
    }

    void UpdateHealth(PlayerData player)
    {
        float percent = (float)player.Health.Value / player.MaxHealth.Value;
        healthSlider.value = percent;
        healthText.text = $"{player.Health.Value}/{player.MaxHealth.Value}";
    }

    void UpdateMana(PlayerData player)
    {
        float percent = (float)player.Mana.Value / player.MaxMana.Value;
        manaSlider.value = percent;
        manaText.text = $"{player.Mana.Value}/{player.MaxMana.Value}";
    }

    void UpdateExp(PlayerData player)
    {
        float percent = (float)player.Experience.Value / player.ExperienceToNext.Value;
        expSlider.value = percent;
        expText.text = $"{player.Experience.Value}/{player.ExperienceToNext.Value}";
    }
}
```

### 3.3 游戏状态管理示例

#### 示例 4：游戏流程控制器
```csharp
using Asaki.Core.Reactive;
using UnityEngine;

public enum GameState
{
    Menu,
    Loading,
    Playing,
    Paused,
    GameOver,
    Victory
}

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    public AsakiProperty<GameState> CurrentState { get; } = new(GameState.Menu);
    public AsakiProperty<float> GameTime { get; } = new(0f);
    public AsakiProperty<bool> IsGameActive { get; } = new(false);

    void Awake()
    {
        Instance = this;

        // 状态变化时更新游戏活动状态
        CurrentState.Subscribe(this, state =>
        {
            IsGameActive.Value = state == GameState.Playing;
        });

        // 监听状态变化
        CurrentState.Subscribe(this, OnStateChanged);
    }

    void Update()
    {
        if (IsGameActive.Value)
        {
            GameTime.Value += Time.deltaTime;
        }
    }

    void OnStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Menu:
                Time.timeScale = 1f;
                ShowMenuUI();
                break;

            case GameState.Loading:
                Time.timeScale = 1f;
                ShowLoadingUI();
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                HideAllMenus();
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                ShowPauseMenu();
                break;

            case GameState.GameOver:
                Time.timeScale = 0.5f;
                ShowGameOverScreen();
                break;

            case GameState.Victory:
                Time.timeScale = 0.5f;
                ShowVictoryScreen();
                break;
        }
    }

    public void StartGame()
    {
        CurrentState.Value = GameState.Loading;
        // 加载完成后
        CurrentState.Value = GameState.Playing;
    }

    public void PauseGame()
    {
        if (CurrentState.Value == GameState.Playing)
        {
            CurrentState.Value = GameState.Paused;
        }
    }

    public void ResumeGame()
    {
        if (CurrentState.Value == GameState.Paused)
        {
            CurrentState.Value = GameState.Playing;
        }
    }

    public void GameOver()
    {
        CurrentState.Value = GameState.GameOver;
    }

    public void Victory()
    {
        CurrentState.Value = GameState.Victory;
    }

    public void ReturnToMenu()
    {
        CurrentState.Value = GameState.Menu;
    }
}
```

### 3.4 观察者模式示例

#### 示例 5：成就系统
```csharp
using Asaki.Core.Reactive;
using UnityEngine;

// 成就数据
public class Achievement
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool Unlocked { get; set; }
}

// 成就观察者
public class AchievementObserver : IAsakiObserver<int>
{
    private readonly string _achievementId;
    private readonly int _threshold;
    private readonly string _achievementName;
    private bool _unlocked;

    public AchievementObserver(string id, string name, int threshold)
    {
        _achievementId = id;
        _achievementName = name;
        _threshold = threshold;
    }

    public void OnValueChange(int value)
    {
        if (!_unlocked && value >= _threshold)
        {
            _unlocked = true;
            UnlockAchievement();
        }
    }

    void UnlockAchievement()
    {
        Debug.Log($"Achievement Unlocked: {_achievementName}!");
        // 显示成就弹窗、保存到本地等
    }
}

// 游戏统计
public class GameStats : MonoBehaviour
{
    public static GameStats Instance { get; private set; }

    public AsakiProperty<int> EnemiesKilled { get; } = new(0);
    public AsakiProperty<int> GoldCollected { get; } = new(0);
    public AsakiProperty<int> DistanceTraveled { get; } = new(0);
    public AsakiProperty<int> ItemsCollected { get; } = new(0);

    void Awake()
    {
        Instance = this;
        SetupAchievements();
    }

    void SetupAchievements()
    {
        // 击杀成就
        EnemiesKilled.Bind(new AchievementObserver("kill_10", "Novice Hunter", 10));
        EnemiesKilled.Bind(new AchievementObserver("kill_100", "Expert Hunter", 100));
        EnemiesKilled.Bind(new AchievementObserver("kill_1000", "Master Hunter", 1000));

        // 金币成就
        GoldCollected.Bind(new AchievementObserver("gold_100", "Treasure Seeker", 100));
        GoldCollected.Bind(new AchievementObserver("gold_1000", "Treasure Hunter", 1000));
        GoldCollected.Bind(new AchievementObserver("gold_10000", "Treasure Master", 10000));
    }

    public void AddKill()
    {
        EnemiesKilled.Value++;
    }

    public void AddGold(int amount)
    {
        GoldCollected.Value += amount;
    }
}
```

### 3.5 高级示例

#### 示例 6：计算属性（派生属性）
```csharp
using Asaki.Core.Reactive;
using UnityEngine;

public class CharacterStats
{
    // 基础属性
    public AsakiProperty<int> Strength { get; } = new(10);
    public AsakiProperty<int> Agility { get; } = new(10);
    public AsakiProperty<int> Intelligence { get; } = new(10);
    public AsakiProperty<int> Vitality { get; } = new(10);

    // 等级
    public AsakiProperty<int> Level { get; } = new(1);

    // 派生属性（计算属性）
    public AsakiProperty<int> MaxHealth { get; }
    public AsakiProperty<int> MaxMana { get; }
    public AsakiProperty<int> AttackPower { get; }
    public AsakiProperty<int> Defense { get; }
    public AsakiProperty<float> MoveSpeed { get; }

    public CharacterStats()
    {
        // 初始化派生属性
        MaxHealth = new AsakiProperty<int>(CalculateMaxHealth());
        MaxMana = new AsakiProperty<int>(CalculateMaxMana());
        AttackPower = new AsakiProperty<int>(CalculateAttackPower());
        Defense = new AsakiProperty<int>(CalculateDefense());
        MoveSpeed = new AsakiProperty<float>(CalculateMoveSpeed());

        // 订阅基础属性变化，更新派生属性
        Strength.Subscribe(_ => UpdateDerivedStats());
        Agility.Subscribe(_ => UpdateDerivedStats());
        Intelligence.Subscribe(_ => UpdateDerivedStats());
        Vitality.Subscribe(_ => UpdateDerivedStats());
        Level.Subscribe(_ => UpdateDerivedStats());
    }

    void UpdateDerivedStats()
    {
        MaxHealth.Value = CalculateMaxHealth();
        MaxMana.Value = CalculateMaxMana();
        AttackPower.Value = CalculateAttackPower();
        Defense.Value = CalculateDefense();
        MoveSpeed.Value = CalculateMoveSpeed();
    }

    int CalculateMaxHealth() => Vitality.Value * 10 + Level.Value * 5;
    int CalculateMaxMana() => Intelligence.Value * 10 + Level.Value * 3;
    int CalculateAttackPower() => Strength.Value * 2 + Level.Value;
    int CalculateDefense() => Vitality.Value + Level.Value / 2;
    float CalculateMoveSpeed() => 5f + Agility.Value * 0.1f;
}
```

#### 示例 7：属性组合与转换
```csharp
using Asaki.Core.Reactive;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public AsakiProperty<int> CurrentWeight { get; } = new(0);
    public AsakiProperty<int> MaxWeight { get; } = new(100);

    // 派生属性：负重百分比
    public AsakiProperty<float> EncumbrancePercent { get; }
    public AsakiProperty<bool> IsOverburdened { get; }
    public AsakiProperty<Color> WeightColor { get; }

    void Awake()
    {
        // 计算负重百分比
        EncumbrancePercent = new AsakiProperty<float>(0f);
        IsOverburdened = new AsakiProperty<bool>(false);
        WeightColor = new AsakiProperty<Color>(Color.green);

        void UpdateEncumbrance()
        {
            float percent = (float)CurrentWeight.Value / MaxWeight.Value;
            EncumbrancePercent.Value = percent;
            IsOverburdened.Value = percent > 1f;

            // 根据负重设置颜色
            if (percent < 0.5f)
                WeightColor.Value = Color.green;
            else if (percent < 0.8f)
                WeightColor.Value = Color.yellow;
            else if (percent < 1f)
                WeightColor.Value = new Color(1f, 0.5f, 0f); // 橙色
            else
                WeightColor.Value = Color.red;
        }

        CurrentWeight.Subscribe(this, _ => UpdateEncumbrance());
        MaxWeight.Subscribe(this, _ => UpdateEncumbrance());
    }

    public bool AddItem(Item item)
    {
        int newWeight = CurrentWeight.Value + item.Weight;
        if (newWeight > MaxWeight.Value)
        {
            Debug.Log("Cannot carry more items!");
            return false;
        }

        CurrentWeight.Value = newWeight;
        return true;
    }

    public void RemoveItem(Item item)
    {
        CurrentWeight.Value = Mathf.Max(0, CurrentWeight.Value - item.Weight);
    }
}
```

#### 示例 8：表单验证
```csharp
using Asaki.Core.Reactive;
using UnityEngine;

public class LoginForm : MonoBehaviour
{
    public AsakiProperty<string> Username { get; } = new("");
    public AsakiProperty<string> Password { get; } = new("");
    public AsakiProperty<string> ConfirmPassword { get; } = new("");

    // 验证状态
    public AsakiProperty<bool> IsUsernameValid { get; } = new(false);
    public AsakiProperty<bool> IsPasswordValid { get; } = new(false);
    public AsakiProperty<bool> DoPasswordsMatch { get; } = new(false);
    public AsakiProperty<bool> IsFormValid { get; } = new(false);

    // 错误信息
    public AsakiProperty<string> UsernameError { get; } = new("");
    public AsakiProperty<string> PasswordError { get; } = new("");

    void Start()
    {
        // 验证用户名
        Username.Subscribe(this, value =>
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                IsUsernameValid.Value = false;
                UsernameError.Value = "Username is required";
            }
            else if (value.Length < 3)
            {
                IsUsernameValid.Value = false;
                UsernameError.Value = "Username must be at least 3 characters";
            }
            else
            {
                IsUsernameValid.Value = true;
                UsernameError.Value = "";
            }
            UpdateFormValidity();
        });

        // 验证密码
        Password.Subscribe(this, value =>
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                IsPasswordValid.Value = false;
                PasswordError.Value = "Password is required";
            }
            else if (value.Length < 6)
            {
                IsPasswordValid.Value = false;
                PasswordError.Value = "Password must be at least 6 characters";
            }
            else
            {
                IsPasswordValid.Value = true;
                PasswordError.Value = "";
            }
            CheckPasswordMatch();
            UpdateFormValidity();
        });

        // 验证确认密码
        ConfirmPassword.Subscribe(this, _ =>
        {
            CheckPasswordMatch();
            UpdateFormValidity();
        });
    }

    void CheckPasswordMatch()
    {
        DoPasswordsMatch.Value = Password.Value == ConfirmPassword.Value;
    }

    void UpdateFormValidity()
    {
        IsFormValid.Value = IsUsernameValid.Value &&
                           IsPasswordValid.Value &&
                           DoPasswordsMatch.Value;
    }
}
```

### 3.6 实用工具示例

#### 示例 9：属性调试工具
```csharp
using Asaki.Core.Reactive;
using UnityEngine;

public static class ReactiveDebug
{
    /// <summary>
    /// 为属性添加日志订阅
    /// </summary>
    public static IDisposable Log<T>(this AsakiProperty<T> property, string label = null)
    {
        string name = label ?? $"Property<{typeof(T).Name}>";
        return property.Subscribe(value =>
        {
            Debug.Log($"[{name}] Changed to: {value}");
        });
    }

    /// <summary>
    /// 为属性添加条件日志
    /// </summary>
    public static IDisposable LogWhen<T>(
        this AsakiProperty<T> property,
        System.Func<T, bool> condition,
        string label = null)
    {
        string name = label ?? $"Property<{typeof(T).Name}>";
        return property.Subscribe(value =>
        {
            if (condition(value))
            {
                Debug.Log($"[{name}] Condition met with value: {value}");
            }
        });
    }
}

// 使用示例
public class DebugExample : MonoBehaviour
{
    void Start()
    {
        var health = new AsakiProperty<int>(100);
        var score = new AsakiProperty<int>(0);

        // 添加日志订阅
        health.Log("PlayerHealth");
        score.LogWhen(v => v > 1000, "HighScore");

        health.Value = 80;  // 输出: [PlayerHealth] Changed to: 80
        score.Value = 1500; // 输出: [HighScore] Condition met with value: 1500
    }
}
```

#### 示例 10：属性持久化
```csharp
using Asaki.Core.Reactive;
using UnityEngine;

public static class ReactivePersistence
{
    /// <summary>
    /// 绑定到 PlayerPrefs 自动保存
    /// </summary>
    public static IDisposable BindToPlayerPrefs(
        this AsakiProperty<int> property,
        string key,
        MonoBehaviour owner)
    {
        // 加载已保存的值
        if (PlayerPrefs.HasKey(key))
        {
            property.Value = PlayerPrefs.GetInt(key);
        }

        // 订阅变化自动保存
        return property.Subscribe(owner, value =>
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
        });
    }

    /// <summary>
    /// 绑定到 PlayerPrefs 自动保存（float 版本）
    /// </summary>
    public static IDisposable BindToPlayerPrefs(
        this AsakiProperty<float> property,
        string key,
        MonoBehaviour owner)
    {
        if (PlayerPrefs.HasKey(key))
        {
            property.Value = PlayerPrefs.GetFloat(key);
        }

        return property.Subscribe(owner, value =>
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
        });
    }

    /// <summary>
    /// 绑定到 PlayerPrefs 自动保存（string 版本）
    /// </summary>
    public static IDisposable BindToPlayerPrefs(
        this AsakiProperty<string> property,
        string key,
        MonoBehaviour owner)
    {
        if (PlayerPrefs.HasKey(key))
        {
            property.Value = PlayerPrefs.GetString(key);
        }

        return property.Subscribe(owner, value =>
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        });
    }
}

// 使用示例
public class Settings : MonoBehaviour
{
    public AsakiProperty<float> MasterVolume { get; } = new(1f);
    public AsakiProperty<float> MusicVolume { get; } = new(0.8f);
    public AsakiProperty<int> GraphicsQuality { get; } = new(2);

    void Start()
    {
        // 自动加载和保存设置
        MasterVolume.BindToPlayerPrefs("Settings_MasterVolume", this);
        MusicVolume.BindToPlayerPrefs("Settings_MusicVolume", this);
        GraphicsQuality.BindToPlayerPrefs("Settings_Quality", this);
    }
}
```

---

## 总结

Asaki Reactive 提供了一套完整的响应式编程解决方案，适用于 Unity 游戏开发中的各种场景。通过合理使用：

1. **UI 数据绑定** - 自动同步数据和界面
2. **状态管理** - 集中管理游戏状态
3. **事件系统** - 替代传统事件，自动生命周期管理
4. **观察者模式** - 实现复杂的观察逻辑

可以有效减少代码耦合，提高可维护性，防止内存泄漏。
