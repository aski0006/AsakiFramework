# Asaki 保存系统

Asaki 保存系统提供了完整的游戏存档管理功能，包括自动槽位管理、自动保存、存档备份等功能。

## 核心特性

- **自动槽位管理** - 无需手动管理 slotId，系统自动分配和查找槽位
- **丰富的元数据** - 支持游戏时长、进度百分比、封面截图等
- **自动保存** - 支持多种触发条件（时间间隔、检查点、场景切换等）
- **存档备份** - 支持创建备份和从备份恢复
- **快速保存/加载** - 专用槽位支持快速保存和加载

## 架构概览

```
Core/Serialization/          # 核心接口和定义
├── IAsakiSaveService        # 基础保存服务接口
├── IAsakiSaveSlotManager    # 槽位管理器接口 [新增]
├── IAsakiSaveSlot           # 槽位信息接口 [新增]
├── IAsakiAutoSaveService    # 自动保存服务接口 [新增]
├── IAsakiAutoSaveConfig     # 自动保存配置接口 [新增]
└── IAsakiSavable            # 可序列化对象接口

Unity/Services/Serialization/# Unity 实现
├── AsakiSaveService         # 基础保存服务实现
├── AsakiSaveSlotManager     # 槽位管理器实现 [新增]
├── AsakiAutoSaveService     # 自动保存服务实现 [新增]
└── AsakiBinarySerialization # 二进制序列化
```

## 快速开始

### 1. 定义存档数据

```csharp
using Asaki.Core.Serialization;
using UnityEngine;

[AsakiSave(Version = 1)]
public partial class GameSaveData : IAsakiSavable
{
    [AsakiSaveMember(0)]
    public Vector3 PlayerPosition;
    
    [AsakiSaveMember(1)]
    public int PlayerLevel;
    
    [AsakiSaveMember(2)]
    public string PlayerName;
}
```

### 2. 创建新存档

```csharp
using Asaki.Core.Context;
using Asaki.Core.Serialization;

public class GameManager : MonoBehaviour
{
    private IAsakiSaveSlotManager _slotManager;
    
    private void Start()
    {
        _slotManager = AsakiContext.Get<IAsakiSaveSlotManager>();
    }
    
    public async UniTask CreateNewSave()
    {
        var data = new GameSaveData
        {
            PlayerPosition = transform.position,
            PlayerLevel = 10,
            PlayerName = "勇者"
        };
        
        // 自动分配槽位
        var slot = await _slotManager.CreateSaveAsync("第一章", data);
        Debug.Log($"存档已创建: 槽位 {slot.SlotId}");
    }
}
```

### 3. 加载存档

```csharp
public async UniTask LoadSave(int slotId)
{
    var (slot, data) = await _slotManager.LoadSaveAsync<GameSaveData>(slotId);
    
    transform.position = data.PlayerPosition;
    Debug.Log($"已加载: {slot.SaveName}, 游戏时长: {slot.GetFormattedPlayTime()}");
}
```

### 4. 配置自动保存

```csharp
private void SetupAutoSave()
{
    var autoSaveService = AsakiContext.Get<IAsakiAutoSaveService>();
    
    // 注册数据提供者
    autoSaveService.RegisterDataProvider(() => CreateSaveData());
    
    // 配置
    var config = new AsakiAutoSaveConfig
    {
        Enabled = true,
        Triggers = AsakiAutoSaveTrigger.Checkpoint | AsakiAutoSaveTrigger.ApplicationPause,
        ShowNotification = true,
        MaxAutoSaveCount = 3
    };
    
    autoSaveService.SetConfig(config);
    autoSaveService.StartService();
}
```

## 槽位管理

### 获取存档列表

```csharp
// 获取所有已占用的槽位（按时间倒序）
var slots = _slotManager.GetOccupiedSlots();

foreach (var slot in slots)
{
    Debug.Log($"[{slot.SlotId}] {slot.SaveName} - {slot.GetFormattedSaveTime()}");
}
```

### 槽位操作

```csharp
// 检查槽位状态
bool isEmpty = _slotManager.IsSlotEmpty(0);
bool isValid = _slotManager.IsSlotValid(0);

// 查找最佳保存槽位
int bestSlot = _slotManager.FindBestSlotForSave();

// 删除存档
_slotManager.DeleteSave(slotId);

// 复制存档
var newSlot = await _slotManager.CopySaveAsync(sourceSlotId);

// 锁定/解锁槽位
_slotManager.LockSlot(slotId);
_slotManager.UnlockSlot(slotId);
```

