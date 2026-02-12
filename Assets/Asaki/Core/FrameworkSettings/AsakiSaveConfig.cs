using System;
using UnityEngine;

namespace Asaki.Core.FrameworkSettings
{
    /// <summary>
    /// 存档系统配置类，用于管理存档服务的全局设置。
    /// </summary>
    [Serializable]
    public class AsakiSaveConfig
    {
        #region 路径配置

        [Header("Path Settings")]
        [Tooltip("存档根目录名称，相对于 Application.persistentDataPath")]
        public string SaveDirectoryName = "Saves";

        [Tooltip("自定义存档根路径（留空则使用默认路径）")]
        public string CustomSavePath = "";

        #endregion

        #region 槽位配置

        [Header("Slot Settings")]
        [Tooltip("最大存档槽位数量 (1-999)")]
        [Range(1, 999)]
        public int MaxSlots = 99;

        [Tooltip("自动存档槽位索引")]
        [Range(0, 998)]
        public int AutoSaveSlotIndex = 0;

        [Tooltip("快速存档槽位索引")]
        [Range(0, 998)]
        public int QuickSaveSlotIndex = 1;

        #endregion

        #region 备份配置

        [Header("Backup Settings")]
        [Tooltip("是否启用存档备份")]
        public bool EnableBackup = true;

        [Tooltip("备份目录名称")]
        public string BackupDirectoryName = "Backups";

        [Tooltip("每个槽位保留的最大备份数量")]
        [Range(1, 10)]
        public int MaxBackupCount = 3;

        #endregion

        #region 调试配置

        [Header("Debug Settings")]
        [Tooltip("是否启用调试模式（生成可读元数据文件）")]
        public bool EnableDebugMode = true;

        [Tooltip("是否在日志中显示详细存档操作信息")]
        public bool VerboseLogging = false;

        #endregion

        #region 性能配置

        [Header("Performance Settings")]
        [Tooltip("存档操作超时时间（秒，0表示无超时）")]
        [Range(0f, 60f)]
        public float OperationTimeout = 30f;

        [Tooltip("是否启用存档压缩")]
        public bool EnableCompression = false;

        [Tooltip("压缩级别 (1-9，9为最高压缩)")]
        [Range(1, 9)]
        public int CompressionLevel = 6;

        #endregion

        #region 运行时属性

        /// <summary>
        /// 获取实际的存档根路径
        /// </summary>
        public string GetSaveRootPath()
        {
            if (!string.IsNullOrEmpty(CustomSavePath))
            {
                return CustomSavePath;
            }
            return System.IO.Path.Combine(Application.persistentDataPath, SaveDirectoryName);
        }

        /// <summary>
        /// 获取备份目录路径
        /// </summary>
        public string GetBackupPath()
        {
            return System.IO.Path.Combine(GetSaveRootPath(), BackupDirectoryName);
        }

        /// <summary>
        /// 验证槽位索引是否有效
        /// </summary>
        public bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < MaxSlots;
        }

        /// <summary>
        /// 确保自动存档和快速存档索引在有效范围内
        /// </summary>
        public void ValidateSlotIndices()
        {
            AutoSaveSlotIndex = Mathf.Clamp(AutoSaveSlotIndex, 0, MaxSlots - 1);
            QuickSaveSlotIndex = Mathf.Clamp(QuickSaveSlotIndex, 0, MaxSlots - 1);
        }

        #endregion
    }
}
