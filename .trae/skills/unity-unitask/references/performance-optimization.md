# UniTask Performance Optimization

## Allocation-Free Patterns

### ValueTask Optimization

```csharp
// UniTask is a struct (value type) - zero heap allocation
public async UniTask<int> GetDataAsync()
{
    await UniTask.Yield();
    return 42;
}

// Comparison: Task allocates on heap
public async Task<int> GetDataTaskAsync()
{
    await Task.Yield(); // Allocates
    return 42;
}
```

### Avoiding Allocations

```csharp
// ❌ Bad: Allocates lambda closure
await UniTask.WaitUntil(() => health <= 0);

// ✅ Good: Use cached predicate
private bool IsHealthZero() => health <= 0;
await UniTask.WaitUntil(IsHealthZero);

// ❌ Bad: Allocates array for WhenAll
await UniTask.WhenAll(task1, task2, task3);

// ✅ Good: Use ValueTuple for small counts
var (r1, r2, r3) = await UniTask.WhenAll(task1, task2, task3);
```

## GC Pressure Reduction

### Memory Profiling

```csharp
#if UNITY_EDITOR
using UnityEngine;

public class UniTaskMemoryMonitor : MonoBehaviour
{
    void OnGUI()
    {
        GUILayout.Label("UniTask Memory Stats:");

        foreach (var (type, size) in TaskPool.GetCacheSizeInfo())
        {
            GUILayout.Label($"{type.Name}: {size} pooled");
        }

        var tracker = TaskTracker.GetInstance();
        GUILayout.Label($"Active Tasks: {tracker.Count}");
    }
}
#endif
```

### Pool Management

```csharp
// UniTask automatically pools completed tasks
// Manual pool clearing if needed (rare)
#if UNITY_EDITOR
[MenuItem("Tools/Clear UniTask Pool")]
static void ClearPool()
{
    TaskPool.Clear();
}
#endif
```

## PlayerLoop Timing Control

### Optimal Timing Selection

```csharp
// Execute during specific update phase
public class PlayerController : MonoBehaviour
{
    async UniTaskVoid Start()
    {
        // Physics-related operations
        await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
        ApplyPhysics();

        // Camera updates
        await UniTask.Yield(PlayerLoopTiming.LateUpdate);
        UpdateCamera();

        // UI updates
        await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
        UpdateUI();
    }
}
```

### Custom PlayerLoop Injection

```csharp
// Minimal injection for better performance
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
static void Initialize()
{
    var loop = PlayerLoop.GetCurrentPlayerLoop();
    PlayerLoopHelper.Initialize(ref loop, InjectPlayerLoopTimings.Minimum);
}
```

## Memory Profiling with UniTask.Tracker

### Enable Tracking

```csharp
// Enable in development builds only
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Cysharp.Threading.Tasks.Diagnostics;

[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
static void EnableTracking()
{
    UniTaskScheduler.UnobservedTaskException += (ex) =>
    {
        Debug.LogError($"Unobserved UniTask exception: {ex}");
    };
}
#endif
```

### Monitoring Active Tasks

```csharp
#if UNITY_EDITOR
public class UniTaskDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            var tracker = TaskTracker.GetInstance();
            Debug.Log($"Active UniTasks: {tracker.Count}");

            foreach (var taskInfo in tracker.GetActiveTasks())
            {
                Debug.Log($"Task: {taskInfo.Position} - Status: {taskInfo.Status}");
            }
        }
    }
}
#endif
```

### Memory Leak Detection

```csharp
// Check for leaking tasks on scene unload
void OnDestroy()
{
    #if UNITY_EDITOR
    var tracker = TaskTracker.GetInstance();
    if (tracker.Count > 0)
    {
        Debug.LogWarning($"Potential memory leak: {tracker.Count} active UniTasks");

        foreach (var task in tracker.GetActiveTasks())
        {
            Debug.LogWarning($"Leaked task at: {task.Position}");
        }
    }
    #endif
}
```

## Advanced Optimization Techniques

### Struct-Based Async Methods

```csharp
// ValueTask pattern for hot paths
public readonly struct FastOperation : IUniTaskSource<int>
{
    private readonly UniTaskCompletionSource<int> core;

    public FastOperation(int value)
    {
        core = AutoResetUniTaskCompletionSource<int>.Create();
        core.TrySetResult(value);
    }

    public int GetResult(short token) => core.GetResult(token);
    public UniTaskStatus GetStatus(short token) => core.GetStatus(token);
    public void OnCompleted(Action<object> continuation, object state, short token)
        => core.OnCompleted(continuation, state, token);
    public UniTaskStatus UnsafeGetStatus() => core.UnsafeGetStatus();
}
```

### Cached Awaiters

```csharp
// Cache frequently used awaiters
private static class CachedAwaits
{
    public static readonly UniTask NextFrame = UniTask.NextFrame();
    public static readonly UniTask Yield = UniTask.Yield();

    public static UniTask Delay100ms() => UniTask.Delay(100);
}

// Usage
await CachedAwaits.Yield;
await CachedAwaits.NextFrame;
```

### Batch Operations

```csharp
// Process in batches to reduce frame time impact
public async UniTask ProcessLargeDataset(List<Data> dataset)
{
    const int batchSize = 100;

    for (int i = 0; i < dataset.Count; i += batchSize)
    {
        // Process batch
        var batch = dataset.Skip(i).Take(batchSize);
        foreach (var item in batch)
        {
            ProcessItem(item);
        }

        // Yield every batch to prevent frame drops
        await UniTask.Yield();
    }
}
```

## Platform-Specific Optimizations

### WebGL

```csharp
#if UNITY_WEBGL && !UNITY_EDITOR
// No threading in WebGL - use frame-based async
public async UniTask<T> LoadWebGLAsync<T>() where T : Object
{
    var request = Resources.LoadAsync<T>(path);

    // Yield every frame for responsiveness
    while (!request.isDone)
    {
        await UniTask.Yield();
    }

    return request.asset as T;
}
#endif
```

### Mobile

```csharp
// Optimize for battery life
public class MobileOptimization : MonoBehaviour
{
    async UniTaskVoid Start()
    {
        while (true)
        {
            // Reduce update frequency for background tasks
            await UniTask.DelayFrame(10); // Update every 10 frames

            UpdateBackgroundSystems();
        }
    }
}
```

## Benchmark Examples

### Performance Comparison

```csharp
using System.Diagnostics;
using UnityEngine;

public class BenchmarkUniTask : MonoBehaviour
{
    async void Start()
    {
        // Benchmark Task
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            await Task.Yield();
        }
        Debug.Log($"Task: {sw.ElapsedMilliseconds}ms");

        // Benchmark UniTask
        sw.Restart();
        for (int i = 0; i < 10000; i++)
        {
            await UniTask.Yield();
        }
        Debug.Log($"UniTask: {sw.ElapsedMilliseconds}ms");

        // UniTask is typically 10-20x faster with zero allocation
    }
}
```

## Best Practices Summary

1. **Use UniTask for Unity code**: 10-20x faster than Task with zero allocation
2. **Profile with Tracker**: Monitor active tasks in development builds
3. **Choose proper timing**: Use PlayerLoopTiming for precise execution
4. **Cache awaiters**: Reuse common await patterns
5. **Batch operations**: Prevent frame drops with large datasets
6. **Monitor memory**: Check for task leaks on scene changes
7. **Platform awareness**: Optimize for WebGL and mobile constraints