## 自动保存

### 触发条件

```csharp
[Flags]
public enum AsakiAutoSaveTrigger
{
    None = 0,              // 禁用
    TimeInterval = 1,      // 时间间隔
    Checkpoint = 2,        // 检查点
    SceneChange = 4,       // 场景切换
    ApplicationPause = 8,  // 应用暂停
    Manual = 16            // 手动触发
}
```

### 手动触发

```csharp
// 强制立即保存
await _autoSaveService.ForceAutoSaveAsync();

// 触发检查点
await _autoSaveService.TriggerCheckpointSaveAsync("关卡完成");

// 场景切换时
await _autoSaveService.TriggerSceneSaveAsync(sceneName, isEnter: true);
```

### 事件监听

```csharp
_autoSaveService.OnAutoSaveBegin += (args) =>
{
    Debug.Log($"自动保存开始: {args.Trigger}");
};

_autoSaveService.OnAutoSaveComplete += (args) =>
{
    if (args.Success)
        Debug.Log($"自动保存完成，耗时: {args.ElapsedMilliseconds}ms");
    else
        Debug.LogError($"自动保存失败: {args.ErrorMessage}");
};

_autoSaveService.OnCountdownBegin += (seconds) =>
{
    Debug.Log($"{seconds}秒后开始自动保存...");
};
```

## 快速保存/加载

```csharp
// 快速保存（通常绑定到 F5）
public async UniTask QuickSave()
{
    var data = CreateSaveData();
    var slot = await _slotManager.QuickSaveAsync(data);
}

// 快速加载（通常绑定到 F9）
public async UniTask QuickLoad()
{
    var result = await _slotManager.LoadQuickSaveAsync<GameSaveData>();
    if (result.HasValue)
    {
        var (slot, data) = result.Value;
        ApplySaveData(data);
    }
}
```

## 存档备份

```csharp
// 创建备份
var backup = await _slotManager.CreateBackupAsync(slotId, "通关前备份");

// 从备份恢复（需实现备份索引系统）
var restoredSlot = await _slotManager.RestoreFromBackupAsync(backupSlotId);
```

## 高级配置

### 槽位管理器配置

```csharp
public class AsakiSaveSlotManager : IAsakiSaveSlotManager
{
    public AsakiSaveSlotManager(
        IAsakiSaveService saveService,
        IAsakiEventService eventService,
        int maxSlots = 99,              // 最大槽位数
        int autoSaveSlotIndex = 0,      // 自动保存槽位
        int quickSaveSlotIndex = 1      // 快速保存槽位
    )
}
```

### 自定义元数据

```csharp
public class MySaveSlot : AsakiSaveSlot
{
    public string CustomField { get; set; }
    
    public override void Serialize(IAsakiWriter writer)
    {
        base.Serialize(writer);
        writer.WriteString("CustomField", CustomField);
    }
    
    public override void Deserialize(IAsakiReader reader)
    {
        base.Deserialize(reader);
        CustomField = reader.ReadString("CustomField");
    }
}
```

## 存储结构

```
persistentDataPath/
└── Saves/
    ├── Slot_0/              # 自动保存槽位
    │   ├── data.bin         # 二进制存档数据
    │   └── meta.json        # 元数据
    ├── Slot_1/              # 快速保存槽位
    │   ├── data.bin
    │   └── meta.json
    ├── Slot_2/              # 普通存档
    │   ├── data.bin
    │   └── meta.json
    └── Backups/             # 备份目录
        ├── Slot_2_Backup_1234567890/
        └── ...
```

## 最佳实践

1. **及时更新元数据** - 保存前更新 `PlayTime`、`ProgressPercent` 等字段
2. **使用异步 API** - 所有保存/加载操作都是异步的，避免阻塞主线程
3. **处理异常** - 保存操作可能失败（存储空间不足等），需要适当处理
4. **合理配置自动保存** - 过于频繁的自动保存会影响性能和体验
5. **提供用户控制** - 允许玩家关闭自动保存或调整自动保存频率

## 与旧版本兼容

旧的 `IAsakiSaveService` API 仍然可用，但推荐使用新的 `IAsakiSaveSlotManager` 来获得更好的开发体验。

```csharp
// 旧 API（仍然支持）
var saveService = AsakiContext.Get<IAsakiSaveService>();
await saveService.SaveSlotAsync(slotId, meta, data);

// 新 API（推荐）
var slotManager = AsakiContext.Get<IAsakiSaveSlotManager>();
var slot = await slotManager.CreateSaveAsync("存档名", data);
```
