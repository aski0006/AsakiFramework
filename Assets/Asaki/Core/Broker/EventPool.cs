using System;
using System.Collections.Concurrent;

namespace Asaki.Core.Broker
{
    /// <summary>
    /// 事件对象池管理器，用于管理大事件类的复用
    /// </summary>
    public static class EventPool
    {
        /// <summary>
        /// 默认阈值：32字节以下使用结构体，以上使用类
        /// </summary>
        public const int DefaultThreshold = 32;

        /// <summary>
        /// 当前阈值配置
        /// </summary>
        public static int Threshold { get; set; } = DefaultThreshold;

        /// <summary>
        /// 类型到对象池的映射
        /// </summary>
        private static readonly ConcurrentDictionary<Type, object> _pools = new();

        /// <summary>
        /// 获取指定类型的对象池
        /// </summary>
        private static ObjectPool<T> GetPool<T>()
            where T : class, new()
        {
            return (ObjectPool<T>)_pools.GetOrAdd(typeof(T), _ => new ObjectPool<T>());
        }

        /// <summary>
        /// 从池中租借一个对象
        /// </summary>
        public static T Rent<T>()
            where T : class, new()
        {
            return GetPool<T>().Rent();
        }

        /// <summary>
        /// 将对象归还到池中
        /// </summary>
        public static void Return<T>(T obj)
            where T : class, new()
        {
            if (obj != null)
            {
                GetPool<T>().Return(obj);
            }
        }

        /// <summary>
        /// 清空所有池
        /// </summary>
        public static void ClearAll()
        {
            foreach (var pool in _pools.Values)
            {
                (pool as IDisposable)?.Dispose();
            }
            _pools.Clear();
        }

        /// <summary>
        /// 内部对象池实现
        /// </summary>
        private class ObjectPool<T> : IDisposable
            where T : class, new()
        {
            private readonly ConcurrentBag<T> _items = new();
            private readonly object _lock = new();
            private int _count;
            private const int MaxPoolSize = 64;

            public T Rent()
            {
                if (_items.TryTake(out T item))
                {
                    lock (_lock)
                    {
                        _count--;
                    }
                    return item;
                }
                return new T();
            }

            public void Return(T item)
            {
                lock (_lock)
                {
                    if (_count < MaxPoolSize)
                    {
                        _items.Add(item);
                        _count++;
                    }
                }
            }

            public void Dispose()
            {
                _items.Clear();
                _count = 0;
            }
        }
    }
}
