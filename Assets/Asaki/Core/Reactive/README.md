# Asaki Reactive - MVVM 响应式系统

Asaki Reactive 是 Asaki 框架的核心 MVVM（Model-View-ViewModel）响应式系统，提供了一套完整的可观察属性实现，支持值变化通知、自动生命周期管理和内存泄漏防护。

## 目录

- [概述](#概述)
- [核心组件](#核心组件)
- [快速开始](#快速开始)
- [API 参考](#api-参考)
- [高级用法](#高级用法)
- [最佳实践](#最佳实践)
- [性能优化](#性能优化)

## 概述

Asaki Reactive 实现了观察者设计模式，允许对象订阅属性值的变化事件。当属性值发生变化时，所有订阅者都会自动收到通知。该系统专为 Unity 环境优化，提供了：

- **值变化通知**：属性值变化时自动通知订阅者
- **生命周期管理**：与 MonoBehaviour 生命周期自动绑定
- **内存安全**：防止内存泄漏的自动清理机制
- **线程安全**：单线程环境下的安全值更新
- **类型安全**：泛型实现确保编译时类型检查

## 核心组件

### 1. AsakiProperty&lt;T&gt;

可观察属性的核心实现，包装任意类型的值并提供变化通知机制。

**特性：**
- 泛型实现，支持任意类型
- 自动相等性比较，避免重复通知
- 支持委托订阅和接口绑定两种方式
- 隐式类型转换，使用便捷
- 完整的相等性运算符重载

### 2. IAsakiObserver&lt;T&gt;

观察者接口，用于接收属性值变化通知。

**用途：**
- 实现该接口以创建自定义观察者
- 通过 `Bind()` 方法绑定到属性
- 在 `OnValueChange()` 中处理值变化

### 3. IAsakiPropertyBase

属性基接口，提供非泛型的属性访问能力。

**用途：**
- 类型擦除场景
- 通用属性管理
- 反射操作支持

### 4. AsakiBindingTracker

绑定生命周期追踪器，自动管理订阅的生命周期。

**特性：**
- 自动附加到 MonoBehaviour 游戏对象
- 在对象销毁时自动释放所有订阅
- 防止内存泄漏

## 快速开始

### 基础用法

```csharp
using Asaki.Core.Reactive;
using UnityEngine;

public class Example : MonoBehaviour
{
    void Start()
    {
        // 创建可观察属性
        var health = new AsakiProperty<int>(100);

        // 订阅值变化
        health.Subscribe(value =>
        {
            Debug.Log($"Health changed to: {value}");
        });

        // 更新值，会自动通知订阅者
        health.Value = 80;  // 输出: Health changed to: 80
        health.Value = 50;  // 输出: Health changed to: 50
    }
}
```

### 使用 using 语句自动取消订阅

```csharp
public class Example : MonoBehaviour
{
    void Start()
    {
        var score = new AsakiProperty<int>(0);

        // 使用 using 语句，超出作用域自动取消订阅
        using (var subscription = score.Subscribe(value =>
        {
            Debug.Log($"Score: {value}");
        }))
        {
            score.Value = 100;  // 输出: Score: 100
        }

        // 已取消订阅，不会输出
        score.Value = 200;
    }
}
```

### 绑定到 MonoBehaviour 生命周期

```csharp
public class PlayerView : MonoBehaviour
{
    void Start()
    {
        // 自动绑定到生命周期，无需手动取消订阅
        // 当此 MonoBehaviour 被销毁时，订阅自动释放
        GameState.Instance.Health.Subscribe(this, value =>
        {
            UpdateHealthUI(value);
        });

        GameState.Instance.Mana.Subscribe(this, value =>
        {
            UpdateManaUI(value);
        });
    }

    void UpdateHealthUI(int health)
    {
        healthText.text = $"HP: {health}";
    }

    void UpdateManaUI(int mana)
    {
        manaText.text = $"MP: {mana}";
    }
}
```

### 使用观察者接口

```csharp
// 实现观察者接口
public class HealthObserver : IAsakiObserver<int>
{
    public void OnValueChange(int value)
    {
        Debug.Log($"Health updated: {value}");
        // 执行其他业务逻辑...
    }
}

public class Example : MonoBehaviour
{
    void Start()
    {
        var health = new AsakiProperty<int>(100);
        var observer = new HealthObserver();

        // 绑定观察者
        using (var binding = health.Bind(observer))
        {
            health.Value = 80;  // 调用 observer.OnValueChange(80)
        }

        // 已解绑，不会通知
        health.Value = 50;
    }
}
```

## API 参考

### AsakiProperty&lt;T&gt;

#### 构造函数

| 签名 | 说明 |
|------|------|
| `AsakiProperty(T initialValue = default)` | 使用指定初始值创建属性 |

#### 属性

| 名称 | 类型 | 说明 |
|------|------|------|
| `Value` | `T` | 获取或设置属性值，设置时自动通知订阅者 |
| `ValueType` | `Type` | 获取值的类型 |

#### 方法

| 签名 | 返回值 | 说明 |
|------|--------|------|
| `Subscribe(Action<T> action)` | `IDisposable` | 订阅值变化，返回可释放的订阅凭证 |
| `Subscribe(MonoBehaviour owner, Action<T> action)` | `IDisposable` | 订阅并绑定到 MonoBehaviour 生命周期 |
| `Unsubscribe(Action<T> action)` | `void` | 取消订阅 |
| `Bind(IAsakiObserver<T> observer)` | `IDisposable` | 绑定观察者，返回可释放的绑定凭证 |
| `Unbind(IAsakiObserver<T> observer)` | `void` | 解除观察者绑定 |
| `Dispose()` | `void` | 释放所有订阅和绑定 |

#### 运算符

| 运算符 | 说明 |
|--------|------|
| `implicit operator T` | 隐式转换为 T 类型 |
| `==` / `!=` | 相等性比较（支持 null 安全） |

### IAsakiObserver&lt;T&gt;

| 方法 | 说明 |
|------|------|
| `void OnValueChange(T value)` | 值变化时调用的方法 |

### AsakiBindingTracker

| 方法 | 说明 |
|------|------|
| `void Track(IDisposable subscription)` | 追踪订阅，在销毁时自动释放 |
| `void ReleaseAll()` | 立即释放所有追踪的订阅 |

## 高级用法

### 组合多个属性

```csharp
public class PlayerStats
{
    public AsakiProperty<int> Health { get; } = new(100);
    public AsakiProperty<int> MaxHealth { get; } = new(100);
    public AsakiProperty<float> HealthPercent { get; }

    public PlayerStats()
    {
        // 计算生命值百分比
        HealthPercent = new AsakiProperty<float>(1f);

        void UpdatePercent()
        {
            HealthPercent.Value = (float)Health.Value / MaxHealth.Value;
        }

        Health.Subscribe(_ => UpdatePercent());
        MaxHealth.Subscribe(_ => UpdatePercent());
    }
}
```

### 属性转换

```csharp
public class Example : MonoBehaviour
{
    void Start()
    {
        var rawValue = new AsakiProperty<float>(0.5f);

        // 转换为百分比显示
        rawValue.Subscribe(value =>
        {
            int percent = Mathf.RoundToInt(value * 100);
            Debug.Log($"{percent}%");
        });

        rawValue.Value = 0.75f;  // 输出: 75%
    }
}
```

### 条件订阅

```csharp
public class Example : MonoBehaviour
{
    void Start()
    {
        var health = new AsakiProperty<int>(100);

        // 只在生命值低于阈值时警告
        health.Subscribe(value =>
        {
            if (value < 20)
            {
                Debug.LogWarning("Low health!");
            }
        });

        health.Value = 50;  // 无输出
        health.Value = 15;  // 输出警告
    }
}
```

## 最佳实践

### 1. 使用生命周期绑定

在 MonoBehaviour 中订阅属性时，始终使用生命周期绑定版本：

```csharp
// 推荐
health.Subscribe(this, value => UpdateUI(value));

// 不推荐（可能导致内存泄漏）
health.Subscribe(value => UpdateUI(value));
```

### 2. 使用 using 语句

对于非 MonoBehaviour 场景的订阅，使用 `using` 语句确保正确释放：

```csharp
using (var sub = property.Subscribe(OnValueChanged))
{
    // 使用订阅
}
// 自动释放
```

### 3. 避免在通知中修改属性

在订阅回调中修改同一属性可能导致意外行为：

```csharp
// 避免这样做
property.Subscribe(value =>
{
    if (value > 100)
        property.Value = 100;  // 可能导致递归或意外行为
});

// 推荐做法
property.Subscribe(value =>
{
    if (value > 100)
    {
        // 延迟到下一帧或使用其他机制
        StartCoroutine(ClampValueNextFrame());
    }
});
```

### 4. 使用属性而非字段

将 AsakiProperty 作为类的属性暴露，提供更好的封装：

```csharp
public class GameState
{
    // 推荐
    public AsakiProperty<int> Score { get; } = new(0);

    // 不推荐
    public AsakiProperty<int> Score = new(0);
}
```

### 5. 实现自定义观察者

对于复杂的观察逻辑，实现 `IAsakiObserver<T>` 接口：

```csharp
public class AchievementObserver : IAsakiObserver<int>
{
    private readonly int _threshold;
    private readonly string _achievementId;
    private bool _unlocked;

    public AchievementObserver(string id, int threshold)
    {
        _achievementId = id;
        _threshold = threshold;
    }

    public void OnValueChange(int value)
    {
        if (!_unlocked && value >= _threshold)
        {
            UnlockAchievement(_achievementId);
            _unlocked = true;
        }
    }
}
```

## 性能优化

### 相等性比较

`AsakiProperty<T>` 在设置值时会自动进行相等性比较，如果值相等则不会触发通知：

```csharp
var property = new AsakiProperty<int>(10);
property.Subscribe(value => Debug.Log($"Changed to: {value}"));

property.Value = 10;  // 不会触发通知（值相等）
property.Value = 20;  // 触发通知
```

### 批量更新

如果需要批量更新多个相关属性，考虑使用事务模式：

```csharp
public class BatchUpdateExample
{
    public AsakiProperty<bool> IsBatchUpdating { get; } = new(false);
    public AsakiProperty<int> Value1 { get; } = new(0);
    public AsakiProperty<int> Value2 { get; } = new(0);

    public void BatchUpdate(int v1, int v2)
    {
        IsBatchUpdating.Value = true;
        try
        {
            Value1.Value = v1;
            Value2.Value = v2;
        }
        finally
        {
            IsBatchUpdating.Value = false;
        }
    }
}
```

### 快照机制

通知过程中使用快照机制，确保在通知过程中修改订阅者列表不会导致错误：

```csharp
// 这是内部实现细节，但了解它有助于理解系统的健壮性
private void Notify()
{
    // 创建快照，防止在通知过程中修改集合
    var actionSnapshot = _onValueChangedAction;
    actionSnapshot?.Invoke(_value);

    IAsakiObserver<T>[] observerSnapshot;
    lock (_observers)
    {
        observerSnapshot = _observers.ToArray();
    }

    // 从后往前遍历，支持在通知过程中解除绑定
    for (int i = observerSnapshot.Length - 1; i >= 0; i--)
    {
        observerSnapshot[i]?.OnValueChange(_value);
    }
}
```

---

**注意**：AsakiProperty 是可变对象，不适合用作字典键或哈希集合元素。系统会在尝试获取哈希码时抛出 `NotSupportedException`。
