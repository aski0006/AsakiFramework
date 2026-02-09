# Asaki Unity 场景管理服务实现

本目录包含 Asaki 框架场景管理服务的 Unity 引擎实现，提供完整的场景加载、预加载和过渡功能。

## 目录结构

```
Unity/Services/Scene/
├── AsakiSceneManagerService.cs              # 场景管理器服务实现
└── SceneManagement/                         # 场景管理相关组件
    ├── LoadingSceneController.cs            # 过渡场景控制器
    ├── SceneLoadPayload.cs                  # 场景加载参数
    ├── SceneLoadStateService.cs             # 场景加载状态服务
    ├── ScenePreloadConfig.cs                # 场景预加载配置
    └── ScenePreloadDatabase.cs              # 场景预加载配置数据库
```

## 核心实现

### AsakiSceneManagerService

场景管理服务的 Unity 实现，继承自 `IAsakiSceneManagerService`。

#### 功能特性

- **场景验证**: 自动验证场景是否在 BuildSettings 中注册
- **资源清理**: 单场景模式下自动卸载未使用资源并触发 GC
- **进度报告**: 实时报告加载进度（0% ~ 90% 为实际加载，90% ~ 100% 为激活）
- **取消支持**: 支持通过 CancellationToken 取消加载
- **过渡动画**: 支持自定义过渡动画（进入/退出/进度更新）

#### 依赖注入

```csharp
public AsakiSceneManagerService(
    IAsakiEventService asakiEventService,
    IAsakiAsyncService asakiAsyncService,
    IAsakiResourceService asakiResourceService
)
```

| 依赖 | 用途 |
|------|------|
| `IAsakiEventService` | 发布场景状态事件 |
| `IAsakiAsyncService` | 帧等待和异步操作 |
| `IAsakiResourceService` | 资源预加载和卸载 |

## 预加载系统

### 工作流程

```
当前场景
    ↓
加载 LoadingScene（过渡场景）
    ↓
在 LoadingScene 中预加载目标场景所需资源
    ↓
加载目标场景
    ↓
进入目标场景（资源已预加载，立即可用）
```

### 核心组件

#### SceneLoadPayload

场景加载参数，用于在场景间传递加载信息。

```csharp
public class SceneLoadPayload
{
    public string TargetSceneName { get; set; }      // 目标场景
    public string LoadingSceneName { get; set; }     // 过渡场景
    public AsakiLoadSceneMode LoadMode { get; set; } // 加载模式
    public AsakiSceneActivation Activation { get; set; } // 激活方式
    public object CustomData { get; set; }           // 自定义数据
    public bool UsePreload { get; set; }             // 是否使用预加载
    public int TimeoutSeconds { get; set; }          // 超时时间
}
```

#### SceneLoadStateService

静态状态服务，用于跨场景传递场景加载参数。

```csharp
// 设置参数（在发起场景切换前）
SceneLoadStateService.SetPayload(payload);

// 获取参数（在 LoadingScene 中，获取后自动清空）
var payload = SceneLoadStateService.GetPayload();

// 查看参数（不清空）
var payload = SceneLoadStateService.PeekPayload();

// 清空参数
SceneLoadStateService.ClearPayload();
```

#### LoadingSceneController

过渡场景控制器，挂载在 LoadingScene 中，负责执行资源预加载。

**Inspector 配置:**

| 字段 | 类型 | 说明 |
|------|------|------|
| `_preloadDatabase` | ScenePreloadDatabase | 预加载配置数据库 |
| `_defaultLoadingSceneName` | string | 默认过渡场景名称 |
| `_progressBar` | RectTransform | 进度条变换组件 |
| `_progressText` | TextMeshProUGUI | 进度百分比文本 |
| `_tipText` | TextMeshProUGUI | 提示信息文本 |

**公共方法:**

```csharp
// 手动触发场景切换（用于非自动过渡模式）
public void TriggerSceneTransition()

// 取消加载
public void CancelLoading()
```

