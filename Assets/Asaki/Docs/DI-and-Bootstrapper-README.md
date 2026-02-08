# Asaki 框架 - 依赖注入与启动系统

## 概述

Asaki框架提供了一套高性能、零反射（运行时）的依赖注入与启动系统，专为Unity游戏开发设计。系统采用**Copy-On-Write**架构、**代码生成**技术和**DAG拓扑排序**，确保启动速度和运行性能达到最优。

## 核心特性

- **极速服务容器**：基于Copy-On-Write + Snapshot Swap的无锁读取架构
- **零反射运行时**：依赖注入通过Roslyn代码生成器实现，运行时零反射开销
- **模块化启动**：支持DAG（有向无环图）依赖排序，确保模块按正确顺序初始化
- **多层级解析**：全局服务、场景服务、临时参数三级解析体系
- **异步初始化**：支持UniTask的异步模块初始化
- **动态组件初始化**：自动处理动态附加组件和场景切换时的初始化

---

## 架构概览

```
┌─────────────────────────────────────────────────────────────────┐
│                     AsakiBootstrapper                           │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │  模块发现    │→│  模块加载    │→│      场景注入系统         │  │
│  │(Discovery)  │  │  (Loader)   │  │   (Scene Injection)     │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│              AsakiMonoLifecycleManager                          │
│         [AsakiMono 生命周期管理器 - V2.0 新增]                     │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  • 动态组件初始化                                        │    │
│  │  • 场景切换处理                                          │    │
│  │  • 初始化状态跟踪                                        │    │
│  │  • 依赖注入协调                                          │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      AsakiContext                               │
│              [极速微内核 - 服务容器]                              │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  Copy-On-Write + Snapshot Swap (V5.1 Lock-Free Edition) │    │
│  │  • 读操作 (Get): O(1), 无锁, 仅一次引用解引用              │    │
│  │  • 写操作 (Register): O(n), 有锁, 仅在启动时发生          │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
          ┌───────────────────┼───────────────────┐
          ▼                   ▼                   ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│ AsakiGlobal     │  │ AsakiScene      │  │ AsakiTransient  │
│   Resolver      │  │   Context       │  │   Resolver      │
│   (全局解析)     │  │   (场景解析)     │  │   (临时解析)     │
└─────────────────┘  └─────────────────┘  └─────────────────┘
```

---

## 核心组件

### 1. 服务容器 (AsakiContext)

**文件**: `Core/Context/AsakiContext.cs`

框架的核心服务容器，采用**Copy-On-Write**架构实现极速读取。

#### 核心API

```csharp
// 读取服务 (无锁，O(1)性能)
T service = AsakiContext.Get<T>();
bool found = AsakiContext.TryGet<T>(out var service);

// 注册服务 (启动期使用)
AsakiContext.Register<T>(service);
AsakiContext.Register(type, service);

// 运行时替换 (热更新场景)
AsakiContext.Replace<T>(newService);

// 获取或注册 (懒加载模式)
var service = AsakiContext.GetOrRegister<T>(() => new T());

// 架构控制
AsakiContext.Freeze();      // 冻结容器，防止运行时注册
AsakiContext.ClearAll();    // 清空所有服务
```

#### 性能特征

| 操作 | 时间复杂度 | 线程安全 | 使用场景 |
|------|-----------|---------|---------|
| Get | O(1) | 无锁 | 主程热路径 |
| TryGet | O(1) | 无锁 | 主程热路径 |
| Register | O(n) | 有锁 | 启动期 |
| Replace | O(n) | 有锁 | 热更新 |

---

### 2. AsakiMono 生命周期管理器 (V2.0 新增)

**文件**: `Unity/AsakiMonoLifecycleManager.cs`

专门解决动态附加组件和场景切换时的初始化问题。

#### 核心职责

- **动态组件初始化**：自动检测和处理通过代码动态附加的AsakiMono组件
- **场景切换处理**：在场景加载时自动发现并初始化新场景中的组件
- **状态跟踪**：跟踪所有AsakiMono组件的初始化状态
- **依赖注入协调**：确保组件在正确的时间点接收依赖注入

#### 工作流程

