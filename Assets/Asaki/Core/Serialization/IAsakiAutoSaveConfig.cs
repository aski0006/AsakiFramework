using System;

namespace Asaki.Core.Serialization
{
    /// <summary>
    /// 自动保存触发条件类型
    /// </summary>
    [Flags]
    public enum AsakiAutoSaveTrigger
    {
        /// <summary>
        /// 禁用自动保存
        /// </summary>
        None = 0,

        /// <summary>
        /// 按时间间隔触发
        /// </summary>
        TimeInterval = 1,

        /// <summary>
        /// 进入检查点时触发
        /// </summary>
        Checkpoint = 2,

        /// <summary>
        /// 场景切换时触发
        /// </summary>
        SceneChange = 4,

        /// <summary>
        /// 应用进入后台时触发
        /// </summary>
        ApplicationPause = 8,

        /// <summary>
        /// 玩家手动触发（通过快捷键）
        /// </summary>
        Manual = 16,

        /// <summary>
        /// 全部触发条件
        /// </summary>
        All = TimeInterval | Checkpoint | SceneChange | ApplicationPause | Manual
    }

    /// <summary>
    /// 自动保存配置接口
    /// </summary>
    /// <remarks>
    /// 配置自动保存的行为，包括触发条件、时间间隔、最大存档数等。
    /// 可通过 IAsakiAutoSaveService 注册此配置来控制自动保存行为。
    /// </remarks>
    public interface IAsakiAutoSaveConfig
    {
        /// <summary>
        /// 是否启用自动保存
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// 自动保存触发条件
        /// </summary>
        AsakiAutoSaveTrigger Triggers { get; }

        /// <summary>
        /// 自动保存时间间隔（秒），仅在 TimeInterval 触发时有效
        /// </summary>
        float TimeIntervalSeconds { get; }

        /// <summary>
        /// 自动保存前倒计时（秒），给玩家取消的机会
        /// </summary>
        float CountdownSeconds { get; }

        /// <summary>
        /// 是否显示自动保存提示
        /// </summary>
        bool ShowNotification { get; }

        /// <summary>
        /// 自动保存提示文本
        /// </summary>
        string NotificationText { get; }

        /// <summary>
        /// 自动保存存档的最大数量（循环覆盖）
        /// </summary>
        int MaxAutoSaveCount { get; }

        /// <summary>
        /// 自动保存槽位起始索引
        /// </summary>
        int AutoSaveSlotStartIndex { get; }

        /// <summary>
        /// 是否在自动保存时创建缩略图
        /// </summary>
        bool GenerateThumbnail { get; }

        /// <summary>
        /// 缩略图宽度（像素）
        /// </summary>
        int ThumbnailWidth { get; }

        /// <summary>
        /// 缩略图高度（像素）
        /// </summary>
        int ThumbnailHeight { get; }

        /// <summary>
        /// 缩略图质量（0-100，仅 JPEG 有效）
        /// </summary>
        int ThumbnailQuality { get; }

        /// <summary>
        /// 是否在触发前检查存档空间
        /// </summary>
        bool CheckStorageSpace { get; }

        /// <summary>
        /// 最小可用空间要求（MB）
        /// </summary>
        long MinFreeSpaceMB { get; }

        /// <summary>
        /// 两次自动保存之间的最小间隔（秒，防止过于频繁）
        /// </summary>
        float MinIntervalBetweenSaves { get; }

        /// <summary>
        /// 是否保留最近的自动存档（不被覆盖）
        /// </summary>
        bool KeepLatestAutoSave { get; }

        /// <summary>
        /// 是否在进入新场景时自动保存
        /// </summary>
        bool SaveOnSceneEnter { get; }

        /// <summary>
        /// 是否在退出场景时自动保存
        /// </summary>
        bool SaveOnSceneExit { get; }

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        bool Validate(out string errorMessage);
    }

    /// <summary>
    /// 自动保存配置默认实现
    /// </summary>
    [Serializable]
    public class AsakiAutoSaveConfig : IAsakiAutoSaveConfig
    {
        /// <inheritdoc />
        public bool Enabled { get; set; } = true;

