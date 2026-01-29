using Asaki.Core.Pooling.Interfaces;
using System.Threading;

namespace Asaki.Core.Pooling
{
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

        public void IncrementCreated()
        {
            Interlocked.Increment(ref _totalCreated);
            Interlocked.Increment(ref _inactiveCount);
        }

        public void IncrementDestroyed()
        {
            Interlocked.Increment(ref _totalDestroyed);
            Interlocked.Decrement(ref _inactiveCount);
        }

        public void IncrementGet()
        {
            Interlocked.Increment(ref _getCallCount);
            Interlocked.Decrement(ref _inactiveCount);
            Interlocked.Increment(ref _activeCount);
        }

        public void IncrementReturn()
        {
            Interlocked.Increment(ref _returnCallCount);
            Interlocked.Decrement(ref _activeCount);
            Interlocked.Increment(ref _inactiveCount);
        }

        public void AdjustInactive(int delta)
        {
            Interlocked.Add(ref _inactiveCount, delta);
        }

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
            return $"[AsakiPool] : \n [Total: {TotalCreated}, Active: {ActiveCount}, Inactive: {InactiveCount}, "
                + $"Destroyed: {TotalDestroyed}, Gets: {GetCallCount}, Returns: {ReturnCallCount}]";
        }
    }
}