```
┌─────────────────────────────────────────────────────────────────┐
│                    AsakiMono 初始化流程                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1. 组件创建 (Awake)                                             │
│     │                                                           │
│     ▼                                                           │
│  ┌─────────────────┐                                            │
│  │ 注册到生命周期   │                                            │
│  │ 管理器          │                                            │
│  └─────────────────┘                                            │
│     │                                                           │
│     ▼                                                           │
│  2. 检查框架状态                                                 │
│     │                                                           │
│     ├── 框架已就绪 ──→ 立即执行依赖注入 + 激活                    │
│     │                                                           │
│     └── 框架未就绪 ──→ 加入等待队列                               │
│                          │                                      │
│                          ▼                                      │
│                    等待框架就绪事件                               │
│                          │                                      │
│                          ▼                                      │
│  3. 框架就绪时 ────────→ 批量处理等待队列                         │
│                          │                                      │
│                          ▼                                      │
│                    执行依赖注入 → 调用 OnStart                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

#### 使用方式

**对于普通场景组件**：
无需额外操作，LifecycleManager会自动处理。

```csharp
public class MyComponent : AsakiMono
{
    protected override void OnStart()
    {
        // 框架已就绪，可以安全使用服务
        var service = AsakiContext.Get<IMyService>();
    }
}
```

**对于动态附加组件**：
```csharp
// 动态创建GameObject并添加组件
var go = new GameObject("DynamicObject");
var component = go.AddComponent<MyComponent>();

// 如果框架已就绪，LifecycleManager会自动处理
// 如果需要立即处理：
if (AsakiBootstrapper.IsReady)
{
    AsakiMonoLifecycleManager.Instance.ProcessComponentImmediately(component);
}
```

**对于Prefab实例化**：
```csharp
// 使用AsakiUIWindow.Create工厂方法
var window = AsakiUIWindow.Create(myWindowPrefab, parent, usePool: false);

// 或使用IAsakiUIService
await uiService.OpenAsync<MyWindow>(windowId);
```

---

### 3. AsakiMono 基类

**文件**: `Unity/AsakiMono.cs`

所有使用Asaki框架的MonoBehaviour的基类。

#### 关键特性

```csharp
public abstract class AsakiMono : MonoBehaviour
{
    // 组件是否已激活（框架就绪且OnStart已调用）
    protected bool IsActivated { get; private set; }
    
    // 公共属性，用于外部检查初始化状态
    public bool IsInitialized => IsActivated;
    
    // 是否正在等待初始化
    public bool IsPendingInitialization => !IsActivated && _lifecycleTrackingId > 0;
}
```

#### 生命周期方法

| 方法 | 调用时机 | 用途 |
|------|---------|------|
| `OnAwake()` | Awake | 本地初始化，不依赖框架服务 |
| `OnStart()` | 框架就绪后 | 访问框架服务，执行业务逻辑初始化 |
| `EnableComponent()` | OnEnable | 注册事件监听 |
| `DisableComponent()` | OnDisable | 注销事件监听 |
| `OnUpdate()` | 每帧（激活后） | 业务逻辑更新 |
| `Cleanup()` | OnDestroy | 资源清理 |

#### 静态辅助方法

```csharp
// 等待框架就绪
AsakiMono.WhenReady(() =>
{
    // 安全地访问框架服务
    var config = AsakiContext.Get<IConfigService>();
    InitializeWithConfig(config);
});
```

---

### 4. 服务接口体系

**文件**: `Core/Context/IAsakiService.cs`

```csharp
// 基础服务标记接口
public interface IAsakiService { }

// 场景级服务
public interface IAsakiSceneService : IAsakiService { }

// 全局MonoBehaviour服务
public interface IAsakiGlobalService : IAsakiService
{
    void OnBootstrapInit();  // 引导程序初始化时调用
}
```

---

### 5. 模块系统 (IAsakiModule)

**文件**: `Core/Context/IAsakiModule.cs`

模块是框架的核心组织单元，支持同步和异步两阶段初始化。

```csharp
[AsakiModule(priority: 100, dependencies: new[] { typeof(OtherModule) })]
public class MyModule : IAsakiModule
{
    // ========== 同步初始化阶段 ==========
    public void OnInit()
    {
        // 职责：
        // • 获取配置 (AsakiContext.Get<AsakiConfig>)
        // • 获取依赖模块 (AsakiContext.Get<OtherModule>())
        // • 注册子服务 (AsakiContext.Register<IService>(...))
    }

