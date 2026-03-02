# Changelog

All notable changes to the Asaki Framework will be documented in this file.

## [2.3.0] - 2026-03-02

### Added

#### AsakiMono 组件缓存方法扩展
- **新增 GetCachedComponentInParent<T>()** - 获取父级组件（带缓存）
- **新增 GetCachedComponentInSelfOrParent<T>()** - 获取自身或父级组件（带缓存）
- **新增 GetCachedComponentExact<T>()** - 精确获取特定类型组件（排除自身）
- 所有方法泛型约束为 `where T : Component`，支持任意 Unity 组件类型

#### 编辑器工具
- **新增 CompilationTimeMonitor** - 编译耗时监控工具
  - 自动监控 Unity 编译过程并输出耗时
  - 根据编译时间显示不同颜色（绿/黄/橙/红）
  - 支持通过 EditorPrefs 启用/禁用

### Changed

#### AsakiMono 生命周期重构
- **Unity 原生生命周期方法改为 private** - 强制子类通过 OnXxx 虚方法接入
  - `Awake`、`OnEnable`、`OnDisable`、`Update`、`FixedUpdate`、`LateUpdate`、`OnDestroy` 改为 private
  - 添加 `[MethodImpl(MethodImplOptions.AggressiveInlining)]` 优化性能
  - 防止子类忘记调用 base 方法导致框架初始化逻辑被跳过
- **代码结构优化** - 使用 #region 划分功能区域
  - 字段和属性、Unity 生命周期、框架生命周期虚方法、框架核心方法、组件缓存方法、工具方法、静态方法、内部类型

#### 日志系统性能优化
- **Trace 日志跳过时间戳输出** - 高频日志（Update 中的 Trace）不再输出时间戳
  - 新增 `isHighFrequency` 参数到 `IALogUnityBridge.ForwardToUnityConsole`
  - 减少 `DateTime.Now.ToString()` 调用开销

## [2.2.2] - 2026-02-28

### Added

#### Architecture 生命周期事件增强
- **新增 OnAsakiArchitectureDisposeEvent 事件** - Architecture 释放时的通知事件
  - 当 Architecture 调用 `Dispose()` 时发布此事件
  - 包含 ArchitectureType 和 Architecture 实例引用
  - 便于外部系统监听 Architecture 销毁状态，进行资源清理

#### 框架配置
- **新增 AsakiFrameworkSetting 资产文件** - 框架运行时配置
  - 集中管理框架各项配置参数
  - 支持通过 Unity Inspector 进行可视化配置

### Changed

#### 代码优化
- **优化 ArchitectureHost** - 改进跨场景持久化逻辑
- **优化代码格式** - 统一代码块大括号格式和 using 语句顺序
- **优化日志输出** - 改进多行日志消息的格式化

## [2.2.1] - 2026-02-27

### Fixed

#### UI 配置同步问题修复
- **修复 UIConfig 与 WindowAssetId 枚举不同步问题** - 解决 UI 无法正常加载的问题
  - `AsakiUIGeneratorWindow` 新增 `Validate UI Config Sync` 菜单项用于验证同步状态
  - 实现差异报告输出功能，显示缺失条目、多余条目和 ID 不匹配
  - 实现自动修复功能，根据枚举更新配置并保留现有 Layer 和 UsePool 设置
  - `AsakiUIManageService.OnInitAsync()` 添加运行时验证，检测 UIList 为空时输出详细警告
  - 增强同步操作的成功/失败反馈信息，使用 `Debug.LogException` 输出完整异常堆栈

## [2.2.0] - 2026-02-25

### Added

#### Architecture 生命周期增强
- **新增 OnStart() 生命周期方法** - Architecture 初始化完成时的回调
  - 在所有 System 启动完成后、注册到 ArchitectureRegister 前调用
  - 子类可重写此方法以执行自定义初始化逻辑
  - 提供更细粒度的生命周期控制

- **新增 OnAsakiArchitectureReadyEvent 事件** - Architecture 启动完成事件
  - 当 Architecture 完成所有初始化流程后发布此事件
  - 包含 ArchitectureType 和 Architecture 实例引用
  - 便于外部系统监听 Architecture 初始化状态

