# UniTask Integration Patterns

## Addressables Integration

### Async Asset Loading

```csharp
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AssetLoader
{
    public async UniTask<GameObject> LoadPrefabAsync(string key, CancellationToken ct)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(key);

        try
        {
            return await handle.ToUniTask(cancellationToken: ct);
        }
        catch (OperationCanceledException)
        {
            Addressables.Release(handle);
            throw;
        }
    }
}
```

### Progress Reporting

```csharp
public async UniTask<T> LoadWithProgressAsync<T>(
    string key,
    IProgress<float> progress,
    CancellationToken ct) where T : Object
{
    var handle = Addressables.LoadAssetAsync<T>(key);

    return await handle.ToUniTask(
        progress: progress,
        cancellationToken: ct
    );
}

// Usage
var progress = Progress.Create<float>(p =>
{
    progressBar.value = p;
    progressText.text = $"{p * 100:F0}%";
});

var asset = await LoadWithProgressAsync<GameObject>("key", progress, ct);
```

### Batch Loading

```csharp
public async UniTask<List<T>> LoadMultipleAsync<T>(
    IEnumerable<string> keys,
    IProgress<float> progress,
    CancellationToken ct) where T : Object
{
    var tasks = keys.Select(key =>
        Addressables.LoadAssetAsync<T>(key).ToUniTask(cancellationToken: ct)
    );

    var results = new List<T>();
    var total = keys.Count();
    var completed = 0;

    foreach (var task in tasks)
    {
        results.Add(await task);
        completed++;
        progress?.Report((float)completed / total);
    }

    return results;
}
```

## DOTween Integration

### Setup

```csharp
// Add UNITASK_DOTWEEN_SUPPORT to Scripting Define Symbols
// in Player Settings
```

### Basic Tweening

```csharp
#if UNITASK_DOTWEEN_SUPPORT
using DG.Tweening;

public class TweenExample : MonoBehaviour
{
    async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        // Await tween completion
        await transform.DOMoveX(10f, 1f).ToUniTask(cancellationToken: ct);

        // Chain tweens
        await transform.DOScale(2f, 0.5f).ToUniTask(cancellationToken: ct);
        await transform.DORotate(Vector3.up * 180, 0.5f).ToUniTask(cancellationToken: ct);
    }
}
#endif
```

### Parallel Tweens

```csharp
public async UniTask AnimateUIAsync(CancellationToken ct)
{
    // Run multiple tweens in parallel
    await UniTask.WhenAll(
        panel.DOFade(1f, 0.3f).ToUniTask(cancellationToken: ct),
        panel.transform.DOScale(Vector3.one, 0.3f).ToUniTask(cancellationToken: ct),
        panel.transform.DOLocalMoveY(0, 0.3f).ToUniTask(cancellationToken: ct)
    );
}
```

### Sequence Control

```csharp
public async UniTask PlaySequenceAsync(CancellationToken ct)
{
    var sequence = DOTween.Sequence();

    sequence.Append(transform.DOMoveX(5, 1f));
    sequence.Append(transform.DOMoveY(5, 1f));
    sequence.Join(transform.DORotate(Vector3.up * 360, 1f));

    await sequence.ToUniTask(cancellationToken: ct);
}
```

## UnityWebRequest Integration

### HTTP GET Request

```csharp
public async UniTask<string> GetAsync(string url, CancellationToken ct)
{
    using var request = UnityWebRequest.Get(url);

    try
    {
        await request.SendWebRequest().ToUniTask(cancellationToken: ct);

        if (request.result == UnityWebRequest.Result.Success)
        {
            return request.downloadHandler.text;
        }
        else
        {
            throw new Exception($"Request failed: {request.error}");
        }
    }
    catch (OperationCanceledException)
    {
        request.Abort();
        throw;
    }
}
```

### POST Request with JSON

```csharp
using System.Text;

public async UniTask<TResponse> PostJsonAsync<TRequest, TResponse>(
    string url,
    TRequest data,
    CancellationToken ct)
{
    var json = JsonUtility.ToJson(data);
    var bodyRaw = Encoding.UTF8.GetBytes(json);

    using var request = new UnityWebRequest(url, "POST");
    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
    request.downloadHandler = new DownloadHandlerBuffer();
    request.SetRequestHeader("Content-Type", "application/json");

    await request.SendWebRequest().ToUniTask(cancellationToken: ct);

    if (request.result == UnityWebRequest.Result.Success)
    {
        var responseJson = request.downloadHandler.text;
        return JsonUtility.FromJson<TResponse>(responseJson);
    }
    else
    {
        throw new Exception($"POST failed: {request.error}");
    }
}
```

### Download with Timeout

```csharp
public async UniTask<byte[]> DownloadWithTimeoutAsync(
    string url,
    TimeSpan timeout,
    IProgress<float> progress,
    CancellationToken ct)
{
    using var request = UnityWebRequest.Get(url);
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

    cts.CancelAfterSlim(timeout);

    try
    {
        await request.SendWebRequest().ToUniTask(
            progress: progress,
            cancellationToken: cts.Token
        );

        return request.downloadHandler.data;
    }
    catch (OperationCanceledException)
    {
        if (ct.IsCancellationRequested)
            throw; // External cancellation
        else
            throw new TimeoutException($"Download timed out after {timeout.TotalSeconds}s");
    }
}
```

