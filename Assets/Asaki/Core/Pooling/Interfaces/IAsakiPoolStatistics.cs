namespace Asaki.Core.Pooling.Interfaces
{
    /// <summary>
    /// 对象池统计信息接口
    /// </summary>
    public interface IAsakiPoolStatistics
    {
        /// <summary>总创建数量</summary>
        int TotalCreated { get; }

        /// <summary>当前活动对象数量</summary>
        int ActiveCount { get; }

        /// <summary>当前非活动对象数量</summary>
        int InactiveCount { get; }

        /// <summary>最大大小限制</summary>
        int MaxSize { get; }

        /// <summary>总销毁数量</summary>
        int TotalDestroyed { get; }

        /// <summary>获取调用次数</summary>
        long GetCallCount { get; }

        /// <summary>归还调用次数</summary>
        long ReturnCallCount { get; }

        /// <summary>重置统计数据</summary>
        void Reset();
    }
}
