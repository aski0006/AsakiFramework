using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asaki.Core.Async;
using Asaki.Core.Broker;
using Asaki.Core.Logging;
using Asaki.Core.Resources; // 引用 Phase 1 定义的资源模块
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Core.Pooling
{
    /// <summary>
    /// [Asaki Native] 对象池服务实现 (Skeleton Phase)
    /// </summary>
    public class AsakiPoolService : IAsakiPoolService, IDisposable
    {
        // =========================================================
        // 1. 依赖与状态
        // =========================================================

        // 强依赖：分帧生成服务
        private readonly IAsakiAsyncService _asyncService;

        // 强依赖：资源加载服务
        private readonly IAsakiResourceService _resourceService;

        // 强依赖：事件服务
        private readonly IAsakiEventService _eventService;

        // 核心存储：Key -> PoolData (包含 Handle + Stack)
        // PoolData 是我们在 Phase 1 定义的 internal 类
        private readonly Dictionary<string, PoolData> _pools = new Dictionary<string, PoolData>();

        // 全局根节点：所有具体 Pool 的父节点
        private Transform _globalRoot;
        private const string GLOBAL_ROOT_NAME = "[Asaki.Pool.Service]";

        // 销毁标志位
        private bool _isDisposed = false;

        // =========================================================
        // 2. 构造与初始化 (DI & Root Setup)
        // =========================================================

        /// <summary>
        /// 构造函数由 Bootstrapper 或 Module 手动注入依赖
        /// </summary>
        public AsakiPoolService(
            IAsakiAsyncService asyncService,
            IAsakiResourceService resourceService,
            IAsakiEventService eventService
        )
        {
            // 守卫子句：确保依赖不为空
            _asyncService = asyncService ?? throw new ArgumentNullException(nameof(asyncService));
            _resourceService =
                resourceService ?? throw new ArgumentNullException(nameof(resourceService));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));

            InitializeGlobalRoot();
        }

        private void InitializeGlobalRoot()
        {
            // 创建一个持久的根节点
            GameObject go = new GameObject(GLOBAL_ROOT_NAME);
            Object.DontDestroyOnLoad(go);
            _globalRoot = go.transform;
        }

        // =========================================================
        // 3. 异步预热 (Phase 3 Core)
        // =========================================================

        public async Task PrewarmAsync(string key, int count, int itemsPerFrame = 5)
        {
            // 1. 基础守卫
            if (_isDisposed)
                return;
            if (string.IsNullOrEmpty(key))
                return;

            PoolData poolData;

            // 2. 检查池是否已存在
            // 如果池子已经存在，说明资源肯定加载过了，我们只需要补充数量即可
            if (!_pools.TryGetValue(key, out poolData))
            {
                // [Step A] 资源加载
                // 调用资源服务，异步获取句柄。这里我们使用 default(CancellationToken)
                // 如果你的业务需要取消预热，可以在接口中增加 Token 参数
                var handle = await _resourceService.LoadAsync<GameObject>(
                    key,
                    CancellationToken.None
                );

                // 校验资源有效性
                if (handle == null || !handle.IsValid)
                {
                    Debug.LogError($"[AsakiPool] Failed to load resource: {key}. Prewarm aborted.");
                    return;
                }

                // [Step B] 创建容器
                // 创建一个新的 GameObject 作为这个池的根节点
                GameObject rootGo = new GameObject($"Pool_{key}");
                rootGo.transform.SetParent(_globalRoot); // 挂在全局根下

                // 组装 PoolData (我们在 Phase 1 定义的)
                poolData = new PoolData(handle, rootGo.transform, count);

                // 注册到字典
                _pools.Add(key, poolData);
            }

            // 3. 计算需要生成的数量
            // 如果池里已经有 5 个，目标是 10 个，我们只需要生成 5 个
            // 如果目标比现有少，我们不做缩减操作 (通常由清理策略负责)
            int currentCount = poolData.Stack.Count;
            int needToSpawn = count - currentCount;

            if (needToSpawn <= 0)
                return;

            // [Step C] 分帧实例化 (利用 Asaki Native 桥接技术)
            // 我们将耗时的 Instantiate 操作委托给 RoutineService，避免卡死主线程
            await _asyncService.RunTask(async () =>
            {
                int batchCount = 0;
                GameObject prefab = poolData.PrefabHandle.Asset; // 从句柄中取出 Prefab

                for (int i = 0; i < needToSpawn; i++)
                {
                    // 再次检查销毁状态 (防止在 await 期间服务被 Dispose)
                    if (_isDisposed)
                        break;

                    // 实例化并挂载到 PoolRoot
                    GameObject go = Object.Instantiate(prefab, poolData.Root);
                    go.SetActive(false); // 默认隐藏

                    // 封装并入栈
                    PoolItem item = new PoolItem(go);
                    poolData.Stack.Push(item);

                    // 分帧控制：每生成 N 个等待一帧
                    batchCount++;
                    if (batchCount >= itemsPerFrame)
                    {
                        batchCount = 0;
                        await _asyncService.WaitFrame();
                    }
                }
            });

            // 预热完成
            // Debug.Log($"[AsakiPool] Prewarmed {key}: +{needToSpawn} items.");
        }

        // =========================================================
        // 4. 运行时操作 (Phase 4 Core)
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

            // 1. 核心检查：必须预热！
            // 这一步体现了"强制异步"策略。如果 Key 没在字典里，说明没有预热，
            // 而我们无法在这里进行异步加载(因为 Spawn 是同步的)，所以只能报错。
            if (!_pools.TryGetValue(key, out PoolData poolData))
            {
                ALog.Error(
                    $"[AsakiPool] Key not prewarmed: '{key}'. \n"
                        + "Solution: Call 'await PrewarmAsync(\"{key}\", ...)' during game initialization."
                );
                return null;
            }

            PoolItem item = null;

            // 2. 尝试出栈 (Hit Cache)
            while (poolData.Stack.Count > 0)
            {
                PoolItem popped = poolData.Stack.Pop();
                // 保护性检查：防止对象在外部被 Destroy 导致引用丢失
                if (popped.GameObject != null)
                {
                    item = popped;
                    break;
                }
            }

            // 3. 栈空补货 (Miss Cache)
            // 能够执行到这里，说明 PrefabHandle 肯定有效 (否则 poolData 不会存在)
            if (item == null)
            {
                // 直接同步实例化，因为资源已在内存
                GameObject go = Object.Instantiate(poolData.PrefabHandle.Asset, poolData.Root);
                item = new PoolItem(go);

                // [可选] 如果你想统计"池扩容"次数，可以在这里打点
            }

            // 4. 设置变换信息
            Transform t = item.Transform;
            // 这里的逻辑：如果传入 parent，挂过去；如果没传，设为 null (置于场景根节点)
            // 注意：不要挂在 poolData.Root 下，激活的对象应该在外界
            t.SetParent(parent ? parent : null);

            if (position.HasValue)
                t.position = position.Value;
            if (rotation.HasValue)
                t.rotation = rotation.Value;

            // 5. 激活与生命周期
            item.GameObject.SetActive(true);

            // 调用接口回调 (0 GC，因为我们在 PoolItem 构造时缓存了接口)
            item.AsakiPoolable?.OnSpawn();

            item.LastActiveTime = UnityEngine.Time.time;

            return item.GameObject;
        }

        public void Despawn(GameObject go, string key)
        {
            if (_isDisposed || !go)
                return;

            // 1. 查找池
            if (!_pools.TryGetValue(key, out PoolData poolData))
            {
                ALog.Warn(
                    $"[AsakiPool] Despawn target pool '{key}' not found. Destroying object directly."
                );
                Object.Destroy(go);
                return;
            }

            // 2. 触发回调
            // 必须在 SetActive(false) 之前调用，方便逻辑处理
            if (go.TryGetComponent<IAsakiPoolable>(out IAsakiPoolable poolable))
            {
                poolable.OnDespawn();
            }

            // 3. 重置状态
            go.SetActive(false);

            // 4. 归位
            // 将对象重新挂载到该池的 Root 下，保持 Hierarchy 干净
            if (poolData.Root)
            {
                go.transform.SetParent(poolData.Root);
            }

            // 5. 入栈
            // 重新包装一个新的 PoolItem (或者复用旧的？为了避免复杂性，这里 new 一个 struct 开销极小)
            // 注意：我们在 PoolItem 构造函数里缓存了组件，这里 new 一次会导致再次 GetComponent。
            // 优化点：如果我们要极致性能，Spawn 时返回的是 PoolItem 引用而不是 GO，Despawn 传回 PoolItem。
            // 但为了 API 易用性 (GameObject)，这里重新 new PoolItem 是可接受的权衡。
            poolData.Stack.Push(new PoolItem(go));
        }

        public void ReleasePool(string key)
        {
            if (_pools.TryGetValue(key, out PoolData poolData))
            {
                // 这将触发 PoolData.Dispose -> ResHandle.Dispose -> 引用计数减一
                poolData.Dispose();
                _pools.Remove(key);
                Debug.Log($"[AsakiPool] Released pool: {key}");
            }
        }

        // =========================================================
        // 4. 生命周期销毁
        // =========================================================

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;

            // 1. 释放所有池数据 (这也将释放所有 ResHandle)
            foreach (var kvp in _pools)
            {
                kvp.Value.Dispose();
            }
            _pools.Clear();

            // 2. 销毁全局根节点
            if (_globalRoot != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(_globalRoot.gameObject);
                else
                    Object.DestroyImmediate(_globalRoot.gameObject);
                _globalRoot = null;
            }
        }
    }
}
