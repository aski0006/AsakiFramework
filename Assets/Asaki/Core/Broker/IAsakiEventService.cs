using System;
using Asaki.Core.Context;

namespace Asaki.Core.Broker
{
    /// <summary>
    /// 定义事件服务的接口，继承自 <see cref="IAsakiService"/> 和 <see cref="IDisposable"/>。
    /// 该接口提供了事件总线系统中事件订阅、取消订阅和发布的方法，
    /// 用于管理和处理应用程序中的各种事件。
    /// </summary>
    public interface IAsakiEventService : IAsakiService, IDisposable
    {
        /// <summary>
        /// 订阅一个事件处理程序到事件服务。
        /// 泛型类型参数 <typeparamref name="T"/> 表示事件类型，必须实现 <see cref="IAsakiEvent"/> 接口。
        /// <paramref name="handler"/> 是要订阅的事件处理程序，必须实现 <see cref="IAsakiHandler{T}"/> 接口。
        /// 此方法将处理程序注册到事件服务中，以便在相应事件发布时能够被调用。
        /// </summary>
        /// <typeparam name="T">要订阅的事件类型，必须实现 <see cref="IAsakiEvent"/> 接口。</typeparam>
        /// <param name="handler">要订阅的事件处理程序，必须实现 <see cref="IAsakiHandler{T}"/> 接口。</param>
        void Subscribe<T>(IAsakiHandler<T> handler)
            where T : IAsakiEvent;

        /// <summary>
        /// 使用弱引用订阅事件。
        /// 当处理程序被 GC 回收后，订阅将自动失效。
        /// </summary>
        /// <typeparam name="T">事件类型，必须实现 <see cref="IAsakiEvent"/> 接口。</typeparam>
        /// <param name="handler">要订阅的事件处理程序，必须实现 <see cref="IAsakiHandler{T}"/> 接口。</param>
        /// <remarks>
        /// <para>弱引用订阅适用于以下场景：</para>
        /// <para>1. 处理程序的生命周期不确定，无法保证及时取消订阅。</para>
        /// <para>2. 需要避免因忘记取消订阅导致的内存泄漏。</para>
        /// <para>注意：弱引用订阅的性能略低于强引用订阅，建议仅在必要时使用。</para>
        /// </remarks>
        void SubscribeWeak<T>(IAsakiHandler<T> handler)
            where T : IAsakiEvent;

        /// <summary>
        /// 从事件服务中取消订阅一个事件处理程序。
        /// 泛型类型参数 <typeparamref name="T"/> 表示事件类型，必须实现 <see cref="IAsakiEvent"/> 接口。
        /// <paramref name="handler"/> 是要取消订阅的事件处理程序，必须实现 <see cref="IAsakiHandler{T}"/> 接口。
        /// 此方法将处理程序从事件服务的订阅列表中移除，使其不再接收相应事件。
        /// </summary>
        /// <typeparam name="T">要取消订阅的事件类型，必须实现 <see cref="IAsakiEvent"/> 接口。</typeparam>
        /// <param name="handler">要取消订阅的事件处理程序，必须实现 <see cref="IAsakiHandler{T}"/> 接口。</param>
        void Unsubscribe<T>(IAsakiHandler<T> handler)
            where T : IAsakiEvent;

        /// <summary>
        /// 在事件服务中发布一个事件。
        /// 泛型类型参数 <typeparamref name="T"/> 表示事件类型，必须实现 <see cref="IAsakiEvent"/> 接口。
        /// <paramref name="e"/> 是要发布的事件实例。此方法将触发所有已订阅该事件的处理程序的 <see cref="IAsakiHandler{T}.OnEvent(in T)"/> 方法。
        /// </summary>
        /// <typeparam name="T">要发布的事件类型，必须实现 <see cref="IAsakiEvent"/> 接口。</typeparam>
        /// <param name="e">要发布的事件实例（只读引用传递）。</param>
        void Publish<T>(in T e)
            where T : IAsakiEvent;
    }
}
