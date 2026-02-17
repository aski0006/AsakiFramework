# Asaki Framework - Unity Game Development Framework

## Project Overview

The Asaki Framework is a comprehensive Unity game development framework that provides architecture patterns, ECS entity system, service management, UI system, and development tools. It's designed to streamline game development by offering well-structured architecture patterns and reusable components.

**Key Characteristics:**
- **Project Type:** Unity Game Development Framework
- **Version:** 1.3.2 (as of 2025-02-17)
- **Unity Version Requirement:** Unity 2022.3 or higher
- **License:** MIT License
- **Architecture:** Modular framework with separation of concerns

## Core Architecture Components

### Main Architecture Patterns
- **Command Pattern** - Command system with undo/redo functionality and object pooling
- **Query System** - Query caching and performance profiling support
- **0GC Object Pooling** - Zero garbage collection object pool with bidirectional mapping
- **Event Broker** - Type-safe event bus system with mixed mode support
- **MVVM Pattern** - Model-View-ViewModel pattern with reactive data binding
- **Dependency Injection** - Context-based service resolution and auto-injection

### Core Modules
- **Async Service** - UniTask-based async/await support
- **Audio System** - Audio playback with pooling and parameter control
- **Blackboard System** - Type-safe variable storage for AI and graphs
- **Configuration** - CSV/JSON config loading with hot reload
- **FSM** - Finite state machine implementation
- **Graph System** - Visual scripting with node-based graphs
- **Logging** - Structured logging with file output and aggregation
- **Networking** - Web service and download management
- **Resource Management** - Addressables and Resources strategies
- **Scene Management** - Scene loading with transitions and progress
- **Save System** - Binary and JSON serialization
- **Simulation** - Tick-based update system
- **Timer Service** - Delayed and repeating timer management
- **UI Framework** - Layer-based UI management with MVVM binding

### Directory Structure
- **Asaki/Core** - Core framework components (Architecture, Async, Audio, Blackboard, Broker, Collections, Context, DataTable, FSM, Graphs, Logging, Network, Pooling, Reactive, Resources, Scene, Serialization, Simulation, Time, UI)
- **Asaki/Unity** - Unity-specific implementations (Bootstrapper, Bridge, Extensions, Logging, Modules, Services, Utils)
- **Asaki/Editor** - Editor tools and utilities (Bootstrapper, Context, DataTable, Debugging, Diagnostics, Entities, FrameworkSettings, GraphEditors, ModuleSystem, Profiler, PropertyDrawers, Simulation, Style, UI, Utilities)
- **Asaki/CodeGen** - Code generation tools
- **Asaki/Generated** - Generated code files
- **Asaki/Plugin** - Third-party plugins
- **Asaki/Tests** - Unit tests
- **Game** - Sample game implementation
- **Scenes** - Unity scenes (e.g., MainScene.unity)

## Building and Running

### Prerequisites
- Unity 2022.3 or higher
- Git (for installation via UPM)

### Installation Methods

#### Via Git URL (UPM)
Open Unity Package Manager and add package from git URL:
```
https://github.com/aski0006/AsakiFramework.git#v1.3.0
```

#### Via manifest.json
Add to `Packages/manifest.json`:
```json
{
    "dependencies": {
        "com.asaki.framework": "https://github.com/aski0006/AsakiFramework.git#v1.3.0"
    }
}
```

### Dependencies
The framework depends on several Unity packages:
- com.unity.addressables: 1.21.0
- com.unity.burst: 1.8.0
- com.unity.collections: 2.1.0
- com.unity.mathematics: 1.2.0
- com.unity.textmeshpro: 3.0.0
- com.cysharp.unitask: 2.5.0

### Quick Start Example
```csharp
public class GameArchitecture : AsakiArchitecture<GameArchitecture>
{
    protected override void OnInit()
    {
        RegisterModel<IGameModel, GameModel>();
        RegisterSystem<IGameSystem, GameSystem>();
    }
}

public class AddScoreCommand : AsakiCommand
{
    private int _amount;

    public static AddScoreCommand Create(int amount)
    {
        var cmd = Pool.Spawn<AddScoreCommand>();
        cmd._amount = amount;
        return cmd;
    }

    protected override void OnExecute()
    {
        var model = this.GetModel<IGameModel>();
        model.Score += _amount;
    }
}
```

## Development Conventions

### Code Organization
- **Separation of Concerns:** Each module has its own directory under Core/
- **Dependency Injection:** Automatic injection of dependencies before initialization
- **Event-Driven Architecture:** Type-safe event bus system for loose coupling
- **Object Pooling:** Zero-GC object pools to prevent memory allocations during gameplay

### Breaking Changes (v1.3.1)
- Renamed `IAsakiInit` → `IAsakiInject` for semantic clarity
- Renamed method `Init()` → `Inject()` in dependency injection interfaces
- Renamed `AsakiInitFactory` → `AsakiInjectFactory`

### Editor Tools Available
- Configuration Dashboard
- Context Debugger
- Event Debugger
- Log Dashboard
- Pool Debugger
- Graph Editor
- UI Generator
- Asset Explorer
- Batch Rename Tool
- Duplicate Finder
- Ground Aligner

## Recent Updates (v1.3.2)

### Fixes
- Fixed simulation registration bug where a System could only register to one simulation interface
- Changed switch to independent if statements in BindSimulation() and UnbindSimulation()
- Now supports Systems implementing multiple interfaces: IAsakiTickable, IAsakiLateTickable, IAsakiFixedTickable

### Previous Updates (v1.3.1)
- Added IAsakiInject Interface with extended generic parameter support (from 5 to 10 arguments)
- Added Auto Dependency Injection for automatic dependency injection before Setup()/Create()
- Fixed assembly reference issue and dependency injection timing

### Major Updates (v1.3.0)
- Added Query System with caching and performance profiling
- Added full Command Pattern with undo/redo functionality
- Implemented 0GC Object Pooling system
- Refactored architecture code to optimize command system and object pool management

## Testing
The framework includes a Tests directory containing unit tests for verifying functionality.

## Documentation and Support
- **Documentation:** Available on the GitHub Wiki
- **Author:** Asaki (GitHub: https://github.com/aski0006, Email: aski0006@gmail.com)
- **Issues:** Bug reports can be submitted at the GitHub repository
- **Repository:** https://github.com/aski0006/AsakiFramework.git