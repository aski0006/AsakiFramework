using Asaki.Core.Context;

namespace Asaki.Core.Broker
{
    /// <summary>
    /// [Facade] 静态入口外观。
    /// 该类作为静态入口点，负责连接静态调用（如 Roslyn 编译时生成的代码）与动态实例（如 <see cref="AsakiEventService"/> 实现的事件总线）。
    /// 提供了发布、订阅和取消订阅事件的便捷静态方法。
    /// </summary>
    public static class AsakiBroker
    {
        /// <summary>
        /// 懒加载获取事件总线实例。
        /// 首先尝试从 <see cref="AsakiContext"/> 中获取已有的 <see cref="IAsakiEventService"/> 实例。
        /// 如果未找到，则主动创建一个并注册到 <see cref="AsakiContext"/> 中。
        /// 使用双重检查锁定机制防止并发创建。
        /// </summary>
        /// <returns>获取到的 <see cref="IAsakiEventService"/> 实例。</returns>
        private static IAsakiEventService GetOrRegisterBus()
        {
            return AsakiContext.TryGet(out IAsakiEventService bus)
                ? bus
                : AsakiContext.GetOrRegister<IAsakiEventService>(() => new AsakiEventService());
        }

        /// <summary>
        /// 发布一个事件。
        /// 通过懒加载获取事件总线实例，并调用其 <see cref="IAsakiEventService.Publish{T}(T)"/> 方法发布事件。
        /// </summary>
        /// <typeparam name="T">事件类型，必须实现 <see cref="IAsakiEvent"/> 接口。</typeparam>
        /// <param name="e">要发布的事件实例。</param>
        public static void Publish<T>(T e)
            where T : struct, IAsakiEvent
        {
            GetOrRegisterBus().Publish(e);
        }

        /// <summary>
        /// 订阅一个事件处理程序。
        /// 通过懒加载获取事件总线实例，并调用其 <see cref="IAsakiEventService.Subscribe{T}(IAsakiHandler{T})"/> 方法订阅事件处理程序。
        /// </summary>
        /// <typeparam name="T">事件类型，必须实现 <see cref="IAsakiEvent"/> 接口。</typeparam>
        /// <param name="handler">要订阅的事件处理程序，必须实现 <see cref="IAsakiHandler{T}"/> 接口。</param>
        public static void Subscribe<T>(IAsakiHandler<T> handler)
            where T : IAsakiEvent
        {
            GetOrRegisterBus().Subscribe(handler);
        }

        /// <summary>
        /// 取消订阅一个事件处理程序。
        /// 首先尝试从 <see cref="AsakiContext"/> 中获取事件总线实例，如果获取成功，则调用其
        /// <see cref="IAsakiEventService.Unsubscribe{T}(IAsakiHandler{T})"/> 方法取消订阅事件处理程序。
        /// 如果未获取到总线实例，则直接忽略，因为未订阅过。
        /// </summary>
        /// <typeparam name="T">事件类型，必须实现 <see cref="IAsakiEvent"/> 接口。</typeparam>
        /// <param name="handler">要取消订阅的事件处理程序，必须实现 <see cref="IAsakiHandler{T}"/> 接口。</param>
        public static void Unsubscribe<T>(IAsakiHandler<T> handler)
            where T : IAsakiEvent
        {
            // Unsubscribe 时如果 Bus 不存在，那肯定没订阅过，直接忽略即可
            if (AsakiContext.TryGet<IAsakiEventService>(out IAsakiEventService bus))
            {
                bus.Unsubscribe(handler);
            }
        }
    }

    /// <summary>
    /// 为 <see cref="IAsakiHandler{T}"/> 接口提供扩展方法。
    /// 这些扩展方法简化了事件处理程序的订阅和取消订阅操作。
    /// </summary>
    public static class AsakiBrokerExtensions
    {
        /// <summary>
        /// 注册事件处理程序。
        /// 此扩展方法调用 <see cref="AsakiBroker.Subscribe{T}(IAsakiHandler{T})"/> 方法，
        /// 方便地将当前事件处理程序订阅到事件总线上。
        /// </summary>
        /// <typeparam name="T">事件类型，必须实现 <see cref="IAsakiEvent"/> 接口。</typeparam>
        /// <param name="handler">要注册的事件处理程序，必须实现 <see cref="IAsakiHandler{T}"/> 接口。</param>
        public static void AsakiRegister<T>(this IAsakiHandler<T> handler)
            where T : IAsakiEvent
        {
            AsakiBroker.Subscribe(handler);
        }

        /// <summary>
        /// 取消注册事件处理程序。
        /// 此扩展方法调用 <see cref="AsakiBroker.Unsubscribe{T}(IAsakiHandler{T})"/> 方法，
        /// 方便地将当前事件处理程序从事件总线上取消订阅。
        /// </summary>
        /// <typeparam name="T">事件类型，必须实现 <see cref="IAsakiEvent"/> 接口。</typeparam>
        /// <param name="handler">要取消注册的事件处理程序，必须实现 <see cref="IAsakiHandler{T}"/> 接口。</param>
        public static void AsakiUnregister<T>(this IAsakiHandler<T> handler)
            where T : IAsakiEvent
        {
            AsakiBroker.Unsubscribe(handler);
        }
    }
}
