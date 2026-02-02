# Asaki Framework - Data Versioning & Migration Pipeline

## 系统概览

本系统为 Asaki Framework 提供了完整的数据版本控制和自动迁移功能，确保游戏数据模型可以安全演进而不破坏现有玩家的存档。

### 核心特性

✅ **自动版本检测** - 加载存档时自动识别数据版本  
✅ **链式迁移** - 自动查找并执行最短迁移路径（如 V1→V2→V3）  
✅ **跳跃式迁移** - 支持直接迁移路径以提高性能（如 V1→V3）  
✅ **类型安全** - 强类型API减少运行时错误  
✅ **源生成器集成** - 自动生成版本元数据代码  
✅ **详细日志** - 完整的迁移过程日志便于调试  
✅ **安全机制** - 未知版本和失败迁移的安全处理  

## 架构设计

### 组件清单

| 组件 | 位置 | 职责 |
|------|------|------|
| **IAsakiMigration** | Core/Serialization/Migration/ | 迁移接口（低级和强类型） |
| **IAsakiMigrationRegistry** | Core/Serialization/Migration/ | 迁移注册和路径查找 |
| **AsakiMigrationRegistry** | Core/Serialization/Migration/ | 注册表实现（BFS路径查找） |
| **AsakiMigrationBase\<T\>** | Core/Serialization/Migration/ | 迁移基类，简化实现 |
| **IAsakiVersionedSavable** | Core/Serialization/ | 版本化可序列化接口 |
| **AsakiVersionMetadata** | Core/Serialization/Migration/ | 版本元数据 |
| **AsakiMigrationBinaryReader** | Unity/Services/Serialization/ | 支持迁移的二进制读取器 |
| **AsakiMigrationModule** | Unity/Modules/ | 迁移系统初始化模块 |
| **AsakiSaveGenerator** | CodeGen/Generators/ | 源生成器（自动生成版本方法） |

### 数据流

```
┌─────────────────┐
│  加载旧存档     │
│  (Version=1)    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  读取版本号     │
│  ReadVersion()  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ 版本不匹配检测  │
│ (1 ≠ 3)        │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  查找迁移路径   │
│  FindPath(1,3)  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  应用迁移链     │
│  V1→V2→V3      │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  返回V3数据     │
└─────────────────┘
```

## 使用指南

### 快速开始（5分钟）

查看 [QUICKSTART.md](./QUICKSTART.md) 获取快速上手指南。

### 完整文档

查看 [README.md](./README.md) 获取详细的使用说明、API参考和最佳实践。

### 示例代码

- **基础示例**：`Assets/Game/Scripts/Examples/MigrationExample.cs`
  - PlayerData V1→V2→V3 演进
  - 三种迁移方式演示
  
- **端到端演示**：`Assets/Game/Scripts/Examples/MigrationDemoScene.cs`
  - 完整的迁移流程演示
  - 验证和日志记录

### 单元测试

- **核心测试**：`Assets/Tests/Serialization/AsakiMigrationTests.cs`
  - 迁移注册测试
  - 路径查找测试（单步、多步、直接路径）
  - 版本元数据测试

## 集成说明

### 1. 模块初始化

迁移模块会在框架启动时自动初始化（优先级140）：

```csharp
[AsakiModule(140)]
public class AsakiMigrationModule : IAsakiModule
{
    // 自动创建并注册 IAsakiMigrationRegistry
}
```

### 2. 注册迁移

在游戏启动时注册所有迁移：

```csharp
public class GameBootstrap : MonoBehaviour
{
    void Awake()
    {
        var registry = AsakiContext.Get<IAsakiMigrationRegistry>();
        
        // 注册迁移
        registry.RegisterMigration(new PlayerDataMigration_V1_to_V2());
        registry.RegisterMigration(new PlayerDataMigration_V2_to_V3());
        // ... 更多迁移
    }
}
```

### 3. 声明版本化数据

```csharp
[AsakiSave(Version = 2)]
public partial class PlayerData : IAsakiVersionedSavable
{
    [AsakiSaveMember(Order = 1)]
    public string Name;
    
    [AsakiSaveMember(Order = 2)]
    public int Level;
    
    [AsakiSaveMember(Order = 3)]
    public int Gold; // V2新增
    
    // GetDataVersion() 由源生成器自动生成
}
```

### 4. 创建迁移

```csharp
public class PlayerDataMigration_V1_to_V2 : AsakiMigrationBase<PlayerData>
{
    public override int FromVersion => 1;
    public override int ToVersion => 2;
    
    public override void Migrate(PlayerData data)
    {
        data.Gold = 100; // 给老玩家100金币
    }
}
```

### 5. 正常使用