#### ScenePreloadConfig

场景预加载配置 ScriptableObject。

**配置项:**

| 属性 | 说明 |
|------|------|
| `TargetSceneName` | 目标场景名称 |
| `Resources` | 需要预加载的资源列表 |
| `AutoTransition` | 预加载完成后是否自动切换 |
| `TimeoutSeconds` | 加载超时时间 |

**资源条目:**

```csharp
public class ScenePreloadResourceEntry
{
    public string Location;              // 资源加载路径
    public SerializableResourceType ResourceType;  // 资源类型
}
```

#### ScenePreloadDatabase

预加载配置数据库，管理所有场景的预加载配置。

```csharp
// 获取配置
ScenePreloadConfig config = database.GetConfig("GameScene");

// 检查是否有配置
bool hasConfig = database.HasConfig("GameScene");

// 获取所有已注册场景
IEnumerable<string> scenes = database.GetRegisteredSceneNames();

// 注册/移除配置
database.RegisterConfig(config);
database.UnregisterConfig("GameScene");
```

## 使用示例

### 基础场景加载

```csharp
public class MenuController : AsakiMono, IAsakiInit<IAsakiSceneManagerService>
{
    private IAsakiSceneManagerService _sceneManager;
    
    [AsakiInject]
    public void Init(IAsakiSceneManagerService sceneManager)
    {
        _sceneManager = sceneManager;
    }
    
    public async void OnStartGameClicked()
    {
        var result = await _sceneManager.LoadSceneAsync("Level1");
        
        if (!result.IsSuccess)
        {
            Debug.LogError($"加载失败: {result.ErrorMessage}");
        }
    }
}
```

### 带预加载的场景切换

```csharp
// 1. 创建预加载配置
// 在 Project 窗口右键 -> Create -> Asaki -> Scene -> Scene Preload Config

// 2. 配置预加载资源
// 在 Inspector 中设置 TargetSceneName 和 Resources 列表

// 3. 创建配置数据库
// 在 Project 窗口右键 -> Create -> Asaki -> Scene -> Scene Preload Database
// 将所有配置添加到数据库中

// 4. 在 LoadingScene 中配置 LoadingSceneController
// 将 ScenePreloadDatabase 赋值给 _preloadDatabase 字段

// 5. 代码中调用
public async void LoadGameLevel(string levelName)
{
    // 使用预加载流程
    var result = await _sceneManager.LoadSceneWithPreloadAsync(levelName);
}
```

### 自定义过渡动画

```csharp
public class CustomTransition : IAsakiSceneTransition
{
    private readonly Animator _animator;
    private readonly CanvasGroup _canvasGroup;
    
    public CustomTransition(Animator animator, CanvasGroup canvasGroup)
    {
        _animator = animator;
        _canvasGroup = canvasGroup;
    }
    
    public async UniTask EnterAsync(CancellationToken ct)
    {
        _canvasGroup.blocksRaycasts = true;
        _animator.SetTrigger("FadeIn");
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: ct);
    }
    
    public void OnProgress(float normalizedProgress)
    {
        // 可以在这里更新进度条
    }
    
    public async UniTask ExitAsync(CancellationToken ct)
    {
        _animator.SetTrigger("FadeOut");
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: ct);
        _canvasGroup.blocksRaycasts = false;
    }
    
    public void Dispose()
    {
        // 清理资源
    }
}

// 使用
var transition = new CustomTransition(animator, canvasGroup);
await _sceneManager.LoadSceneAsync("GameScene", 
    AsakiLoadSceneMode.Single,
    AsakiSceneActivation.Immediate,
    transition);
```

### 叠加场景加载

```csharp
// 加载叠加场景，保留当前场景
await _sceneManager.LoadSceneAsync(
    "HUDScene", 
    AsakiLoadSceneMode.Additive
);

// 之后可以卸载
await SceneManager.UnloadSceneAsync("HUDScene");
```

### 手动确认模式

