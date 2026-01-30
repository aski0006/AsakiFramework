namespace Asaki.Core.Pooling.Interfaces
{
    /// <summary>
    /// 可重置对象接口
    /// 实现此接口的对象可以在归还到池时自动重置状态
    /// </summary>
    public interface IAsakiResettable
    {
        /// <summary>
        /// 重置对象状态
        /// </summary>
        void Reset();
    }
}
