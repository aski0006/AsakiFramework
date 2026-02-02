# Asaki Framework - 数据版本控制与迁移系统

## 概述

Asaki Framework 的数据版本控制与迁移系统提供了一套完整的解决方案，用于处理游戏数据模型的演进。当你的数据结构需要更改时（添加字段、删除字段、重命名等），此系统能够自动将旧版本的存档数据迁移到新版本，确保玩家的存档不会丢失。

## 核心概念

### 1. 数据版本控制

每个可序列化的数据类都有一个版本号，通过 `[AsakiSave(Version = n)]` 特性声明：

```csharp
[AsakiSave(Version = 1)]
public partial class PlayerData : IAsakiVersionedSavable
{
    [AsakiSaveMember(Order = 1)]
    public string PlayerName;
    
    [AsakiSaveMember(Order = 2)]
    public int Level;
    
    public int GetDataVersion() => 1;
}
```

**关键点：**
- 版本号必须是正整数
- 实现 `IAsakiVersionedSavable` 接口以启用版本控制
- `GetDataVersion()` 方法返回的版本号必须与特性中声明的版本一致

### 2. 数据迁移

当数据结构发生变化时，创建迁移类来定义如何转换数据：

```csharp
[AsakiMigration(typeof(PlayerDataV2), 1, 2)]
public class PlayerDataMigration_V1_to_V2 : AsakiMigrationBase<PlayerDataV2>
{
    public override int FromVersion => 1;
    public override int ToVersion => 2;
    
    public override void Migrate(PlayerDataV2 data)
    {
        // 设置新增字段的默认值
        data.Gold = 0;
    }
}
```

## 使用指南

### 第一步：声明数据版本

为你的数据类添加版本号：

```csharp
using Asaki.Core.Attributes;
using Asaki.Core.Serialization;

[AsakiSave(Version = 1)]
public partial class GameSaveData : IAsakiVersionedSavable
{
    [AsakiSaveMember(Order = 1)]
    public string SaveName;
    
    [AsakiSaveMember(Order = 2)]
    public int PlayTime;
    
    public int GetDataVersion() => 1;
}
```

### 第二步：修改数据结构时增加版本号

当需要添加、删除或修改字段时，创建新版本：

```csharp
[AsakiSave(Version = 2)]
public partial class GameSaveData : IAsakiVersionedSavable
{
    [AsakiSaveMember(Order = 1)]
    public string SaveName;
    
    [AsakiSaveMember(Order = 2)]
    public int PlayTime;
    
    [AsakiSaveMember(Order = 3)]
    public int Difficulty; // 新增字段
    
    public int GetDataVersion() => 2;
}
```

### 第三步：创建迁移类

定义如何从旧版本迁移到新版本：

```csharp
using Asaki.Core.Attributes;
using Asaki.Core.Serialization.Migration;

[AsakiMigration(typeof(GameSaveData), 1, 2)]
public class GameSaveDataMigration_V1_to_V2 : AsakiMigrationBase<GameSaveData>
{
    public override int FromVersion => 1;
    public override int ToVersion => 2;
    
    public override void Migrate(GameSaveData data)
    {
        // 为新字段设置合理的默认值
        data.Difficulty = 1; // 普通难度
    }
}
```

### 第四步：注册迁移到系统

在游戏启动时注册所有迁移：

```csharp
using Asaki.Core.Context;
using Asaki.Core.Serialization.Migration;

public class GameBootstrapper : MonoBehaviour
{
    void Start()
    {
        // 获取迁移注册表
        var registry = AsakiContext.Get<IAsakiMigrationRegistry>();
        
        // 注册迁移
        registry.RegisterMigration(new GameSaveDataMigration_V1_to_V2());
        
        // 如果有更多迁移，继续注册
        // registry.RegisterMigration(new GameSaveDataMigration_V2_to_V3());
    }
}
```

## 高级特性

### 链式迁移

系统自动支持链式迁移。例如，如果你有：
- V1 -> V2 的迁移
- V2 -> V3 的迁移

当加载V1的存档时，系统会自动执行 V1 -> V2 -> V3 的链式迁移。

```csharp
// 系统会自动查找最短路径
// V1 -> V2
[AsakiMigration(typeof(DataV2), 1, 2)]
public class Migration_V1_to_V2 : AsakiMigrationBase<DataV2> { ... }

// V2 -> V3
[AsakiMigration(typeof(DataV3), 2, 3)]
public class Migration_V2_to_V3 : AsakiMigrationBase<DataV3> { ... }

// 加载V1数据时，自动执行: V1 -> V2 -> V3
```

### 跳跃式迁移（性能优化）

