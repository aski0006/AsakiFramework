using System;
using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Logging;
using Asaki.Core.Pooling.Interfaces;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Pooling
{
    /// <summary>
    /// 通用对象池实现
    /// 支持异步/同步创建、预热、对象验证、重复归还检测等功能
    /// </summary>
    public class AsakiGenericPool<T> : IAsakiPool<T>
        where T : class
    {
        private readonly Stack<T> _stack;
        private readonly HashSet<T> _activeObjects;
        private readonly IAsakiPoolObjectFactory<T> _factory;
        private readonly AsakiPoolStatistics _statistics;
        private readonly object _lock = new object();

        public string Key { get; }
        public AsakiPoolConfig Config { get; }
        public IAsakiPoolStatistics Statistics => _statistics;
        public Type ObjectType => typeof(T);
        private bool _isDisposed;

        public AsakiGenericPool(
            string key,
            IAsakiPoolObjectFactory<T> factory,
            AsakiPoolConfig poolConfig
        )
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            Config = poolConfig ?? AsakiPoolConfig.Default;

            int capacity = Config.InitialSize > 0 ? Config.InitialSize : 16;
            _stack = new Stack<T>(capacity);
            _activeObjects = Config.EnableCollectionCheck ? new HashSet<T>() : null;
            _statistics = new AsakiPoolStatistics { MaxSize = Config.MaxSize };
        }

        /// <summary>
        /// 异步预热池，分批创建对象避免卡顿
        /// </summary>
        public async UniTask PrewarmAsync(
            int count,
            int itemsPerFrame = 5,
            CancellationToken token = default(CancellationToken)
        )
        {
            ThrowIfDisposed();
            if (count <= 0)
                return;

            int batchCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (token.IsCancellationRequested)
                    break;

                try
                {
                    T obj = await _factory.CreateAsync(token);
                    if (obj == null)
                    {
                        ALog.Warn($"[AsakiPool] {Key} Factory returned null object during prewarm");
                        continue;
                    }

                    _factory.OnReturn(obj);
                    lock (_lock)
                    {
                        _stack.Push(obj);
                    }
                    _statistics.IncrementCreated();
                    batchCount++;

                    if (batchCount >= itemsPerFrame)
                    {
                        batchCount = 0;
                        await UniTask.Yield(PlayerLoopTiming.Update);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ALog.Error($"[AsakiPool] {Key} PrewarmAsync exception: {ex.Message}", ex);
                }
            }

            ALog.Info(
                $"[AsakiPool] {Key} Prewarm completed, created {_statistics.TotalCreated} objects"
            );
        }

        /// <summary>
        /// 异步获取对象
        /// </summary>
        public async UniTask<T> GetAsync(CancellationToken token = default(CancellationToken))
        {
            ThrowIfDisposed();
            _statistics.IncrementGet();

            // 尝试从池中获取可用对象
            T obj = TryGetFromPool();

            // 池中没有可用对象，创建新对象
            if (obj == null)
            {
                try
                {
                    obj = await CreateObjectAsync(token);
                    if (obj == null)
                    {
                        ALog.Warn($"[AsakiPool] {Key} Factory returned null object");
                        return null;
                    }
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
                catch (Exception ex)
                {
                    ALog.Error($"[AsakiPool] {Key} GetAsync exception: {ex.Message}", ex);
                    return null;
                }
            }

            // 记录活动对象（线程安全）
            if (_activeObjects != null)
            {
                lock (_lock)
                {
                    if (!_activeObjects.Add(obj))
                    {
                        ALog.Warn($"[AsakiPool] {Key} Duplicate object detected");
                    }
                }
            }

            // 触发获取回调
            try
            {
                _factory.OnGet(obj);
            }
            catch (Exception ex)
            {
                ALog.Error($"[AsakiPool] {Key} OnGet callback failed: {ex.Message}", ex);
            }

            return obj;
        }

        /// <summary>
        /// 同步获取对象 - 优先从池中获取，避免死锁
        /// </summary>
        public T Get()
        {
            ThrowIfDisposed();
            _statistics.IncrementGet();

            // 优先从池中获取（无需异步）
            T obj = TryGetFromPool();
            if (obj != null)
            {
                if (_activeObjects != null)
                {
                    lock (_lock)
                    {
                        _activeObjects.Add(obj);
                    }
                }

                try
                {
                    _factory.OnGet(obj);
                }
                catch (Exception ex)
                {
                    ALog.Error($"[AsakiPool] {Key} OnGet callback failed: {ex.Message}", ex);
                }
                return obj;
            }

            // 池为空，检查是否允许同步创建
            if (!Config.AllowSyncCreation)
            {
                ALog.Warn($"[AsakiPool] {Key} Sync creation not allowed, pool is empty");
                return null;
            }

            // 使用同步创建
            try
            {
                obj = _factory.CreateSync();
                if (obj != null)
                {
                    _statistics.IncrementCreated();

                    if (_activeObjects != null)
                    {
                        lock (_lock)
                        {
                            _activeObjects.Add(obj);
                        }
                    }

                    _factory.OnGet(obj);
                    return obj;
                }

                ALog.Warn($"[AsakiPool] {Key} Factory returned null object");
                return null;
            }
            catch (Exception ex)
            {
                ALog.Error($"[AsakiPool] {Key} Sync create failed: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 尝试从池中获取对象（线程安全）
        /// </summary>
        private T TryGetFromPool()
        {
            lock (_lock)
            {
                while (_stack.Count > 0)
                {
                    T candidate = _stack.Pop();

                    // 验证对象有效性
                    if (Config.EnableValidation && !_factory.Validate(candidate))
                    {
                        _factory.OnDestroy(candidate);
                        _statistics.IncrementDestroyed();
                        continue;
                    }

                    _statistics.AdjustInactive(-1);
                    return candidate;
                }
                return null;
            }
        }

        /// <summary>
        /// 异步创建对象（带超时支持）
        /// </summary>
        private async UniTask<T> CreateObjectAsync(CancellationToken token)
        {
            if (Config.OperationTimeout > 0)
            {
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(
                    token
                );
                cts.CancelAfter(TimeSpan.FromSeconds(Config.OperationTimeout));
                return await _factory.CreateAsync(cts.Token);
            }
            return await _factory.CreateAsync(token);
        }

        /// <summary>
        /// 归还对象到池
        /// </summary>
        public bool Return(T obj)
        {
            if (_isDisposed)
            {
                if (obj != null)
                {
                    try
                    {
                        _factory.OnDestroy(obj);
                    }
                    catch
                    { /* ignore */
                    }
                }
                return false;
            }

            if (obj == null)
            {
                ALog.Warn($"[AsakiPool] {Key} Null object returned");
                return false;
            }

            // 检查对象是否来自此池（线程安全）
            if (_activeObjects != null)
            {
                lock (_lock)
                {
                    if (!_activeObjects.Remove(obj))
                    {
                        ALog.Error(
                            $"[AsakiPool] {Key} Invalid object returned - not from this pool or already returned"
                        );
                        return false;
                    }
                }
            }

            // 验证对象有效性
            if (Config.EnableValidation && !_factory.Validate(obj))
            {
                ALog.Warn($"[AsakiPool] {Key} Object validation failed, destroying");
                _factory.OnDestroy(obj);
                _statistics.IncrementDestroyed();
                return false;
            }

            // 检查池是否已满（线程安全）
            lock (_lock)
            {
                if (Config.MaxSize > 0 && _stack.Count >= Config.MaxSize)
                {
                    ALog.Info(
                        $"[AsakiPool] {Key} Pool full ({_stack.Count}/{Config.MaxSize}), destroying object"
                    );
                    _factory.OnDestroy(obj);
                    _statistics.IncrementDestroyed();
                    return false;
                }

                // 执行归还回调并入池
                try
                {
                    _factory.OnReturn(obj);
                }
                catch (Exception ex)
                {
                    ALog.Error($"[AsakiPool] {Key} OnReturn callback failed: {ex.Message}", ex);
                }

                _stack.Push(obj);
                _statistics.IncrementReturn();
                return true;
            }
        }

        /// <summary>
        /// 清空池中所有对象（线程安全）
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();

            lock (_lock)
            {
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
                        ALog.Error(
                            $"[AsakiPool] {Key} OnDestroy callback failed: {ex.Message}",
                            ex
                        );
                    }
                }

                _statistics.AdjustInactive(-count);
                ALog.Info($"[AsakiPool] {Key} Cleared {count} objects");
            }
        }

        /// <summary>
        /// 收缩池到指定大小（线程安全）
        /// </summary>
        public void Shrink(int targetSize)
        {
            ThrowIfDisposed();
            if (targetSize < 0)
                targetSize = 0;

            lock (_lock)
            {
                int toRemove = _stack.Count - targetSize;
                if (toRemove <= 0)
                    return;

                for (int i = 0; i < toRemove; i++)
                {
                    T obj = _stack.Pop();
                    try
                    {
                        _factory.OnDestroy(obj);
                    }
                    catch (Exception ex)
                    {
                        ALog.Error(
                            $"[AsakiPool] {Key} OnDestroy callback failed: {ex.Message}",
                            ex
                        );
                    }
                }

                _statistics.AdjustInactive(-toRemove);
                ALog.Info($"[AsakiPool] {Key} Shrunk by {toRemove} objects");
            }
        }

        /// <summary>
        /// 释放池资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;

            Clear();

            if (_statistics.ActiveCount > 0)
            {
                ALog.Warn(
                    $"[AsakiPool] {Key} Disposed with {_statistics.ActiveCount} active objects"
                );
            }

            _activeObjects?.Clear();
            ALog.Info($"[AsakiPool] {Key} Disposed - {_statistics}");
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
