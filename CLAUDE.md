# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Asaki Framework is a comprehensive Unity game development framework providing architecture patterns, ECS entity system, service management, UI system, and development tools.

## Build & Development

### Running Tests
```bash
# Run all tests in Unity Test Framework
# Open Unity Editor and run tests via Test Runner window
# Tests are located in Assets/Asaki/Tests/
```

### Code Formatting
- Uses CSharpier for C# formatting (configured in `.vscode/settings.json`)
- Format on save is enabled
- Rider is the preferred IDE

### Solution Files
- Main solution: `Asaki.sln`
- Key project files: `Asaki.Core.csproj`, `Asaki.Unity.csproj`, `Assembly-CSharp.csproj`

## Architecture Overview

### Core Layers

```
Assets/Asaki/
├── Core/           # Pure C# framework (no Unity dependencies)
│   ├── Architecture/   # CQRS pattern (Commands, Queries, Entities)
│   ├── Context/        # Service container (Lock-Free, Copy-On-Write)
│   ├── Pooling/        # Zero-GC object pooling
│   ├── Broker/         # Event bus system
│   ├── Serialization/  # Binary/JSON serialization
│   ├── Time/           # Timer service
│   └── ...
├── Unity/          # Unity-specific implementations
│   ├── Modules/        # IAsakiModule implementations
│   ├── Services/       # Service implementations
│   └── Bootstrapper/   # Framework initialization
├── Editor/         # Unity Editor tools & debuggers
├── CodeGen/        # Roslyn Source Generators
└── Plugin/         # Optional plugins (e.g., ComboSystem)
```

### Key Architectural Patterns

**1. Service Container (AsakiContext)**
- Lock-free read operations via volatile snapshot
- Copy-On-Write for writes (creates new Dictionary, swaps reference)
- Freeze mechanism prevents runtime modifications
- Key APIs: `Get<T>()`, `Register<T>()`, `Replace<T>()`, `Freeze()`

**2. Module System (IAsakiModule)**
- Modules declare dependencies via `[AsakiModule]` attribute
- DAG-based initialization order resolution
- Supports sync (`OnInit`) and async (`OnInitAsync`) initialization
- Loaded via `AsakiModuleLoader.Startup()`

**3. CQRS Architecture**
- `AsakiCommand` - Write operations with pooling and undo support
- `AsakiQuery<T>` - Read operations with caching
- `IAsakiEvent` - Event-driven notifications via `AsakiBroker`

**4. Entity Component System**
- `MagicContainer` - O(1) add/remove/query, memory contiguous
- Entity ID with generation for ABA problem prevention
- Type-safe queries via `world.QueryWith<T>()`

**5. Code Generation (Roslyn Source Generators)**
- `AsakiInjectGenerator` - Property injection code gen
- `AsakiSaveGenerator` - Serialization boilerplate
- `AsakiTypeRegistryGenerator` - Type bridge registration
- `BrokerGenerator` - Event handler registration
- `ModuleRegistryGenerator` - Module registration
- `GraphRegistryGenerator` - Graph node registration

### Dependency Injection

```csharp
// Property injection (compile-time generated)
public class Example : MonoBehaviour
{
    [AsakiInject]
    private IAsakiAudioService _audioService;
}

// Manual resolution
var service = AsakiContext.Get<IAsakiService>();
```

### Service Hierarchy

```
IAsakiService (base marker interface)
├── IAsakiSceneService    -- Scene-level services (managed by AsakiSceneContext)
└── IAsakiGlobalService   -- Global MonoBehaviour services (managed by Bootstrapper)
    └── void OnBootstrapInit()
```

**Global Service Design Pattern:**
```csharp
// Recommended: Global services inherit AsakiMono and implement IAsakiGlobalService
public class AudioService : AsakiMono, IAsakiGlobalService
{
    [AsakiInject]
    private IAsakiResourceService _resourceService;

    void IAsakiGlobalService.OnBootstrapInit()
    {
        // Initialization logic (framework is ready, services are injected)
    }
}
```

**Important:** Services that implement `IAsakiGlobalService` are injected and initialized by `AsakiBootstrapper`. The `AsakiMonoLifecycleManager` automatically detects and skips duplicate injection to prevent double-initialization.

