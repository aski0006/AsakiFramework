# Asaki 场景管理服务文档

本文档详细介绍 Asaki 框架的场景管理服务，包括核心接口定义和 Unity 引擎实现。

---

## 目录

- [概述](#概述)
- [目录结构](#目录结构)
- [核心接口](#核心接口)
- [Unity 实现](#unity-实现)
- [预加载系统](#预加载系统)
- [使用示例](#使用示例)
- [配置步骤](#配置步骤)
- [性能优化建议](#性能优化建议)
- [注意事项](#注意事项)
- [设计原则](#设计原则)
- [依赖关系](#依赖关系)

---

## 概述

Asaki 场景管理服务提供完整的场景加载、预加载和过渡功能。采用分层架构设计：

- **Core 层**: 定义平台无关的抽象接口和数据结构
- **Unity 层**: 提供 Unity 引擎的具体实现

---

## 目录结构

### Core 层（接口定义）

```
Assets/Asaki/Core/Scene/
├── IAsakiSceneManagerService.cs    # 场景管理器服务接口
├── IAsakiSceneTransition.cs        # 场景过渡动画接口
├── AsakiLoadSceneMode.cs           # 场景加载模式枚举
├── AsakiSceneResult.cs             # 场景加载结果结构
└── AsakiSceneProgressEvent.cs      # 场景加载进度事件
```

### Unity 层（引擎实现）

```
Assets/Asaki/Unity/Services/Scene/
├── AsakiSceneManagerService.cs              # 场景管理器服务实现
└── SceneManagement/                         # 场景管理相关组件
    ├── LoadingSceneController.cs            # 过渡场景控制器
    ├── SceneLoadPayload.cs                  # 场景加载参数
    ├── SceneLoadStateService.cs             # 场景加载状态服务
    ├── ScenePreloadConfig.cs                # 场景预加载配置
    └── ScenePreloadDatabase.cs              # 场景预加载配置数据库
```

---

## 核心接口

### IAsakiSceneManagerService

场景管理服务的核心接口，定义了场景加载的所有操作。

```csharp
public interface IAsakiSceneManagerService : IAsakiService, IDisposable
{
    string LastLoadedSceneName { get; }
    
    // 预构建场景缓存
    void PerBuildScene();
    
    // 加载场景
    UniTask<AsakiSceneResult> LoadSceneAsync(
        string sceneName,
        AsakiLoadSceneMode mode = AsakiLoadSceneMode.Single,
        AsakiSceneActivation activation = AsakiSceneActivation.Immediate,
        IAsakiSceneTransition transition = null,
        CancellationToken token = default
    );
    
    // 激活场景（用于 ManualConfirm 模式）
    void ActivateScene();
    
    // 带预加载的场景切换
    UniTask<AsakiSceneResult> LoadSceneWithPreloadAsync(
        string targetSceneName,
        string loadingSceneName = "LoadingScene",
        CancellationToken token = default
    );
}
```

#### 方法说明

| 方法 | 说明 |
|------|------|
| `LoadSceneAsync` | 异步加载指定场景，支持过渡动画和取消令牌 |
| `LoadSceneWithPreloadAsync` | A->C(Loading)->B 流程，先加载过渡场景，在过渡场景中预加载目标场景资源 |
| `ActivateScene` | 手动激活场景（配合 `ManualConfirm` 模式使用） |
| `PerBuildScene` | 预构建场景名称缓存，用于快速验证场景有效性 |

### IAsakiSceneTransition

场景过渡动画接口，用于实现自定义的转场效果。

```csharp
public interface IAsakiSceneTransition : IDisposable
{
    UniTask EnterAsync(CancellationToken ct);   // 进入过渡动画
    void OnProgress(float normalizedProgress);  // 进度更新回调
    UniTask ExitAsync(CancellationToken ct);    // 退出过渡动画
}
```

---

## 枚举类型

### AsakiLoadSceneMode

```csharp
public enum AsakiLoadSceneMode
{
    Single,     // 单场景模式，加载新场景时卸载当前场景
    Additive    // 叠加模式，保留当前场景
}
```

### AsakiSceneActivation

```csharp
public enum AsakiSceneActivation
{
    Immediate,      // 加载完成后立即激活
    ManualConfirm   // 等待手动确认（如"按任意键继续"）
}
```

---

## 数据结构

### AsakiSceneResult

场景加载操作的结果结构。

```csharp
public readonly struct AsakiSceneResult
{
    public readonly bool Success;       // 是否成功
    public readonly string SceneName;   // 场景名称
    public readonly string ErrorMessage;// 错误信息
    
    // 工厂方法
    public static AsakiSceneResult Ok(string sceneName);
    public static AsakiSceneResult Failed(string sceneName, string errorMessage);
    public static AsakiSceneResult OperationCanceled(string sceneName);
}
```

### AsakiSceneProgressEvent

场景加载进度事件，通过事件总线发布。

```csharp
public struct AsakiSceneProgressEvent : IAsakiEvent
{
    public readonly string SceneName;
    public readonly float Progress;  // 0.0 ~ 1.0
}
```

### AsakiSceneStateEvent

场景加载状态事件。

```csharp
public readonly struct AsakiSceneStateEvent : IAsakiEvent
{
    public enum State
    {
        Started,    // 开始加载
        Completed,  // 加载完成
        Failed,     // 加载失败
        Cancelled   // 已取消
    }
    
    public readonly string SceneName;
    public readonly State CurrentState;
    public readonly string ErrorMessage;
}
```

---

## Unity 实现

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

---

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

> **重要**: 该组件通过 `ILoadingSceneView` 接口与 UI 解耦，便于 Asaki 框架打包为 UPM 包。

**Inspector 配置:**

| 字段 | 类型 | 说明 |
|------|------|------|
| `_preloadDatabase` | ScenePreloadDatabase | 预加载配置数据库 |
| `_defaultLoadingSceneName` | string | 默认过渡场景名称 |
| `_loadingView` | MonoBehaviour | 加载场景视图组件（需实现 `ILoadingSceneView` 接口） |

**公共方法:**

```csharp
// 手动触发场景切换（用于非自动过渡模式）
public void TriggerSceneTransition()

// 取消加载
public void CancelLoading()
```

#### ILoadingSceneView

加载场景视图接口，由游戏开发者在 Game 程序集中实现。

> **设计原则**: 接口只关注进度报告，不涉及业务相关的提示信息（如"准备加载..."、"加载完成"等），这些由开发者自行在实现中处理。

```csharp
public interface ILoadingSceneView
{
    void UpdateProgress(float progress);    // 更新进度 0.0 ~ 1.0
    void Show();                            // 显示视图
    void Hide();                            // 隐藏视图
}
```

**实现示例**（在 Game 项目中）：

```csharp
// 在 Game/Scripts/UI/LoadingScene/ 中实现
public class LoadingPanelWindow : AsakiUIWindow, ILoadingSceneView
{
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private TMP_Text progressDetails;

    public void UpdateProgress(float progress)
    {
        progressBar.value = progress;
        progressDetails.text = $"{progress * 100:F0}%";
        
        // 开发者自行处理提示信息
        if (progress < 0.3f)
            loadingText.text = "准备加载...";
        else if (progress < 0.9f)
            loadingText.text = "加载资源中...";
        else if (progress < 1f)
            loadingText.text = "即将完成...";
        else
            loadingText.text = "加载完成";
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}
```

**场景配置**:

1. 创建 LoadingScene
2. 添加 GameObject，挂载 `LoadingSceneController`
3. 创建 UI GameObject，挂载实现 `ILoadingSceneView` 的组件（如 `LoadingPanelWindow`）
4. 将 UI 组件赋值给 `LoadingSceneController` 的 `_loadingView` 字段

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

---

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

### 监听加载进度

```csharp
// 订阅进度事件
AsakiBroker.Subscribe<AsakiSceneProgressEvent>(OnSceneProgress);
AsakiBroker.Subscribe<AsakiSceneStateEvent>(OnSceneStateChanged);

private void OnSceneProgress(AsakiSceneProgressEvent evt)
{
    _progressBar.value = evt.Progress;
    _progressText.text = $"{evt.Progress * 100:F0}%";
}

private void OnSceneStateChanged(AsakiSceneStateEvent evt)
{
    switch (evt.CurrentState)
    {
        case AsakiSceneStateEvent.State.Started:
            ShowLoadingUI();
            break;
        case AsakiSceneStateEvent.State.Completed:
            HideLoadingUI();
            break;
        case AsakiSceneStateEvent.State.Failed:
            ShowError(evt.ErrorMessage);
            break;
    }
}
```

---

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

---

## 性能优化建议

1. **预加载策略**: 只预加载关键资源，避免加载过多导致内存压力
2. **超时设置**: 合理设置超时时间，防止加载卡住
3. **资源清理**: 单场景模式下会自动调用 `UnloadUnusedAssets` 和 `GC.Collect`
4. **进度平滑**: 使用过渡动画掩盖加载过程中的卡顿

---

## 注意事项

1. 场景必须添加到 Build Settings 才能被加载
2. `LoadSceneWithPreloadAsync` 需要在 LoadingScene 中配置 `LoadingSceneController`
3. 预加载的资源会在目标场景加载完成后自动可用
4. 取消加载操作会触发 `AsakiSceneStateEvent.State.Cancelled` 事件

---

## 设计原则

1. **平台无关性**: 核心接口不依赖 Unity 引擎，可在任何 C# 环境中使用
2. **异步优先**: 所有场景加载操作都是异步的，使用 UniTask 提供高性能异步支持
3. **可取消**: 支持 CancellationToken，可随时取消加载操作
4. **事件驱动**: 通过事件总线发布加载进度和状态变化
5. **扩展性**: 通过 IAsakiSceneTransition 接口支持自定义过渡动画

---

## 依赖关系

### Core 层依赖

```
Core/Scene
├── Core/Context      (IAsakiService)
├── Core/Broker       (IAsakiEvent)
└── UniTask           (UniTask<T>)
```

### Unity 层依赖

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

---

## API 快速参考

### 服务接口

| API | 返回值 | 说明 |
|-----|--------|------|
| `LoadSceneAsync(name, mode, activation, transition, token)` | `UniTask<AsakiSceneResult>` | 加载场景 |
| `LoadSceneWithPreloadAsync(target, loading, token)` | `UniTask<AsakiSceneResult>` | 带预加载的场景切换 |
| `ActivateScene()` | `void` | 手动激活场景 |
| `PerBuildScene()` | `void` | 预构建场景缓存 |

### 事件类型

| 事件 | 说明 |
|------|------|
| `AsakiSceneProgressEvent` | 加载进度更新 |
| `AsakiSceneStateEvent` | 加载状态变化 |

### 配置类

| 类 | 说明 |
|----|------|
| `ScenePreloadConfig` | 单场景预加载配置 |
| `ScenePreloadDatabase` | 预加载配置数据库 |
| `SceneLoadPayload` | 场景加载参数 |
| `SceneLoadStateService` | 跨场景状态服务 |
