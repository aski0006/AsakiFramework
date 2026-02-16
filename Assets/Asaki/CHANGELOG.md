# Changelog

All notable changes to the Asaki Framework will be documented in this file.

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
