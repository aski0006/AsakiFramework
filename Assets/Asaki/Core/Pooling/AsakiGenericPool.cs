using Asaki.Core.Logging;
using Asaki.Core.Pooling.Interfaces;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Asaki.Core.Pooling
{
	public class AsakiGenericPool<T> : IAsakiPool<T> where T : class
	{
		private readonly Stack<T> _stack;
		private readonly HashSet<T> _activeObjects; // 用于重复归还检测
		private readonly IAsakiPoolObjectFactory<T> _factory;
		private readonly AsakiPoolStatistics _statistics;

		public string Key { get; }
		public AsakiPoolConfig Config { get; }
		public IAsakiPoolStatistics Statistics => _statistics;
		public Type ObjectType => typeof(T);
		private bool _isDisposed;

		public AsakiGenericPool(string key, IAsakiPoolObjectFactory<T> factory, AsakiPoolConfig poolConfig)
		{
			Key = key ?? throw new ArgumentNullException(nameof(key));
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			Config = poolConfig ?? AsakiPoolConfig.Default;

			int capacity = Config.InitialSize > 0 ? Config.InitialSize : 16;
			_stack = new Stack<T>(capacity);
			_activeObjects = Config.EnableCollectionCheck
				? new HashSet<T>()
				: null;
			_statistics = new AsakiPoolStatistics { MaxSize = Config.MaxSize };
		}

		public async UniTask PrewarmAsync(int count, int itemsPerFrame = 5, CancellationToken token = default)
		{
			ThrowIfDisposed();
			if (count <= 0) return;
			int batchCount = 0;
			for (int i = 0; i < count; i++)
			{
				if (token.IsCancellationRequested) break;
				try
				{
					T obj = await _factory.CreateAsync(token);
					if (obj == null)
					{
						ALog.Warn($"[AsakiPool] {Key} Factory return a Null Object");
						continue;
					}
					_factory.OnReturn(obj);
					_stack.Push(obj);
					_statistics.IncrementCreated();
					batchCount++;
					if (batchCount >= itemsPerFrame)
					{
						batchCount = 0;
						await UniTask.Yield(PlayerLoopTiming.Update);
					}
				}
				catch (OperationCanceledException) { break; }
				catch (Exception ex)
				{
					ALog.Error($"[AsakiPool] {Key} PrewarmAsync Exception: {ex}");
				}
				ALog.Info($"[AsakiPool] {Key} PrewarmAsync Count: {batchCount}");
			}
		}

		public async UniTask<T> GetAsync(CancellationToken token = default)
		{
			ThrowIfDisposed();
			_statistics.IncrementGet();
			T obj = null;
			while (_stack.Count > 0)
			{
				T candidate = _stack.Pop();
				if (Config.EnableValidation && !_factory.Validate(candidate))
				{
					_factory.OnDestroy(candidate);
					_statistics.IncrementDestroyed();
					continue;
				}
				obj = candidate;
				break;
			}
			if (obj == null)
			{
				try
				{
					obj = await _factory.CreateAsync(token);
					if (obj == null)
					{
						ALog.Warn($"[AsakiPool] {Key} Factory return a Null Object");
						return null;
					}
					_statistics.IncrementCreated();
				}
				catch (OperationCanceledException) { }
				catch (Exception ex)
				{
					ALog.Error($"[AsakiPool] {Key} GetAsync Exception: {ex}");
					return null;
				}
			}
			else
			{
				_statistics.AdjustInactive(-1);
			}
			if (_activeObjects != null)
			{
				if (!_activeObjects.Add(obj))
				{
					ALog.Warn($"[AsakiPool] {Key} Duplicate Object Returned");
				}
			}
			try
			{
				_factory.OnGet(obj);
			}
			catch (Exception ex)
			{
				ALog.Error($"[AsakiPool] {Key} OnGet Exception: {ex}");
			}
			return obj;
		}

		public T Get()
		{
			ThrowIfDisposed();
			if (_stack.Count > 0)
			{
				var task = GetAsync();
				if (task.Status == UniTaskStatus.Succeeded)
				{
					return task.GetAwaiter().GetResult();
				}
			}
			if (!Config.AllowSyncCreation)
			{
				ALog.Warn($"[AsakiPool] {Key} Sync Creation Not Allowed");
				return null;
			}
			ALog.Info($"[AsakiPool] {Key} Sync Creation");
			try
			{
				var task = _factory.CreateAsync();
				T obj = task.GetAwaiter().GetResult();
				if (obj != null)
				{
					_statistics.IncrementCreated();
					_statistics.IncrementGet();
					if (_activeObjects != null)
						_activeObjects.Add(obj);
					_factory.OnGet(obj);
					return obj;
				}
				ALog.Warn($"[AsakiPool] {Key} Factory return a Null Object");
				return null;
			}
			catch (Exception e)
			{
				ALog.Error($"[AsakiPool] {Key} Sync Create Failed, Exception: {e}");
				return null;
			}
		}

		public bool Return(T obj)
		{
			if (_isDisposed)
			{
				if (obj != null)
				{
					try { _factory.OnDestroy(obj); }
					catch
					{ /* ignore */
					}
				}
				return false;
			}
			if (obj == null)
			{
				ALog.Warn($"[AsakiPool] {Key} Null Object Returned");
				return false;
			}
			if (_activeObjects != null)
			{
				if (!_activeObjects.Remove(obj))
				{
					ALog.Error($"[AsakiPool] {Key} Invalid Object Returned, The object is not removed from the pool or has been returned, refused to be returned");
					return false;
				}
			}
			if (Config.EnableValidation && !_factory.Validate(obj))
			{
				ALog.Warn($"[AsakiPool][{Key}] Object validation failed, object is destroyed");
				_factory.OnDestroy(obj);
				_statistics.IncrementDestroyed();
				return false;
			}

			if (Config.MaxSize > 0 && _stack.Count >= Config.MaxSize)
			{
				ALog.Info($"[AsakiPool][{Key}] The pool is full ({_stack.Count}/{Config.MaxSize})，Destroying object");
				_factory.OnDestroy(obj);
				_statistics.IncrementDestroyed();
				return false;
			}
			try
			{
				_factory.OnReturn(obj);
			}
			catch (Exception ex)
			{
				ALog.Error($"[AsakiPool][{Key}] OnReturn Callback failure: {ex.Message}", ex);
			}
			_stack.Push(obj);
			_statistics.IncrementReturn();

			return true;

		}

		public void Clear()
		{
			ThrowIfDisposed();
			int count = _stack.Count;
			while (_stack.Count > 0)
			{
				T obj = _stack.Pop();
				try
				{
					_factory.OnDestroy(obj);
				}
				catch (Exception ex)
				{
					ALog.Error($"[AsakiPool][{Key}] OnDestroy Callback failure: {ex.Message}", ex);
				}
			}
			_statistics.AdjustInactive(-count);
			ALog.Info($"[AsakiPool][{Key}] Clear {count} Objects");
		}

		public void Shrink(int targetSize)
		{
			ThrowIfDisposed();
			if (targetSize < 0) targetSize = 0;
			int toRemove = _stack.Count - targetSize;
			if (toRemove <= 0) return;
			for (int i = 0; i < toRemove; i++)
			{
				T obj = _stack.Pop();
				try
				{
					_factory.OnDestroy(obj);
				}
				catch (Exception ex)
				{
					ALog.Error($"[AsakiPool][{Key}] OnDestroy Callback failure: {ex.Message}", ex);
				}
			}
			_statistics.AdjustInactive(-toRemove);
			ALog.Info($"[AsakiPool][{Key}] Shrink {toRemove} Objects");
		}
		public void Dispose()
		{
			if (_isDisposed) return;
			_isDisposed = true;
			Clear();
			if (_statistics.ActiveCount > 0)
			{
				ALog.Warn($"[AsakiPool][{Key}] Dispose, {_statistics.ActiveCount} Objects are still active");
			}
			_activeObjects?.Clear();
			ALog.Info($"[AsakiPool][{Key}] Dispose - {_statistics}");
		}

		private void ThrowIfDisposed()
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException($"[AsakiPool] {Key} has been disposed");
			}
		}
	}
}
