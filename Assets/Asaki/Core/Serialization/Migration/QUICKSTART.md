# Asaki Framework - 数据迁移快速入门

## 5分钟快速上手

### 步骤1：定义带版本的数据类

```csharp
using Asaki.Core.Attributes;
using Asaki.Core.Serialization;

// 声明版本号为1
[AsakiSave(Version = 1)]
public partial class PlayerData : IAsakiVersionedSavable
{
    [AsakiSaveMember(Order = 1)]
    public string PlayerName;
    
    [AsakiSaveMember(Order = 2)]
    public int Level;
    
    // GetDataVersion() 会由源生成器自动生成
}
```

**注意：**
- 使用 `partial class` 让源生成器可以添加代码
- 实现 `IAsakiVersionedSavable` 接口（源生成器会自动实现GetDataVersion方法）
- 版本号从1开始

### 步骤2：修改数据结构时增加版本号

当需要添加新字段时：

```csharp
// 更新为版本2
[AsakiSave(Version = 2)]
public partial class PlayerData : IAsakiVersionedSavable
{
    [AsakiSaveMember(Order = 1)]
    public string PlayerName;
    
    [AsakiSaveMember(Order = 2)]
    public int Level;
    
    [AsakiSaveMember(Order = 3)]
    public int Gold; // 新增字段
}
```

### 步骤3：创建迁移类

```csharp
using Asaki.Core.Serialization.Migration;

public class PlayerDataMigration_V1_to_V2 : AsakiMigrationBase<PlayerData>
{
    public override int FromVersion => 1;
    public override int ToVersion => 2;
    
    public override void Migrate(PlayerData data)
    {
        // 为新字段设置默认值
        data.Gold = 100; // 给所有老玩家100金币
    }
}
```

### 步骤4：注册迁移

在游戏启动时注册迁移：

```csharp
using Asaki.Core.Context;
using Asaki.Core.Serialization.Migration;

public class GameBootstrap : MonoBehaviour
{
    void Awake()
    {
        // 获取迁移注册表
        var registry = AsakiContext.Get<IAsakiMigrationRegistry>();
        
        // 注册迁移
        registry.RegisterMigration(new PlayerDataMigration_V1_to_V2());
    }
}
```

### 步骤5：正常使用 - 迁移自动执行

```csharp
using Asaki.Core.Context;
using Asaki.Core.Serialization;

public class GameManager : MonoBehaviour
{
    async void LoadGame()
    {
        var saveService = AsakiContext.Get<IAsakiSaveService>();
        
        // 加载存档 - 如果是V1存档，会自动迁移到V2
        var (meta, data) = await saveService.LoadSlotAsync<SlotMeta, PlayerData>(0);
        
        Debug.Log($"Player: {data.PlayerName}, Level: {data.Level}, Gold: {data.Gold}");
        // 即使是V1存档，也能正常读取Gold字段（迁移已设置为100）
    }
}
```

## 完整示例：多版本演进

```csharp
// ===== 版本1：初始版本 =====
[AsakiSave(Version = 1)]
public partial class InventoryData : IAsakiVersionedSavable
{
    [AsakiSaveMember] public List<string> Weapons;
    [AsakiSaveMember] public List<string> Armors;
}

// ===== 版本2：统一为物品系统 =====
[AsakiSave(Version = 2)]
public partial class InventoryData : IAsakiVersionedSavable
{
    [AsakiSaveMember] public List<Item> AllItems;
}

[AsakiSave]
public partial class Item
{
    [AsakiSaveMember] public string Name;
    [AsakiSaveMember] public string Type;
    [AsakiSaveMember] public int Count;
}

// ===== 迁移 V1 -> V2 =====
public class InventoryMigration_V1_to_V2 : AsakiMigrationBase<InventoryData>
{
    public override int FromVersion => 1;
    public override int ToVersion => 2;
    
    public override void Migrate(InventoryData data)
    {
        data.AllItems = new List<Item>();
        
        // 合并武器
        if (data.Weapons != null)
        {
            foreach (var weapon in data.Weapons)
            {
                data.AllItems.Add(new Item 
                { 
                    Name = weapon, 
                    Type = "Weapon", 
                    Count = 1 
                });
            }
        }
        
        // 合并护甲
        if (data.Armors != null)
        {
            foreach (var armor in data.Armors)
            {
                data.AllItems.Add(new Item 
                { 
                    Name = armor, 
                    Type = "Armor", 
                    Count = 1 
                });
            }
        }
    }
}

// ===== 版本3：添加稀有度 =====
[AsakiSave(Version = 3)]
public partial class InventoryData : IAsakiVersionedSavable
{
    [AsakiSaveMember] public List<Item> AllItems;
}

[AsakiSave]
public partial class Item
{
    [AsakiSaveMember] public string Name;
    [AsakiSaveMember] public string Type;
    [AsakiSaveMember] public int Count;
    [AsakiSaveMember] public int Rarity; // 新增
}

// ===== 迁移 V2 -> V3 =====
public class InventoryMigration_V2_to_V3 : AsakiMigrationBase<InventoryData>
{
    public override int FromVersion => 2;
    public override int ToVersion => 3;
    
    public override void Migrate(InventoryData data)
    {
        // 为所有物品设置默认稀有度
        foreach (var item in data.AllItems)
        {
            item.Rarity = 1; // 默认普通品质
        }
    }
}
```

