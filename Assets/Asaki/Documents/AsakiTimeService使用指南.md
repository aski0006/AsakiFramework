# Asaki TimeService 使用指南

## 概述

Asaki TimerService 是一个高性能的定时器服务，专为 Unity 游戏开发设计。它提供了零分配、高性能的定时器管理功能，支持延迟执行、循环定时器、暂停/恢复等功能。

### 核心特性

- **零分配 (Zero-Alloc)**: 基于 Struct 和 List 复用，运行时不产生 GC 压力
- **O(1) 移除**: 使用 Swap-Removal 算法，移除操作时间复杂度为 O(1)
- **资源安全**: 实现 IDisposable 防止委托引用导致的内存泄漏
- **版本控制**: 使用 ID + Version 机制防止 ID 复用导致的"错误的取消"问题
- **支持 TimeScale**: 可选择是否受 Unity TimeScale 影响

## 架构设计

### 核心组件

```
Asaki.Time
├── IAsakiTimerService      # 定时器服务接口
├── AsakiTimerHandle        # 定时器句柄（值类型）
└── AsakiTimerService       # 定时器服务实现
```

### 模块集成

TimeService 通过 `AsakiTimeModule` 模块自动集成到框架中：

```csharp
[AsakiModule(200, typeof(AsakiSimulationModule))]
public class AsakiTimeModule : IAsakiModule
{
    private IAsakiTimerService _asakiTimerService;
    private IAsakiSimulationService _simulation;

    [AsakiInject]
    public void Init(IAsakiSimulationService simulation)
    {
        _simulation = simulation;
    }

    public void OnInit()
    {
        _asakiTimerService = new AsakiTimerService();
        _simulation.Register(_asakiTimerService);
    }
}
```

## API 参考

### IAsakiTimerService 接口

```csharp
public interface IAsakiTimerService : IAsakiTickable, IDisposable
{
    // 注册定时器
    AsakiTimerHandle Register(
        float duration,
        Action onComplete,
        Action<float> onUpdate = null,
        bool isLooped = false,
        bool useUnscaledTime = false
    );

    // 取消定时器
    void Cancel(AsakiTimerHandle handle);

    // 暂停/恢复定时器
    void Pause(AsakiTimerHandle handle, bool isPaused);
}
```

### AsakiTimerHandle 结构

```csharp
public readonly struct AsakiTimerHandle : IEquatable<AsakiTimerHandle>
{
    public readonly int Id;       // 唯一 ID
    public readonly ulong Version; // 版本号

    public static AsakiTimerHandle Invalid => new AsakiTimerHandle(0, 0);
}
```

## 使用方法

### 1. 注入 TimerService

```csharp
using Asaki.Core.Time;

public class GameManager : MonoBehaviour
{
    [AsakiInject]
    private IAsakiTimerService _timerService;

    private void Start()
    {
        // TimerService 已自动注入
    }
}
```

### 2. 创建一次性定时器

```csharp
// 3秒后执行回调
var handle = _timerService.Register(
    duration: 3f,
    onComplete: () =>
    {
        Debug.Log("定时器完成！");
    }
);
```

### 3. 创建带进度更新的定时器

```csharp
// 5秒倒计时，每帧更新进度
var handle = _timerService.Register(
    duration: 5f,
    onComplete: () =>
    {
        Debug.Log("倒计时结束");
    },
    onUpdate: (progress) =>
    {
        // progress 范围: 0.0 ~ 1.0
        Debug.Log($"进度: {progress * 100:F1}%");
    }
);
```

### 4. 创建循环定时器

```csharp
// 每2秒执行一次
var handle = _timerService.Register(
    duration: 2f,
    onComplete: () =>
    {
        Debug.Log("循环执行");
    },
    isLooped: true
);
```

### 5. 使用不受 TimeScale 影响的定时器

```csharp
// 即使游戏暂停，此定时器仍会继续运行
var handle = _timerService.Register(
    duration: 1f,
    onComplete: () =>
    {
        Debug.Log("不受 TimeScale 影响");
    },
    useUnscaledTime: true
);
```

### 6. 取消定时器

