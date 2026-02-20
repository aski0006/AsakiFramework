# Changelog

All notable changes to the Asaki Framework will be documented in this file.

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
