using System.Threading;
using Asaki.Core.Context;

namespace Asaki.Core.Audio
{
    /// <summary>
    /// 音频播放器接口
    /// <para>提供音频播放、暂停、恢复、停止等核心播放功能。</para>
    /// <para>适用于大多数客户端的基础音频操作需求。</para>
    /// </summary>
    /// <author>Asaki Framework</author>
    /// <version>2.0</version>
    public interface IAsakiAudioPlayer : IAsakiModule
    {
        /// <summary>
        /// 播放音频
        /// </summary>
        /// <param name="assetId">音频资源ID</param>
        /// <param name="parameters">播放参数，默认使用配置参数</param>
        /// <param name="token">取消令牌</param>
        /// <returns>音频句柄，播放失败返回Invalid</returns>
        AsakiAudioHandle Play(
            int assetId,
            AsakiAudioParams parameters = default,
            CancellationToken token = default
        );

        /// <summary>
        /// 停止音频
        /// </summary>
        /// <param name="handle">音频句柄</param>
        /// <param name="fadeDuration">淡出时长(秒)</param>
        void Stop(AsakiAudioHandle handle, float fadeDuration = AudioConstants.DefaultFadeDuration);

        /// <summary>
        /// 暂停音频
        /// </summary>
        /// <param name="handle">音频句柄</param>
        void Pause(AsakiAudioHandle handle);

        /// <summary>
        /// 恢复音频
        /// </summary>
        /// <param name="handle">音频句柄</param>
        void Resume(AsakiAudioHandle handle);

        /// <summary>
        /// 判断音频是否正在播放
        /// </summary>
        /// <param name="handle">音频句柄</param>
        /// <returns>正在播放返回true</returns>
        bool IsPlaying(AsakiAudioHandle handle);

        /// <summary>
        /// 判断音频是否已暂停
        /// </summary>
        /// <param name="handle">音频句柄</param>
        /// <returns>已暂停返回true</returns>
        bool IsPaused(AsakiAudioHandle handle);
    }
}
