namespace Asaki.Core.Pooling.Interfaces
{
    public interface IAsakiPoolStatistics
    {
        int TotalCreated { get; }
        int ActiveCount { get; }
        int InactiveCount { get; }
        int MaxSize { get; }
        int TotalDestroyed { get; }
        long GetCallCount { get; }
        long ReturnCallCount { get; }
        void Reset();
    }
}
