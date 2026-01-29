using System;
using System.Collections.Generic;
using Asaki.Core.Pooling;

namespace Asaki.Core.Architecture.Command
{
    internal static class AsakiCommandPoolManager
    {
        private static readonly Dictionary<Type, object> _pools = new Dictionary<Type, object>();
        private static readonly object _globalLock = new object();

        public static TCommand Rent<TCommand>()
            where TCommand : class, new()
        {
            Type type = typeof(TCommand);

            lock (_globalLock)
            {
                if (!_pools.TryGetValue(type, out var poolObj))
                {
                    // 创建新池
                    poolObj = new Stack<TCommand>(16);
                    _pools[type] = poolObj;
                }

                var pool = (Stack<TCommand>)poolObj;

                if (pool.Count > 0)
                {
                    return pool.Pop();
                }
            }

            return new TCommand();
        }

        public static void Return<TCommand>(TCommand cmd)
            where TCommand : class
        {
            if (cmd == null)
                return;

            Type type = typeof(TCommand);

            // 重置状态（如果实现了 IResettable）
            if (cmd is IAsakiResettable resettable)
            {
                resettable.Reset();
            }

            lock (_globalLock)
            {
                if (!_pools.TryGetValue(type, out var poolObj))
                {
                    poolObj = new Stack<TCommand>(16);
                    _pools[type] = poolObj;
                }

                var pool = (Stack<TCommand>)poolObj;

                const int MAX_POOL_SIZE = 64;
                if (pool.Count < MAX_POOL_SIZE)
                {
                    pool.Push(cmd);
                }
            }
        }

        public static void ClearAll()
        {
            lock (_globalLock)
            {
                _pools.Clear();
            }
        }
    }
}
