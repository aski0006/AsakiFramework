using Asaki.Core.Broker;

namespace Asaki.Core.Audio
{
    /// <summary>
    /// 音频播放开始事件
    /// </summary>
    public struct AsakiPlayAudioEvent : IAsakiEvent
    {
        public AsakiAudioParams Params;
        public AsakiAudioHandle OutputHandle; // 用于回传句柄 (如果同步调用)
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
}