对于常见的迁移路径，可以提供直接迁移以提高性能：

```csharp
// 直接从V1跳到V3（跳过V2）
[AsakiMigration(typeof(DataV3), 1, 3)]
public class Migration_V1_to_V3_Direct : AsakiMigrationBase<DataV3>
{
    public override int FromVersion => 1;
    public override int ToVersion => 3;
    
    public override void Migrate(DataV3 data)
    {
        // 一次性处理所有变更
        data.Gold = 0;          // V2 的变更
        data.Equipment = new List<string>(); // V3 的变更
    }
}
```

系统会自动选择最短路径（V1 -> V3）而不是链式路径（V1 -> V2 -> V3）。

### 复杂数据迁移

对于复杂的迁移逻辑，可以在 `Migrate` 方法中执行任何操作：

```csharp
[AsakiMigration(typeof(InventoryDataV3), 2, 3)]
public class InventoryMigration_V2_to_V3 : AsakiMigrationBase<InventoryDataV3>
{
    public override int FromVersion => 2;
    public override int ToVersion => 3;
    
    public override void Migrate(InventoryDataV3 data)
    {
        // 复杂逻辑：合并多个旧字段到新字段
        data.AllItems = new List<Item>();
        
        // 迁移武器
        if (data.OldWeapons != null)
        {
            foreach (var weapon in data.OldWeapons)
            {
                data.AllItems.Add(new Item 
                { 
                    Type = ItemType.Weapon, 
                    Name = weapon 
                });
            }
        }
        
        // 迁移消耗品
        if (data.OldConsumables != null)
        {
            foreach (var consumable in data.OldConsumables)
            {
                data.AllItems.Add(new Item 
                { 
                    Type = ItemType.Consumable, 
                    Name = consumable 
                });
            }
        }
        
        // 清理旧数据（可选）
        data.OldWeapons = null;
        data.OldConsumables = null;
    }
}
```

## 安全机制

### 1. 未知版本处理

当系统遇到未知版本的数据时（没有对应的迁移路径）：

```
警告日志：No migration path found for YourDataType from v1 to v3. 
         Attempting to deserialize directly (may fail or produce incorrect data).
```

系统会尝试直接反序列化数据，但不保证成功。建议为所有版本对提供迁移。

### 2. 迁移失败处理

如果迁移过程中发生异常：
- 记录详细的错误日志
- 抛出异常，阻止加载损坏的数据
- 保留原始存档文件不被修改

```csharp
try 
{
    var (meta, data) = await saveService.LoadSlotAsync<Meta, Data>(slotId);
}
catch (Exception ex)
{
    Debug.LogError($"Failed to load save: {ex.Message}");
    // 提示玩家存档可能损坏
}
```

### 3. 版本验证

系统在运行时验证：
- 迁移的 FromVersion 和 ToVersion 必须不同
- 迁移链不能有循环（V1 -> V2 -> V1）
- 目标版本必须与数据类的当前版本匹配

## 最佳实践

### 1. 永远不要删除旧的迁移类

即使某个版本已经很老了，也不要删除它的迁移类。有些玩家可能很长时间才回来玩游戏，他们的存档仍然是旧版本。

```csharp
// ✅ 保留所有历史迁移
V1 -> V2  // 保留
V2 -> V3  // 保留
V3 -> V4  // 保留

// ❌ 不要删除旧迁移
// 假设删除了 V1 -> V2，那么V1存档将无法加载
```

### 2. 使用语义化版本号

版本号应该反映变更的重要性：

```csharp
Version 1: 初始版本
Version 2: 添加了金币系统（小更新）
Version 3: 添加了装备系统（中等更新）
Version 4: 完全重构了数据结构（大更新）
```

### 3. 在迁移中记录日志

帮助调试和监控迁移过程：

```csharp
public override void Migrate(PlayerData data)
{
    ALog.Info($"[Migration] Migrating player {data.PlayerName} from V1 to V2");
    
    data.Gold = 0;
    
    ALog.Info($"[Migration] Set default gold: {data.Gold}");
}
```

### 4. 测试迁移

为每个迁移编写测试用例：

```csharp
[Test]
public void TestPlayerDataMigration_V1_to_V2()
{
    // 创建V1数据
    var v1Data = new PlayerDataV1 
    {
        PlayerName = "TestPlayer",
        Level = 10,
        Experience = 1000
    };
    
    // 执行迁移
    var migration = new PlayerDataMigration_V1_to_V2();
    var v2Data = new PlayerDataV2 
    {
        PlayerName = v1Data.PlayerName,
        Level = v1Data.Level,
        Experience = v1Data.Experience
    };
    migration.Migrate(v2Data);
    
    // 验证结果
    Assert.AreEqual(0, v2Data.Gold);
    Assert.AreEqual("TestPlayer", v2Data.PlayerName);
}
```

