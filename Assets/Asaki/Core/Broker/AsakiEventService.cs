using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Asaki.Core.Broker
{
    /// <summary>
    /// [Implementation] 基于实例的高性能事件总线。
    /// 该类解决了 IL2CPP 静态构造函数无法热更、无法重置的致命缺陷，
    /// 通过使用实例化的事件桶策略来高效管理事件的订阅、发布和取消订阅操作。
    /// </summary>
    public class AsakiEventService : IAsakiEventService
    {
        // ========================================================================
        // Internal Bucket Strategy
        // ========================================================================

        /// <summary>
        /// 抽象接口，用于统一管理事件桶的清理操作。
        /// 所有具体的事件桶类都应实现此接口，以提供清理其内部状态的方法。
        /// </summary>
        private interface IEventBucket
        {
            /// <summary>
            /// 清理事件桶的内部状态。
            /// 此方法应清空所有相关的订阅列表和缓存，为事件桶的重置或销毁做准备。
            /// </summary>
            void Cleanup();
        }

        /// <summary>
        /// 具体的泛型事件桶类，用于存储和管理特定类型事件的订阅者和发布操作。
        /// 此类不是静态类，通过内部的锁机制来保证线程安全，适用于高写入和高读取的场景。
        /// </summary>
        /// <typeparam name="T">事件类型，必须实现 <see cref="IAsakiEvent"/> 接口。</typeparam>
        private class EventBucket<T> : IEventBucket
            where T : IAsakiEvent
        {
            /// <summary>
            /// 存储事件处理程序的列表，适用于频繁写入操作。
            /// 初始容量为 8，可根据需要动态扩展。
            /// </summary>
            private readonly List<IAsakiHandler<T>> _handlers = new List<IAsakiHandler<T>>(8);

            /// <summary>
            /// 用于快速读取的缓存数组，采用 Copy - On - Write 策略。
            /// 当订阅列表发生变化时，会重新生成此缓存数组以确保读取的一致性。
            /// </summary>
            private IAsakiHandler<T>[] _cache = Array.Empty<IAsakiHandler<T>>();

            /// <summary>
            /// 脏标记，用于指示订阅列表是否发生变化。
            /// 如果为 true，则表示缓存数组需要更新。
            /// </summary>
            private bool _dirty = false;

            /// <summary>
            /// 用于同步访问事件桶内部状态的锁对象。
            /// 确保在多线程环境下对订阅列表和缓存数组的操作是线程安全的。
            /// </summary>
            private readonly object _bucketLock = new object();

            /// <summary>
            /// 订阅事件处理程序到当前事件桶。
            /// 如果处理程序尚未在订阅列表中，则将其添加，并设置脏标记。
            /// </summary>
            /// <param name="handler">要订阅的事件处理程序，必须实现 <see cref="IAsakiHandler{T}"/> 接口。</param>
            public void Subscribe(IAsakiHandler<T> handler)
            {
                lock (_bucketLock)
                {
                    if (!_handlers.Contains(handler))
                    {
                        _handlers.Add(handler);
                        _dirty = true;
                    }
                }
            }

            /// <summary>
            /// 从当前事件桶中取消订阅事件处理程序。
            /// 如果处理程序在订阅列表中，则将其移除，并设置脏标记。
            /// </summary>
            /// <param name="handler">要取消订阅的事件处理程序，必须实现 <see cref="IAsakiHandler{T}"/> 接口。</param>
            public void Unsubscribe(IAsakiHandler<T> handler)
            {
                lock (_bucketLock)
                {
                    if (_handlers.Remove(handler))
                    {
                        _dirty = true;
                    }
                }
            }

            /// <summary>
            /// 发布事件到所有订阅的处理程序。
            /// 首先检查脏标记，如果需要则更新缓存数组，然后遍历缓存数组并调用每个处理程序的 <see cref="IAsakiHandler{T}.OnEvent(in T)"/> 方法。
            /// </summary>
            /// <param name="e">要发布的事件实例（只读引用传递）。</param>
            /// <remarks>
            /// <para>异常处理策略：不捕获异常，让异常冒泡到调用者。</para>
            /// <para>这意味着如果任一 handler 抛出异常，后续 handler 将不会被执行。</para>
            /// <para>此设计允许调用者获取完整的堆栈跟踪信息，便于调试。</para>
            /// <para>使用 in 关键字避免结构体复制，优化栈空间使用。</para>
            /// </remarks>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Publish(in T e)
            {
                // 1. 检查脏标记 (Double-Check Locking 变种)
                if (_dirty)
                {
                    lock (_bucketLock)
                    {
                        if (_dirty)
                        {
                            _cache = _handlers.ToArray();
                            _dirty = false;
                        }
                    }
                }

                // 2. 获取缓存数组引用 (原子操作)
                var array = _cache;
                int count = array.Length;

                // 3. 极速遍历 (Zero GC)
                for (int i = 0; i < count; i++)
                {
                    array[i].OnEvent(e);
                }
            }

            /// <summary>
            /// 清理事件桶的内部状态。
            /// 清空订阅列表，重置缓存数组，并清除脏标记。
            /// </summary>
            public void Cleanup()
            {
                lock (_bucketLock)
                {
                    _handlers.Clear();
                    _cache = Array.Empty<IAsakiHandler<T>>();
                    _dirty = false;
                }
            }
        }

        // ========================================================================
        // Bus Implementation
        // ========================================================================

        /// <summary>
        /// 存储所有类型事件桶的字典。
        /// 键为事件类型，值为对应的 <see cref="IEventBucket"/> 实例，实际为 <see cref="EventBucket{T}"/> 类型。
        /// </summary>
        private readonly Dictionary<Type, IEventBucket> _buckets =
            new Dictionary<Type, IEventBucket>();

        /// <summary>
        /// 用于同步访问事件总线内部状态的锁对象。
        /// 确保在多线程环境下对事件桶字典的操作是线程安全的。
        /// </summary>
        private readonly object _busLock = new object();

        /// <summary>
        /// 订阅事件处理程序到事件总线。
        /// 通过获取对应的事件桶，并调用其 <see cref="EventBucket{T}.Subscribe(IAsakiHandler{T})"/> 方法来完成订阅操作。
        /// </summary>
        /// <typeparam name="T">事件类型，必须实现 <see cref="IAsakiEvent"/> 接口。</typeparam>
        /// <param name="handler">要订阅的事件处理程序，必须实现 <see cref="IAsakiHandler{T}"/> 接口。</param>
        public void Subscribe<T>(IAsakiHandler<T> handler)
            where T : IAsakiEvent
        {
            GetBucket<T>().Subscribe(handler);
        }

        /// <summary>
        /// 从事件总线中取消订阅事件处理程序。
        /// 通过获取对应的事件桶，并调用其 <see cref="EventBucket{T}.Unsubscribe(IAsakiHandler{T})"/> 方法来完成取消订阅操作。
        /// </summary>
        /// <typeparam name="T">事件类型，必须实现 <see cref="IAsakiEvent"/> 接口。</typeparam>
        /// <param name="handler">要取消订阅的事件处理程序，必须实现 <see cref="IAsakiHandler{T}"/> 接口。</param>
        public void Unsubscribe<T>(IAsakiHandler<T> handler)
            where T : IAsakiEvent
        {
            GetBucket<T>().Unsubscribe(handler);
        }

        /// <summary>
        /// 发布事件到事件总线。
        /// 如果对应的事件桶存在，则调用其 <see cref="EventBucket{T}.Publish(in T)"/> 方法发布事件；否则直接跳过。
        /// </summary>
        /// <typeparam name="T">事件类型，必须实现 <see cref="IAsakiEvent"/> 接口。</typeparam>
        /// <param name="e">要发布的事件实例（只读引用传递）。</param>
        public void Publish<T>(in T e)
            where T : IAsakiEvent
        {
            // 极速路径：如果桶不存在，说明没订阅者，直接跳过 (比静态类访问还快，因为省了泛型初始化检查)
            if (TryGetBucket<T>(out EventBucket<T> bucket))
            {
                bucket.Publish(e);
            }
        }

        /// <summary>
        /// 获取指定类型的事件桶。
        /// 首先尝试快速获取事件桶，如果不存在则通过锁机制创建并添加到事件桶字典中。
        /// </summary>
        /// <typeparam name="T">事件类型，必须实现 <see cref="IAsakiEvent"/> 接口。</typeparam>
        /// <returns>指定类型的 <see cref="EventBucket{T}"/> 实例。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EventBucket<T> GetBucket<T>()
            where T : IAsakiEvent
        {
            // 快速检查
            if (TryGetBucket<T>(out var bucket))
                return bucket;

            // 慢速创建
            lock (_busLock)
            {
                // Double check
                if (TryGetBucket(out bucket))
                    return bucket;

                bucket = new EventBucket<T>();
                _buckets[typeof(T)] = bucket;
                return bucket;
            }
        }

        /// <summary>
        /// 尝试获取指定类型的事件桶。
        /// 从事件桶字典中尝试获取指定类型的事件桶，如果找到则返回 true 并输出事件桶实例；否则返回 false 且输出 null。
        /// </summary>
        /// <typeparam name="T">事件类型，必须实现 <see cref="IAsakiEvent"/> 接口。</typeparam>
        /// <param name="bucket">输出的事件桶实例，如果未找到则为 null。</param>
        /// <returns>如果找到指定类型的事件桶则返回 true，否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetBucket<T>(out EventBucket<T> bucket)
            where T : IAsakiEvent
        {
            lock (_busLock)
            {
                if (_buckets.TryGetValue(typeof(T), out IEventBucket b))
                {
                    bucket = (EventBucket<T>)b;
                    return true;
                }
                bucket = null;
                return false;
            }
        }

        /// <summary>
        /// 释放事件总线占用的资源。
        /// 锁定事件总线锁，清理所有事件桶的内部状态，并清空事件桶字典。
        /// </summary>
        public void Dispose()
        {
            // [Fix Defect-3 & 4] 真正的清理
            lock (_busLock)
            {
                foreach (IEventBucket bucket in _buckets.Values)
                {
                    bucket.Cleanup();
                }
                _buckets.Clear();
            }
        }
    }
}