    // ========== 异步初始化阶段 ==========
    public async UniTask OnInitAsync()
    {
        // 职责：
        // • 资源加载
        // • 网络连接
        // • 数据库预热
        await LoadResourcesAsync();
    }

    // ========== 销毁阶段 ==========
    public void OnDispose()
    {
        // 职责：
        // • 清理非托管资源
        // • 断开连接
    }
}
```

#### 模块特性 (AsakiModuleAttribute)

**文件**: `Core/Attributes/AsakiModuleAttribute.cs`

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class AsakiModuleAttribute : Attribute
{
    public int Priority { get; }           // 优先级 (值越小越早，默认1000)
    public Type[] Dependencies { get; }    // 依赖模块列表

    public AsakiModuleAttribute(int priority = 1000, params Type[] dependencies)
}
```

#### 注入特性 (AsakiInjectAttribute)

**文件**: `Core/Attributes/AsakiInjectAttribute.cs`

```csharp
[AttributeUsage(AttributeTargets.Method)]  // 注意：只能标记方法！
public sealed class AsakiInjectAttribute : Attribute { }
```

**重要说明**：
- `[AsakiInject]` **只能标记方法**，不能标记字段或属性
- 需要配合 `IAsakiAutoInject` 接口和 `IAsakiInit<T...>` 接口使用
- 依赖通过**方法参数**显式传递，体现Asaki框架"显式声明依赖"的设计理念

**使用示例**：

```csharp
public class MyView : MonoBehaviour, IAsakiAutoInject, IAsakiInit<IPlayerService>
{
    private IPlayerService _playerService;

    [AsakiInject]  // 标记在方法上
    public void Init(IPlayerService playerService)  // 依赖通过参数接收
    {
        _playerService = playerService;
    }
}
```

---

### 6. 依赖解析器体系

#### 6.1 全局解析器 (AsakiGlobalResolver)

**文件**: `Core/Context/Resolvers/AsakiGlobalResolver.cs`

```csharp
// 结构体实现，零分配
public readonly struct AsakiGlobalResolver : IAsakiResolver
{
    public stati
c readonly AsakiGlobalResolver Instance = new();

    public T Get<T>() where T : class, IAsakiService
        => AsakiContext.Get<T>();

    public bool TryGet<T>(out T service) where T : class, IAsakiService
        => AsakiContext.TryGet(out service);
}
```

#### 6.2 场景上下文 (AsakiSceneContext)

**文件**: `Core/Context/Resolvers/AsakiSceneContext.cs`

管理场景级别的服务，采用两阶段初始化：
1. **Awake**: 仅注册服务，不调用Init
2. **Build**: 由Bootstrapper显式调用，执行Init

```csharp
// 在场景根物体上挂载
public class AsakiSceneContext : MonoBehaviour, IAsakiResolver
{
    [SerializeReference]
    private List<IAsakiSceneService> _pureCSharpServices;

    [SerializeField]
    private List<MonoBehaviour> _behaviourServices;
}
```

**解析优先级**: 本地服务 → 全局服务

#### 6.3 临时解析器 (AsakiTransientResolver)

**文件**: `Core/Context/Resolvers/AsakiTransientResolver.cs`

用于传递临时参数或覆盖默认服务。

```csharp
public readonly struct AsakiTransientResolver : IAsakiResolver
{
    public AsakiTransientResolver(IAsakiResolver parent, object param) { }

    // 解析优先级: 临时参数 → 父级解析器
}
```

---

### 7. 引导程序 (AsakiBootstrapper)

**文件**: `Unity/Bootstrapper/AsakiBootstrapper.cs`

框架的入口点，负责整个系统的启动流程。

#### 启动流程

