using Asaki.Core.Context;
using Asaki.Core.Pooling;
using UnityEngine;

namespace Asaki.Unity.Extensions
{
    public static class AsakiPoolExtensions
    {
        /// <summary>
        /// [Asaki V5] 类型安全的 Spawn
        /// <para>1. 从池中取出 GameObject</para>
        /// <para>2. 获取组件 T</para>
        /// <para>3. 调用 Init(arg1) 注入依赖</para>
        /// </summary>
        public static T Spawn<T, TArg1>(
            this IAsakiPoolService pool,
            string key,
            TArg1 arg1,
            Transform parent = null
        )
            where T : Component, IAsakiInit<TArg1>
        {
            // 1. 获取原生 GameObject
            GameObject go = pool.Spawn(key, null, null, parent);
            if (go == null)
                return null;

            // 2. 获取组件 (这是池化不可避免的开销，除非把 Component 缓存在 PoolItem 里)
            // 优化建议：如果 T 是特定高频类，可以考虑修改 PoolItem 泛型化，但现在这样足够通用。
            if (!go.TryGetComponent<T>(out T component))
            {
                Debug.LogError(
                    $"[AsakiPool] Object '{go.name}' missing component '{typeof(T).Name}'!"
                );
                pool.Despawn(go, key); // 错误回退
                return null;
            }

            // 3. [关键] 生命周期注入
            // 即使对象是复用的，这里也会再次调用 Init，覆盖旧的引用
            component.Init(arg1);

            return component;
        }

        public static T Spawn<T, TArg1, TArg2>(
            this IAsakiPoolService pool,
            string key,
            TArg1 arg1,
            TArg2 arg2,
            Transform parent = null
        )
            where T : Component, IAsakiInit<TArg1, TArg2>
        {
            GameObject go = pool.Spawn(key, null, null, parent);
            if (go == null)
                return null;

            if (go.TryGetComponent<T>(out T component))
            {
                component.Init(arg1, arg2);
                return component;
            }

            pool.Despawn(go, key);
            return null;
        }

        public static T Spawn<T, TArg1, TArg2, TArg3>(
            this IAsakiPoolService pool,
            string key,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            Transform parent = null
        )
            where T : Component, IAsakiInit<TArg1, TArg2, TArg3>
        {
            GameObject go = pool.Spawn(key, null, null, parent);
            if (go == null)
                return null;

            if (go.TryGetComponent<T>(out T component))
            {
                component.Init(arg1, arg2, arg3);
                return component;
            }

            pool.Despawn(go, key);
            return null;
        }

        public static T Spawn<T, TArg1, TArg2, TArg3, TArg4>(
            this IAsakiPoolService pool,
            string key,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            TArg4 arg4,
            Transform parent = null
        )
            where T : Component, IAsakiInit<TArg1, TArg2, TArg3, TArg4>
        {
            GameObject go = pool.Spawn(key, null, null, parent);
            if (go == null)
                return null;

            if (go.TryGetComponent<T>(out T component))
            {
                component.Init(arg1, arg2, arg3, arg4);
                return component;
            }
            pool.Despawn(go, key);

            return null;
        }

        public static T Spawn<T, TArg1, TArg2, TArg3, TArg4, TArg5>(
            this IAsakiPoolService pool,
            string key,
            TArg1 arg1,
            TArg2 arg2,
            TArg3 arg3,
            TArg4 arg4,
            TArg5 arg5,
            Transform parent = null
        )
            where T : Component, IAsakiInit<TArg1, TArg2, TArg3, TArg4, TArg5>
        {
            GameObject go = pool.Spawn(key, null, null, parent);
            if (go == null)
                return null;

            if (go.TryGetComponent<T>(out T component))
            {
                component.Init(arg1, arg2, arg3, arg4, arg5);
                return component;
            }
            pool.Despawn(go, key);
            return null;
        }
    }
}
