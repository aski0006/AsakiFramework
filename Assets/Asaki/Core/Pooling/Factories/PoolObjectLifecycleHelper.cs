using System;
using Asaki.Core.Logging;
using Asaki.Core.Pooling.Interfaces;
using UnityEngine;

namespace Asaki.Core.Pooling.Factories
{
    /// <summary>
    /// 池对象生命周期辅助类
    /// 提供统一的 IAsakiPoolable 回调处理和 GameObject 状态管理
    /// </summary>
    internal static class PoolObjectLifecycleHelper
    {
        /// <summary>
        /// 触发 OnSpawn 回调并激活 GameObject
        /// </summary>
        public static void OnGet(GameObject obj)
        {
            if (!obj)
                return;

            obj.SetActive(true);
            InvokeOnSpawn(obj);
        }

        /// <summary>
        /// 触发 OnDespawn 回调、停用 GameObject 并重置父节点
        /// </summary>
        public static void OnReturn(
            GameObject obj,
            Transform parent = null,
            bool worldPositionStays = false
        )
        {
            if (!obj)
                return;

            InvokeOnDespawn(obj);
            obj.SetActive(false);

            if (parent && obj.transform.parent != parent)
            {
                obj.transform.SetParent(parent, worldPositionStays);
            }
        }

        /// <summary>
        /// 触发 OnSpawn 回调（仅对实现了 IAsakiPoolable 的组件）
        /// </summary>
        public static void InvokeOnSpawn(GameObject obj)
        {
            IAsakiPoolable poolable = obj.GetComponent<IAsakiPoolable>();
            if (poolable != null)
            {
                try
                {
                    poolable.OnSpawn();
                }
                catch (Exception ex)
                {
                    ALog.Error($"[AsakiPool] OnSpawn callback failed: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 触发 OnDespawn 回调（仅对实现了 IAsakiPoolable 的组件）
        /// </summary>
        public static void InvokeOnDespawn(GameObject obj)
        {
            IAsakiPoolable poolable = obj.GetComponent<IAsakiPoolable>();
            if (poolable != null)
            {
                try
                {
                    poolable.OnDespawn();
                }
                catch (Exception ex)
                {
                    ALog.Error($"[AsakiPool] OnDespawn callback failed: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 触发组件的 OnSpawn 回调
        /// </summary>
        public static void InvokeOnSpawnForComponent<T>(T component)
            where T : Component
        {
            if (!component)
                return;

            if (component is IAsakiPoolable poolable)
            {
                try
                {
                    poolable.OnSpawn();
                }
                catch (Exception ex)
                {
                    ALog.Error($"[AsakiPool] OnSpawn callback failed: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 触发组件的 OnDespawn 回调
        /// </summary>
        public static void InvokeOnDespawnForComponent<T>(T component)
            where T : Component
        {
            if (!component)
                return;

            if (component is IAsakiPoolable poolable)
            {
                try
                {
                    poolable.OnDespawn();
                }
                catch (Exception ex)
                {
                    ALog.Error($"[AsakiPool] OnDespawn callback failed: {ex.Message}", ex);
                }
            }
        }
    }
}