```
1. Awake
   ├── 单例初始化
   ├── 加载配置 (AsakiConfig)
   ├── 注册日志服务
   └── 注册全局MonoBehaviour服务

2. StartAsync
   ├── 模块发现 (Discovery)
   ├── 模块加载 (DAG排序 → 实例化 → OnInit → OnInitAsync)
   ├── 冻结容器 (Freeze)
   ├── 初始化全局服务 (OnBootstrapInit)
   ├── 注册场景加载事件
   ├── 注入当前场景
   ├── 初始化 LifecycleManager
   └── 广播就绪事件 (OnAsakiFrameworkReadyEvent)
```

#### 配置选项

```csharp
public class AsakiBootstrapper : MonoBehaviour
{
    [SerializeField] private bool _autoScanOnSceneLoad = true;  // 自动扫描场景注入
    [SerializeField] private MonoBehaviour[] _manualTargets;     // 手动指定注入目标
    [SerializeField] private MonoBehaviour[] _globalBehaviourServices; // 全局服务
    [SerializeField] private AsakiConfig _config;                // 框架配置
}
```

#### 公共API

```csharp
// 等待框架就绪
await AsakiBootstrapper.WaitForReadyAsync();

// 检查就绪状态
bool ready = AsakiBootstrapper.IsReady;

// 确保实例存在
AsakiBootstrapper.EnsureInstance();
```

---

### 8. 模块加载器 (AsakiModuleLoader)

**文件**: `Unity/Bootstrapper/AsakiModuleLoader.cs`

负责模块的DAG排序和生命周期管理。

#### 初始化流程

```
Phase 1: Registration & Sync Init
├── 拓扑排序 (DAG)
├── 实例化模块 (无参构造)
├── 依赖注入 (InjectDependenciesSafe)
├── 注册到容器 (AsakiContext.Register)
└── 同步初始化 (OnInit)

Phase 2: Async Initialization
└── 异步初始化 (OnInitAsync) - 按DAG顺序
```

#### 拓扑排序算法

模块加载器使用Kahn算法进行拓扑排序，确保：
- 依赖项先于被依赖项初始化
- 同层级按Priority排序
- 循环依赖检测

---

### 9. 依赖注入系统

Asaki框架采用**显式方法注入**模式，通过 `[AsakiInject]` 特性标记方法，依赖通过方法参数传递。这种设计遵循显式声明依赖的理念，使代码更加清晰可维护。

#### 9.1 注入原理

```csharp
// 1. 实现 IAsakiAutoInject 标记接口
// 2. 实现 IAsakiInit<T...> 接口声明需要的依赖
// 3. 使用 [AsakiInject] 标记方法接收依赖

public class MyView : MonoBehaviour, IAsakiAutoInject, IAsakiInit<IPlayerService, IAudioService>
{
    private IPlayerService _playerService;
    private IAudioService _audioService;

    [AsakiInject]  // 标记在方法上，不是字段！
    public void Init(IPlayerService playerService, IAudioService audioService)
    {
        _playerService = playerService;
        _audioService = audioService;
    }
}
```

#### 9.2 全局注入器 (AsakiGlobalInjector)

**文件**: `Unity/Bootstrapper/AsakiGlobalInjector.cs`

```csharp
public static class AsakiGlobalInjector
{
    // 注册程序集注入器 (由生成代码调用)
    public static void Register(IAsakiInjector injector)

    // 执行注入
    public static void Inject(object target, IAsakiResolver resolver = null)
}
```

#### 9.3 自动注入标记 (IAsakiAutoInject)

**文件**: `Core/Context/IAsakiAutoInject.cs`

```csharp
// 标记接口，实现此接口的类将被自动注入
public interface IAsakiAutoInject { }

// 注入器接口 (由Roslyn生成器实现)
public interface IAsakiInjector
{
    void Inject(object target, IAsakiResolver resolver = null);
}
```

#### 9.4 场景注入流程

```
场景加载
    │
    ▼
查找 AsakiSceneContext
    │
    ├── 存在 → 调用 Build() → 作为Resolver
    │
    └── 不存在 → 使用 AsakiGlobalResolver
    │
    ▼
扫描场景中的 MonoBehaviour
    │
    ▼
对标记 IAsakiAutoInject 的对象执行注入
```