### 5. 文档化变更

在每个版本的数据类中添加注释，说明相比上一版本的变更：

```csharp
/// <summary>
/// 玩家数据 V2
/// 
/// 变更记录 (相比 V1):
/// - 新增: Gold (int) - 玩家金币数量，默认为0
/// 
/// 迁移: PlayerDataMigration_V1_to_V2
/// </summary>
[AsakiSave(Version = 2)]
public partial class PlayerDataV2 : IAsakiVersionedSavable
{
    ...
}
```

## 示例场景

### 场景1：添加新字段

**需求：** 游戏需要添加难度设置。

```csharp
// 旧版本
[AsakiSave(Version = 1)]
public partial class GameSettings
{
    [AsakiSaveMember] public float MusicVolume;
    [AsakiSaveMember] public float SfxVolume;
}

// 新版本
[AsakiSave(Version = 2)]
public partial class GameSettings
{
    [AsakiSaveMember] public float MusicVolume;
    [AsakiSaveMember] public float SfxVolume;
    [AsakiSaveMember] public int Difficulty; // 新增
}

// 迁移
[AsakiMigration(typeof(GameSettings), 1, 2)]
public class GameSettingsMigration_V1_to_V2 : AsakiMigrationBase<GameSettings>
{
    public override int FromVersion => 1;
    public override int ToVersion => 2;
    
    public override void Migrate(GameSettings data)
    {
        data.Difficulty = 1; // 默认普通难度
    }
}
```

### 场景2：重命名字段

**需求：** 将 `PlayerName` 重命名为 `Username`。

```csharp
// V1
[AsakiSave(Version = 1)]
public partial class UserProfile
{
    [AsakiSaveMember(Order = 1)] public string PlayerName;
}

// V2
[AsakiSave(Version = 2)]
public partial class UserProfile
{
    [AsakiSaveMember(Order = 1)] public string Username; // 重命名
}

// 迁移
[AsakiMigration(typeof(UserProfile), 1, 2)]
public class UserProfileMigration_V1_to_V2 : AsakiMigrationBase<UserProfile>
{
    public override int FromVersion => 1;
    public override int ToVersion => 2;
    
    public override void Migrate(UserProfile data)
    {
        // 注意：由于字段Order相同，反序列化时PlayerName的值
        // 会被读入到Username中，因此不需要额外操作
        // 但建议在此添加验证逻辑
        
        if (string.IsNullOrEmpty(data.Username))
        {
            data.Username = "Player"; // 后备默认值
        }
    }
}
```

### 场景3：合并多个字段

**需求：** 将分散的物品列表合并为统一的背包系统。

```csharp
// V2 (旧版本)
[AsakiSave(Version = 2)]
public partial class PlayerInventory
{
    [AsakiSaveMember] public List<string> Weapons;
    [AsakiSaveMember] public List<string> Armors;
    [AsakiSaveMember] public List<string> Consumables;
}

// V3 (新版本)
[AsakiSave(Version = 3)]
public partial class PlayerInventory
{
    [AsakiSaveMember] public List<InventoryItem> AllItems;
}

[AsakiSave]
public partial class InventoryItem
{
    [AsakiSaveMember] public string Name;
    [AsakiSaveMember] public string Category;
    [AsakiSaveMember] public int Quantity;
}

// 迁移
[AsakiMigration(typeof(PlayerInventory), 2, 3)]
public class InventoryMigration_V2_to_V3 : AsakiMigrationBase<PlayerInventory>
{
    public override int FromVersion => 2;
    public override int ToVersion => 3;
    
    public override void Migrate(PlayerInventory data)
    {
        data.AllItems = new List<InventoryItem>();
        
        // 合并武器
        foreach (var weapon in data.Weapons ?? new List<string>())
        {
            data.AllItems.Add(new InventoryItem 
            { 
                Name = weapon, 
                Category = "Weapon", 
                Quantity = 1 
            });
        }
        
        // 合并护甲
        foreach (var armor in data.Armors ?? new List<string>())
        {
            data.AllItems.Add(new InventoryItem 
            { 
                Name = armor, 
                Category = "Armor", 
                Quantity = 1 
            });
        }
        
        // 合并消耗品
        foreach (var consumable in data.Consumables ?? new List<string>())
        {
            data.AllItems.Add(new InventoryItem 
            { 
                Name = consumable, 
                Category = "Consumable", 
                Quantity = 1 
            });
        }
    }
}
```