- **新增 ArchitectureHost 泛型类** - 支持跨场景持久化的 Architecture
  - 继承 AsakiMono 并实现 IAsakiGlobalService
  - 自动设置 DontDestroyOnLoad 实现跨场景持久化
  - 线程安全的实例创建和销毁机制
  - 与 AsakiBootstrapper 集成，确保正确的初始化顺序

### Fixed

#### 线程安全优化
- **优化全局注入器的锁机制** - 避免竞争条件
  - 修复多线程环境下可能出现的竞争条件问题
  - 提升并发场景下的稳定性

## [2.1.0] - 2026-02-25

### Added

#### 可选依赖注入支持
- **新增 [ANull] 特性** - 标记注入方法参数为可空依赖
  - 使用 `[ANull]` 特性标记可选依赖，依赖不存在时注入 null 而非抛出异常
  - 支持三种可选依赖标记方式：
    - `[ANull] IService service` - Asaki 框架特性
    - `IService? service` - C# 可空引用类型
    - `int? value` - Nullable<T> 值类型
  - 保持向后兼容，现有代码无需修改

### Fixed

#### 持久化组件激活问题修复
- **修复持久化组件重新注入后未激活的 Bug**
  - `AsakiMonoLifecycleManager.ReinjectGlobalServices` 在成功注入后检查并激活未激活的组件
  - `AsakiMono.ActivateFrameworkReady` 访问修饰符从 `internal` 改为 `public`
  - 解决跨场景持久化对象陷入"已注入但未激活"僵尸状态的问题

## [2.0.0] - 2025-02-24

### BREAKING CHANGES
- **AsakiAutoSaveService 构造函数签名变更** - 参数类型从 `IAsakiAutoSaveConfig` 改为 `AsakiSaveConfig`
  - 迁移：将 `new AsakiAutoSaveService(slotManager, eventService, autoSaveConfig)` 改为 `new AsakiAutoSaveService(slotManager, eventService, saveConfig)`
  - 服务内部会自动从 `saveConfig.AutoSave` 获取自动保存配置

- **IAsakiAutoSaveConfig.AutoSaveSlotStartIndex 已移除** - 槽位索引统一由 `AsakiSaveConfig.AutoSaveSlotIndex` 管理
  - 迁移：使用 `AsakiSaveConfig.AutoSaveSlotIndex` 替代 `AsakiAutoSaveConfig.AutoSaveSlotStartIndex`

### Added
- **统一存档配置入口** - `AsakiSaveConfig` 现在包含 `AutoSave` 子配置属性
  - 开发者只需管理一个配置对象即可控制所有存档行为
  - Unity Inspector 中可直接编辑嵌套的自动保存配置

### Changed
- **重构存档系统服务分层架构** - 明确职责分层，消除代码重复
  - `AsakiSaveSlotManager` 现在完全依赖 `IAsakiSaveService` 进行底层存储操作
  - 所有文件操作统一由 `SaveService` 管理
  - 事件发布统一由 `SaveService` 处理，避免重复通知
  - 移除 `AsakiSaveSlotManager` 中的路径 fallback 逻辑

- **AsakiAutoSaveConfig 属性序列化** - 添加 `[field: SerializeField]` 特性
  - 所有属性现在可以在 Unity Inspector 中正确显示和编辑

## [1.4.0] - 2025-02-24

### Added

#### 生命周期与注册时序增强
- **动态注册窗口机制** - `AsakiContext` 支持运行时动态注册服务
  - `EnterDynamicPhase()` / `ExitDynamicPhase()` 方法控制动态注册阶段
  - 支持嵌套调用，使用计数器管理
  - 插件系统可在动态阶段注册新服务类型

- **模块可选性与错误隔离** - 增强模块加载容错能力
  - `AsakiModuleAttribute.Optional` 属性标记可选模块
  - `AsakiModuleAttribute.TimeoutMs` 属性设置初始化超时
  - 可选模块失败不影响框架启动
  - `ModuleLoadResult` / `ModuleLoadSummary` 提供详细加载报告

- **场景上下文构建状态机** - `AsakiSceneContext.Build()` 状态管理
  - `BuildState` 枚举：NotBuilt, Building, Built, Failed
  - 构建失败时记录异常，防止重复尝试
  - `State` 和 `BuildException` 属性公开查询