---

### 10. 模块发现系统

#### 10.1 静态模块发现 (AsakiStaticModuleDiscovery)

**文件**: `Unity/Bootstrapper/AsakiStaticModuleDiscovery.cs`

**零反射**的模块发现机制，配合Roslyn生成器使用。

```csharp
public class AsakiStaticModuleDiscovery : IAsakiModuleDiscovery
{
    // 由Roslyn生成的代码调用
    public static void Register(Type moduleType)

    // 返回所有已注册的模块类型
    public IEnumerable<Type> GetModuleTypes()
}
```

**生成的代码示例**:

```csharp
// 由Roslyn生成器自动生成
static class GeneratedModuleRegistry
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterModules()
    {
        AsakiStaticModuleDiscovery.Register(typeof(CoreModule));
        AsakiStaticModuleDiscovery.Register(typeof(DataModule));
        AsakiStaticModuleDiscovery.Register(typeof(GameModule));
    }
}
```

---

### 11. 初始化工厂 (AsakiInitFactory)

**文件**: `Core/Context/IAsakiInit.cs`

提供统一的对象实例化和初始化模式。

```csharp
// 初始化接口 (支持0-5个参数)
public interface IAsakiInit { void Init(IAsakiResolver resolver = null); }
public interface IAsakiInit<in T1> { void Init(T1 args); }
public interface IAsakiInit<in T1, in T2> { void Init(T1 args1, T2 args2); }
// ... 最多5个参数

// 工厂方法
public static class AsakiInitFactory
{
    public static T Instantiate<T>(T prefab, Transform parent = null)
        where T : MonoBehaviour, IAsakiInit;

    public static T Instantiate<T, TArg1>(T prefab, TArg1 arg1, Transform parent = null)
        where T : MonoBehaviour, IAsakiInit<TArg1>;

    // ... 支持位置和旋转参数的重载
}
```

**使用示例**:

```csharp
// 定义可初始化的MonoBehaviour
public class MyView : MonoBehaviour, IAsakiInit<IPlayerData>
{
    private IPlayerData _data;

    public void Init(IPlayerData data)
    {
        _data = data;
        UpdateUI();
    }
}

// 实例化并初始化
var view = AsakiInitFactory.Instantiate(_viewPrefab, playerData, parent);
```

---

## 使用指南

### 快速开始

#### 1. 创建模块

```csharp
using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Cysharp.Threading.Tasks;

[AsakiModule(priority: 100)]
public class GameDataModule : IAsakiModule
{
    private GameConfig _config;

    public void OnInit()
    {
        // 获取配置
        _config = AsakiContext.Get<AsakiConfig>().GameConfig;
    }

    public async UniTask OnInitAsync()
    {
        // 异步加载数据
        await LoadGameDataAsync();
    }

    public void OnDispose()
    {
        // 清理资源
        SaveGameData();
    }
}
```

#### 2. 创建服务

```csharp
// 定义服务接口
public interface IPlayerService : IAsakiService
{
    PlayerData GetPlayerData();
    void UpdatePlayerData(PlayerData data);
}

// 实现服务
public class PlayerService : IPlayerService
{
    private PlayerData _data;

    public PlayerData GetPlayerData() => _data;
    public void UpdatePlayerData(PlayerData data) => _data = data;
}
```

#### 3. 注册和使用服务

```csharp
// 在模块中注册
public void OnInit()
{
    AsakiContext.Register<IPlayerService>(new PlayerService());
}

// 在其他地方使用 - 方法注入（显式声明依赖）
public class PlayerView : MonoBehaviour, IAsakiAutoInject, IAsakiInit<IPlayerService>
{
    private IPlayerService _playerService;

    // [AsakiInject] 标记方法，通过参数接收依赖
    [AsakiInject]
    public void Init(IPlayerService playerService)
    {
        _playerService = playerService;
        var data = _playerService.GetPlayerData();
        UpdateUI(data);
    }
}
```

#### 4. 场景上下文设置

