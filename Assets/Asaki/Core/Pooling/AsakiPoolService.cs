using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Asaki.Core.Logging;
using Asaki.Core.Pooling.Interfaces;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Pooling
{
    /// <summary>
    /// Asaki 对象池服务实现
    /// 提供池的创建、管理、销毁和统计功能
    /// </summary>
    public class AsakiPoolService : IAsakiPoolService
    {
        private readonly Dictionary<string, IAsakiPoolBase> _pools =
            new Dictionary<string, IAsakiPoolBase>();
        private bool _isDisposed;

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
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

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
        /// 释放服务资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;

            ALog.Info($"[AsakiPool] Service disposing, cleaning up {_pools.Count} pools");

            foreach (var kvp in _pools)
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
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("AsakiPoolService has been disposed");
        }
    }
}