#### 依赖注入机制健壮性增强
- **循环依赖检测** - 防止服务解析时的无限递归
  - `CircularDependencyException` 异常类，包含完整依赖链信息
  - `AsakiResolveContext` 使用 `AsyncLocal<HashSet<Type>>` 线程隔离
  - 所有 Resolver 实现（Global、Transient、Scene）支持循环依赖检测

- **注入器类型冲突检测** - 警告同一类型被多个注入器处理
  - `IAsakiInjector.Inject` 接口新增 `injectedTypes` 参数
  - 冲突时输出警告日志，帮助发现配置问题

- **AsakiTransientResolver 类型安全** - 防止参数误用
  - 构造函数验证参数是否实现 `IAsakiService`
  - 开发模式下输出警告日志

#### 并发与线程安全增强
- **服务清理前事件** - `OnAsakiContextClearingEvent`
  - 在 `AsakiContext.ClearAll()` 销毁服务前发布
  - 允许使用者提前释放服务引用

- **线程安全销毁接口** - `IAsakiThreadSafeDisposable`
  - 实现此接口的服务在锁内销毁，确保线程安全

- **注入器线程安全注册** - `AsakiGlobalInjector` 支持运行时动态注册
  - 使用 `ReaderWriterLockSlim` 读写锁
  - 支持并发注册与注入操作

#### 内存安全与重复检测增强
- **弱引用事件订阅** - `AsakiBroker.SubscribeWeak<T>()`
  - 使用弱引用存储处理程序，避免内存泄漏
  - 无效弱引用在发布事件时自动清理

- **AsakiMono 取消订阅安全** - 组件销毁时强制取消订阅
  - `OnDestroy()` 中调用 `AsakiUnregister()`
  - 防止组件销毁后事件订阅残留

- **全局服务重复实例检测** - 编辑器下检测重复全局服务
  - `AsakiBootstrapper` 初始化时检测重复实例
  - 输出警告并跳过重复实例

#### 依赖注入日志增强
- **源服务追踪** - `AsakiResolveContext.SetSourceType()`
  - 记录注入请求的发起者信息
  - 便于追踪完整的依赖关系链

- **标准化日志格式** - 统一的依赖注入日志格式
  - `[DI] Resolve | Type: xxx | Status: xxx | Source: xxx | Duration: xxxms`
  - `[DI] Inject | Source: xxx | Status: xxx | Duration: xxxms`
  - `[DI] InjectComplete | Target: xxx | Success: xxx | Failure: xxx | Duration: xxxms`

### Changed
- `AsakiContext.Get()` 新增非泛型重载 `Get(Type type)`
- `AsakiModuleLoader.Startup()` 返回 `ModuleLoadSummary` 汇总
- `IAsakiInjector.Inject` 接口签名新增 `injectedTypes` 参数

## [1.3.15] - 2025-02-22

### Added
- **IAsakiArchitecture Interface Enhancement** - Extended interface with CQRS methods
  - Added 8 `SendCommand` method declarations (sync/async, with/without return value, with/without configure delegate)
  - Added 6 `SendQuery` method declarations (sync/async, with/without cache, with/without configure delegate)
  - Added 2 `SendUndoCommand` method declarations
  - Added Undo/Redo operations (`Undo()`, `Redo()`) and state properties (`CanUndo`, `CanRedo`, `UndoCount`, `RedoCount`)
  - Enables interface-based programming without casting to concrete `AsakiArchitecture` class

### Fixed
- **AsakiSystemBase.ServiceProvider Null Reference** - Fixed critical bug where `ServiceProvider` was never set
  - Root cause: `AsakiArchitecture.Inject()` called parameterless `Create()` through `IAsakiSystem` interface
  - Solution: Added type check to call `Create(this)` for `AsakiSystemBase` derived systems
  - Systems can now properly access architecture via `ServiceProvider` to send commands/queries
  - Maintains backward compatibility for systems directly implementing `IAsakiSystem`

### Changed
- **AsakiMono Lifecycle** - Improved `EnableComponent` call timing
  - Added `_enableComponentCalled` flag to prevent duplicate calls
  - `EnableComponent()` now called after `OnStart()` when component is already active
  - Ensures proper initialization order: Awake → OnEnable → Start → EnableComponent

## [1.3.14] - 2025-02-21

