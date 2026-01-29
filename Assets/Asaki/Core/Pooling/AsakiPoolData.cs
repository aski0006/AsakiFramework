using System;
using System.Collections.Generic;
using Asaki.Core.Resources;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Core.Pooling
{
    /// <summary>
    /// [Internal Container] 池数据结构
    /// 职责：管理资源生命周期 + 对象实例集合
    /// </summary>
    internal class AsakiPoolData : IDisposable
    {
        /// <summary>
        /// 资源句柄（RAII）
        /// </summary>
        public ResHandle<GameObject> PrefabHandle;

        /// <summary>
        /// 闲置对象栈
        /// </summary>
        public readonly Stack<AsakiPoolItem> Stack;

        /// <summary>
        /// ===== [新增] 活跃对象映射表 =====
        /// GameObject → AsakiPoolItem
        /// 用于在 Despawn 时快速找到 AsakiPoolItem
        /// </summary>
        public readonly Dictionary<GameObject, AsakiPoolItem> ActiveItems;

        /// <summary>
        /// 层级根节点
        /// </summary>
        public Transform Root;

        /// <summary>
        /// 池的 Key（用于日志和反向查找）
        /// </summary>
        public string Key;

        public AsakiPoolData(ResHandle<GameObject> handle, Transform root, string key, int capacity)
        {
            PrefabHandle = handle;
            Root = root;
            Key = key;
            Stack = new Stack<AsakiPoolItem>(capacity);
            ActiveItems = new Dictionary<GameObject, AsakiPoolItem>(capacity);
        }

        public void Dispose()
        {
            // 1. 销毁所有闲置实例
            if (Stack != null)
            {
                while (Stack.Count > 0)
                {
                    AsakiPoolItem item = Stack.Pop();
                    if (item.GameObject != null)
                    {
                        Object.Destroy(item.GameObject);
                    }
                }
            }

            // 2. 销毁所有活跃实例
            if (ActiveItems != null)
            {
                foreach (var kvp in ActiveItems)
                {
                    if (kvp.Key != null)
                    {
                        Object.Destroy(kvp.Key);
                    }
                }
                ActiveItems.Clear();
            }

            // 3. 销毁层级根节点
            if (Root != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(Root.gameObject);
                else
                    Object.DestroyImmediate(Root.gameObject);
            }

            // 4. 释放资源引用计数
            PrefabHandle?.Dispose();
            PrefabHandle = null;
        }
    }
}
