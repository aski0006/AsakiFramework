using System;
using System.Collections.Generic;
using Asaki.Core.Resources; // 引用资源模块
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Core.Pooling
{
	/// <summary>
	/// [Internal Container] 池数据结构
	/// <para>职责：将 "资源生命周期" 与 "对象实例集合" 绑定。</para>
	/// </summary>
	internal class PoolData : IDisposable
	{
		/// <summary>
		/// 资源句柄 (RAII)。
		/// <para>只要 PoolData 存在，这个 Handle 就不释放，</para>
		/// <para>保证 Prefab 不会被 Addressables/AssetBundle 卸载。</para>
		/// </summary>
		public ResHandle<GameObject> PrefabHandle;

		/// <summary>
		/// 闲置对象栈
		/// </summary>
		public readonly Stack<PoolItem> Stack;

		/// <summary>
		/// 层级根节点 (用于收纳隐藏的对象，保持 Hierarchy 干净)
		/// </summary>
		public Transform Root;

		public PoolData(ResHandle<GameObject> handle, Transform root, int capacity)
		{
			PrefabHandle = handle;
			Root = root;
			Stack = new Stack<PoolItem>(capacity);
		}

		public void Dispose()
		{
			// 1. 销毁所有闲置实例
			if (Stack != null)
			{
				while (Stack.Count > 0)
				{
					PoolItem item = Stack.Pop();
					if (item.GameObject != null)
					{
						Object.Destroy(item.GameObject);
					}
				}
			}

			// 2. 销毁层级根节点
			if (Root != null)
			{
				if (Application.isPlaying)
					Object.Destroy(Root.gameObject);
				else
					Object.DestroyImmediate(Root.gameObject);
			}

			// 3. [核心] 释放资源引用计数
			// 这会通知 AsakiResService 该 Prefab 不再被此池占用
			PrefabHandle?.Dispose();
			PrefabHandle = null;
		}
	}
}
