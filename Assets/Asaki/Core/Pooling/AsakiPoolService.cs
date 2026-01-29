// 文件: Assets/Asaki/Core/Pooling/V2/Services/AsakiPoolServiceV2.cs

using Asaki.Core.Logging;
using Asaki.Core.Pooling.Interfaces;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Asaki.Core.Pooling
{
	/// <summary>
	/// Asaki 对象池服务 V2 实现（完全无反射）
	/// </summary>
	public class AsakiPoolService : IAsakiPoolService
	{
		// ✅ 改用 IPoolBase 存储
		private readonly Dictionary<string, IAsakiPoolBase> _pools = new Dictionary<string, IAsakiPoolBase>();
		private bool _isDisposed;

		// =========================================================
		// 创建池
		// =========================================================
		public async UniTask<IAsakiPool<T>> CreatePoolAsync<T>(
			string key,
			IAsakiPoolObjectFactory<T> factory,
			AsakiPoolConfig config = null,
			CancellationToken token = default
		) where T : class
		{
			ThrowIfDisposed();

			if (string.IsNullOrEmpty(key))
				throw new ArgumentException("Key cannot be null or empty", nameof(key));

			if (factory == null)
				throw new ArgumentNullException(nameof(factory));

			if (_pools.ContainsKey(key))
			{
				throw new ArgumentException($"The pool '{key}' already exists", nameof(key));
			}

			var pool = new AsakiGenericPool<T>(key, factory, config ?? AsakiPoolConfig.Default);

			if (pool.Config.InitialSize > 0)
			{
				await pool.PrewarmAsync(pool.Config.InitialSize, token: token);
			}

			// ✅ 存储为 IPoolBase（无需类型转换）
			_pools[key] = pool;

			ALog.Info($"[AsakiPoolService] Create pool '{key}' (type: {typeof(T).Name})");

			return pool;
		}

		// =========================================================
		// 获取池
		// =========================================================
		public IAsakiPool<T> GetPool<T>(string key) where T : class
		{
			ThrowIfDisposed();

			if (string.IsNullOrEmpty(key))
				return null;

			if (!_pools.TryGetValue(key, out IAsakiPoolBase poolBase))
				return null;

			// ✅ 类型安全转换（编译时检查）
			if (poolBase is IAsakiPool<T> pool)
				return pool;

			ALog.Error($"[AsakiPoolService] Pool '{key}' type mismatch, expected {typeof(T).Name}, actual {poolBase.ObjectType.Name}");
			return null;
		}

		// =========================================================
		// 检查存在
		// =========================================================
		public bool HasPool(string key)
		{
			ThrowIfDisposed();
			return !string.IsNullOrEmpty(key) && _pools.ContainsKey(key);
		}

		// =========================================================
		// 销毁池
		// =========================================================
		public bool DestroyPool(string key)
		{
			ThrowIfDisposed();

			if (string.IsNullOrEmpty(key) || !_pools.TryGetValue(key, out IAsakiPoolBase pool))
				return false;

			pool.Dispose();
			_pools.Remove(key);
			ALog.Info($"[AsakiPoolService] Destroy pool '{key}'");

			return true;
		}

		// =========================================================
		// 统计信息（✅ 无反射）
		// =========================================================
		public string GetStatisticsSummary()
		{
			ThrowIfDisposed();

			if (_pools.Count == 0)
				return "[AsakiPoolService] Inactive pool";

			var sb = new StringBuilder();
			sb.AppendLine($"[AsakiPoolService] Pool statistics (total: {_pools.Count}):");

			// ✅ 直接访问 IPoolBase 成员，无需反射
			foreach (var kvp in _pools)
			{
				IAsakiPoolBase pool = kvp.Value;
				sb.AppendLine($"  [{kvp.Key}] Type ={pool.ObjectType.Name} {pool.Statistics}");
			}

			return sb.ToString();
		}

		// =========================================================
		// 销毁服务
		// =========================================================
		public void Dispose()
		{
			if (_isDisposed) return;
			_isDisposed = true;

			ALog.Info($"[AsakiPoolService]Destroy the service and clean up {_pools.Count} pools");

			foreach (var kvp in _pools)
			{
				try
				{
					kvp.Value.Dispose();
				}
				catch (Exception ex)
				{
					ALog.Error($"[AsakiPoolService]Failure to destroy pool '{kvp.Key}' : {ex.Message}", ex);
				}
			}

			_pools.Clear();
		}

		private void ThrowIfDisposed()
		{
			if (_isDisposed)
				throw new ObjectDisposedException("AsakiPoolServiceV2 has been disposed");
		}
	}
}
