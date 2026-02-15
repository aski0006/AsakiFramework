namespace Asaki.Core.Broker
{
    /// <summary>
    /// 可重置事件接口，用于对象池中的事件清理
    /// </summary>
    public interface IResettableEvent : IAsakiEvent
    {
        /// <summary>
        /// 重置事件状态，以便复用
        /// </summary>
        void Reset();
    }
}