        /// <inheritdoc />
        public AsakiAutoSaveTrigger Triggers { get; set; } = AsakiAutoSaveTrigger.Checkpoint | AsakiAutoSaveTrigger.ApplicationPause;

        /// <inheritdoc />
        public float TimeIntervalSeconds { get; set; } = 300f; // 5分钟

        /// <inheritdoc />
        public float CountdownSeconds { get; set; } = 3f;

        /// <inheritdoc />
        public bool ShowNotification { get; set; } = true;

        /// <inheritdoc />
        public string NotificationText { get; set; } = "正在自动保存...";

        /// <inheritdoc />
        public int MaxAutoSaveCount { get; set; } = 3;

        /// <inheritdoc />
        public int AutoSaveSlotStartIndex { get; set; } = 0;

        /// <inheritdoc />
        public bool GenerateThumbnail { get; set; } = true;

        /// <inheritdoc />
        public int ThumbnailWidth { get; set; } = 320;

        /// <inheritdoc />
        public int ThumbnailHeight { get; set; } = 180;

        /// <inheritdoc />
        public int ThumbnailQuality { get; set; } = 75;

        /// <inheritdoc />
        public bool CheckStorageSpace { get; set; } = true;

        /// <inheritdoc />
        public long MinFreeSpaceMB { get; set; } = 100;

        /// <inheritdoc />
        public float MinIntervalBetweenSaves { get; set; } = 60f; // 1分钟

        /// <inheritdoc />
        public bool KeepLatestAutoSave { get; set; } = true;

        /// <inheritdoc />
        public bool SaveOnSceneEnter { get; set; } = false;

        /// <inheritdoc />
        public bool SaveOnSceneExit { get; set; } = true;

        /// <inheritdoc />
        public bool Validate(out string errorMessage)
        {
            if (!Enabled)
            {
                errorMessage = null;
                return true;
            }

            if (Triggers == AsakiAutoSaveTrigger.None)
            {
                errorMessage = "自动保存已启用但未设置任何触发条件";
                return false;
            }

            if (Triggers.HasFlag(AsakiAutoSaveTrigger.TimeInterval) && TimeIntervalSeconds < 30f)
            {
                errorMessage = "自动保存时间间隔不能小于 30 秒";
                return false;
            }

            if (MaxAutoSaveCount < 1)
            {
                errorMessage = "最大自动存档数必须至少为 1";
                return false;
            }

            if (AutoSaveSlotStartIndex < 0)
            {
                errorMessage = "自动保存槽位起始索引不能为负数";
                return false;
            }

            if (GenerateThumbnail)
            {
                if (ThumbnailWidth < 64 || ThumbnailHeight < 64)
                {
                    errorMessage = "缩略图尺寸不能小于 64x64";
                    return false;
                }
                if (ThumbnailQuality < 1 || ThumbnailQuality > 100)
                {
                    errorMessage = "缩略图质量必须在 1-100 之间";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        public static AsakiAutoSaveConfig CreateDefault()
        {
            return new AsakiAutoSaveConfig();
        }

        /// <summary>
        /// 创建宽松配置（频繁保存）
        /// </summary>
        public static AsakiAutoSaveConfig CreateFrequent()
        {
            return new AsakiAutoSaveConfig
            {
                Enabled = true,
                Triggers = AsakiAutoSaveTrigger.All,
                TimeIntervalSeconds = 60f,
                MaxAutoSaveCount = 5,
                ShowNotification = true,
                GenerateThumbnail = true
            };
        }

        /// <summary>
        /// 创建严格配置（仅在关键点保存）
        /// </summary>
        public static AsakiAutoSaveConfig CreateConservative()
        {
            return new AsakiAutoSaveConfig
            {
                Enabled = true,
                Triggers = AsakiAutoSaveTrigger.Checkpoint | AsakiAutoSaveTrigger.ApplicationPause,
                MaxAutoSaveCount = 1,
                ShowNotification = false,
                GenerateThumbnail = false
            };
        }

        /// <summary>
        /// 创建禁用配置
        /// </summary>
        public static AsakiAutoSaveConfig CreateDisabled()
        {
            return new AsakiAutoSaveConfig
            {
                Enabled = false,
                Triggers = AsakiAutoSaveTrigger.None
            };
        }
    }
}
