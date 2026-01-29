// 文件: Assets/Asaki/Core/Pooling/AsakiPoolItem.cs
using UnityEngine;

namespace Asaki.Core.Pooling
{
    /// <summary>
    /// [Data Unit] 对象池的基础存储单元
    /// 缓存所有接口引用，实现 0GC 优化
    /// </summary>
    internal class AsakiPoolItem
    {
        /// <summary>
        /// 实际的 Unity GameObject 引用
        /// </summary>
        public readonly GameObject GameObject;

        /// <summary>
        /// 缓存 Transform 访问
        /// </summary>
        public readonly Transform Transform;

        /// <summary>
        /// 缓存生命周期接口（可选）
        /// </summary>
        public readonly IAsakiPoolable AsakiPoolable;

        /// <summary>
        /// ===== [新增] 缓存重置接口（可选）=====
        /// </summary>
        public readonly IAsakiResettable AsakiResettable;

        /// <summary>
        /// 上次激活时间（用于 LRU 清理）
        /// </summary>
        public float LastActiveTime;

        /// <summary>
        /// 所属池的 Key（用于反向查找）
        /// </summary>
        public string PoolKey;

        public AsakiPoolItem(GameObject go, string poolKey = null)
        {
            GameObject = go;
            Transform = go.transform;
            PoolKey = poolKey;

            // 一次性缓存所有接口（0GC）
            go.TryGetComponent(out AsakiPoolable);
            go.TryGetComponent(out AsakiResettable);

            LastActiveTime = UnityEngine.Time.time;
        }
    }
}