```csharp
// 创建场景服务
public class BattleSceneService : MonoBehaviour, IAsakiSceneService, IAsakiInit
{
    private IPlayerService _playerService;

    public void Init(IAsakiResolver resolver)
    {
        _playerService = resolver.Get<IPlayerService>();
    }
}

// 在场景中
// 1. 创建空物体，挂载 AsakiSceneContext
// 2. 将 BattleSceneService 添加到 _behaviourServices 列表
```

---

### 最佳实践

#### 1. 显式声明依赖（核心设计理念）

Asaki框架遵循**显式声明依赖**的设计理念，通过方法参数明确表达类的依赖关系，而非使用字段级别的隐藏注入。

```csharp
// ✅ Asaki推荐：显式方法注入
// 依赖通过方法参数清晰可见
public class PlayerView : MonoBehaviour, IAsakiAutoInject, IAsakiInit<IPlayerService, IAudioService>
{
    private IPlayerService _playerService;
    private IAudioService _audioService;

    [AsakiInject]
    public void Init(IPlayerService playerService, IAudioService audioService)
    {
        _playerService = playerService;
        _audioService = audioService;
    }
}

// ❌ 不推荐：字段注入（隐藏依赖）
// 其他框架可能这样做，但Asaki不支持
public class PlayerView : MonoBehaviour
{
    [Inject] private IPlayerService _playerService;  // 依赖隐藏，不清晰
    [Inject] private IAudioService _audioService;    // 不知道这些依赖从哪里来
}
```

**显式注入的优势**：
- **代码自文档化**：方法签名清晰展示所有依赖
- **易于测试**：可以直接调用Init方法传入Mock对象
- **编译期安全**：接口约束确保依赖完整性
- **IDE友好**：自动补全和重构支持更好

#### 2. 服务设计原则

```csharp
// ✅ 好的做法：接口隔离
public interface IAudioService : IAsakiService
{
    void PlaySound(string id);
    void PlayMusic(string id);
}

public interface ISettingsService : IAsakiService
{
    float MasterVolume { get; set; }
    bool IsMuted { get; set; }
}

// ❌ 避免：上帝接口
public interface IGameService : IAsakiService  // 不要这样做
{
    void PlaySound(string id);
    void PlayMusic(string id);
    float MasterVolume { get; set; }
    PlayerData GetPlayerData();
    // ... 更多功能
}
```

#### 3. 模块依赖管理

```csharp
// ✅ 显式声明依赖
[AsakiModule(priority: 200, dependencies: new[] { typeof(AudioModule), typeof(DataModule) })]
public class GameModule : IAsakiModule
{
    public void OnInit()
    {
        // 可以安全地获取依赖模块
        var audio = AsakiContext.Get<AudioModule>();
        var data = AsakiContext.Get<DataModule>();
    }
}

// ❌ 避免：隐式依赖
[AsakiModule(priority: 200)]  // 缺少依赖声明
public class GameModule : IAsakiModule
{
    public void OnInit()
    {
        var audio = AsakiContext.Get<AudioModule>();  // 可能未初始化！
    }
}
```

#### 4. 异步初始化

```csharp
// ✅ 好的做法：超时处理
public async UniTask OnInitAsync()
{
    // 使用超时防止无限等待
    await LoadResourcesAsync()
        .Timeout(TimeSpan.FromSeconds(30));
}

// ✅ 好的做法：错误处理
public async UniTask OnInitAsync()
{
    try
    {
        await ConnectToServerAsync();
    }
    catch (Exception ex)
    {
        ALog.Error($"Failed to connect: {ex}");
        // 降级处理或重试
    }
}
```

#### 5. 生命周期管理

```csharp
// ✅ 好的做法：正确实现IDisposable
public class ResourceService : IAsakiService, IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // 清理资源
        UnloadAllAssets();
    }
}

// ✅ 好的做法：模块中清理
public void OnDispose()
{
    // 清理非托管资源
    _nativeHandle?.Free();
    _networkConnection?.Disconnect();
}
```

#### 6. 动态组件创建 (V2.0 新增)

