// 文件: Assets/Asaki/Core/Pooling/V2/Extensions/PoolServiceExtensions.cs

using Asaki.Core.Pooling;
using Asaki.Core.Pooling.Factories;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.Resources;
using Cysharp.Threading.Tasks;
using System.Threading;
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
		/// <param name="service">池服务</param>
		/// <param name="key">池的唯一标识符</param>
		/// <param name="resourcePath">预制体资源路径</param>
		/// <param name="resourceService">资源服务</param>
		/// <param name="parent">实例化父节点</param>
		/// <param name="config">池配置</param>
		/// <param name="token">取消令牌</param>
		/// <returns>创建的 GameObject 池</returns>
		public static async UniTask<IAsakiPool<GameObject>> CreateGameObjectPoolAsync(
			this IAsakiPoolService service,
			string key,
			string resourcePath,
			IAsakiResourceService resourceService,
			Transform parent = null,
			AsakiPoolConfig config = null,
			CancellationToken token = default
		)
		{
			var factory = new PrefabInstanceFactory(resourceService, resourcePath, parent);
			return await service.CreatePoolAsync(key, factory, config, token);
		}

		/// <summary>
		/// 快捷创建组件池（从资源路径）
		/// </summary>
		/// <typeparam name="T">组件类型</typeparam>
		public static async UniTask<IAsakiPool<T>> CreateComponentPoolAsync<T>(
			this IAsakiPoolService service,
			string key,
			string resourcePath,
			IAsakiResourceService resourceService,
			Transform parent = null,
			AsakiPoolConfig config = null,
			CancellationToken token = default
		) where T : Component
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
			CancellationToken token = default
		) where T : Object
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
			CancellationToken token = default
		)
		{
			var factory = new GameObjectFactory(prefab, parent);
			return await service.CreatePoolAsync(key, factory, config, token);
		}
	}
}
