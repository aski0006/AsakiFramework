// 文件: Assets/Asaki/Core/Pooling/AsakiPoolService.cs
using System;
using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Async;
using Asaki.Core.Broker;
using Asaki.Core.Logging;
using Asaki.Core.Resources;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Core.Pooling
{
    /// <summary>
    /// [Asaki Native] 对象池服务实现
    /// 核心策略：Async-First Prewarm + Sync Spawn + 0GC
    /// </summary>
    public class AsakiPoolService : IAsakiPoolService, IDisposable
    {
        // =========================================================
        // 1. 依赖与状态
        // =========================================================

        private readonly IAsakiAsyncService _asyncService;
        private readonly IAsakiResourceService _resourceService;
        private readonly IAsakiEventService _eventService;
        private readonly Dictionary<string, AsakiPoolData> _pools =
            new Dictionary<string, AsakiPoolData>();

        private Transform _globalRoot;
        private const string GLOBAL_ROOT_NAME = "[Asaki.Pool.Service]";
        private bool _isDisposed = false;

        // =========================================================
        // 2. 构造与初始化
        // =========================================================

        public AsakiPoolService(
            IAsakiAsyncService asyncService,
            IAsakiResourceService resourceService,
            IAsakiEventService eventService
        )
        {
            _asyncService = asyncService ?? throw new ArgumentNullException(nameof(asyncService));
            _resourceService =
                resourceService ?? throw new ArgumentNullException(nameof(resourceService));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));

            InitializeGlobalRoot();
        }

        private void InitializeGlobalRoot()
        {
            GameObject go = new GameObject(GLOBAL_ROOT_NAME);
            Object.DontDestroyOnLoad(go);
            _globalRoot = go.transform;
        }

        // =========================================================
        // 3. 异步预热
        // =========================================================

        public async UniTask PrewarmAsync(
            string key,
            int count,
            int itemsPerFrame = 5,
            CancellationToken cancellationToken = default
        )
        {
            if (_isDisposed || string.IsNullOrEmpty(key))
                return;

            AsakiPoolData asakiPoolData;

            // 检查池是否已存在
            if (!_pools.TryGetValue(key, out asakiPoolData))
            {
                // 加载资源
                var handle = await _resourceService.LoadAsync<GameObject>(key, cancellationToken);

                if (handle == null || !handle.IsValid)
                {
                    ALog.Error($"[AsakiPool] Failed to load resource: {key}. Prewarm aborted.");
                    return;
                }

                // 创建池容器
                GameObject rootGo = new GameObject($"Pool[{key}]");
                rootGo.transform.SetParent(_globalRoot);

                // ===== [修改] 传入 key 参数 =====
                asakiPoolData = new AsakiPoolData(handle, rootGo.transform, key, count);
                _pools.Add(key, asakiPoolData);
            }

            // 计算需要生成的数量
            int currentCount = asakiPoolData.Stack.Count;
            int needToSpawn = count - currentCount;

            if (needToSpawn <= 0)
                return;

            // 分帧实例化
            int batchCount = 0;
            GameObject prefab = asakiPoolData.PrefabHandle.Asset;

            for (int i = 0; i < needToSpawn; i++)
            {
                if (_isDisposed || cancellationToken.IsCancellationRequested)
                    break;

                GameObject go = Object.Instantiate(prefab, asakiPoolData.Root);
                go.SetActive(false);

                // ===== [修改] 传入 key 参数 =====
                AsakiPoolItem item = new AsakiPoolItem(go, key);
                asakiPoolData.Stack.Push(item);

                batchCount++;
                if (batchCount >= itemsPerFrame)
                {
                    batchCount = 0;
                    await UniTask.Yield(cancellationToken: cancellationToken);
                }
            }
        }

        // =========================================================
        // 4. Spawn（获取对象）
        // =========================================================

        public GameObject Spawn(
            string key,
            Vector3? position = null,
            Quaternion? rotation = null,
            Transform parent = null
        )
        {
            if (_isDisposed)
                return null;

            // 检查池是否预热
            if (!_pools.TryGetValue(key, out AsakiPoolData poolData))
            {
                ALog.Error(
                    $"[AsakiPool] Key not prewarmed: '{key}'. \n"
                        + "Solution: Call 'await PrewarmAsync(\"{key}\", ...)' first."
                );
                return null;
            }

            AsakiPoolItem item = null;

            // 从栈中弹出
            while (poolData.Stack.Count > 0)
            {
                AsakiPoolItem popped = poolData.Stack.Pop();
                if (popped.GameObject)
                {
                    item = popped;
                    break;
                }
            }

            // 栈空补货
            if (item == null)
            {
                GameObject go = Object.Instantiate(poolData.PrefabHandle.Asset, poolData.Root);
                item = new AsakiPoolItem(go, key);
            }

            // ===== [新增] 注册到活跃对象映射表 =====
            poolData.ActiveItems[item.GameObject] = item;

            // 设置变换信息
            Transform t = item.Transform;
            t.SetParent(parent);

            if (position.HasValue)
                t.position = position.Value;
            if (rotation.HasValue)
                t.rotation = rotation.Value;

            // 激活与生命周期
            item.GameObject.SetActive(true);

            // ===== [修改] 使用缓存的接口，0GC =====
            item.AsakiPoolable?.OnSpawn();

            item.LastActiveTime = UnityEngine.Time.unscaledTime;

            return item.GameObject; // 依然返回 GameObject（保持 API 不变）
        }

        // =========================================================
        // 5. Despawn（回收对象）
        // =========================================================

        public void Despawn(GameObject go, string key)
        {
            if (_isDisposed || !go)
                return;

            // 查找池
            if (!_pools.TryGetValue(key, out AsakiPoolData poolData))
            {
                ALog.Warn(
                    $"[AsakiPool] Despawn target pool '{key}' not found. Destroying object directly."
                );
                Object.Destroy(go);
                return;
            }

            // ===== [核心改动] 从映射表中获取 AsakiPoolItem =====
            if (!poolData.ActiveItems.Remove(go, out AsakiPoolItem item))
            {
                ALog.Warn(
                    $"[AsakiPool] GameObject not spawned from this pool: '{key}'. Destroying."
                );
                Object.Destroy(go);
                return;
            }

            // 从活跃表中移除

            // ===== [新增] 生命周期回调（使用缓存的接口，0GC）=====
            // 1. Reset: 重置物理、动画状态
            item.AsakiResettable?.Reset();

            // 2. OnDespawn: 清理外部引用
            item.AsakiPoolable?.OnDespawn();

            // 3. 重置显示状态
            go.SetActive(false);

            // 4. 归位到 Root 节点
            if (poolData.Root)
            {
                go.transform.SetParent(poolData.Root);
            }

            poolData.Stack.Push(item);
        }

        // =========================================================
        // 6. 释放池
        // =========================================================

        public void ReleasePool(string key)
        {
            if (_pools.TryGetValue(key, out AsakiPoolData poolData))
            {
                poolData.Dispose();
                _pools.Remove(key);
                ALog.Info($"[AsakiPool] Released pool: {key}");
            }
        }

        // =========================================================
        // 7. 生命周期销毁
        // =========================================================

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;

            foreach (var kvp in _pools)
            {
                kvp.Value.Dispose();
            }
            _pools.Clear();

            if (!_globalRoot)
                return;
            if (Application.isPlaying)
                Object.Destroy(_globalRoot.gameObject);
            else
                Object.DestroyImmediate(_globalRoot.gameObject);
            _globalRoot = null;
        }
    }
}