```csharp
// ✅ 推荐：使用工厂方法创建UI窗口
var window = AsakiUIWindow.Create(myWindowPrefab, parent, usePool: true);

// ✅ 推荐：使用IAsakiUIService打开窗口
await uiService.OpenAsync<MyWindow>(windowId);

// ✅ 推荐：动态添加组件后手动触发初始化
var go = new GameObject("Dynamic");
var component = go.AddComponent<MyComponent>();
if (AsakiBootstrapper.IsReady)
{
    AsakiMonoLifecycleManager.Instance.ProcessComponentImmediately(component);
}

// ✅ 推荐：使用WhenReady等待框架就绪
AsakiMono.WhenReady(() =>
{
    var service = AsakiContext.Get<IMyService>();
    Initialize(service);
});
```

---

## 性能优化

### 1. 读取性能

```csharp
// ✅ 缓存服务引用，避免重复查找
public class GameManager : MonoBehaviour, IAsakiAutoInject, IAsakiInit<IPlayerService, IAudioService>
{
    private IPlayerService _playerService;
    private IAudioService _audioService;

    [AsakiInject]
    public void Init(IPlayerService playerService, IAudioService audioService)
    {
        _playerService = playerService;
        _audioService = audioService;
    }

    private void Update()
    {
        // 使用缓存的引用，无需查找
        _playerService.Update();
    }
}

// ❌ 避免：每帧查找
private void Update()
{
    AsakiContext.Get<IPlayerService>().Update();  // 虽然很快，但没必要
}
```

### 2. 启动优化

```csharp
// ✅ 使用懒加载
public void OnInit()
{
    // 延迟加载重型资源
    AsakiContext.GetOrRegister<IHeavyService>(() =>
    {
        return new HeavyService();
    });
}

// ✅ 异步加载
public async UniTask OnInitAsync()
{
    // 并行加载多个资源
    await UniTask.WhenAll(
        LoadAssetAsync("asset1"),
        LoadAssetAsync("asset2"),
        LoadAssetAsync("asset3")
    );
}
```

### 3. 场景切换优化 (V2.0 新增)

```csharp
// LifecycleManager会自动处理场景切换时的组件初始化
// 无需手动操作

// 如果需要监控场景切换性能
public class PerformanceMonitor : AsakiMono
{
    protected override void OnStart()
    {
        var stats = AsakiMonoLifecycleManager.Instance.GetStats();
        ALog.Info($"Lifecycle Stats: {stats}");
    }
}
```

---

## 故障排除

### 常见问题

#### 1. 服务未找到 (KeyNotFoundException)

```
[AsakiContext] Service not found: IPlayerService
```

**原因**: 服务未注册或注册顺序错误

**解决**:
- 检查模块是否正确标记 `[AsakiModule]`
- 检查依赖声明是否正确
- 确认服务在 `OnInit` 中注册

#### 2. 动态组件服务为null

```
[MyComponent] Service not available. This may happen if the component was not properly initialized.
```

**原因**: 动态附加的组件没有正确接收依赖注入

**解决**:
- 确保使用 `AsakiUIWindow.Create` 或 `IAsakiUIService` 创建UI窗口
- 对于普通组件，确保在Awake中调用 `RegisterWithLifecycleManager`
- 检查 `AsakiBootstrapper.IsReady` 状态

#### 3. 场景切换后组件未初始化

```
[MyComponent] Update called but IsActivated is false
```

**原因**: 新场景中的组件没有被LifecycleManager检测到

**解决**:
- 确保 `AsakiBootstrapper._autoScanOnSceneLoad` 为 `true`
- 检查场景是否正确加载
- 手动调用 `AsakiMonoLifecycleManager.Instance.ProcessComponentImmediately`

#### 4. 循环依赖

```
[Asaki] Circular dependency detected! Initialization aborted.
```

**原因**: 模块A依赖B，B又依赖A

**解决**: 重构代码，打破循环依赖，或使用事件机制解耦

#### 5. 容器已冻结

```
[AsakiContext] Container is Frozen! Cannot register new service 'X' at runtime.
```

**原因**: 尝试在 `Freeze()` 后注册服务

**解决**:
- 将注册移到模块的 `OnInit` 中
- 或使用 `Replace()` 方法进行热更新

---

## API参考

### AsakiContext

