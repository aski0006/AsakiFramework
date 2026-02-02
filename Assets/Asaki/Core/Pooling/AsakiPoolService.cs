using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Asaki.Core.Logging;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.Simulation;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Core.Pooling
{
    /// <summary>
    /// Asaki 对象池服务实现
    /// 提供池的创建、管理、销毁和统计功能
    /// 支持自动治理（Auto-Shrink & LRU Eviction）
    /// </summary>
    public class AsakiPoolService : IAsakiPoolService, IAsakiTickable
    {
        private readonly Dictionary<string, IAsakiPoolBase> _pools =
            new Dictionary<string, IAsakiPoolBase>();
        private bool _isDisposed;
        private bool _lowMemoryHandlerRegistered = false;
        private IAsakiSimulationService _simulationService;

        public AsakiPoolService(IAsakiSimulationService simulationService)
        {
            _simulationService =
                simulationService ?? throw new ArgumentNullException(nameof(simulationService));
            _simulationService.Register(this);
        }

        /// <summary>
        /// 创建对象池
        /// </summary>
        public async UniTask<IAsakiPool<T>> CreatePoolAsync<T>(
            string key,
            IAsakiPoolObjectFactory<T> factory,
            AsakiPoolConfig config = null,
            CancellationToken token = default(CancellationToken)
        )
            where T : class
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (_pools.ContainsKey(key))
            {
                throw new ArgumentException($"Pool '{key}' already exists", nameof(key));
            }

            var pool = new AsakiGenericPool<T>(key, factory, config ?? AsakiPoolConfig.Default);

            if (pool.Config.InitialSize > 0)
            {
                await pool.PrewarmAsync(pool.Config.InitialSize, token: token);
            }

            _pools[key] = pool;
            ALog.Info($"[AsakiPool] Service created pool '{key}' (type: {typeof(T).Name})");

            return pool;
        }

        /// <summary>
        /// 获取指定类型的池
        /// </summary>
        public IAsakiPool<T> GetPool<T>(string key)
            where T : class
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(key))
                return null;

            if (!_pools.TryGetValue(key, out IAsakiPoolBase poolBase))
                return null;

            if (poolBase is IAsakiPool<T> pool)
                return pool;

            ALog.Error(
                $"[AsakiPool] Service pool '{key}' type mismatch, expected {typeof(T).Name}, actual {poolBase.ObjectType.Name}"
            );
            return null;
        }

        /// <summary>
        /// 检查池是否存在
        /// </summary>
        public bool HasPool(string key)
        {
            ThrowIfDisposed();
            return !string.IsNullOrEmpty(key) && _pools.ContainsKey(key);
        }

        /// <summary>
        /// 销毁指定池
        /// </summary>
        public bool DestroyPool(string key)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(key) || !_pools.TryGetValue(key, out IAsakiPoolBase pool))
                return false;

            pool.Dispose();
            _pools.Remove(key);
            ALog.Info($"[AsakiPool] Service destroyed pool '{key}'");

            return true;
        }

        /// <summary>
        /// 获取统计信息摘要
        /// </summary>
        public string GetStatisticsSummary()
        {
            ThrowIfDisposed();

            if (_pools.Count == 0)
                return "[AsakiPool] Service: No active pools";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[AsakiPool] Service statistics (total: {_pools.Count} pools):");

            foreach (var kvp in _pools)
            {
                IAsakiPoolBase pool = kvp.Value;
                sb.AppendLine($"  [{kvp.Key}] Type: {pool.ObjectType.Name}, {pool.Statistics}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 注册低内存事件监听（应在服务初始化时调用）
        /// </summary>
        public void RegisterLowMemoryHandler()
        {
            if (_lowMemoryHandlerRegistered)
                return;

            Application.lowMemory += OnLowMemory;
            _lowMemoryHandlerRegistered = true;
            ALog.Info("[AsakiPool] Service registered low memory handler");
        }

        /// <summary>
        /// 注销低内存事件监听
        /// </summary>
        public void UnregisterLowMemoryHandler()
        {
            if (!_lowMemoryHandlerRegistered)
                return;

            Application.lowMemory -= OnLowMemory;
            _lowMemoryHandlerRegistered = false;
            ALog.Info("[AsakiPool] Service unregistered low memory handler");
        }

        /// <summary>
        /// 低内存事件处理 - 紧急收缩所有池
        /// </summary>
        private void OnLowMemory()
        {
            ALog.Warn("[AsakiPool] Low memory detected! Emergency shrinking all pools...");

            float currentTime = UnityEngine.Time.time;
            int totalRemoved = 0;

            foreach (var kvp in _pools)
            {
                if (kvp.Value is AsakiGenericPool<object> pool)
                {
                    // 使用反射调用泛型方法
                    totalRemoved += InvokeShrinkByLRU(kvp.Value, currentTime, force: true);
                }
            }

            ALog.Info(
                $"[AsakiPool] Emergency shrink completed, total removed: {totalRemoved} objects"
            );
        }

        /// <summary>
        /// 通过反射调用泛型池的 ShrinkByLRU 方法
        /// </summary>
        private int InvokeShrinkByLRU(IAsakiPoolBase poolBase, float currentTime, bool force)
        {
            try
            {
                var method = poolBase.GetType().GetMethod("ShrinkByLRU");
                if (method != null)
                {
                    var result = method.Invoke(poolBase, new object[] { currentTime, force });
                    return (int)result;
                }
            }
            catch (Exception ex)
            {
                ALog.Error($"[AsakiPool] Failed to invoke ShrinkByLRU: {ex.Message}");
            }
            return 0;
        }

        /// <summary>
        /// 执行所有池的治理检查（通过 Simulation 模块调用）
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_isDisposed || _pools.Count == 0)
                return;

            float currentTime = UnityEngine.Time.time;
            int totalRemoved = 0;

            foreach (var kvp in _pools)
            {
                try
                {
                    totalRemoved += InvokeShrinkByLRU(kvp.Value, currentTime, force: false);
                }
                catch (Exception ex)
                {
                    ALog.Error($"[AsakiPool] Governance failed for pool '{kvp.Key}': {ex.Message}");
                }
            }

            if (totalRemoved > 0)
            {
                ALog.Info(
                    $"[AsakiPool] Governance tick completed, total removed: {totalRemoved} objects"
                );
            }
        }

        /// <summary>
        /// 手动触发所有池的治理
        /// </summary>
        /// <param name="force">是否强制收缩到 KeepMinSize</param>
        /// <returns>总共销毁的对象数量</returns>
        public int PerformManualGovernance(bool force = false)
        {
            ThrowIfDisposed();

            float currentTime = UnityEngine.Time.time;
            int totalRemoved = 0;

            foreach (var kvp in _pools)
            {
                try
                {
                    totalRemoved += InvokeShrinkByLRU(kvp.Value, currentTime, force);
                }
                catch (Exception ex)
                {
                    ALog.Error(
                        $"[AsakiPool] Manual governance failed for pool '{kvp.Key}': {ex.Message}"
                    );
                }
            }

            ALog.Info(
                $"[AsakiPool] Manual governance completed, total removed: {totalRemoved} objects"
            );
            return totalRemoved;
        }

        /// <summary>
        /// 释放服务资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            // 注销低内存监听
            UnregisterLowMemoryHandler();

            ALog.Info($"[AsakiPool] Service disposing, cleaning up {_pools.Count} pools");

            foreach (KeyValuePair<string, IAsakiPoolBase> kvp in _pools)
            {
                try
                {
                    kvp.Value.Dispose();
                }
                catch (Exception ex)
                {
                    ALog.Error(
                        $"[AsakiPool] Service failed to destroy pool '{kvp.Key}': {ex.Message}",
                        ex
                    );
                }
            }

            _pools.Clear();
            _simulationService?.Unregister(this);
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("AsakiPoolService has been disposed");
            }
        }
    }
}
