using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Context;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Serialization
{
    /// <summary>
    /// 保存槽位管理器接口，提供自动槽位分配和管理功能
    /// </summary>
    /// <remarks>
    /// 槽位管理器简化了存档操作流程，提供高级 API 如自动创建、查找空槽、
    /// 获取存档列表等功能，开发者无需手动管理 slotId。
    /// </remarks>
    public interface IAsakiSaveSlotManager : IAsakiModule
    {
        /// <summary>
        /// 最大槽位数量（通常为 10-99）
        /// </summary>
        int MaxSlots { get; }

        /// <summary>
        /// 自动保存槽位索引（通常为 0，表示自动存档专用槽）
        /// </summary>
        int AutoSaveSlotIndex { get; }

        /// <summary>
        /// 快速保存槽位索引（通常为 1，表示快速存档专用槽）
        /// </summary>
        int QuickSaveSlotIndex { get; }

        /// <summary>
        /// 获取所有槽位的信息（包含空槽位）
        /// </summary>
        /// <returns>槽位信息列表，按 SlotId 排序</returns>
        IReadOnlyList<IAsakiSaveSlot> GetAllSlots();

        /// <summary>
        /// 获取所有已占用的槽位
        /// </summary>
        /// <returns>已占用槽位列表，按最后保存时间倒序排列</returns>
        IReadOnlyList<IAsakiSaveSlot> GetOccupiedSlots();

        /// <summary>
        /// 获取第一个可用的空槽位
        /// </summary>
        /// <returns>空槽位的 SlotId，如果没有可用槽位返回 -1</returns>
        int GetFirstEmptySlot();

        /// <summary>
        /// 查找最适合保存的槽位（优先空槽位，其次最早的存档）
        /// </summary>
        /// <returns>推荐的槽位 SlotId</returns>
        int FindBestSlotForSave();

        /// <summary>
        /// 获取指定槽位的信息
        /// </summary>
        /// <param name="slotId">槽位 ID</param>
        /// <returns>槽位信息，如果不存在返回 null</returns>
        IAsakiSaveSlot GetSlotInfo(int slotId);

        /// <summary>
        /// 检查槽位是否为空
        /// </summary>
        /// <param name="slotId">槽位 ID</param>
        /// <returns>是否为空</returns>
        bool IsSlotEmpty(int slotId);

        /// <summary>
        /// 检查槽位是否有效（存在且未损坏）
        /// </summary>
        /// <param name="slotId">槽位 ID</param>
        /// <returns>是否有效</returns>
        bool IsSlotValid(int slotId);

        /// <summary>
        /// 获取已使用槽位数量
        /// </summary>
        int GetUsedSlotCount();

        /// <summary>
        /// 获取剩余可用槽位数量
        /// </summary>
        int GetRemainingSlotCount();

        /// <summary>
        /// 检查是否还有可用槽位
        /// </summary>
        bool HasEmptySlot();

        /// <summary>
        /// 锁定槽位（防止被覆盖）
        /// </summary>
        /// <param name="slotId">槽位 ID</param>
        /// <returns>是否成功</returns>
        bool LockSlot(int slotId);

        /// <summary>
        /// 解锁槽位
        /// </summary>
        /// <param name="slotId">槽位 ID</param>
        /// <returns>是否成功</returns>
        bool UnlockSlot(int slotId);

        /// <summary>
        /// 刷新槽位信息缓存
        /// </summary>
        UniTask RefreshSlotsAsync(CancellationToken token = default);

        /// <summary>
        /// 创建新的存档槽位（自动分配 ID）
        /// </summary>
        /// <param name="saveName">存档名称</param>
        /// <param name="data">存档数据</param>
        /// <param name="token">取消令牌</param>
        /// <returns>创建的槽位信息</returns>
        UniTask<IAsakiSaveSlot> CreateSaveAsync<TData>(
            string saveName,
            TData data,
            CancellationToken token = default
        )
            where TData : IAsakiSavable;

        /// <summary>
        /// 覆盖指定槽位的存档
        /// </summary>
        /// <param name="slotId">槽位 ID</param>
        /// <param name="saveName">存档名称</param>
        /// <param name="data">存档数据</param>
        /// <param name="token">取消令牌</param>
        /// <returns>更新后的槽位信息</returns>
        UniTask<IAsakiSaveSlot> OverwriteSaveAsync<TData>(
            int slotId,
            string saveName,
            TData data,
            CancellationToken token = default
        )
            where TData : IAsakiSavable;

        /// <summary>
        /// 加载指定槽位的存档
        /// </summary>
        /// <param name="slotId">槽位 ID</param>
        /// <param name="token">取消令牌</param>
        /// <returns>存档数据和槽位信息</returns>
        UniTask<(IAsakiSaveSlot Slot, TData Data)> LoadSaveAsync<TData>(
            int slotId,
            CancellationToken token = default
        )
            where TData : IAsakiSavable, new();

        /// <summary>
        /// 加载最新的存档
        /// </summary>
        /// <param name="token">取消令牌</param>
        /// <returns>存档数据和槽位信息，如果没有存档返回 null</returns>
        UniTask<(IAsakiSaveSlot Slot, TData Data)?> LoadLatestSaveAsync<TData>(
            CancellationToken token = default
        )
            where TData : IAsakiSavable, new();

        /// <summary>
        /// 删除指定槽位的存档
        /// </summary>
        /// <param name="slotId">槽位 ID</param>
        /// <returns>是否成功</returns>
        bool DeleteSave(int slotId);

        /// <summary>
        /// 复制存档到另一个槽位
        /// </summary>
        /// <param name="sourceSlotId">源槽位 ID</param>
        /// <param name="targetSlotId">目标槽位 ID（-1 表示自动分配）</param>
        /// <param name="token">取消令牌</param>
        /// <returns>目标槽位信息</returns>
        UniTask<IAsakiSaveSlot> CopySaveAsync(
            int sourceSlotId,
            int targetSlotId = -1,
            CancellationToken token = default
        );

        /// <summary>
        /// 创建存档备份
        /// </summary>
        /// <param name="slotId">槽位 ID</param>
        /// <param name="backupName">备份名称（可选）</param>
        /// <param name="token">取消令牌</param>
        /// <returns>备份槽位信息</returns>
        UniTask<IAsakiSaveSlot> CreateBackupAsync(
            int slotId,
            string backupName = null,
            CancellationToken token = default
        );

        /// <summary>
        /// 从备份恢复存档
        /// </summary>
        /// <param name="backupSlotId">备份槽位 ID</param>
        /// <param name="targetSlotId">目标槽位 ID（-1 表示原槽位）</param>
        /// <param name="token">取消令牌</param>
        /// <returns>恢复后的槽位信息</returns>
        UniTask<IAsakiSaveSlot> RestoreFromBackupAsync(
            int backupSlotId,
            int targetSlotId = -1,
            CancellationToken token = default
        );

        /// <summary>
        /// 执行自动保存
        /// </summary>
        /// <param name="data">存档数据</param>
        /// <param name="token">取消令牌</param>
        /// <returns>槽位信息</returns>
        UniTask<IAsakiSaveSlot> AutoSaveAsync<TData>(TData data, CancellationToken token = default)
            where TData : IAsakiSavable;

        /// <summary>
        /// 执行快速保存
        /// </summary>
        /// <param name="data">存档数据</param>
        /// <param name="token">取消令牌</param>
        /// <returns>槽位信息</returns>
        UniTask<IAsakiSaveSlot> QuickSaveAsync<TData>(TData data, CancellationToken token = default)
            where TData : IAsakiSavable;

        /// <summary>
        /// 加载自动保存的存档
        /// </summary>
        /// <param name="token">取消令牌</param>
        /// <returns>存档数据和槽位信息，如果不存在返回 null</returns>
        UniTask<(IAsakiSaveSlot Slot, TData Data)?> LoadAutoSaveAsync<TData>(
            CancellationToken token = default
        )
            where TData : IAsakiSavable, new();

        /// <summary>
        /// 加载快速保存的存档
        /// </summary>
        /// <param name="token">取消令牌</param>
        /// <returns>存档数据和槽位信息，如果不存在返回 null</returns>
        UniTask<(IAsakiSaveSlot Slot, TData Data)?> LoadQuickSaveAsync<TData>(
            CancellationToken token = default
        )
            where TData : IAsakiSavable, new();
    }
}
