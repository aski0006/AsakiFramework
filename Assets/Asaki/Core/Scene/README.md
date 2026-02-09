# Asaki 场景管理核心接口

本目录包含 Asaki 框架场景管理服务的核心抽象接口和类型定义，遵循依赖倒置原则，不依赖任何 Unity 引擎代码。

## 目录结构

```
Core/Scene/
├── IAsakiSceneManagerService.cs    # 场景管理器服务接口
├── IAsakiSceneTransition.cs        # 场景过渡动画接口
├── AsakiLoadSceneMode.cs           # 场景加载模式枚举
├── AsakiSceneResult.cs             # 场景加载结果结构
└── AsakiSceneProgressEvent.cs      # 场景加载进度事件
```

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

## 使用示例

### 基础场景加载

```csharp
public class GameController : IAsakiInit<IAsakiSceneManagerService>
{
    private IAsakiSceneManagerService _sceneManager;
    
    [AsakiInject]
    public void Init(IAsakiSceneManagerService sceneManager)
    {
        _sceneManager = sceneManager;
    }
    
    public async UniTask LoadGameLevel(string levelName)
    {
        var result = await _sceneManager.LoadSceneAsync(levelName);
        
        if (result.IsSuccess)
        {
            Debug.Log($"场景 {levelName} 加载成功");
        }
        else
        {
            Debug.LogError($"场景加载失败: {result.ErrorMessage}");
        }
    }
}
```

### 带过渡动画的场景切换

```csharp
// 实现自定义过渡动画
public class FadeTransition : IAsakiSceneTransition
{
    private readonly CanvasGroup _canvasGroup;
    
    public async UniTask EnterAsync(CancellationToken ct)
    {
        // 淡入黑屏
        await _canvasGroup.DOFade(1f, 0.5f)
            .ToUniTask(cancellationToken: ct);
    }
    
    public void OnProgress(float normalizedProgress)
    {
        // 更新进度条显示
    }
    
    public async UniTask ExitAsync(CancellationToken ct)
    {
        // 淡出黑屏
        await _canvasGroup.DOFade(0f, 0.5f)
            .ToUniTask(cancellationToken: ct);
    }
    
    public void Dispose() { }
}

// 使用过渡动画
var transition = new FadeTransition(canvasGroup);
var result = await _sceneManager.LoadSceneAsync(
    "GameScene",
    AsakiLoadSceneMode.Single,
    AsakiSceneActivation.Immediate,
    transition
);
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

### 手动确认模式

```csharp
// 加载场景但不立即激活
await _sceneManager.LoadSceneAsync(
    "GameScene",
    AsakiLoadSceneMode.Single,
    AsakiSceneActivation.ManualConfirm  // 手动确认模式
);

// 显示"按任意键继续"
await ShowPressAnyKeyPrompt();

// 用户按键后激活场景
_sceneManager.ActivateScene();
```

### 带预加载的场景切换

```csharp
// A -> LoadingScene -> B 流程
// 先加载 LoadingScene，在其中预加载资源，最后切换到目标场景
public async UniTask LoadLevelWithPreload(string targetLevel)
{
    var result = await _sceneManager.LoadSceneWithPreloadAsync(
        targetLevel,
        "LoadingScene"  // 可选，默认为 "LoadingScene"
    );
    
    if (result.IsSuccess)
    {
        // LoadingScene 已加载，预加载逻辑在 LoadingSceneController 中执行
    }
}
```

## 设计原则

1. **平台无关性**: 核心接口不依赖 Unity 引擎，可在任何 C# 环境中使用
2. **异步优先**: 所有场景加载操作都是异步的，使用 UniTask 提供高性能异步支持
3. **可取消**: 支持 CancellationToken，可随时取消加载操作
4. **事件驱动**: 通过事件总线发布加载进度和状态变化
5. **扩展性**: 通过 IAsakiSceneTransition 接口支持自定义过渡动画

## 依赖关系

```
Core/Scene
├── Core/Context      (IAsakiService)
├── Core/Broker       (IAsakiEvent)
└── UniTask           (UniTask<T>)
```