```csharp
var handle = _timerService.Register(5f, () => Debug.Log("不会执行"));

// 立即取消定时器
_timerService.Cancel(handle);
```

### 7. 暂停/恢复定时器

```csharp
var handle = _timerService.Register(
    10f,
    () => Debug.Log("完成"),
    (p) => Debug.Log($"进度: {p}")
);

// 暂停定时器
_timerService.Pause(handle, true);

// 恢复定时器
_timerService.Pause(handle, false);
```

### 8. 组合使用示例：倒计时系统

```csharp
public class CountdownSystem : MonoBehaviour
{
    [AsakiInject]
    private IAsakiTimerService _timerService;

    private AsakiTimerHandle _countdownHandle;

    public void StartCountdown(int seconds)
    {
        // 如果已有倒计时，先取消
        if (_countdownHandle != AsakiTimerHandle.Invalid)
        {
            _timerService.Cancel(_countdownHandle);
        }

        // 创建新的倒计时
        _countdownHandle = _timerService.Register(
            duration: seconds,
            onComplete: () =>
            {
                Debug.Log("倒计时结束！");
                _countdownHandle = AsakiTimerHandle.Invalid;
            },
            onUpdate: (progress) =>
            {
                int remaining = Mathf.CeilToInt((1f - progress) * seconds);
                Debug.Log($"剩余时间: {remaining} 秒");
            }
        );
    }

    private void OnDestroy()
    {
        // 清理定时器
        if (_countdownHandle != AsakiTimerHandle.Invalid)
        {
            _timerService.Cancel(_countdownHandle);
        }
    }
}
```

### 9. 游戏循环定时器示例

```csharp
public class Spawner : MonoBehaviour
{
    [AsakiInject]
    private IAsakiTimerService _timerService;

    private AsakiTimerHandle _spawnHandle;

    private void Start()
    {
        _spawnHandle = _timerService.Register(
            duration: 3f,
            onComplete: SpawnEnemy,
            isLooped: true
        );
    }

    private void SpawnEnemy()
    {
        Debug.Log("生成敌人");
        // 生成逻辑...
    }

    private void OnDestroy()
    {
        _timerService.Cancel(_spawnHandle);
    }
}
```

## 高级用法

### 暂停系统示例

```csharp
public class PauseManager : MonoBehaviour
{
    [AsakiInject]
    private IAsakiTimerService _timerService;

    private List<AsakiTimerHandle> _gameTimers = new List<AsakiTimerHandle>();

    public void RegisterGameTimer(AsakiTimerHandle handle)
    {
        _gameTimers.Add(handle);
    }

    public void PauseGame()
    {
        foreach (var handle in _gameTimers)
        {
            _timerService.Pause(handle, true);
        }
    }

    public void ResumeGame()
    {
        foreach (var handle in _gameTimers)
        {
            _timerService.Pause(handle, false);
        }
    }
}
```

### 延迟链式操作

```csharp
public class SequenceExecutor : MonoBehaviour
{
    [AsakiInject]
    private IAsakiTimerService _timerService;

    public void ExecuteSequence()
    {
        // 步骤1: 立即执行
        Debug.Log("步骤1");

        // 步骤2: 1秒后执行
        _timerService.Register(1f, () =>
        {
            Debug.Log("步骤2");

            // 步骤3: 再过2秒执行
            _timerService.Register(2f, () =>
            {
                Debug.Log("步骤3");
            });
        });
    }
}
```

## 性能优化建议

### 1. 合理使用 Update 回调

`onUpdate` 回调每帧都会调用，只在需要实时进度时使用：

```csharp
// ❌ 不推荐：只需要完成通知时不要使用 onUpdate
_timerService.Register(5f, () => Debug.Log("完成"), (p) => { });

// ✅ 推荐：直接使用 onComplete
_timerService.Register(5f, () => Debug.Log("完成"));
```

### 2. 及时取消不需要的定时器

```csharp
private AsakiTimerHandle _delayHandle;

private void Start()
{
    _delayHandle = _timerService.Register(10f, OnTimeout);
}

private void OnDestroy()
{
    // 确保对象销毁时取消定时器
    _timerService.Cancel(_delayHandle);
}
```

