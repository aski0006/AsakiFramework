using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asaki.Core.Async;
using Asaki.Core.Logging;
using Asaki.Core.Resources;
using Asaki.Unity.Extensions;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Unity.Services.Resources
{
    /// <summary>
    /// Asaki资源加载服务核心实现
    /// <para>负责资源加载、缓存管理、引用计数和依赖处理。</para>
    /// <para>采用策略模式支持多种底层加载方式（Resources/Addressables/自定义）。</para>
    /// </summary>
    /// <remarks>
    /// <para>核心特性：</para>
    /// <list type="bullet">
    /// <item><description>线程安全：使用ConcurrentDictionary + 记录级锁实现高并发支持</description></item>
    /// <item><description>引用计数：自动管理资源生命周期，支持依赖级联释放</description></item>
    /// <item><description>异步加载：基于UniTask的异步加载，支持取消和超时</description></item>
    /// <item><description>进度回调：支持单个和批量加载的进度反馈</description></item>
    /// </list>
    /// </remarks>
    public class AsakiResourceService : IAsakiResourceService
    {
        private readonly IAsakiResStrategy _strategy;
        private readonly IAsakiAsyncService _asyncService;
        private readonly IAsakiResDependencyLookup _dependencyLookup;

        private const int DefaultTimeoutMs = 30000;
        private const int MinTimeoutMs = 1000;
        private const int SegmentCount = 16;

        private int _timeoutMs = DefaultTimeoutMs;
        private readonly ConcurrentDictionary<int, ResRecord> _cache;
        private readonly object[] _segmentLocks;

        /// <summary>
        /// 资源记录内部类
        /// <para>存储单个资源的加载状态、引用计数和依赖关系。</para>
        /// </summary>
        private sealed class ResRecord
        {
            public readonly string Location;
            public readonly Type AssetType;
            public readonly int CacheKey;
            public readonly object SyncRoot = new object();

            private Object _asset;
            private int _refCount;
            private TaskCompletionSource<Object> _loadingTcs;
            private HashSet<int> _dependencyKeys;
            private Action<float> _progressCallbacks;

            public ResRecord(string location, Type assetType, int cacheKey)
            {
                Location = location;
                AssetType = assetType;
                CacheKey = cacheKey;
                _loadingTcs = new TaskCompletionSource<Object>(
                    TaskCreationOptions.RunContinuationsAsynchronously
                );
            }

            public Object Asset
            {
                get => _asset;
                set => _asset = value;
            }

            public int RefCount
            {
                get => Volatile.Read(ref _refCount);
                set => Volatile.Write(ref _refCount, value);
            }

            public TaskCompletionSource<Object> LoadingTcs => _loadingTcs;

            public HashSet<int> DependencyKeys
            {
                get
                {
                    if (_dependencyKeys == null)
                        Interlocked.CompareExchange(ref _dependencyKeys, new HashSet<int>(), null);
                    return _dependencyKeys;
                }
            }

            public Action<float> ProgressCallbacks
            {
                get => _progressCallbacks;
                set => _progressCallbacks = value;
            }

            public void IncrementRefCount()
            {
                Interlocked.Increment(ref _refCount);
            }

            public int DecrementRefCount()
            {
                return Interlocked.Decrement(ref _refCount);
            }

            public void ReportProgress(float progress)
            {
                var handlers = _progressCallbacks;
                if (handlers == null)
                    return;

                foreach (var handler in handlers.GetInvocationList())
                {
                    try
                    {
                        ((Action<float>)handler).Invoke(progress);
                    }
                    catch (Exception ex)
                    {
                        ALog.Error($"[Resources] Progress callback failed: {ex.Message}");
                    }
                }
            }

            public void Reset()
            {
                lock (SyncRoot)
                {
                    _asset = null;
                    _refCount = 0;
                    _loadingTcs = new TaskCompletionSource<Object>(
                        TaskCreationOptions.RunContinuationsAsynchronously
                    );
                    _dependencyKeys?.Clear();
                    _progressCallbacks = null;
                }
            }
        }

        /// <summary>
        /// 初始化资源服务实例
        /// </summary>
        /// <param name="strategy">资源加载策略实现</param>
        /// <param name="asyncService">异步驱动服务</param>
        /// <param name="dependencyLookup">依赖查询服务</param>
        public AsakiResourceService(
            IAsakiResStrategy strategy,
            IAsakiAsyncService asyncService,
            IAsakiResDependencyLookup dependencyLookup
        )
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _asyncService = asyncService ?? throw new ArgumentNullException(nameof(asyncService));
            _dependencyLookup = dependencyLookup ?? throw new ArgumentNullException(nameof(dependencyLookup));

            _cache = new ConcurrentDictionary<int, ResRecord>();
            _segmentLocks = new object[SegmentCount];
            for (int i = 0; i < SegmentCount; i++)
            {
                _segmentLocks[i] = new object();
            }
        }

        private object GetSegmentLock(int key)
        {
            return _segmentLocks[Math.Abs(key) % SegmentCount];
        }

        private static int GetCacheKey(string location, Type type)
        {
            if (type == null)
                type = typeof(Object);

            int hash = 17;
            hash = hash * 31 + (location?.GetHashCode() ?? 0);
            hash = hash * 31 + type.FullName.GetHashCode();
            return hash;
        }

        public UniTask OnInitAsync()
        {
            return _strategy.InitializeAsync();
        }

        public void OnInit() { }

        public void OnDispose()
        {
            var records = _cache.Values.ToList();
            _cache.Clear();

            foreach (var record in records)
            {
                if (record.Asset != null)
                {
                    try
                    {
                        _strategy.UnloadAssetInternal(record.Location, record.Asset);
                    }
                    catch (Exception ex)
                    {
                        ALog.Error($"[Resources] Failed to unload asset '{record.Location}': {ex.Message}");
                    }
                }
            }
        }

        public void SetTimeoutSeconds(int timeoutSeconds)
        {
            _timeoutMs = Math.Max(MinTimeoutMs, timeoutSeconds * 1000);
        }

        #region Load Operations

        public UniTask<ResHandle<T>> LoadAsync<T>(string location, CancellationToken token)
            where T : class
        {
            return LoadAsync<T>(location, null, token);
        }

        public async UniTask<ResHandle<T>> LoadAsync<T>(
            string location,
            Action<float> onProgress,
            CancellationToken token
        )
            where T : class
        {
            if (string.IsNullOrEmpty(location))
                throw new ArgumentNullException(nameof(location));

            var record = GetOrCreateRecord(location, typeof(T));
            bool needUnregister = false;

            if (onProgress != null)
            {
                if (record.Asset != null)
                {
                    onProgress(1f);
                }
                else
                {
                    lock (record.SyncRoot)
                    {
                        record.ProgressCallbacks += onProgress;
                    }
                    needUnregister = true;
                }
            }

            record.IncrementRefCount();

            try
            {
                var assetObj = await record.LoadingTcs.Task.WaitAsync(token);

                if (assetObj is T tAsset)
                {
                    return new ResHandle<T>(location, tAsset, this);
                }

                throw new InvalidCastException(
                    $"[Resources] Type mismatch for '{location}'. Expected {typeof(T).Name}, got {assetObj?.GetType().Name ?? "null"}"
                );
            }
            catch (Exception)
            {
                ReleaseInternal(location, typeof(T));
                throw;
            }
            finally
            {
                if (needUnregister && onProgress != null)
                {
                    lock (record.SyncRoot)
                    {
                        record.ProgressCallbacks -= onProgress;
                    }
                }
            }
        }

        public async UniTask<ResHandle<Object>> LoadAsync(
            string location,
            Type type,
            Action<float> onProgress,
            CancellationToken token
        )
        {
            if (string.IsNullOrEmpty(location))
                throw new ArgumentNullException(nameof(location));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var record = GetOrCreateRecord(location, type);
            bool needUnregister = false;

            if (onProgress != null)
            {
                if (record.Asset != null)
                {
                    onProgress(1f);
                }
                else
                {
                    lock (record.SyncRoot)
                    {
                        record.ProgressCallbacks += onProgress;
                    }
                    needUnregister = true;
                }
            }

            record.IncrementRefCount();

            try
            {
                var assetObj = await record.LoadingTcs.Task.WaitAsync(token);
                return new ResHandle<Object>(location, assetObj, this);
            }
            catch (Exception)
            {
                ReleaseInternal(location, type);
                throw;
            }
            finally
            {
                if (needUnregister && onProgress != null)
                {
                    lock (record.SyncRoot)
                    {
                        record.ProgressCallbacks -= onProgress;
                    }
                }
            }
        }

        private ResRecord GetOrCreateRecord(string location, Type type)
        {
            int key = GetCacheKey(location, type);

            if (_cache.TryGetValue(key, out var existingRecord))
            {
                return existingRecord;
            }

            var newRecord = new ResRecord(location, type, key);
            var segmentLock = GetSegmentLock(key);

            lock (segmentLock)
            {
                if (_cache.TryGetValue(key, out existingRecord))
                {
                    return existingRecord;
                }

                _cache.TryAdd(key, newRecord);
                StartLoadTask(newRecord);
                return newRecord;
            }
        }

        private async void StartLoadTask(ResRecord record)
        {
            try
            {
                await LoadTaskInternal(record);
            }
            catch (OperationCanceledException)
            {
                record.LoadingTcs.TrySetCanceled();
                CleanupFailedRecord(record);
            }
            catch (Exception ex)
            {
                record.LoadingTcs.TrySetException(ex);
                CleanupFailedRecord(record);
            }
        }

        private async UniTask LoadTaskInternal(ResRecord record)
        {
            var deps = _dependencyLookup.GetDependencies(record.Location);

            if (deps != null && deps.Any())
            {
                await LoadDependenciesParallelAsync(record, deps);
            }

            var asset = await _asyncService.RunTask(
                async () => await _strategy.LoadAssetInternalAsync(
                    record.Location,
                    record.AssetType,
                    record.ReportProgress,
                    default
                ),
                default
            );

            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"[Resources] Asset not found: '{record.Location}' (Type: {record.AssetType.Name})"
                );
            }

            record.Asset = asset;
            record.LoadingTcs.TrySetResult(asset);
            record.ReportProgress(1f);
        }

        private async UniTask LoadDependenciesParallelAsync(ResRecord record, IEnumerable<string> dependencies)
        {
            var depList = dependencies.ToList();
            if (depList.Count == 0)
                return;

            var depRecords = new List<(ResRecord DepRecord, int DepKey)>();

            foreach (var depLoc in depList)
            {
                var depRecord = GetOrCreateRecord(depLoc, typeof(Object));
                depRecord.IncrementRefCount();

                lock (record.SyncRoot)
                {
                    record.DependencyKeys.Add(depRecord.CacheKey);
                }

                depRecords.Add((depRecord, depRecord.CacheKey));
            }

            var loadTasks = depRecords.Select(dep =>
                WaitForDependencyWithTimeoutAsync(dep.DepRecord, record.Location)
            );

            await UniTask.WhenAll(loadTasks);
        }

        private async UniTask WaitForDependencyWithTimeoutAsync(ResRecord depRecord, string parentLocation)
        {
            var loadTask = depRecord.LoadingTcs.Task.AsUniTask();
            var timeoutTask = UniTask.Delay(_timeoutMs);

            var (hasResult, _) = await UniTask.WhenAny(loadTask, timeoutTask);

            if (!hasResult)
            {
                throw new TimeoutException(
                    $"[Resources] Dependency '{depRecord.Location}' timeout while loading '{parentLocation}'"
                );
            }

            await loadTask;
        }

        private void CleanupFailedRecord(ResRecord record)
        {
            _cache.TryRemove(record.CacheKey, out _);

            var depKeys = record.DependencyKeys;
            if (depKeys != null && depKeys.Count > 0)
            {
                foreach (var depKey in depKeys)
                {
                    ReleaseInternalByKey(depKey);
                }
            }
        }

        #endregion

        #region Release Operations

        public void Release(string location, Type type)
        {
            ReleaseInternal(location, type);
        }

        public void Release(string location)
        {
            ReleaseInternal(location, typeof(Object));
        }

        private void ReleaseInternal(string location, Type type)
        {
            int key = GetCacheKey(location, type);
            ReleaseInternalByKey(key);
        }

        private void ReleaseInternalByKey(int rootKey)
        {
            var assetsToUnload = new List<(string Location, Object Asset)>();
            var pendingRelease = new Stack<int>();
            pendingRelease.Push(rootKey);

            while (pendingRelease.Count > 0)
            {
                int currentKey = pendingRelease.Pop();

                if (!_cache.TryGetValue(currentKey, out var record))
                    continue;

                var segmentLock = GetSegmentLock(currentKey);
                lock (segmentLock)
                {
                    if (!_cache.TryGetValue(currentKey, out record))
                        continue;

                    int newRefCount = record.DecrementRefCount();

                    if (newRefCount > 0)
                        continue;

                    _cache.TryRemove(currentKey, out _);

                    if (record.Asset != null)
                    {
                        assetsToUnload.Add((record.Location, record.Asset));
                    }

                    var depKeys = record.DependencyKeys;
                    if (depKeys != null)
                    {
                        foreach (var depKey in depKeys)
                        {
                            pendingRelease.Push(depKey);
                        }
                    }
                }
            }

            foreach (var (location, asset) in assetsToUnload)
            {
                try
                {
                    _strategy.UnloadAssetInternal(location, asset);
                }
                catch (Exception ex)
                {
                    ALog.Error($"[Resources] Failed to unload asset '{location}': {ex.Message}");
                }
            }
        }

        #endregion

        #region Batch Operations

        public async UniTask<List<ResHandle<T>>> LoadBatchAsync<T>(
            IEnumerable<string> locations,
            Action<float> onProgress,
            CancellationToken token
        )
            where T : class
        {
            var locList = locations?.ToList() ?? new List<string>();

            if (locList.Count == 0)
            {
                onProgress?.Invoke(1f);
                return new List<ResHandle<T>>();
            }

            var progresses = new float[locList.Count];
            var tasks = new UniTask<ResHandle<T>>[locList.Count];

            for (int i = 0; i < locList.Count; i++)
            {
                int index = i;
                Action<float> progressHandler = onProgress != null
                    ? p =>
                    {
                        progresses[index] = p;
                        float avg = progresses.Average();
                        onProgress(avg);
                    }
                    : null;

                tasks[i] = LoadAsync<T>(locList[i], progressHandler, token);
            }

            var results = await UniTask.WhenAll(tasks);
            return results.ToList();
        }

        public UniTask<List<ResHandle<T>>> LoadBatchAsync<T>(
            IEnumerable<string> locations,
            CancellationToken token
        )
            where T : class
        {
            return LoadBatchAsync<T>(locations, null, token);
        }

        public void ReleaseBatch<T>(IEnumerable<string> locations)
            where T : class
        {
            if (locations == null)
                return;

            foreach (var location in locations)
            {
                Release(location, typeof(T));
            }
        }

        #endregion

        #region Unload Operations

        public async UniTask UnloadUnusedAssets(CancellationToken token = default)
        {
            await _strategy.UnloadUnusedAssets(token);
        }

        #endregion
    }
}