### Added
- **Architecture Register** - Added centralized architecture registration system
  - `ArchitectureRegister` - Central registry for managing all architecture instances
  - `AsakiArchitectureModule` - Unity module for architecture lifecycle management
  - `AsakiArchitectureDebugger` - Editor window for debugging architecture states
  - `ArchitectureRegisterWindow` - Editor tool for visualizing registered architectures

### Changed
- **Framework Settings** - Refactored framework configuration system
  - Moved pool configuration to dedicated `AsakiPoolConfig` class
  - Moved timer configuration to dedicated `AsakiTimerConfig` class
  - Improved settings serialization and validation

### Fixed
- **Scene Context Editor** - Optimized editor performance and initialization
- **Pool System** - Fixed generic pool implementation issues
- **Audio Constants** - Unified constant naming conventions

## [1.3.13] - 2025-02-20

### Performance
- **Resource Service Concurrency** - Optimized locking mechanism for high-concurrency scenarios
  - Replaced global lock with `ConcurrentDictionary` + 16-segment striped locks
  - Fast path: cache hits return without lock contention
  - Slow path: only record creation uses segment locks
  - Expected >30% improvement in concurrent processing capacity

### Architecture
- **Preloader Responsibility Split** - Refactored `AsakiResourcePreloader` following SRP
  - `PreloadConfigProvider` - Configuration management
  - `PreloadExecutor` - Loading execution and state management
  - `PreloadResourceRegistry` - Resource handle management and access
  - `AsakiResourcePreloader` now acts as coordinator

### Changed
- **Dependency Parallel Loading** - Dependencies now load in parallel instead of sequentially
- **Removed Obsolete API** - Removed deprecated `ReleaseBatch(IEnumerable<string>)` method
- **Constant Naming** - Unified constant naming style to PascalCase

### Documentation
- Added UnityDoc standard comments to all core interfaces and classes
  - `IAsakiResourceService` - Full API documentation with examples
  - `IAsakiResStrategy` - Strategy pattern documentation
  - `AsakiResourceService` - Internal implementation details
  - `AsakiResourcesStrategy` / `AsakiAddressablesStrategy` - Usage scenarios
  - `AsakiResKitFactory` - Factory pattern examples
  - `AsakiResKitMode` - Mode selection guidelines
  - `SerializableResourceType` - Type system documentation

## [1.3.12] - 2025-02-20

### Fixed
- **Logging Thread Safety** - Fixed race condition in `AsakiLogModel.LastTimestamp`
  - Changed from direct assignment to `Interlocked.Exchange` for atomic updates
  - Added `System.Threading` namespace and updated documentation

### Optimized
- **FormatPayload GC Reduction** - Reduced garbage collection in `ALog.FormatPayload()`
  - Added `ThreadStatic` StringBuilder pool for thread-safe reuse
  - Implemented dedicated formatting methods for Unity types (Vector2/3/4, Quaternion, Color)
  - Eliminated string interpolation allocations for common payload types

### Fixed
- **Stack Trace Capture** - Fixed incomplete stack traces in Info/Warn level logs
  - Trace and Info methods now capture stack traces at call site
  - `CaptureSmartStackTrace` now filters out Asaki internal frames
  - Stack traces now correctly show user code call chain

### Changed
- **Log Dashboard Layout** - Improved editor window layout
  - Changed split view ratio to 70% (log list) / 30% (details)
  - Added dynamic height adjustment for long messages
  - Added message tooltips and auto-wrap for long content
  - Added clickable location label in detail panel

### Added
- **Rich Text Console Output** - Enhanced Unity console log formatting
  - Added color-coded log levels (Debug=gray, Info=white, Warning=yellow, Error=red, Fatal=dark red)
  - Added millisecond-precision timestamps to all console logs
  - Added level badges with emoji icons (🔍 DBG, ℹ️ INF, ⚠️ WRN, ❌ ERR, 💀 FTL)

### Added
- **Log Export Feature** - Added one-click log export to TXT file
  - Export button in Log Dashboard toolbar
  - Generates formatted report with summary statistics
  - Includes full log details with stack traces
  - User can specify save location via file dialog

## [1.3.11] - 2025-02-20

### Added
- **FSM Type Erasure Support** - Added type-erased state management methods
  - `GetState(Type stateType)` - Get state instance by Type at runtime
  - `ChangeState(Type stateType)` - Switch state by Type at runtime
  - `ValidateStateType()` - Type safety validation for state types
  - Supports configuration-driven and reflection-based state transitions
  - Enables dynamic state loading from config files or data-driven systems