## 故障排除

### 问题1：迁移未被执行

**症状：** 加载旧存档时没有应用迁移。

**可能原因：**
1. 迁移未注册到 `IAsakiMigrationRegistry`
2. 数据类未实现 `IAsakiVersionedSavable`
3. `GetDataVersion()` 返回的版本号不正确

**解决方案：**
```csharp
// 确保注册迁移
var registry = AsakiContext.Get<IAsakiMigrationRegistry>();
registry.RegisterMigration(new YourMigration());

// 确保实现接口
public partial class YourData : IAsakiVersionedSavable
{
    public int GetDataVersion() => 2; // 必须匹配 [AsakiSave(Version = 2)]
}
```

### 问题2：找不到迁移路径

**症状：** 日志显示 "No migration path found"。

**可能原因：** 缺少中间版本的迁移。

**解决方案：**
```csharp
// 错误：缺少 V1 -> V2 的迁移
V1 数据
V2 -> V3 迁移存在
V3 数据

// 正确：提供完整路径
V1 -> V2 迁移
V2 -> V3 迁移
或者
V1 -> V3 直接迁移
```

### 问题3：迁移后数据不正确

**症状：** 迁移执行了，但数据值不符合预期。

**可能原因：**
1. 字段 Order 顺序不匹配
2. 迁移逻辑有误
3. 反序列化顺序问题

**解决方案：**
```csharp
// 确保字段顺序一致
[AsakiSave(Version = 1)]
public partial class Data
{
    [AsakiSaveMember(Order = 1)] public string Name;
    [AsakiSaveMember(Order = 2)] public int Value;
}

[AsakiSave(Version = 2)]
public partial class Data
{
    [AsakiSaveMember(Order = 1)] public string Name;  // 保持相同顺序
    [AsakiSaveMember(Order = 2)] public int Value;     // 保持相同顺序
    [AsakiSaveMember(Order = 3)] public int NewField;  // 新字段放在最后
}
```

## API 参考

### 接口

#### IAsakiMigration
基础迁移接口。

```csharp
public interface IAsakiMigration
{
    int FromVersion { get; }
    int ToVersion { get; }
    string TypeName { get; }
    void Migrate(IAsakiReader reader, IAsakiWriter writer);
}
```

#### IAsakiMigration<TData>
强类型迁移接口。

```csharp
public interface IAsakiMigration<TData> : IAsakiMigration 
    where TData : IAsakiSavable
{
    void Migrate(TData data);
}
```

#### IAsakiMigrationRegistry
迁移注册表接口。

```csharp
public interface IAsakiMigrationRegistry
{
    void RegisterMigration(IAsakiMigration migration);
    List<IAsakiMigration> FindMigrationPath(string typeName, int fromVersion, int toVersion);
    bool HasMigrationPath(string typeName, int fromVersion, int toVersion);
    List<IAsakiMigration> GetMigrations(string typeName);
}
```

#### IAsakiVersionedSavable
版本化可序列化接口。

```csharp
public interface IAsakiVersionedSavable : IAsakiSavable
{
    int GetDataVersion();
}
```

### 基类

#### AsakiMigrationBase<TData>
迁移的抽象基类，简化实现。

```csharp
public abstract class AsakiMigrationBase<TData> : IAsakiMigration<TData>
    where TData : IAsakiSavable, new()
{
    public abstract int FromVersion { get; }
    public abstract int ToVersion { get; }
    public virtual string TypeName { get; }
    public abstract void Migrate(TData data);
    public virtual void Migrate(IAsakiReader reader, IAsakiWriter writer);
}
```

### 特性

#### [AsakiSave(Version = n)]
标记数据类的版本号。

```csharp
[AsakiSave(Version = 1)]
public partial class YourData { }
```

#### [AsakiMigration(dataType, fromVersion, toVersion)]
标记迁移类（可选，用于自动发现）。

```csharp
[AsakiMigration(typeof(YourData), 1, 2)]
public class YourMigration : AsakiMigrationBase<YourData> { }
```

## 总结

Asaki Framework 的数据版本控制与迁移系统提供了：

✅ **自动版本检测** - 加载时自动识别数据版本
✅ **灵活的迁移路径** - 支持链式迁移和跳跃式迁移
✅ **类型安全** - 强类型迁移API减少错误
✅ **易于使用** - 继承基类即可，无需手动处理reader/writer
✅ **详细日志** - 完整的迁移过程日志，便于调试
✅ **安全机制** - 未知版本和失败迁移的安全处理

通过遵循本文档的指南和最佳实践，你可以安全地演进游戏的数据模型，而不必担心破坏现有玩家的存档。