注册所有迁移：

```csharp
void RegisterMigrations()
{
    var registry = AsakiContext.Get<IAsakiMigrationRegistry>();
    registry.RegisterMigration(new InventoryMigration_V1_to_V2());
    registry.RegisterMigration(new InventoryMigration_V2_to_V3());
}
```

现在，无论玩家的存档是V1、V2还是V3，都能正常加载：
- V1存档：自动执行 V1->V2->V3 链式迁移
- V2存档：自动执行 V2->V3 迁移
- V3存档：无需迁移，直接加载

## 常见问题

### Q: 如何测试迁移？

创建单元测试：

```csharp
[Test]
public void TestMigration_V1_to_V2()
{
    var v1Data = new PlayerDataV1 
    { 
        PlayerName = "Alice", 
        Level = 5 
    };
    
    var migration = new PlayerDataMigration_V1_to_V2();
    var v2Data = new PlayerDataV2 
    { 
        PlayerName = v1Data.PlayerName, 
        Level = v1Data.Level 
    };
    
    migration.Migrate(v2Data);
    
    Assert.AreEqual(100, v2Data.Gold);
}
```

### Q: 如何处理删除字段的情况？

```csharp
// V1: 有 OldField
[AsakiSave(Version = 1)]
public partial class MyData
{
    [AsakiSaveMember] public string Name;
    [AsakiSaveMember] public int OldField;
}

// V2: 删除了 OldField
[AsakiSave(Version = 2)]
public partial class MyData
{
    [AsakiSaveMember] public string Name;
    // OldField 已删除
}

// 迁移 - 无需特殊处理，直接忽略旧字段
public class Migration_V1_to_V2 : AsakiMigrationBase<MyData>
{
    public override int FromVersion => 1;
    public override int ToVersion => 2;
    
    public override void Migrate(MyData data)
    {
        // 删除字段时，无需做任何处理
        // 反序列化会自动忽略不存在的字段
    }
}
```

### Q: 如何重命名字段？

```csharp
// V1
[AsakiSave(Version = 1)]
public partial class MyData
{
    [AsakiSaveMember(Order = 1)] public string OldName;
}

// V2
[AsakiSave(Version = 2)]
public partial class MyData
{
    [AsakiSaveMember(Order = 1)] public string NewName; // 重命名
}

// 迁移 - 由于Order相同，值会自动映射
public class Migration_V1_to_V2 : AsakiMigrationBase<MyData>
{
    public override int FromVersion => 1;
    public override int ToVersion => 2;
    
    public override void Migrate(MyData data)
    {
        // Order相同时，值会自动从OldName传递到NewName
        // 可以添加验证逻辑
        if (string.IsNullOrEmpty(data.NewName))
        {
            data.NewName = "DefaultName";
        }
    }
}
```

## 最佳实践总结

✅ **DO:**
- 每次修改数据结构时增加版本号
- 为每个版本变更创建迁移类
- 在迁移中添加日志，便于调试
- 编写迁移的单元测试
- 保留所有历史迁移类

❌ **DON'T:**
- 不要删除旧的迁移类
- 不要修改已发布版本的数据结构
- 不要在迁移中执行耗时操作（如网络请求）
- 不要假设迁移一定成功，做好错误处理

## 更多资源

- 完整文档：`Assets/Asaki/Core/Serialization/Migration/README.md`
- 示例代码：`Assets/Game/Scripts/Examples/MigrationExample.cs`
- 单元测试：`Assets/Tests/Serialization/AsakiMigrationTests.cs`