### Changed
- **FSM Documentation** - Updated class remarks to document type erasure feature
- Removed deprecated FSM example and README files (consolidated documentation)

## [1.3.10] - 2025-02-18

### Fixed
- **Framework Initialization Timing (v3.3)** - Fixed service injection failure due to timing issues
  - `AsakiSceneContext.Awake()` no longer calls `Build()` directly
  - `Build()` is now triggered by `OnAsakiFrameworkReadyEvent` after global services are ready
  - Added `AsakiContext.IsReady` property and `SetReady()` method
  - `OnAsakiFrameworkReadyEvent` moved to `Asaki.Core.Context` namespace for proper assembly isolation
  - `AsakiSceneContext` now implements `IAsakiHandler<OnAsakiFrameworkReadyEvent>` interface
  - Fixes injection failures when scene services depend on global services (e.g., `IAsakiSceneManagerService`)

## [1.3.9] - 2025-02-18

### Fixed
- **Cross-Assembly Access** - Fixed CS1061 compilation error
  - Changed `IsInitializingServices` from `internal` to `public`
  - Previous `internalsVisibleTo` approach didn't work reliably across Unity versions

## [1.3.8] - 2025-02-18

### Fixed
- **Cross-Assembly Internal Access** - Fixed CS1061 compilation error
  - Added `internalsVisibleTo: ["Asaki.Unity"]` to `Asaki.Core.asmdef`
  - `AsakiSceneContext.IsInitializingServices` (internal) is now accessible from `AsakiMonoLifecycleManager`

## [1.3.7] - 2025-02-18

