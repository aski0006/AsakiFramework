# UniTask Fundamentals

## UniTask vs Task Comparison

### Performance Characteristics

**Standard Task**:
- Allocates memory on heap
- Uses ThreadPool for scheduling
- Generic for all .NET platforms
- Higher GC pressure

**UniTask**:
- Zero allocation through struct-based design
- PlayerLoop-based scheduling (Unity integration)
- Unity-optimized execution
- Minimal GC pressure

### When to Use Each

```csharp
// Use Task for:
// - Cross-platform .NET code
// - Non-performance-critical scenarios
// - Standard library compatibility

// Use UniTask for:
// - Unity-specific code
// - Performance-critical paths
// - Mobile platforms
// - Memory-sensitive scenarios
```

## Coroutine Migration Patterns

### Basic Conversions

```csharp
// Coroutine → UniTask
// Before
IEnumerator OldPattern()
{
    yield return new WaitForSeconds(1f);
    yield return null;
    yield return new WaitForEndOfFrame();
    yield return new WaitForFixedUpdate();
}

// After
async UniTask NewPattern()
{
    await UniTask.Delay(TimeSpan.FromSeconds(1));
    await UniTask.Yield();
    await UniTask.WaitForEndOfFrame();
    await UniTask.WaitForFixedUpdate();
}
```

### Advanced Coroutine Patterns

```csharp
// Coroutine with return value
// Before
IEnumerator LoadData(Action<string> callback)
{
    var request = UnityWebRequest.Get(url);
    yield return request.SendWebRequest();
    callback(request.downloadHandler.text);
}

// After
async UniTask<string> LoadDataAsync(CancellationToken ct)
{
    var request = UnityWebRequest.Get(url);
    await request.SendWebRequest().ToUniTask(cancellationToken: ct);
    return request.downloadHandler.text;
}
```

### Nested Coroutines

```csharp
// Before
IEnumerator ParentCoroutine()
{
    yield return StartCoroutine(ChildCoroutine1());
    yield return StartCoroutine(ChildCoroutine2());
}

// After
async UniTask ParentAsync()
{
    await Child1Async();
    await Child2Async();
}
```

## PlayerLoop Execution Model

### PlayerLoop Timing

UniTask integrates with Unity's PlayerLoop for precise timing control:

```csharp
// Execute at specific PlayerLoop timing
await UniTask.Yield(PlayerLoopTiming.PreUpdate);
await UniTask.Yield(PlayerLoopTiming.Update);
await UniTask.Yield(PlayerLoopTiming.PreLateUpdate);
await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);

// Default is Update timing
await UniTask.Yield(); // Equivalent to PlayerLoopTiming.Update
```

### PlayerLoop Injection

```csharp
// Minimal injection for better performance
PlayerLoopHelper.Initialize(ref loop, InjectPlayerLoopTimings.Minimum);

// Standard injection (default)
PlayerLoopHelper.Initialize(ref loop, InjectPlayerLoopTimings.Standard);

// Full injection (all timing points)
PlayerLoopHelper.Initialize(ref loop, InjectPlayerLoopTimings.All);
```

## Memory Pool Management

### UniTask Pooling

UniTask uses object pooling to eliminate allocations:

```csharp
// UniTask automatically pools completed tasks
// No manual pool management needed

// Check pool statistics in editor
#if UNITY_EDITOR
foreach (var (type, size) in TaskPool.GetCacheSizeInfo())
{
    Debug.Log($"{type}: {size} cached instances");
}
#endif
```

### Custom Pooling with UniTask

```csharp
// Object pooling pattern with UniTask
public class ObjectPool<T> where T : class, new()
{
    private readonly Stack<T> pool = new();

    public async UniTask<T> RentAsync()
    {
        await UniTask.Yield();
        return pool.Count > 0 ? pool.Pop() : new T();
    }

    public void Return(T obj)
    {
        pool.Push(obj);
    }
}
```

## Basic Async Operations

### Delay Operations

```csharp
// Time-based delays
await UniTask.Delay(1000); // 1 second in milliseconds
await UniTask.Delay(TimeSpan.FromSeconds(1));
await UniTask.WaitForSeconds(1f); // Unity-style

// Frame-based delays
await UniTask.DelayFrame(10); // Wait 10 frames
await UniTask.NextFrame();    // Wait 1 frame
await UniTask.Yield();        // Wait 1 frame (Update timing)
```

### Condition Waiting

```csharp
// Wait until condition is true
await UniTask.WaitUntil(() => isReady);

// Wait while condition is true
await UniTask.WaitWhile(() => isLoading);

// Wait for value changed
await UniTask.WaitUntilValueChanged(player, x => x.health);
```

### Unity Operation Integration

```csharp
// AsyncOperation conversion
var op = SceneManager.LoadSceneAsync("Level1");
await op.ToUniTask();

// ResourceRequest conversion
var request = Resources.LoadAsync<GameObject>("Prefab");
await request.ToUniTask();

// With progress reporting
var progress = Progress.Create<float>(p => progressBar.value = p);
await op.ToUniTask(progress);
```

## CancellationToken Patterns

### MonoBehaviour Lifecycle

```csharp
public class Example : MonoBehaviour
{
    async UniTaskVoid Start()
    {
        // Auto-cancel on destroy
        await LoadAsync(this.GetCancellationTokenOnDestroy());
    }

    private CancellationTokenSource customCts;

    void OnEnable()
    {
        customCts = new CancellationTokenSource();
        LoadAsync(customCts.Token).Forget();
    }

    void OnDisable()
    {
        customCts?.Cancel();
        customCts?.Dispose();
    }
}
```

### Linked Cancellation

```csharp
// Combine multiple cancellation sources
var cts1 = new CancellationTokenSource();
var cts2 = this.GetCancellationTokenOnDestroy();

var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
    cts1.Token,
    cts2
);

await OperationAsync(linkedCts.Token);
```

### Timeout with Cancellation

```csharp
var cts = new CancellationTokenSource();
cts.CancelAfterSlim(TimeSpan.FromSeconds(5)); // PlayerLoop-based

try
{
    await LongRunningAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Debug.Log("Operation timed out or cancelled");
}
finally
{
    cts.Dispose();
}
```

## Error Handling

### Try-Catch Patterns

```csharp
async UniTask<bool> SafeOperationAsync()
{
    try
    {
        await RiskyOperationAsync();
        return true;
    }
    catch (OperationCanceledException)
    {
        // Expected when cancelled
        Debug.Log("Operation cancelled");
        return false;
    }
    catch (Exception ex)
    {
        // Unexpected errors
        Debug.LogError($"Operation failed: {ex.Message}");
        return false;
    }
}
```

### Fire-and-Forget Safety

```csharp
// Use UniTaskVoid for fire-and-forget
async UniTaskVoid FireAndForgetAsync()
{
    try
    {
        await SomeOperationAsync();
    }
    catch (Exception ex)
    {
        Debug.LogError(ex);
    }
}

// Or use .Forget() with exception handling
SomeOperationAsync().Forget(ex => Debug.LogError(ex));
```
