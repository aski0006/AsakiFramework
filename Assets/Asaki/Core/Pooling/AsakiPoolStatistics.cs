using System.Threading;
using Asaki.Core.Pooling.Interfaces;

namespace Asaki.Core.Pooling
{
    /// <summary>
    /// 对象池统计信息实现
    /// 注意：此类使用 Interlocked 确保线程安全，但 Reset 方法不是线程安全的
    /// </summary>
    public class AsakiPoolStatistics : IAsakiPoolStatistics
    {
        private int _totalCreated;
        private int _activeCount;
        private int _inactiveCount;
        private int _totalDestroyed;
        private int _getCallCount;
        private int _returnCallCount;

        public int TotalCreated => _totalCreated;
        public int ActiveCount => _activeCount;
        public int InactiveCount => _inactiveCount;
        public int MaxSize { get; set; }
        public int TotalDestroyed => _totalDestroyed;
        public long GetCallCount => _getCallCount;
        public long ReturnCallCount => _returnCallCount;

        /// <summary>
        /// 增加创建计数
        /// 线程安全
        /// </summary>
        public void IncrementCreated()
        {
            Interlocked.Increment(ref _totalCreated);
            Interlocked.Increment(ref _inactiveCount);
        }

        /// <summary>
        /// 增加销毁计数
        /// 线程安全，确保非活动计数不会低于0
        /// </summary>
        public void IncrementDestroyed()
        {
            Interlocked.Increment(ref _totalDestroyed);
            // 使用循环确保不会低于0
            int current;
            do
            {
                current = _inactiveCount;
                if (current <= 0)
                    break;
            } while (
                Interlocked.CompareExchange(ref _inactiveCount, current - 1, current) != current
            );
        }

        /// <summary>
        /// 增加获取计数
        /// 线程安全
        /// </summary>
        public void IncrementGet()
        {
            Interlocked.Increment(ref _getCallCount);
            Interlocked.Increment(ref _activeCount);
            // 使用循环确保非活动计数不会低于0
            int current;
            do
            {
                current = _inactiveCount;
                if (current <= 0)
                    break;
            } while (
                Interlocked.CompareExchange(ref _inactiveCount, current - 1, current) != current
            );
        }

        /// <summary>
        /// 增加归还计数
        /// 线程安全
        /// </summary>
        public void IncrementReturn()
        {
            Interlocked.Increment(ref _returnCallCount);
            Interlocked.Increment(ref _inactiveCount);
            // 使用循环确保活动计数不会低于0
            int current;
            do
            {
                current = _activeCount;
                if (current <= 0)
                    break;
            } while (
                Interlocked.CompareExchange(ref _activeCount, current - 1, current) != current
            );
        }

        /// <summary>
        /// 调整非活动计数
        /// 线程安全
        /// </summary>
        public void AdjustInactive(int delta)
        {
            Interlocked.Add(ref _inactiveCount, delta);
        }

        /// <summary>
        /// 重置所有统计数据
        /// 注意：此方法不是线程安全的，应在单线程环境中调用
        /// </summary>
        public void Reset()
        {
            _totalCreated = 0;
            _activeCount = 0;
            _inactiveCount = 0;
            _totalDestroyed = 0;
            _getCallCount = 0;
            _returnCallCount = 0;
        }

        public override string ToString()
        {
            return $"Total: {TotalCreated}, Active: {ActiveCount}, Inactive: {InactiveCount}, "
                + $"Destroyed: {TotalDestroyed}, Gets: {GetCallCount}, Returns: {ReturnCallCount}";
        }
    }
}