| 方法 | 描述 |
|------|------|
| `Get<T>()` | 获取服务实例 |
| `TryGet<T>(out T)` | 尝试获取服务实例 |
| `Register<T>(T)` | 注册服务实例 |
| `Replace<T>(T)` | 替换现有服务 |
| `GetOrRegister<T>(Func<T>)` | 获取或懒加载服务 |
| `Freeze()` | 冻结容器 |
| `ClearAll()` | 清空所有服务 |

### AsakiMonoLifecycleManager (V2.0 新增)

| 方法 | 描述 |
|------|------|
| `RegisterComponent(AsakiMono)` | 注册组件进行生命周期管理 |
| `UnregisterComponent(int)` | 注销组件 |
| `ProcessComponentImmediately(AsakiMono)` | 立即处理指定组件的初始化 |
| `ProcessPendingComponents(int)` | 批量处理等待中的组件 |
| `GetStats()` | 获取生命周期统计信息 |

### AsakiMono

| 属性 | 描述 |
|------|------|
| `IsActivated` | 组件是否已激活 |
| `IsInitialized` | 组件是否已完成初始化 |
| `IsPendingInitialization` | 组件是否正在等待初始化 |

| 方法 | 描述 |
|------|------|
| `WhenReady(Action)` | 等待框架就绪后执行操作 |

### IAsakiModule

| 方法 | 阶段 | 描述 |
|------|------|------|
| `OnInit()` | Phase 1 | 同步初始化 |
| `OnInitAsync()` | Phase 2 | 异步初始化 |
| `OnDispose()` | Shutdown | 销毁清理 |

### AsakiBootstrapper

| 方法 | 描述 |
|------|------|
| `WaitForReadyAsync()` | 等待框架就绪 |
| `EnsureInstance()` | 确保实例存在 |

| 属性 | 描述 |
|------|------|
| `IsReady` | 框架是否就绪 |
| `Instance` | 单例实例 |

### AsakiUIWindow

| 方法 | 描述 |
|------|------|
| `Create<T>()` | 创建UI窗口实例（支持对象池） |
| `OnOpenAsync()` | 异步打开窗口 |
| `OnCloseAsync()` | 异步关闭窗口 |
| `Close()` | 同步关闭窗口入口 |

---

## 文件结构

```
Assets/Asaki/
├── Unity/
│   ├── AsakiMono.cs                     # AsakiMono基类
│   ├── AsakiMonoLifecycleManager.cs     # 生命周期管理器 (V2.0 新增)
│   └── Bootstrapper/
│       ├── AsakiBootstrapper.cs         # 引导程序
│       ├── AsakiModuleLoader.cs         # 模块加载器
│       ├── AsakiGlobalInjector.cs       # 全局注入器
│       └── AsakiStaticModuleDiscovery.cs # 静态模块发现
├── Core/
│   └── Context/
│       ├── AsakiContext.cs              # 服务容器
│       ├── IAsakiService.cs             # 服务接口
│       ├── IAsakiModule.cs              # 模块接口
│       ├── IAsakiResolver.cs            # 解析器接口
│       ├── IAsakiAutoInject.cs          # 自动注入接口
│       ├── IAsakiInit.cs                # 初始化接口
│       ├── IAsakiModuleDiscovery.cs     # 模块发现接口
│       └── Resolvers/
│           ├── AsakiGlobalResolver.cs   # 全局解析器
│           ├── AsakiSceneContext.cs     # 场景上下文
│           └── AsakiTransientResolver.cs # 临时解析器
└── Core/
    └── Attributes/
        ├── AsakiModuleAttribute.cs      # 模块特性
        ├── AsakiInjectAttribute.cs      # 注入特性
        └── ...
```

---

## 版本历史

| 版本 | 日期 | 变更 |
|------|------|------|
| V2.0 | - | 新增AsakiMonoLifecycleManager，解决动态组件和场景切换初始化问题 |
| V5.1 | - | Lock-Free Edition, Copy-On-Write架构 |
| V5.0 | - | 引入Roslyn代码生成器，零反射运行时 |

---

## 许可证

此文档属于Asaki框架的一部分，遵循项目主许可证。
