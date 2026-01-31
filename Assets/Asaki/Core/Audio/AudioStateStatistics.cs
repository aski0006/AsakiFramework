namespace Asaki.Core.Audio
{
    /// <summary>
    /// 音频状态统计信息
    /// 用于监控和分析音频播放器的整体状态分布
    /// </summary>
    public struct AudioStateStatistics
    {
        /// <summary>正在加载资源的音频数量</summary>
        public int LoadingCount;

        /// <summary>准备就绪等待播放的音频数量</summary>
        public int ReadyCount;

        /// <summary>正在播放的音频数量</summary>
        public int PlayingCount;

        /// <summary>已暂停的音频数量</summary>
        public int PausedCount;

        /// <summary>正在淡出的音频数量</summary>
        public int FadingOutCount;

        /// <summary>处于错误状态的音频数量</summary>
        public int ErrorCount;

        /// <summary>总活跃音频数量</summary>
        public int TotalActive => LoadingCount + ReadyCount + PlayingCount + PausedCount + FadingOutCount + ErrorCount;

        /// <summary>获取统计信息的字符串表示</summary>
        public override string ToString()
        {
            return $"Audio States: [Loading: {LoadingCount}, Ready: {ReadyCount}, Playing: {PlayingCount}, " +
                   $"Paused: {PausedCount}, FadingOut: {FadingOutCount}, Error: {ErrorCount}, Total: {TotalActive}]";
        }
    }
}