### Fixed
- **AsakiSceneContext Service Registration Timing (v3.2)** - Fixed prefab services not being injected
  - Added `_isInitializing` state flag to prevent premature `Build()` calls
  - `AsakiMonoLifecycleManager.GetResolverForComponent()` now checks `IsInitializingServices` before calling `Build()`
  - `Build()` is now called after all services (pure C# and prefab) are registered
  - Fixes injection failures when services depend on prefab services (e.g., `PlayerCameraManager`)

## [1.3.6] - 2025-02-18

### Fixed
- **AsakiSceneContext Service Registration Timing** - Fixed pure C# services not being initialized
  - `Awake()` execution order changed: `RegisterPureCSharpServices()` now runs before `InstantiateServicePrefabs()`
  - Prevents `Build()` from being called with empty `_pendingInitServices` when prefab `Awake()` triggers early
  - Pure C# services (like `AsakiArchitecture`) are now correctly registered before prefab instantiation

## [1.3.5] - 2025-02-17

### Fixed
- **EntityWorldHelper KeyNotFoundException** - Fixed crash when Architecture has no EntityModel
  - `TryGetEntityWorld()` now iterates all architectures and skips those without EntityModel
  - `GetAllEntityWorlds()` now gracefully handles architectures without EntityModel
  - Prevents `KeyNotFoundException` when first architecture doesn't have EntityModel registered

## [1.3.4] - 2025-02-17

### Fixed
- **ECS Editor EntityWorld Access** - Fixed EntityWorld retrieval in editor windows
  - EntityWorld is a POJO class, cannot be retrieved via `FindObjectsByType<MonoBehaviour>`
  - Added `EntityWorldHelper` utility class to get IAsakiArchitecture from AsakiSceneContext via reflection
  - Implemented reflection field cache and Architecture list cache to avoid frequent reflection overhead
- **EntityWorldHelper KeyNotFoundException** - Fixed crash when Architecture has no EntityModel
  - `TryGetEntityWorld()` now iterates all architectures and skips those without EntityModel
  - `GetAllEntityWorlds()` now gracefully handles architectures without EntityModel
  - Prevents `KeyNotFoundException` when first architecture doesn't have EntityModel registered

### Added
- **Extended Query Support** - Extended Query generic support from 3 to 6 components
  - `Query<T1,T2,T3,T4>()` - 4 component query
  - `Query<T1,T2,T3,T4,T5>()` - 5 component query
  - `Query<T1,T2,T3,T4,T5,T6>()` - 6 component query
  - Maintains smallest-group-first traversal optimization strategy
- **ECS Example** - Added complete ECS example code
  - `ECSComponents.cs` - 6 example components (Position, Velocity, Health, Tag, Render, PlayerInput)
  - `ECSSystems.cs` - 5 example systems (Movement, Health, PlayerInput, Render, EntityStats)
  - `ECSArchitecture.cs` - ECS architecture example with entity factory methods
  - `ECSExample.cs` - MonoBehaviour driver with runtime GUI

## [1.3.3] - 2025-02-17

### Added
- **Injector Priority Support** - Added priority-based injection ordering
  - `IAsakiInjector.Priority` property for controlling injection order
  - Higher priority injectors execute first
- **ECS Support** - Added Entity Component System integration
  - `EcsExample` demonstrating ECS architecture usage
  - System lifecycle refactoring for better ECS integration

### Changed
- Refactored system lifecycle management
- Optimized global service management with injection phases

## [1.3.2] - 2025-02-17

### Fixed
- Fixed simulation registration bug where a System could only register to one simulation interface
  - Changed `switch` to independent `if` statements in `BindSimulation()` and `UnbindSimulation()`
  - Now supports Systems implementing multiple interfaces: `IAsakiTickable`, `IAsakiLateTickable`, `IAsakiFixedTickable`

## [1.3.1] - 2025-02-17

### Added
- **IAsakiInject Interface** - Extended generic parameter support from 5 to 10 arguments
  - `IAsakiInject<T1..T10>` interfaces for flexible dependency injection
  - `AsakiInjectFactory` with 20 `Instantiate` method overloads
- **Auto Dependency Injection** - Systems and Models now auto-inject dependencies before `Setup()`/`Create()`
  - `AsakiGlobalInjector.Inject()` called automatically in `AsakiArchitecture.Init()`

### Changed
- **Breaking Change**: Renamed `IAsakiInit` → `IAsakiInject` for semantic clarity
- **Breaking Change**: Renamed method `Init()` → `Inject()` in dependency injection interfaces
- **Breaking Change**: Renamed `AsakiInitFactory` → `AsakiInjectFactory`
- Moved `AsakiGlobalInjector` from `Asaki.Unity.Bootstrapper` to `Asaki.Core.Context` namespace
- Updated Roslyn generator to use new `Asaki.Core.Context.AsakiGlobalInjector` namespace

### Fixed
- Fixed assembly reference issue where Core could not reference Unity assembly
- Fixed dependency injection timing - `[AsakiInject]` methods now execute before `Setup()`

## [1.3.0] - 2025-02-16

### Added
- **Query System** - Query caching and performance profiling support
  - `AsakiQuery<T>` base class for query definitions
  - `QueryCacheManager` for caching query results
  - `QueryPoolManager` for query object pooling
- **Command Pattern** - Full command system with undo/redo functionality
  - `AsakiCommand` base class with object pooling
  - `AsakiCommandPoolManager` for command pooling
  - `AsakiUndoRedoStack` for undo/redo operations
- **0GC Object Pooling** - Zero garbage collection object pool
  - Bidirectional mapping for pool item tracking
  - `AsakiPoolData` for pool metadata
  - `AsakiPoolItem` for pool item wrapper

### Changed
- Refactored architecture code to optimize command system and object pool management
- Enhanced object pool with zero GC allocation support
- Improved query performance with result caching

### Fixed
- Thread safety improvements in ComponentTypeRegistry
- Corrected AsakiUndoRedoStack.TrimStack() implementation
- Multiple critical architecture fixes

## [1.2.1] - 2024-12-XX

### Added
- Simulation debugger window
- Save configuration and command debugging

### Changed
- Removed deprecated code
- Code formatting with CSharpier

### Fixed
- UI component null reference checks
- Scene context construction improvements

## [1.2.0] - 2024-11-XX

### Added
- Global timer manager
- Editor development tools
- UI service upgrade

### Changed
- Architecture improvements
- Core pooling strategy v2

### Fixed
- Scene manager fixes

## [1.1.0] - 2024-10-XX

### Added
- Localization system
- Context system improvements
- Data versioning pipeline

### Changed
- Custom pool implementation migration
- ARPG demo additions

## [1.0.0] - 2024-09-XX

### Added
- Initial release
- Core architecture system
- Event broker
- MVVM pattern
- Object pooling
- Resource management
- Scene management
- Save system
- UI framework
- Audio system
- Logging system
- Configuration system
- Graph-based visual scripting
- Editor tools
