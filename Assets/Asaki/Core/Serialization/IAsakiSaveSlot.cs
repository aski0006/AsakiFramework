using System;

namespace Asaki.Core.Serialization
{
    /// <summary>
    /// 保存槽位状态枚举
    /// </summary>
    public enum AsakiSaveSlotStatus
    {
        /// <summary>
        /// 空槽位（未使用）
        /// </summary>
        Empty,

        /// <summary>
        /// 槽位已被占用（有有效存档）
        /// </summary>
        Occupied,

        /// <summary>
        /// 槽位数据已损坏
        /// </summary>
        Corrupted,

        /// <summary>
        /// 槽位被锁定（无法覆盖）
        /// </summary>
        Locked,
    }

    /// <summary>
    /// 保存槽位信息接口，包含存档的完整元数据
    /// </summary>
    /// <remarks>
    /// 此接口继承自 IAsakiSlotMeta，提供了更丰富的存档信息，
    /// 包括游戏时长、进度百分比、封面截图等用于 UI 展示的数据。
    /// </remarks>
    public interface IAsakiSaveSlot : IAsakiSlotMeta
    {
        /// <summary>
        /// 获取槽位状态
        /// </summary>
        AsakiSaveSlotStatus Status { get; }

        /// <summary>
        /// 获取游戏总游玩时长（秒）
        /// </summary>
        long PlayTimeSeconds { get; set; }

        /// <summary>
        /// 获取游戏进度百分比（0-100）
        /// </summary>
        float ProgressPercent { get; set; }

        /// <summary>
        /// 获取当前关卡/章节名称
        /// </summary>
        string CurrentLevel { get; set; }

        /// <summary>
        /// 获取玩家等级/主等级
        /// </summary>
        int PlayerLevel { get; set; }

        /// <summary>
        /// 获取玩家显示名称
        /// </summary>
        string PlayerName { get; set; }

        /// <summary>
        /// 获取存档封面截图数据（JPEG/PNG 字节数组，可为 null）
        /// </summary>
        byte[] ThumbnailData { get; set; }

        /// <summary>
        /// 获取存档版本（游戏版本号，用于兼容性检查）
        /// </summary>
        string GameVersion { get; set; }

        /// <summary>
        /// 获取云存档同步 ID（用于云存储关联，可为 null）
        /// </summary>
        string CloudSyncId { get; set; }

        /// <summary>
        /// 获取最后修改时间（Unix 时间戳，秒）
        /// </summary>
        long LastModifyTime { get; set; }

        /// <summary>
        /// 获取存档文件大小（字节）
        /// </summary>
        long FileSize { get; }

        /// <summary>
        /// 获取自定义标签（用于分类或标记）
        /// </summary>
        string[] Tags { get; set; }

        /// <summary>
        /// 获取存档描述/备注
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// 检查槽位是否为空
        /// </summary>
        bool IsEmpty => Status == AsakiSaveSlotStatus.Empty;

        /// <summary>
        /// 检查槽位是否有效（存在且未损坏）
        /// </summary>
        bool IsValid => Status == AsakiSaveSlotStatus.Occupied;

        /// <summary>
        /// 获取格式化后的游玩时长字符串（如 "12:34" 或 "12小时34分"）
        /// </summary>
        string GetFormattedPlayTime();

        /// <summary>
        /// 获取格式化后的保存时间字符串
        /// </summary>
        string GetFormattedSaveTime();
    }

    /// <summary>
    /// 保存槽位信息默认实现
    /// </summary>
    [Serializable]
    public class AsakiSaveSlot : IAsakiSaveSlot
    {
        /// <inheritdoc />
        public int SlotId { get; set; }

        /// <inheritdoc />
        public long LastSaveTime { get; set; }

        /// <inheritdoc />
        public string SaveName { get; set; }

        /// <inheritdoc />
        public AsakiSaveSlotStatus Status { get; set; } = AsakiSaveSlotStatus.Empty;

        /// <inheritdoc />
        public long PlayTimeSeconds { get; set; }

        /// <inheritdoc />
        public float ProgressPercent { get; set; }

        /// <inheritdoc />
        public string CurrentLevel { get; set; }

        /// <inheritdoc />
        public int PlayerLevel { get; set; }

        /// <inheritdoc />
        public string PlayerName { get; set; }

        /// <inheritdoc />
        public byte[] ThumbnailData { get; set; }

        /// <inheritdoc />
        public string GameVersion { get; set; }

        /// <inheritdoc />
        public string CloudSyncId { get; set; }

        /// <inheritdoc />
        public long LastModifyTime { get; set; }

        /// <inheritdoc />
        public long FileSize { get; set; }

        /// <inheritdoc />
        public string[] Tags { get; set; } = Array.Empty<string>();

        /// <inheritdoc />
        public string Description { get; set; }

        /// <inheritdoc />
        public string GetFormattedPlayTime()
        {
            var timeSpan = TimeSpan.FromSeconds(PlayTimeSeconds);
            if (timeSpan.TotalHours >= 1)
            {
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
            }
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        }