### 3. 批量暂停游戏定时器

```csharp
// 维护一个游戏相关定时器列表
private readonly List<AsakiTimerHandle> _gameTimers = new List<AsakiTimerHandle>();

public void PauseAllGameTimers()
{
    foreach (var handle in _gameTimers)
    {
        _timerService.Pause(handle, true);
    }
}
```

## 技术细节

### 零分配原理

TimerService 使用 `struct` 存储定时器数据，避免装箱和引用分配：

```csharp
private struct TimerData
{
    public int Id;
    public ulong Version;
    public float Duration;
    public float Elapsed;
    public bool IsLooped;
    public bool UseUnscaledTime;
    public bool IsPaused;
    public bool IsCancelled;
    public Action OnComplete;
    public Action<float> OnUpdate;
}
```

### Swap-Removal 算法

使用 O(1) 时间复杂度移除元素：

```csharp
private void RemoveAtSwap(int index)
{
    int lastIndex = _timers.Count - 1;
    if (index < lastIndex)
    {
        _timers[index] = _timers[lastIndex];
    }
    _timers.RemoveAt(lastIndex);
}
```

### 版本控制机制

防止 ID 复用导致的错误取消：

```csharp
public readonly struct AsakiTimerHandle : IEquatable<AsakiTimerHandle>
{
    public readonly int Id;
    public readonly ulong Version; // 防止 ID 复用冲突
}
```

## 注意事项

### 1. 资源管理

TimerService 实现 `IDisposable`，在模块销毁时会自动清理：

```csharp
public void OnDispose()
{
    _simulation?.Unregister(_asakiTimerService);
    _asakiTimerService.Dispose(); // 释放所有委托引用
}
```

### 2. 异常处理

定时器回调中的异常会被捕获，不会影响其他定时器：

```csharp
try
{
    t.OnComplete?.Invoke();
}
catch (Exception ex)
{
    ALog.Error("[AsakiTimer] Complete Callback Exception", ex);
}
```

### 3. 循环定时器的最小周期

为防止死循环，建议设置最小周期限制：

```csharp
if (t.Duration < 0.0001f)
    t.Elapsed = 0;
```

### 4. ID 溢出处理

ID 计数器在溢出时会重置：

```csharp
_idCounter++;
if (_idCounter < 0)
    _idCounter = 1;
```

## 常见问题

### Q: 如何暂停所有游戏定时器？

A: 维护一个定时器列表，批量暂停：

```csharp
private List<AsakiTimerHandle> _gameTimers = new List<AsakiTimerHandle>();

public void PauseAll()
{
    foreach (var handle in _gameTimers)
    {
        _timerService.Pause(handle, true);
    }
}
```

### Q: 定时器会随游戏暂停而暂停吗？

A: 取决于 `useUnscaledTime` 参数：
- `false` (默认): 受 TimeScale 影响
- `true`: 不受 TimeScale 影响

### Q: 如何在定时器回调中访问当前时间？

A: 通过 `onUpdate` 的 progress 参数：

```csharp
_timerService.Register(
    duration: 10f,
    onUpdate: (progress) =>
    {
        float elapsed = progress * 10f;
        Debug.Log($"已过去: {elapsed:F2} 秒");
    }
);
```

### Q: 定时器在对象销毁时会自动取消吗？

A: 不会，需要在 `OnDestroy` 中手动取消：

```csharp
private void OnDestroy()
{
    _timerService.Cancel(_handle);
}
```

## 总结

Asaki TimerService 提供了一个高性能、易用的定时器解决方案：

✅ **零分配** - 避免运行时 GC 压力  
✅ **高性能** - O(1) 移除操作  
✅ **资源安全** - 自动清理委托引用  
✅ **功能完整** - 支持延迟、循环、暂停、恢复  
✅ **易于集成** - 通过依赖注入自动注入  

在 Unity 游戏开发中，推荐使用 TimerService 替代传统的 `Invoke` 或 `Coroutine`，以获得更好的性能和更清晰的生命周期管理。