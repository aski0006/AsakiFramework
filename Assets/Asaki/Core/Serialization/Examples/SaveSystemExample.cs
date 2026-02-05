// 这是示例代码，展示如何使用新的保存系统
// 实际使用时请根据需要修改

using System;
using System.Collections.Generic;
using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Core.Serialization.Examples
{
    /// <summary>
    /// 游戏存档数据示例
    /// </summary>
    [AsakiSave(Version = 1)]
    public partial class GameSaveData
    {
        [AsakiSaveMember]
        public Vector3 PlayerPosition;

        [AsakiSaveMember]
        public int PlayerLevel;

        [AsakiSaveMember]
        public string PlayerName;

        [AsakiSaveMember]
        public int CurrentChapter;

        [AsakiSaveMember]
        public float PlayTime;

        [AsakiSaveMember]
        public List<int> UnlockedLevels;
    }

    /// <summary>
    /// 保存系统使用示例
    /// </summary>
    public class SaveSystemExample : MonoBehaviour
    {
        private IAsakiSaveSlotManager _slotManager;
        private IAsakiAutoSaveService _autoSaveService;

        private void Start()
        {
            // 从服务容器获取服务
            _slotManager = AsakiContext.Get<IAsakiSaveSlotManager>();
            _autoSaveService = AsakiContext.Get<IAsakiAutoSaveService>();

            // 注册自动保存数据提供者
            _autoSaveService.RegisterDataProvider(CreateSaveData);

            // 配置自动保存
            var config = new AsakiAutoSaveConfig
            {
                Enabled = true,
                Triggers = AsakiAutoSaveTrigger.Checkpoint | AsakiAutoSaveTrigger.ApplicationPause,
                ShowNotification = true,
                MaxAutoSaveCount = 3,
            };
            _autoSaveService.SetConfig(config);
            _autoSaveService.StartService();
        }

        // =========================================================
        // 基础保存/加载操作
        // =========================================================

        /// <summary>
        /// 创建新存档（自动分配槽位）
        /// </summary>
        public async UniTask CreateNewSave()
        {
            var data = CreateSaveData();
            var slot = await _slotManager.CreateSaveAsync("新的冒险", data);

            Debug.Log($"存档已创建: 槽位 {slot.SlotId}, 名称: {slot.SaveName}");
        }

        /// <summary>
        /// 覆盖指定槽位的存档
        /// </summary>
        public async UniTask OverwriteSave(int slotId)
        {
            var data = CreateSaveData();
            var slot = await _slotManager.OverwriteSaveAsync(slotId, "继续冒险", data);

            Debug.Log($"存档已更新: 槽位 {slot.SlotId}, 时间: {slot.GetFormattedSaveTime()}");
        }

        /// <summary>
        /// 加载指定槽位的存档
        /// </summary>
        public async UniTask LoadSave(int slotId)
        {
            try
            {
                var (slot, data) = await _slotManager.LoadSaveAsync<GameSaveData>(slotId);

                ApplySaveData(data);
                Debug.Log($"存档已加载: {slot.SaveName}, 游戏时长: {slot.GetFormattedPlayTime()}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"加载存档失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载最新的存档
        /// </summary>
        public async UniTask LoadLatestSave()
        {
            var result = await _slotManager.LoadLatestSaveAsync<GameSaveData>();

            if (result.HasValue)
            {
                var (slot, data) = result.Value;
                ApplySaveData(data);
                Debug.Log($"已加载最新存档: {slot.SaveName}");
            }
            else
            {
                Debug.Log("没有找到存档");
            }
        }

        // =========================================================
        // 槽位管理
        // =========================================================

        /// <summary>
        /// 获取所有存档列表（用于 UI 展示）
        /// </summary>
        public void ShowAllSaves()
        {
            var slots = _slotManager.GetOccupiedSlots();

            Debug.Log($"=== 共有 {slots.Count} 个存档 ===");
            foreach (var slot in slots)
            {
                Debug.Log(
                    $"[{slot.SlotId}] {slot.SaveName} - "
                        + $"{slot.GetFormattedSaveTime()} - "
                        + $"进度: {slot.ProgressPercent:F1}% - "
                        + $"时长: {slot.GetFormattedPlayTime()}"
                );
            }
        }

        /// <summary>
        /// 删除存档
        /// </summary>
        public void DeleteSave(int slotId)
        {
            bool success = _slotManager.DeleteSave(slotId);
            Debug.Log(success ? $"槽位 {slotId} 已删除" : $"删除槽位 {slotId} 失败");
        }

        /// <summary>
        /// 复制存档
        /// </summary>
        public async UniTask CopySave(int sourceSlotId)
        {
            try
            {
                var newSlot = await _slotManager.CopySaveAsync(sourceSlotId);
                Debug.Log($"存档已复制到新槽位 {newSlot.SlotId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"复制存档失败: {ex.Message}");
            }
        }

        // =========================================================
        // 自动保存
        // =========================================================

        /// <summary>
        /// 手动触发自动保存
        /// </summary>
        public async UniTask TriggerAutoSave()
        {
            bool success = await _autoSaveService.ForceAutoSaveAsync();
            Debug.Log(success ? "自动保存成功" : "自动保存失败");
        }

        /// <summary>
        /// 触发检查点保存
        /// </summary>
        public async UniTask TriggerCheckpoint(string checkpointName)
        {
            bool success = await _autoSaveService.TriggerCheckpointSaveAsync(checkpointName);
            Debug.Log(success ? $"检查点 '{checkpointName}' 已保存" : "检查点保存失败");
        }

        /// <summary>
        /// 加载自动保存
        /// </summary>
        public async UniTask LoadAutoSave()
        {
            var result = await _slotManager.LoadAutoSaveAsync<GameSaveData>();

            if (result.HasValue)
            {
                var (slot, data) = result.Value;
                ApplySaveData(data);
                Debug.Log($"自动存档已加载: {slot.GetFormattedSaveTime()}");
            }
            else
            {
                Debug.Log("没有找到自动存档");
            }
        }

        // =========================================================
        // 快速保存/加载
        // =========================================================

        /// <summary>
        /// 快速保存（通常绑定到 F5 键）
        /// </summary>
        public async UniTask QuickSave()
        {
            var data = CreateSaveData();
            var slot = await _slotManager.QuickSaveAsync(data);
            Debug.Log($"快速保存完成: {slot.SaveName}");
        }

        /// <summary>
        /// 快速加载（通常绑定到 F9 键）
        /// </summary>
        public async UniTask QuickLoad()
        {
            var result = await _slotManager.LoadQuickSaveAsync<GameSaveData>();

            if (result.HasValue)
            {
                var (slot, data) = result.Value;
                ApplySaveData(data);
                Debug.Log($"快速加载完成: {slot.SaveName}");
            }
            else
            {
                Debug.Log("没有找到快速存档");
            }
        }

        // =========================================================
        // 备份管理
        // =========================================================

        /// <summary>
        /// 创建存档备份
        /// </summary>
        public async UniTask CreateBackup(int slotId, string backupName)
        {
            var backup = await _slotManager.CreateBackupAsync(slotId, backupName);
            Debug.Log($"备份已创建: {backup.SaveName}");
        }

        // =========================================================
        // 辅助方法
        // =========================================================

        private GameSaveData CreateSaveData()
        {
            return new GameSaveData
            {
                PlayerPosition = transform.position,
                PlayerLevel = 10,
                PlayerName = "勇者",
                CurrentChapter = 3,
                PlayTime = UnityEngine.Time.time,
                UnlockedLevels = new List<int> { 1, 2, 3 },
            };
        }

        private void ApplySaveData(GameSaveData data)
        {
            transform.position = data.PlayerPosition;
            // 应用其他数据...
        }
    }
}
