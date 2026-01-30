using System.Threading;
using Asaki.Core.Pooling.Factories;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.Resources;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Core.Pooling.Extensions
{
    /// <summary>
    /// IAsakiPoolService 扩展方法（便捷 API）
    /// </summary>
    public static class PoolServiceExtensions
    {
        /// <summary>
        /// 快捷创建 GameObject 池（从资源路径）
        /// </summary>
        public static async UniTask<IAsakiPool<GameObject>> CreateGameObjectPoolAsync(
            this IAsakiPoolService service,
            string key,
            string resourcePath,
            IAsakiResourceService resourceService,
            Transform parent = null,
            AsakiPoolConfig config = null,
            CancellationToken token = default(CancellationToken)
        )
        {
            PrefabInstanceFactory factory = new PrefabInstanceFactory(
                resourceService,
                resourcePath,
                parent
            );
            return await service.CreatePoolAsync(key, factory, config, token);
        }

        /// <summary>
        /// 快捷创建组件池（从资源路径）
        /// </summary>
        public static async UniTask<IAsakiPool<T>> CreateComponentPoolAsync<T>(
            this IAsakiPoolService service,
            string key,
            string resourcePath,
            IAsakiResourceService resourceService,
            Transform parent = null,
            AsakiPoolConfig config = null,
            CancellationToken token = default(CancellationToken)
        )
            where T : Component
        {
            var factory = new ComponentFactory<T>(resourceService, resourcePath, parent);
            return await service.CreatePoolAsync(key, factory, config, token);
        }

        /// <summary>
        /// 快捷创建资源池（Sprite, AudioClip 等）
        /// </summary>
        public static async UniTask<IAsakiPool<T>> CreateResourcePoolAsync<T>(
            this IAsakiPoolService service,
            string key,
            string resourcePath,
            IAsakiResourceService resourceService,
            AsakiPoolConfig config = null,
            CancellationToken token = default(CancellationToken)
        )
            where T : Object
        {
            var factory = new ResourcePoolFactory<T>(resourceService, resourcePath);
            return await service.CreatePoolAsync(key, factory, config, token);
        }

        /// <summary>
        /// 快捷创建 GameObject 池（从已加载的预制体）
        /// </summary>
        public static async UniTask<IAsakiPool<GameObject>> CreateGameObjectPoolFromPrefab(
            this IAsakiPoolService service,
            string key,
            GameObject prefab,
            Transform parent = null,
            AsakiPoolConfig config = null,
            CancellationToken token = default(CancellationToken)
        )
        {
            GameObjectFactory factory = new GameObjectFactory(prefab, parent);
            return await service.CreatePoolAsync(key, factory, config, token);
        }
    }
}
