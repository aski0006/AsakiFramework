using Asaki.Core.Broker;

namespace Asaki.Core.Context
{
    /// <summary>
    /// 框架就绪事件，当框架完全初始化完成时发布
    /// </summary>
    public struct OnAsakiFrameworkReadyEvent : IAsakiEvent { }

    /// <summary>
    /// 服务容器清理前事件。
    /// 在 AsakiContext.ClearAll() 销毁服务前发布，允许使用者提前释放引用。
    /// </summary>
    public readonly struct OnAsakiContextClearingEvent : IAsakiEvent
    {
        /// <summary>
        /// 获取即将清理的服务数量
        /// </summary>
        public int ServiceCount { get; }

        /// <summary>
        /// 初始化清理前事件
        /// </summary>
        /// <param name="serviceCount">即将清理的服务数量</param>
        public OnAsakiContextClearingEvent(int serviceCount)
        {
            ServiceCount = serviceCount;
        }
    }
}
