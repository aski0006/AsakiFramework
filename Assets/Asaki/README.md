# Asaki Framework

A comprehensive Unity game development framework providing architecture patterns, ECS entity system, service management, UI system, and development tools.

## Features

### Core Architecture
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

### Editor Tools
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

## Installation

### Via Git URL (UPM)
Open Unity Package Manager and add package from git URL:
```
https://github.com/aski0006/AsakiFramework.git#v1.3.0
```

### Via manifest.json
Add to `Packages/manifest.json`:
```json
{
    "dependencies": {
        "com.asaki.framework": "https://github.com/aski0006/AsakiFramework.git#v1.3.0"
    }
}
```

## Requirements
- Unity 2022.3 or higher
- Dependencies:
  - com.unity.addressables: 1.21.0
  - com.unity.burst: 1.8.0
  - com.unity.collections: 2.1.0
  - com.unity.mathematics: 1.2.0
  - com.unity.textmeshpro: 3.0.0
  - com.cysharp.unitask: 2.5.0

## Quick Start

### 1. Create Architecture
```csharp
public class GameArchitecture : AsakiArchitecture<GameArchitecture>
{
    protected override void OnInit()
    {
        RegisterModel<IGameModel, GameModel>();
        RegisterSystem<IGameSystem, GameSystem>();
    }
}
```

### 2. Define Commands
```csharp
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

### 3. Use Event Broker
```csharp
// Subscribe
this.Subscribe<ScoreChangedEvent>(OnScoreChanged);

// Publish
this.Publish(new ScoreChangedEvent { NewScore = 100 });
```

## What's New in v1.3.0

### New Features
- **Query System** - Query caching and performance profiling support
- **Command Pattern** - Command system with undo/redo functionality
- **0GC Pooling** - Refactored object pool system with bidirectional mapping
- **Architecture Refactoring** - Optimized command system and pool management

### Improvements
- Enhanced object pool with zero GC allocation
- Bidirectional mapping for pool item tracking
- Query result caching for better performance
- Undo/redo stack with configurable history size

## Documentation
For detailed documentation, visit the [Wiki](https://github.com/aski0006/AsakiFramework/wiki).

## License
MIT License - see [LICENSE](LICENSE) for details.

## Author
- **Asaki** - [GitHub](https://github.com/aski0006)
- Email: aski0006@gmail.com
