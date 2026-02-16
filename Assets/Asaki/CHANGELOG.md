# Changelog

All notable changes to the Asaki Framework will be documented in this file.

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
