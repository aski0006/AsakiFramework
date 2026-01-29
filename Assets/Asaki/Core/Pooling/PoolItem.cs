using UnityEngine;

namespace Asaki.Core.Pooling
{
	/// <summary>
	/// [Data Unit] 对象池的基础存储单元
	/// </summary>
	public class PoolItem
	{
		/// <summary>
		/// 实际的 Unity GameObject 引用
		/// </summary>
		public readonly GameObject GameObject;

		/// <summary>
		/// 缓存 Transform 访问，微乎其微的性能优化
		/// </summary>
		public readonly Transform Transform;

		/// <summary>
		/// 缓存生命周期接口 (如果有)
		/// </summary>
		public readonly IAsakiPoolable AsakiPoolable;

		/// <summary>
		/// 上次激活时间 (用于 LRU 或超时清理策略)
		/// </summary>
		public float LastActiveTime;

		public PoolItem(GameObject go)
		{
			GameObject = go;
			Transform = go.transform;
			// 在构造时一次性获取接口，Spawn 时直接调用，0 GC
			go.TryGetComponent(out AsakiPoolable);
			LastActiveTime = UnityEngine.Time.time;
		}
	}
}