### Framework Initialization Flow

1. `AsakiBootstrapper.Awake()`
   - Single instance check, DontDestroyOnLoad
   - Load AsakiFrameworkSetting config
   - Clear AsakiContext
   - Register logging service
   - Instantiate global service prefabs from GlobalServiceRegistry
   - Collect and register IAsakiGlobalService (with duplicate detection)

2. `AsakiBootstrapper.Start()` (async)
   - `AsakiModuleLoader.Startup()` - Discover and initialize modules
     - Phase 1: Sync init (instantiate, inject, register, OnInit)
     - Phase 2: Async init (OnInitAsync)
   - `AsakiContext.Freeze()` - Lock container
   - `InitializeGlobalServices()` - Inject and call OnBootstrapInit for all global services
   - Register scene load events
   - Inject current scene (manual targets + AsakiSceneContext.Build())

3. `OnAsakiFrameworkReadyEvent` published
   - `AsakiMonoLifecycleManager` processes pending AsakiMono components
   - Skips IAsakiGlobalService (already injected by Bootstrapper)

4. Runtime
   - Scene loads trigger AsakiSceneContext.Build() for new scene
   - Persistent AsakiMono components re-injected (except IAsakiGlobalService)

### Key Conventions

- **Core layer is pure C#** - No UnityEngine references in `Assets/Asaki/Core/`
- **Unity layer implements interfaces** - `Assets/Asaki/Unity/` provides Unity-specific implementations
- **Modules are discovered statically** - Uses `[AsakiModule]` attribute for discovery
- **Events use AsakiBroker** - `AsakiBroker.Subscribe<T>()` / `AsakiBroker.Publish<T>()`

### Common Development Patterns

**Creating a Module:**
```csharp
[AsakiModule(id: "MyModule", dependencies: new[] { typeof(AudioModule) })]
public class MyModule : IAsakiModule
{
    public void OnInit() { /* sync init */ }
    public UniTask OnInitAsync() { /* async init */ }
    public void OnDispose() { /* cleanup */ }
}
```

**Creating a Command:**
```csharp
public class MyCommand : AsakiCommand
{
    protected override void OnExecute() { /* logic */ }
}
// Execute via: architecture.Execute(new MyCommand());
```

**Using Object Pool:**
```csharp
var pool = await AsakiContext.Get<IAsakiPoolService>().CreatePoolAsync(
    key: "MyPool",
    factory: myFactory,
    config: new AsakiPoolConfig { InitialSize = 10, MaxSize = 100 }
);
var obj = await pool.GetAsync();
pool.Return(obj);
```

### Editor Tools

- **AsakiBootstrapperWindow** - Bootstrap configuration
- **AsakiContextDebuggerWindow** - Service container inspection
- **AsakiEventDebuggerWindow** - Event subscription/publish tracking
- **AsakiPoolDebuggerWindow** - Pool statistics and inspection
- **EntityDebuggerWindow** - ECS entity/component inspection
- **AsakiGraphWindow** - Visual node graph editor
- **AsakiConfigDashboardWindow** - Configuration debugging

### External Dependencies

**Unity Packages:**
- `com.unity.addressables` - Resource management
- `com.cysharp.unitask` - Async/await pattern (UniTask)
- `com.unity.collections` - High-performance collections
- `com.unity.mathematics` - Math types
- `com.unity.textmeshpro` - Text rendering

**Code Analysis:**
- Uses CSharpier for formatting
- Rider IDE recommended

## Critical Architecture Notes

### Thread Safety
- `AsakiSceneContext.Build()` uses double-check locking with volatile `_isBuilt` flag
- Service container uses Copy-On-Write for thread-safe reads

### Duplicate Prevention
- Global service registration checks for duplicate types and interfaces
- `IAsakiGlobalService` services are skipped by `AsakiMonoLifecycleManager` to prevent double-injection

### Injector Priority
- `IAsakiInjector.Priority` determines injection order (lower = earlier)
- Core assemblies have priority 100, Unity assemblies 200, Game assemblies 1000
- Injectors are sorted before first use

### Recursion Limits
- Service collection in Bootstrapper and SceneContext limited to 10 levels depth
- Prevents stack overflow on deeply nested hierarchies
