namespace Asaki.Core.Pooling.Interfaces
{
    /// <summary>
    /// 可池化对象接口
    /// 实现此接口的对象可以在从池中获取和归还时接收生命周期回调
    /// </summary>
    public interface IAsakiPoolable
    {
        /// <summary>
        /// 当对象从池中获取时调用
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// 当对象归还到池时调用
        /// </summary>
        void OnDespawn();
    }
}