## UI Event Handling

### Button Click Async

```csharp
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField] private Button actionButton;

    async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        while (!ct.IsCancellationRequested)
        {
            // Wait for button click
            await actionButton.OnClickAsync(ct);

            // Handle click
            await HandleButtonClickAsync(ct);
        }
    }

    async UniTask HandleButtonClickAsync(CancellationToken ct)
    {
        actionButton.interactable = false;

        try
        {
            await PerformActionAsync(ct);
        }
        finally
        {
            actionButton.interactable = true;
        }
    }
}
```

### Input Field Validation

```csharp
using UnityEngine.UI;

public async UniTask<string> WaitForValidInputAsync(
    InputField inputField,
    Func<string, bool> validator,
    CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        // Wait for input change
        await inputField.onValueChanged.OnInvokeAsync(ct);

        var value = inputField.text;
        if (validator(value))
        {
            return value;
        }
    }

    throw new OperationCanceledException();
}
```

## AsyncReactiveProperty Usage

### Reactive State Management

```csharp
using Cysharp.Threading.Tasks.Linq;

public class PlayerHealth : MonoBehaviour
{
    private readonly AsyncReactiveProperty<int> health = new(100);

    public IReadOnlyAsyncReactiveProperty<int> Health => health;

    async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        // React to health changes
        await health
            .Where(h => h <= 0)
            .FirstAsync(ct);

        OnPlayerDeath();
    }

    public void TakeDamage(int amount)
    {
        health.Value -= amount;
    }
}
```

### UI Binding

```csharp
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Text healthText;
    [SerializeField] private Slider healthSlider;

    private PlayerHealth playerHealth;

    async UniTaskVoid Start()
    {
        playerHealth = FindAnyObjectByType<PlayerHealth>();
        var ct = this.GetCancellationTokenOnDestroy();

        // Bind to UI
        await playerHealth.Health
            .ForEachAsync(h =>
            {
                healthText.text = $"HP: {h}";
                healthSlider.value = h / 100f;
            }, ct);
    }
}
```

### Multiple Property Combination

```csharp
public class GameState : MonoBehaviour
{
    private readonly AsyncReactiveProperty<int> score = new(0);
    private readonly AsyncReactiveProperty<int> combo = new(0);

    async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        // Combine multiple properties
        await UniTaskAsyncEnumerable.CombineLatest(
            score.WithoutCurrent(),
            combo.WithoutCurrent(),
            (s, c) => new { Score = s, Combo = c }
        ).ForEachAsync(state =>
        {
            UpdateUI(state.Score, state.Combo);
        }, ct);
    }
}
```

## Scene Management

### Async Scene Loading

```csharp
using UnityEngine.SceneManagement;

public async UniTask LoadSceneAsync(
    string sceneName,
    IProgress<float> progress,
    CancellationToken ct)
{
    var operation = SceneManager.LoadSceneAsync(sceneName);
    operation.allowSceneActivation = false;

    // Wait until 90% loaded
    while (operation.progress < 0.9f)
    {
        progress?.Report(operation.progress);
        await UniTask.Yield(ct);
    }

    progress?.Report(1f);

    // Activate scene
    operation.allowSceneActivation = true;
    await operation.ToUniTask(cancellationToken: ct);
}
```

### Additive Scene Loading

```csharp
public async UniTask LoadAdditiveSceneAsync(
    string sceneName,
    CancellationToken ct)
{
    var operation = SceneManager.LoadSceneAsync(
        sceneName,
        LoadSceneMode.Additive
    );

    await operation.ToUniTask(cancellationToken: ct);

    var scene = SceneManager.GetSceneByName(sceneName);
    SceneManager.SetActiveScene(scene);
}
```

## Physics Integration

### Raycast Async

```csharp
public async UniTask<RaycastHit?> RaycastAsync(
    Ray ray,
    float maxDistance,
    LayerMask layerMask)
{
    // Physics operations should run on FixedUpdate timing
    await UniTask.Yield(PlayerLoopTiming.FixedUpdate);

    if (Physics.Raycast(ray, out var hit, maxDistance, layerMask))
    {
        return hit;
    }

    return null;
}
```

### Trigger Detection

```csharp
public class TriggerDetector : MonoBehaviour
{
    private AsyncReactiveProperty<bool> isPlayerInside = new(false);

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            isPlayerInside.Value = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            isPlayerInside.Value = false;
    }

    public async UniTask WaitForPlayerAsync(CancellationToken ct)
    {
        await isPlayerInside
            .Where(inside => inside)
            .FirstAsync(ct);
    }
}
```

## Best Practices

1. **Always pass CancellationToken**: Prevent memory leaks and dangling operations
2. **Use ToUniTask() for Unity operations**: Convert AsyncOperation, ResourceRequest, etc.
3. **Handle cancellation gracefully**: Clean up resources when operations are cancelled
4. **Progress reporting**: Use IProgress<float> for long-running operations
5. **Timeout handling**: Use CancelAfterSlim for PlayerLoop-based timeouts
6. **UI responsiveness**: Yield between batch operations to prevent frame drops
7. **Platform awareness**: Test integrations on target platforms (WebGL, mobile)
