using System.Threading;

namespace Asaki.Core.Audio
{
    /// <summary>
    /// 音频分组控制接口
    /// <para>提供按音频组(SFX/BGM/UI/Voice)控制音量、静音、暂停等功能。</para>
    /// <para>适用于音频设置面板、场景切换等场景。</para>
    /// </summary>
    /// <author>Asaki Framework</author>
    /// <version>2.0</version>
    public interface IAsakiAudioGroupControl
    {
        /// <summary>
        /// 设置音频组音量(立即生效)
        /// </summary>
        /// <param name="groupId">音频组ID</param>
        /// <param name="volume">音量值(0-1)</param>
        void SetGroupVolume(int groupId, float volume);

        /// <summary>
        /// 设置音频组音量(带渐变效果)
        /// </summary>
        /// <param name="groupId">音频组ID</param>
        /// <param name="targetVolume">目标音量(0-1)</param>
        /// <param name="duration">渐变时长(秒)</param>
        /// <param name="cancellationToken">取消令牌</param>
        void SetGroupVolumeWithFade(int groupId, float targetVolume, float duration, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取音频组音量
        /// </summary>
        /// <param name="groupId">音频组ID</param>
        /// <returns>音量值(0-1)</returns>
        float GetGroupVolume(int groupId);

        /// <summary>
        /// 获取音频组实际音量(分组音量×全局音量)
        /// </summary>
        /// <param name="groupId">音频组ID</param>
        /// <returns>实际音量值(0-1)</returns>
        float GetGroupEffectiveVolume(int groupId);

        /// <summary>
        /// 设置音频组静音状态
        /// </summary>
        /// <param name="groupId">音频组ID</param>
        /// <param name="isMuted">是否静音</param>
        void SetGroupMuted(int groupId, bool isMuted);

        /// <summary>
        /// 获取音频组静音状态
        /// </summary>
        /// <param name="groupId">音频组ID</param>
        /// <returns>是否静音</returns>
        bool IsGroupMuted(int groupId);

        /// <summary>
        /// 停止音频组内所有音频
        /// </summary>
        /// <param name="groupId">音频组ID</param>
        /// <param name="fadeDuration">淡出时长(秒)</param>
        void StopGroup(int groupId, float fadeDuration = AudioConstants.DefaultFadeDuration);

        /// <summary>
        /// 暂停音频组内所有音频
        /// </summary>
        /// <param name="groupId">音频组ID</param>
        void PauseGroup(int groupId);

        /// <summary>
        /// 恢复音频组内所有音频
        /// </summary>
        /// <param name="groupId">音频组ID</param>
        void ResumeGroup(int groupId);
    }
}
