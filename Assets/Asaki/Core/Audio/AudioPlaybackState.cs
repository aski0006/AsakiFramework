namespace Asaki.Core.Audio
{
    /// <summary>
    /// 音频播放状态枚举
    /// 定义音频播放器的所有可能状态
    /// </summary>
    public enum AudioPlaybackState
    {
        /// <summary>空闲状态 - 对象在池中或未初始化</summary>
        Idle = 0,

        /// <summary>正在加载音频资源</summary>
        Loading = 1,

        /// <summary>资源加载完成，准备播放</summary>
        Ready = 2,

        /// <summary>正在播放音频</summary>
        Playing = 3,

        /// <summary>音频已暂停</summary>
        Paused = 4,

        /// <summary>正在淡出停止</summary>
        FadingOut = 5,

        /// <summary>已停止，等待清理</summary>
        Stopped = 6,

        /// <summary>发生错误</summary>
        Error = 7,
    }
}
