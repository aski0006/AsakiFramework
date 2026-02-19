namespace Asaki.Core.Audio
{
    /// <summary>
    /// 音频全局控制接口
    /// <para>提供全局音量控制、全局暂停/恢复等功能。</para>
    /// <para>适用于设置面板、游戏暂停等场景。</para>
    /// </summary>
    /// <author>Asaki Framework</author>
    /// <version>2.0</version>
    public interface IAsakiAudioGlobalControl
    {
        /// <summary>
        /// 设置全局音量
        /// </summary>
        /// <param name="volume">音量值(0-1)</param>
        void SetGlobalVolume(float volume);

        /// <summary>
        /// 获取全局音量
        /// </summary>
        /// <returns>音量值(0-1)</returns>
        float GetGlobalVolume();

        /// <summary>
        /// 停止所有音频
        /// </summary>
        /// <param name="fadeDuration">淡出时长(秒)</param>
        void StopAll(float fadeDuration = AudioConstants.DefaultStopAllFadeDuration);

        /// <summary>
        /// 暂停所有音频
        /// </summary>
        void PauseAll();

        /// <summary>
        /// 恢复所有音频
        /// </summary>
        void ResumeAll();
    }
}