```csharp
// 加载存档 - 自动处理迁移
var saveService = AsakiContext.Get<IAsakiSaveService>();
var (meta, data) = await saveService.LoadSlotAsync<Meta, PlayerData>(slotId);

// 数据已自动迁移到最新版本
Debug.Log($"Player: {data.Name}, Gold: {data.Gold}");
```

## 源生成器集成

### 自动生成版本方法

当数据类实现 `IAsakiVersionedSavable` 时，源生成器会自动生成 `GetDataVersion()` 方法：

```csharp
// 用户代码
[AsakiSave(Version = 2)]
public partial class MyData : IAsakiVersionedSavable
{
    [AsakiSaveMember] public string Name;
}

// 生成的代码
partial class MyData
{
    public int GetDataVersion() => 2;
}
```

### 版本号提取

源生成器从 `[AsakiSave(Version = n)]` 特性中提取版本号：

```csharp
// 支持的格式
[AsakiSave(1)]              // 位置参数
[AsakiSave(Version = 2)]    // 命名参数
```

## 性能考虑

### 迁移路径缓存

迁移注册表使用 BFS 算法查找最短路径，并在内存中维护路径映射。

### 直接迁移优化

对于常见的迁移路径，建议提供直接迁移以减少中间步骤：

```csharp
// 链式迁移（2步）
V1 → V2 (100ms)
V2 → V3 (100ms)
总计：200ms

// 直接迁移（1步）
V1 → V3 (150ms)
总计：150ms （性能提升25%）
```

### 内存占用

- 迁移注册表：O(M) 其中 M 是迁移数量
- 路径查找：O(N) 其中 N 是版本数量
- 迁移执行：O(1) 每个迁移独立执行

## 安全与容错

### 版本不匹配处理

```
┌─────────────────┐
│ 加载数据        │
│ Version = X     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ 找到迁移路径？  │
└────┬───────┬────┘
     │ Yes   │ No
     │       ▼
     │  ┌─────────────────┐
     │  │ 记录警告日志    │
     │  │ 尝试直接反序列化│
     │  └─────────────────┘
     ▼
┌─────────────────┐
│ 应用迁移        │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ 迁移成功？      │
└────┬───────┬────┘
     │ Yes   │ No
     │       ▼
     │  ┌─────────────────┐
     │  │ 记录错误        │
     │  │ 抛出异常        │
     │  └─────────────────┘
     ▼
┌─────────────────┐
│ 返回迁移后数据  │
└─────────────────┘
```

### 错误处理

所有迁移错误都会：
1. 记录详细日志
2. 抛出异常（阻止加载损坏数据）
3. 保留原始存档文件

## 最佳实践

### ✅ DO

1. **版本递增**：每次修改数据结构时增加版本号
2. **保留迁移**：永远不要删除旧的迁移类
3. **测试驱动**：为每个迁移编写单元测试
4. **日志记录**：在迁移中添加详细日志
5. **文档化**：记录每个版本的变更内容

### ❌ DON'T

1. **不要修改**已发布版本的数据结构
2. **不要删除**旧的迁移类
3. **不要跳版本**：确保迁移链完整
4. **不要假设**：验证迁移前后的数据完整性
5. **不要阻塞**：避免在迁移中执行耗时操作

## 故障排除

### 问题：迁移未执行

**检查清单：**
- [ ] 迁移已注册到 `IAsakiMigrationRegistry`
- [ ] 数据类实现了 `IAsakiVersionedSavable`
- [ ] `GetDataVersion()` 返回正确版本号
- [ ] 迁移的 `TypeName` 与数据类完整名称匹配

### 问题：找不到迁移路径

**检查清单：**
- [ ] 所有中间版本都有对应的迁移
- [ ] 迁移的版本号连续（1→2→3）
- [ ] 没有版本跳跃（1→3需要提供直接迁移）

### 问题：迁移后数据不正确

**检查清单：**
- [ ] 字段 `Order` 顺序一致
- [ ] 迁移逻辑正确
- [ ] 新字段有合理的默认值
- [ ] 查看迁移日志了解详情

## 进一步阅读

- **快速开始**：[QUICKSTART.md](./QUICKSTART.md)
- **完整文档**：[README.md](./README.md)
- **示例代码**：
  - `Assets/Game/Scripts/Examples/MigrationExample.cs`
  - `Assets/Game/Scripts/Examples/MigrationDemoScene.cs`
- **单元测试**：`Assets/Tests/Serialization/AsakiMigrationTests.cs`

## 版本历史

- **v1.0.0** (2026-02-02)
  - 初始实现
  - 核心迁移系统
  - BFS路径查找
  - 源生成器集成
  - 完整文档和示例

---

**维护者**：Asaki Framework Team  
**最后更新**：2026-02-02