        /// <inheritdoc />
        public string GetFormattedSaveTime()
        {
            if (LastSaveTime <= 0)
                return "--";

            var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(LastSaveTime).LocalDateTime;
            var now = DateTime.Now;
            var diff = now - dateTime;

            if (diff.TotalDays < 1)
            {
                if (diff.TotalHours < 1)
                    return $"{diff.Minutes} 分钟前";
                return $"{diff.Hours} 小时前";
            }
            if (diff.TotalDays < 7)
            {
                return $"{diff.Days} 天前";
            }
            if (dateTime.Year == now.Year)
            {
                return dateTime.ToString("MM-dd HH:mm");
            }
            return dateTime.ToString("yyyy-MM-dd HH:mm");
        }

        /// <inheritdoc />
        public bool IsEmpty => Status == AsakiSaveSlotStatus.Empty;

        /// <inheritdoc />
        public bool IsValid => Status == AsakiSaveSlotStatus.Occupied;

        /// <summary>
        /// 序列化到写入器
        /// </summary>
        public void Serialize(IAsakiWriter writer)
        {
            writer.BeginObject(nameof(AsakiSaveSlot));
            writer.WriteInt(nameof(SlotId), SlotId);
            writer.WriteLong(nameof(LastSaveTime), LastSaveTime);
            writer.WriteString(nameof(SaveName), SaveName ?? string.Empty);
            writer.WriteInt(nameof(Status), (int)Status);
            writer.WriteLong(nameof(PlayTimeSeconds), PlayTimeSeconds);
            writer.WriteFloat(nameof(ProgressPercent), ProgressPercent);
            writer.WriteString(nameof(CurrentLevel), CurrentLevel ?? string.Empty);
            writer.WriteInt(nameof(PlayerLevel), PlayerLevel);
            writer.WriteString(nameof(PlayerName), PlayerName ?? string.Empty);
            writer.WriteString(nameof(GameVersion), GameVersion ?? string.Empty);
            writer.WriteString(nameof(CloudSyncId), CloudSyncId ?? string.Empty);
            writer.WriteLong(nameof(LastModifyTime), LastModifyTime);
            writer.WriteLong(nameof(FileSize), FileSize);
            writer.WriteString(nameof(Description), Description ?? string.Empty);

            // 缩略图数据（可能较大，单独处理）
            if (ThumbnailData != null && ThumbnailData.Length > 0)
            {
                writer.BeginList(nameof(ThumbnailData), ThumbnailData.Length);
                foreach (var b in ThumbnailData)
                {
                    writer.WriteByte("byte", b);
                }
                writer.EndList();
            }
            else
            {
                writer.BeginList(nameof(ThumbnailData), 0);
                writer.EndList();
            }

            // Tags
            if (Tags != null && Tags.Length > 0)
            {
                writer.BeginList(nameof(Tags), Tags.Length);
                foreach (var tag in Tags)
                {
                    writer.WriteString("tag", tag ?? string.Empty);
                }
                writer.EndList();
            }
            else
            {
                writer.BeginList(nameof(Tags), 0);
                writer.EndList();
            }

            writer.EndObject();
        }

        /// <summary>
        /// 从读取器反序列化
        /// </summary>
        public void Deserialize(IAsakiReader reader)
        {
            SlotId = reader.ReadInt(nameof(SlotId));
            LastSaveTime = reader.ReadLong(nameof(LastSaveTime));
            SaveName = reader.ReadString(nameof(SaveName));
            Status = (AsakiSaveSlotStatus)reader.ReadInt(nameof(Status));
            PlayTimeSeconds = reader.ReadLong(nameof(PlayTimeSeconds));
            ProgressPercent = reader.ReadFloat(nameof(ProgressPercent));
            CurrentLevel = reader.ReadString(nameof(CurrentLevel));
            PlayerLevel = reader.ReadInt(nameof(PlayerLevel));
            PlayerName = reader.ReadString(nameof(PlayerName));
            GameVersion = reader.ReadString(nameof(GameVersion));
            CloudSyncId = reader.ReadString(nameof(CloudSyncId));
            LastModifyTime = reader.ReadLong(nameof(LastModifyTime));
            FileSize = reader.ReadLong(nameof(FileSize));
            Description = reader.ReadString(nameof(Description));

            // ThumbnailData
            int thumbCount = reader.BeginList(nameof(ThumbnailData));
            if (thumbCount > 0)
            {
                ThumbnailData = new byte[thumbCount];
                for (int i = 0; i < thumbCount; i++)
                {
                    ThumbnailData[i] = reader.ReadByte("byte");
                }
            }
            reader.EndList();

            // Tags
            int tagCount = reader.BeginList(nameof(Tags));
            if (tagCount > 0)
            {
                Tags = new string[tagCount];
                for (int i = 0; i < tagCount; i++)
                {
                    Tags[i] = reader.ReadString("tag");
                }
            }
            reader.EndList();
        }
    }
}
