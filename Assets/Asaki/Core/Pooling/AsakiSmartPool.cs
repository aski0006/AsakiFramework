using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Context;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Core.Pooling
{
    /// <summary>
    /// [Legacy Facade] 对象池静态门面
    /// <para>作用：兼容旧代码，将静态调用转发给 IAsakiPoolService。</para>
    /// <para>注意：这是一个"空壳"，真正的逻辑在 AsakiPoolService 中。</para>
    /// </summary>
    public static class AsakiSmartPool
    {
        // 快捷访问器：动态从 Context 获取服务
        // 如果服务没注册 (比如 Bootstrapper 还没跑)，这里会抛出异常，这是符合预期的 Fail-Fast
        private static IAsakiPoolService Service => AsakiContext.Get<IAsakiPoolService>();

        // =========================================================
        // 核心转发
        // =========================================================

        public static UniTask PrewarmAsync(
            string key,
            int count,
            int itemsPerFrame = 5,
            CancellationToken cancellationToken = default
        )
        {
            return Service.PrewarmAsync(key, count, itemsPerFrame, cancellationToken);
        }

        public static GameObject Spawn(
            string key,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null
        )
        {
            return Service.Spawn(key, position, rotation, parent);
        }

        // 重载版本
        public static GameObject Spawn(string key, Vector3 position)
        {
            return Service.Spawn(key, position, Quaternion.identity, null);
        }

        public static void Despawn(GameObject go, string key)
        {
            Service.Despawn(go, key);
        }

        public static void ReleasePool(string key)
        {
            Service.ReleasePool(key);
        }

        // =========================================================
        // 兼容性接口 (这些方法在接口里没有，是 Facade 独有的 helper)
        // =========================================================

        public static List<GameObject> SpawnBatch(string key, int count, Vector3 position)
        {
            var list = new List<GameObject>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(Spawn(key, position));
            }
            return list;
        }

        public static void DespawnBatch(IEnumerable<GameObject> objects, string key)
        {
            foreach (GameObject go in objects)
            {
                Despawn(go, key);
            }
        }
    }
}
