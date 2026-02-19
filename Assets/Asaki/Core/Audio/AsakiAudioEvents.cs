using Asaki.Core.Broker;

namespace Asaki.Core.Audio
{
    /// <summary>
    /// 音频播放开始事件
    /// </summary>
    public struct AsakiPlayAudioEvent : IAsakiEvent
    {
        public AsakiAudioParams Params;
        public AsakiAudioHandle OutputHandle;
    }

    /// <summary>
    /// 音频停止事件
    /// </summary>
    public struct AsakiStopAudioEvent : IAsakiEvent
    {
        public AsakiAudioHandle Handle;
        public float FadeOutDuration;
    }

    /// <summary>
    /// 音频播放完成事件
    /// </summary>
    public struct AsakiAudioFinishedEvent : IAsakiEvent
    {
        public AsakiAudioHandle Handle;
        public int AssetId;
    }

    /// <summary>
    /// 音频分组音量变化事件
    /// <para>当分组音量发生变化时发布，UI层可订阅此事件实时更新音量状态。</para>
    /// </summary>
    public struct AudioGroupVolumeChangedEvent : IAsakiEvent
    {
        /// <summary>分组ID</summary>
        public int GroupId;

        /// <summary>新的分组音量(0-1)</summary>
        public float Volume;

        /// <summary>是否为渐变过程中的更新</summary>
        public bool IsTransitioning;
    }

    /// <summary>
    /// 全局音量变化事件
    /// <para>当全局音量发生变化时发布。</para>
    /// </summary>
    public struct GlobalVolumeChangedEvent : IAsakiEvent
    {
        /// <summary>新的全局音量(0-1)</summary>
        public float Volume;
    }

    /// <summary>
    /// 音频分组静音状态变化事件
    /// </summary>
    public struct AudioGroupMuteChangedEvent : IAsakiEvent
    {
        /// <summary>分组ID</summary>
        public int GroupId;

        /// <summary>是否静音</summary>
        public bool IsMuted;
    }
}
