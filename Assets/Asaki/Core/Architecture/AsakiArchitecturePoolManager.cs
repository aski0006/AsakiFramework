using System;
using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Context;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.Logging;
using Asaki.Core.Pooling;
using Asaki.Core.Pooling.Factories;
using Asaki.Core.Pooling.Interfaces;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Architecture
{
    /// <summary>
    /// 架构对象池管理器 - 统一管理 Command 和 Query 对象池
    /// 基于 IAsakiPoolService 实现，提供统计、验证等高级功能
    /// </summary>
    public static class AsakiArchitecturePoolManager
    {
        private static IAsakiPoolService _poolService;
        private static readonly object _initLock = new object();
        private static bool _isInitialized;

        /// <summary>
        /// 初始化池服务 (延迟初始化)
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            lock (_initLock)
            {
                if (_isInitialized)
                {
                    return;
                }
                _isInitialized = AsakiContext.TryGet(out _poolService);
                ALog.Info("[AsakiArchitecturePool] Architecture pool manager initialized");
            }
        }

        /// <summary>
        /// 租借 Command/Query 对象
        /// </summary>
        public static async UniTask<T> RentAsync<T>(CancellationToken token = default)
            where T : class, new()
        {
            EnsureInitialized();

            string poolKey = GetPoolKey<T>();
            IAsakiPool<T> pool = _poolService.GetPool<T>(poolKey);

            // 懒加载：首次使用时创建池
            if (pool == null)
            {
                pool = await CreatePoolAsync<T>(poolKey, token);
            }

            return await pool.GetAsync(token);
        }

        /// <summary>
        /// 同步租借 (优先使用异步版本)
        /// </summary>
        public static T Rent<T>()
            where T : class, new()
        {
            EnsureInitialized();

            string poolKey = GetPoolKey<T>();
            IAsakiPool<T> pool = _poolService.GetPool<T>(poolKey);

            // 如果池不存在，创建新实例 (不推荐，应使用 RentAsync)
            if (pool == null)
            {
                ALog.Warn(
                    $"[AsakiArchitecturePool] Pool for {typeof(T).Name} not initialized, creating new instance. Consider using RentAsync."
                );
                return new T();
            }

            return pool.Get();
        }

        /// <summary>
        /// 尝试从池租借对象，如果池不存在或池为空则返回 false
        /// 用于非预热类型的 Command/Query，此时应使用 new 创建
        /// 关键：即使池存在，如果池为空也不从工厂创建对象，确保行为可预测
        /// </summary>
        public static bool TryRent<T>(out T obj)
            where T : class, new()
        {
            obj = null;

            if (!_isInitialized)
                return false;

            string poolKey = GetPoolKey<T>();
            IAsakiPool<T> pool;

            try
            {
                pool = _poolService.GetPool<T>(poolKey);
            }
            catch (ObjectDisposedException)
            {
                return false;
            }

            if (pool == null)
                return false;

            // 关键：检查池中是否有可用对象
            // 使用 Statistics.InactiveCount（池内可用对象数量）
            // 如果池为空，则返回 false，让调用方使用 new
            if (pool.Statistics.InactiveCount <= 0)
                return false;

            obj = pool.Get();
            return obj != null;
        }

        /// <summary>
        /// 异步尝试从池租借对象
        /// </summary>
        public static async UniTask<(bool success, T obj)> TryRentAsync<T>(
            CancellationToken token = default
        )
            where T : class, new()
        {
            EnsureInitialized();

            string poolKey = GetPoolKey<T>();
            IAsakiPool<T> pool;

            try
            {
                pool = _poolService.GetPool<T>(poolKey);
            }
            catch (ObjectDisposedException)
            {
                return (false, null);
            }

            if (pool == null)
                return (false, null);

            // 关键：检查池中是否有可用对象
            if (pool.Statistics.InactiveCount <= 0)
                return (false, null);

            T obj = await pool.GetAsync(token);
            return (true, obj);
        }

        /// <summary>
        /// 归还对象到池
        /// </summary>
        public static bool Return<T>(T obj)
            where T : class
        {
            if (!_isInitialized || obj == null || _poolService == null)
                return false;

            string poolKey = GetPoolKey<T>();
            IAsakiPool<T> pool;

            try
            {
                pool = _poolService.GetPool<T>(poolKey);
            }
            catch (ObjectDisposedException)
            {
                // 池服务已被处置，对象将由 GC 回收
                return false;
            }

            if (pool == null)
            {
                ALog.Warn(
                    $"[AsakiArchitecturePool] Pool for {typeof(T).Name} not found, object will be GC'd"
                );
                return false;
            }

            return pool.Return(obj);
        }

        /// <summary>
        /// 尝试归还对象到池，如果池不存在则直接丢弃（由 GC 回收）
        /// 用于非预热类型的 Command/Query
        /// </summary>
        public static void TryReturn<T>(T obj)
            where T : class
        {
            if (!_isInitialized || obj == null || _poolService == null)
                return;

            string poolKey = GetPoolKey<T>();
            IAsakiPool<T> pool;

            try
            {
                pool = _poolService.GetPool<T>(poolKey);
            }
            catch (ObjectDisposedException)
            {
                // 池服务已被处置，对象将由 GC 回收
                return;
            }

            // 池不存在时不记录警告，直接让 GC 回收
            if (pool == null)
                return;

            pool.Return(obj);
        }

        /// <summary>
        /// 创建类型专用池
        /// </summary>
        private static async UniTask<IAsakiPool<T>> CreatePoolAsync<T>(
            string poolKey,
            CancellationToken token
        )
            where T : class, new()
        {
            // 创建轻量级对象工厂
            var factory = new DelegateFactory<T>(
                createSync: () => new T(),
                onReturn: obj =>
                {
                    // 自动重置 (如果实现了 IAsakiResettable)
                    if (obj is IAsakiResettable resettable)
                    {
                        resettable.Reset();
                    }
                },
                validate: obj => obj != null
            );

            // 配置：轻量级对象，大容量，无预热（从全局配置获取）
            var globalConfig = AsakiPoolGlobalConfig.Instance;
            var config = new AsakiPoolConfig
            {
                InitialSize = globalConfig.ArchitecturePoolInitialSize,
                MaxSize = globalConfig.ArchitecturePoolMaxSize,
                EnableValidation = globalConfig.ArchitecturePoolEnableValidation,
                EnableCollectionCheck = globalConfig.ArchitecturePoolEnableCollectionCheck,
                AllowSyncCreation = globalConfig.ArchitecturePoolAllowSyncCreation,
                OperationTimeout = 0f,
            };

            var pool = await _poolService.CreatePoolAsync(poolKey, factory, config, token);
            ALog.Info($"[AsakiArchitecturePool] Created pool for {typeof(T).Name}");

            return pool;
        }

        /// <summary>
        /// 获取类型专用池键
        /// </summary>
        private static string GetPoolKey<T>()
        {
            return $"Architecture_{typeof(T).FullName}";
        }

        /// <summary>
        /// 清空所有池 (场景切换时调用)
        /// 只清空 Architecture 管理的池，不影响其他系统使用的池服务
        /// </summary>
        public static void ClearAll()
        {
            if (!_isInitialized || _poolService == null)
                return;

            // 获取所有 Architecture 相关的池键
            var architecturePoolKeys = new List<string>();
            foreach (var key in _poolService.GetAllPoolKeys())
            {
                if (key.StartsWith("Architecture_", StringComparison.Ordinal))
                {
                    architecturePoolKeys.Add(key);
                }
            }

            // 只处置 Architecture 相关的池
            foreach (var key in architecturePoolKeys)
            {
                try
                {
                    _poolService.DestroyPool(key);
                }
                catch (Exception ex)
                {
                    ALog.Error(
                        $"[AsakiArchitecturePool] Failed to destroy pool '{key}': {ex.Message}"
                    );
                }
            }

            _poolService = null;
            _isInitialized = false;

            ALog.Info(
                $"[AsakiArchitecturePool] Cleared {architecturePoolKeys.Count} architecture pools"
            );
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public static string GetStatistics()
        {
            if (!_isInitialized)
                return "[AsakiArchitecturePool] Not initialized";
            return _poolService.GetStatisticsSummary();
        }
    }
}
