# Asaki Framework Issue: UIConfig 与 WindowAssetId 枚举不同步

## 问题描述

Asaki 框架的 `AsakiUIGeneratorWindow` 工具在生成 `WindowAssetId` 枚举时，未同步更新 `AsakiFrameworkSetting` 中的 `UIConfig.UIList`，导致两者数据不一致。

## 复现步骤

1. 使用 `AsakiUIGeneratorWindow` 生成或更新 UI 资源 ID
2. 检查生成的 `WindowAssetId.cs` 枚举文件
3. 检查 `AsakiFrameworkSetting.asset` 中的 `uiConfig.UIList`
4. 对比两者数据，发现不同步

## 实际行为

- `WindowAssetId.cs` 生成了正确的枚举值：
  ```csharp
  public enum WindowAssetId
  {
      None = 0,
      PlayerStatusUI = 1930753510,
  }
  ```

- `AsakiFrameworkSetting.asset` 中的 `UIList` 可能缺少对应条目或 ID 不匹配

## 预期行为

`AsakiUIGeneratorWindow` 应该：
1. 生成 `WindowAssetId.cs` 枚举
2. **同时**更新 `AsakiFrameworkSetting.asset` 中的 `uiConfig.UIList`
3. 确保两者的 ID、名称、AssetPath 完全一致

## 影响范围

- `IAsakiUIService.OpenAsync<T>()` 返回 `null`
- UI 无法正常加载
- 错误日志：`[AsakiUI] UI ID {uiId} not found.`

## 根本原因

`AsakiUIManageService.OpenAsync()` 中的检查逻辑：

```csharp
if (_uiConfig == null || !_uiConfig.TryGet(uiId, out UIInfo info))
{
    ALog.Warn($"[AsakiUI] UI ID {uiId} not found.");
    return null;  // ← 返回 null，导致后续 NullReferenceException
}
```

当 `UIConfig` 中缺少对应 ID 时，直接返回 `null`。

## 临时解决方案

手动在 `AsakiFrameworkSetting.asset` 中添加/更新 `UIList` 条目：

```yaml
uiConfig:
  UIList:
  - Name: PlayerStatusUI
    ID: 1930753510
    Layer: 1
    AssetPath: Assets/Data/Prefabs/UI/PlayerStatusUI.prefab
    UsePool: 0
```

## 建议修复

1. **修改 `AsakiUIGeneratorWindow`**：在生成枚举后，自动更新 `AsakiFrameworkSetting` 的 `UIConfig`

2. **添加数据验证**：在编辑器中添加菜单项，用于检查并修复 `WindowAssetId` 与 `UIConfig` 的同步问题

3. **运行时检查**：在 `AsakiUIManageService.OnInitAsync()` 中添加验证逻辑，检测不同步并输出警告

## 相关文件

- `Library/PackageCache/com.asaki.framework/Unity/Services/UI/AsakiUIManageService.cs`
- `Library/PackageCache/com.asaki.framework/Editor/UI/AsakiUIGeneratorWindow.cs` (推测路径)
- `Assets/Asaki/Generated/UIAsset_2_Id/WindowAssetId.cs`
- `Assets/Resources/Asaki/DataTable/AsakiFrameworkSetting.asset`

## 环境信息

- Asaki Framework 版本：v1.3.9+
- Unity 版本：2022.3+
- 项目：ARPGDemo

---

**状态**：待提交到 Asaki Framework 仓库
**日期**：2026-02-27
