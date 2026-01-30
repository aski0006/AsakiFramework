using System;
using System.Collections.Generic;
using Asaki.Core.Pooling;
using Asaki.Core.Pooling.Interfaces;

namespace Asaki.Core.Architecture.Queries
{
    /// <summary>
    /// Query 对象池管理器（0GC）
    /// 复用 Query 实例，避免频繁分配
    /// </summary>
    internal static class QueryPoolManager
    {
        // 为每种 Query 类型维护独立的���
        private static readonly Dictionary<Type, object> _pools = new Dictionary<Type, object>();
        private static readonly object _globalLock = new object();

        public static TQuery Rent<TQuery>()
            where TQuery : class, new()
        {
            Type type = typeof(TQuery);

            lock (_globalLock)
            {
                if (!_pools.TryGetValue(type, out object poolObj))
                {
                    poolObj = new Stack<TQuery>(16);
                    _pools[type] = poolObj;
                }

                var pool = (Stack<TQuery>)poolObj;

                if (pool.Count > 0)
                {
                    return pool.Pop();
                }
            }

            return new TQuery();
        }

        public static void Return<TQuery>(TQuery query)
            where TQuery : class
        {
            if (query == null)
                return;

            Type type = typeof(TQuery);

            // 重置状态（如果实现了 IResettable）
            if (query is IAsakiResettable resettable)
            {
                resettable.Reset();
            }

            lock (_globalLock)
            {
                if (!_pools.TryGetValue(type, out object poolObj))
                {
                    poolObj = new Stack<TQuery>(16);
                    _pools[type] = poolObj;
                }

                var pool = (Stack<TQuery>)poolObj;

                const int MAX_POOL_SIZE = 64;
                if (pool.Count < MAX_POOL_SIZE)
                {
                    pool.Push(query);
                }
            }
        }

        /// <summary>
        /// 清空所有池（场景切换时调用）
        /// </summary>
        public static void ClearAll()
        {
            lock (_globalLock)
            {
                _pools.Clear();
            }
        }
    }
}
