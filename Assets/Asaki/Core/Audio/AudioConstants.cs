using Asaki.Core.FrameworkSettings;
using UnityEngine;

namespace Asaki.Core.Audio
{
    /// <summary>
    /// 音频常量定义
    /// <para>集中管理音频系统使用的所有常量值，消除魔法数字。</para>
    /// <para>池相关配置从全局配置 AsakiPoolGlobalConfig 获取。</para>
    /// </summary>
    /// <author>Asaki Framework</author>
    /// <version>2.0</version>
    public static class AudioConstants
    {
        #region Volume & Pitch

        /// <summary>最小音量</summary>
        public const float MinVolume = 0f;

        /// <summary>最大音量</summary>
        public const float MaxVolume = 1f;

        /// <summary>默认音量</summary>
        public const float DefaultVolume = 1f;

        /// <summary>默认音调</summary>
        public const float DefaultPitch = 1f;

        /// <summary>最小音调</summary>
        public const float MinPitch = 0.1f;

        /// <summary>最大音调</summary>
        public const float MaxPitch = 3f;

        #endregion

        #region Fade Duration

        /// <summary>默认淡出时长(秒)</summary>
        public const float DefaultFadeDuration = 0.2f;

        /// <summary>停止所有音频的默认淡出时长(秒)</summary>
        public const float DefaultStopAllFadeDuration = 0.5f;

        /// <summary>立即停止(无淡出)</summary>
        public const float ImmediateStop = 0f;

        #endregion

        #region Priority

        /// <summary>最高优先级</summary>
        public const int HighestPriority = 0;

        /// <summary>默认优先级</summary>
        public const int DefaultPriority = 128;

        /// <summary>最低优先级</summary>
        public const int LowestPriority = 256;

        #endregion

        #region Spatial Blend

        /// <summary>完全2D音效</summary>
        public const float Full2D = 0f;

        /// <summary>完全3D音效</summary>
        public const float Full3D = 1f;

        /// <summary>默认空间混合值(2D)</summary>
        public const float DefaultSpatialBlend = Full2D;

        #endregion

        #region Random Pitch

        /// <summary>默认随机音高变化范围</summary>
        public const float DefaultRandomPitchRange = 0.1f;

        #endregion

        #region Pool (从全局配置获取)

        /// <summary>默认初始池大小（从全局配置获取）</summary>
        public static int DefaultInitialPoolSize =>
            AsakiPoolGlobalConfig.Instance.AudioPoolDefaultInitialSize;

        /// <summary>默认最大池大小（从全局配置获取）</summary>
        public static int DefaultMaxPoolSize =>
            AsakiPoolGlobalConfig.Instance.AudioPoolDefaultMaxSize;

        /// <summary>默认活跃音频字典初始容量（从全局配置获取）</summary>
        public static int DefaultActiveAgentCapacity =>
            AsakiPoolGlobalConfig.Instance.AudioPoolDefaultActiveAgentCapacity;

        #endregion

        #region Audio Group

        /// <summary>SFX音频组ID</summary>
        public const int GroupSFX = 0;

        /// <summary>BGM音频组ID</summary>
        public const int GroupBGM = 1;

        /// <summary>UI音频组ID</summary>
        public const int GroupUI = 2;

        /// <summary>语音音频组ID</summary>
        public const int GroupVoice = 3;

        #endregion
    }
}
