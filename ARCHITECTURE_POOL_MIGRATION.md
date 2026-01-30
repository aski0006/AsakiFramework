# Architecture Pool Migration Guide

## Overview

This migration consolidates the custom object pool implementations in `AsakiCommandPoolManager` and `QueryPoolManager` into the unified Asaki Pooling system (`IAsakiPoolService`).

## What Changed

### Before Migration

**Problems with the old implementation:**
- Code duplication between Command and Query pool managers
- Simple Stack-based implementation with limited features
- No statistics or monitoring capabilities
- No pool size validation
- No object validation mechanism
- No timeout control

### After Migration

**New unified system provides:**
- Single `AsakiArchitecturePoolManager` managing both Command and Query pools
- Built on robust `IAsakiPoolService` infrastructure
- Pool statistics and monitoring via `GetStatistics()`
- Pool size limits (max 128 cached objects per type)
- Object validation support
- Automatic Reset() for `IAsakiResettable` objects
- Both sync and async rental methods
- Thread-safe lazy initialization

## Code Examples

### Basic Usage (Unchanged)

The existing API remains 100% compatible:

```csharp
// Commands - works exactly as before
var cmd = AsakiCommandPoolManager.Rent<MyCommand>();
try
{
    cmd.Execute();
}
finally
{
    AsakiCommandPoolManager.Return(cmd);
}

// Queries - works exactly as before
var query = QueryPoolManager.Rent<MyQuery>();
try
{
    return query.Query();
}
finally
{
    QueryPoolManager.Return(query);
}
```

### New Async API

```csharp
// Async command rental
var cmd = await AsakiCommandPoolManager.RentAsync<MyCommand>(cancellationToken);
try
{
    await cmd.ExecuteAsync();
}
finally
{
    AsakiCommandPoolManager.Return(cmd);
}

// Async query rental
var query = await QueryPoolManager.RentAsync<MyQuery>(cancellationToken);
try
{
    return await query.QueryAsync(cancellationToken);
}
finally
{
    QueryPoolManager.Return(query);
}
```

### New Extension Methods (Recommended)

Extension methods automatically handle rent/return:

```csharp
// Sync command with automatic pooling
this.ExecutePooledCommand<MyCommand>();

// Async command with automatic pooling
await this.ExecutePooledCommandAsync<MyCommand>(cancellationToken);

// Sync query with automatic pooling
var result = this.QueryPooled<MyQuery, int>();

// Async query with automatic pooling
var result = await this.QueryPooledAsync<MyQuery, int>(cancellationToken);
```

### Pool Statistics

Monitor pool usage:

```csharp
// Get statistics for all architecture pools
string stats = AsakiArchitecturePoolManager.GetStatistics();
Debug.Log(stats);

// Example output:
// [AsakiPool] Service statistics (total: 3 pools):
//   [Architecture_MyNamespace.MyCommand] Type: MyCommand, Active: 5, Available: 3, Created: 8
//   [Architecture_MyNamespace.MyQuery] Type: MyQuery, Active: 2, Available: 6, Created: 8
```

### Cleanup

Clear all pools when needed (e.g., scene transitions):

```csharp
// Clear all architecture pools
AsakiArchitecturePoolManager.ClearAll();
```

## Implementation Details

### Pool Configuration

Each type gets its own pool with these settings:
- **InitialSize**: 0 (lazy loading, no prewarm)
- **MaxSize**: 128 (limits memory usage)
- **EnableValidation**: true (validates objects on return)
- **EnableCollectionCheck**: false (architecture objects are lightweight)
- **AllowSyncCreation**: true (supports synchronous Get())
- **OperationTimeout**: 0 (no timeout)

### Automatic Reset

If your Command or Query implements `IAsakiResettable`, the `Reset()` method is automatically called when returned to the pool:

```csharp
public class MyCommand : IAsakiCommand, IAsakiResettable
{
    private SomeState _state;
    
    public void Execute()
    {
        // Use state...
    }
    
    public void Reset()
    {
        // Clear state before returning to pool
        _state = null;
    }
}
```

### Thread Safety

The pool manager uses double-checked locking for lazy initialization and delegates to thread-safe `IAsakiPoolService` implementation.

## Migration Checklist

- [x] Existing Command executions work unchanged
- [x] Existing Query executions work unchanged
- [x] `AsakiUndoRedoStack` works correctly
- [x] Pool statistics are accessible
- [x] Extension methods available
- [x] `ClearAll()` properly releases resources

## Files Modified

### Created
- `Assets/Asaki/Core/Architecture/AsakiArchitecturePoolManager.cs` - Unified pool manager
- `Assets/Asaki/Core/Architecture/Extensions/ArchitectureExtensions.cs` - Convenience methods

### Updated
- `Assets/Asaki/Core/Architecture/Command/AsakiCommandPoolManager.cs` - Now delegates to unified manager
- `Assets/Asaki/Core/Architecture/Queries/QueryPoolManager.cs` - Now delegates to unified manager

### Unchanged (but validated for compatibility)
- `Assets/Asaki/Core/Architecture/Command/AsakiUndoRedoStack.cs` - Uses Return() API
- `Assets/Asaki/Core/Architecture/AsakiArchitecture.Command.cs` - Uses Rent/Return API
- `Assets/Asaki/Core/Architecture/AsakiArchitecture.Query.cs` - Uses Rent/Return API

## Performance Notes

- **Memory**: Max 128 objects per type cached (down from unlimited Stack)
- **CPU**: Slightly higher overhead due to validation (configurable)
- **GC**: Reduced due to better pooling management
- **Stats**: New monitoring capabilities with minimal overhead

## Best Practices

1. **Use extension methods** when possible for automatic pool management
2. **Implement IAsakiResettable** to ensure clean state on reuse
3. **Call ClearAll()** during scene transitions or application quit
4. **Monitor statistics** in development builds to optimize pool sizes
5. **Use async methods** for heavy commands/queries to avoid blocking

## Troubleshooting

### Pool not initialized warning
If you see: `Pool for {Type} not initialized, creating new instance`

**Solution**: Use `RentAsync<T>()` instead of `Rent<T>()` for first-time rentals.

### Object not returned warning
If you see: `Pool for {Type} not found, object will be GC'd`

**Cause**: Trying to return an object after `ClearAll()` was called.

**Solution**: Ensure proper cleanup order or avoid clearing pools while objects are in use.

## Future Enhancements

Potential improvements for the future:
- Per-type pool size configuration
- Pool warming strategies
- Custom validation logic per type
- Pool usage analytics and reporting
- Automatic pool trimming based on usage patterns