```csharp
public class LevelLoader : AsakiMono, IAsakiInit<IAsakiSceneManagerService>
{
    [SerializeField] private GameObject _pressAnyKeyPrompt;
    
    private IAsakiSceneManagerService _sceneManager;
    private bool _waitingForInput;
    
    [AsakiInject]
    public void Init(IAsakiSceneManagerService sceneManager)
    {
        _sceneManager = sceneManager;
    }
    
    public async UniTask LoadLevelWithPrompt(string levelName)
    {
        // 开始加载，使用 ManualConfirm 模式
        var loadTask = _sceneManager.LoadSceneAsync(
            levelName,
            AsakiLoadSceneMode.Single,
            AsakiSceneActivation.ManualConfirm
        );
        
        // 显示加载 UI
        ShowLoadingUI();
        
        var result = await loadTask;
        
        if (result.IsSuccess)
        {
            // 加载完成，等待用户输入
            _waitingForInput = true;
            _pressAnyKeyPrompt.SetActive(true);
        }
    }
    
    private void Update()
    {
        if (_waitingForInput && Input.anyKeyDown)
        {
            _waitingForInput = false;
            _pressAnyKeyPrompt.SetActive(false);
            _sceneManager.ActivateScene();  // 激活场景
        }
    }
}
```

### 取消场景加载

```csharp
private CancellationTokenSource _loadingCts;

public async void StartLoading(string sceneName)
{
    _loadingCts = new CancellationTokenSource();
    
    var result = await _sceneManager.LoadSceneAsync(
        sceneName,
        token: _loadingCts.Token
    );
    
    if (result.ErrorMessage == "Operation canceled.")
    {
        Debug.Log("加载已取消");
    }
}

public void CancelLoading()
{
    _loadingCts?.Cancel();
}
```

## 配置步骤

### 1. 设置 Build Settings

确保所有需要加载的场景都在 **File -> Build Settings** 中添加。

### 2. 创建预加载配置

1. 在 Project 窗口右键 -> **Create -> Asaki -> Scene -> Scene Preload Config**
2. 设置 `Target Scene Name` 为目标场景名称
3. 添加需要预加载的资源

### 3. 创建配置数据库

1. 在 Project 窗口右键 -> **Create -> Asaki -> Scene -> Scene Preload Database**
2. 将所有预加载配置添加到数据库的 `Configs` 列表中

### 4. 设置 LoadingScene

1. 创建一个新的 Scene 作为过渡场景
2. 添加一个 GameObject，挂载 `LoadingSceneController` 组件
3. 配置 UI 元素（进度条、文本等）
4. 将 `ScenePreloadDatabase` 赋值给 `_preloadDatabase` 字段
5. 将该场景添加到 Build Settings

## 性能优化建议

1. **预加载策略**: 只预加载关键资源，避免加载过多导致内存压力
2. **超时设置**: 合理设置超时时间，防止加载卡住
3. **资源清理**: 单场景模式下会自动调用 `UnloadUnusedAssets` 和 `GC.Collect`
4. **进度平滑**: 使用过渡动画掩盖加载过程中的卡顿

## 注意事项

1. 场景必须添加到 Build Settings 才能被加载
2. `LoadSceneWithPreloadAsync` 需要在 LoadingScene 中配置 `LoadingSceneController`
3. 预加载的资源会在目标场景加载完成后自动可用
4. 取消加载操作会触发 `AsakiSceneStateEvent.State.Cancelled` 事件

## 依赖关系

```
Unity/Services/Scene
├── Core/Scene                (接口定义)
├── Core/Async                (IAsakiAsyncService)
├── Core/Broker               (AsakiBroker)
├── Core/Logging              (ALog)
├── Core/Resources            (IAsakiResourceService, ResHandle)
├── Core/Attributes           (AsakiInject)
├── Core/Context              (AsakiMono)
├── Unity/SceneManagement     (SceneManager, AsyncOperation)
└── UniTask                   (UniTask, UniTaskCompletionSource)
```
