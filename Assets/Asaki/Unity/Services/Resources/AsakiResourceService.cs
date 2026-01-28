using System;
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
    public class AsakiResourceService : IAsakiResourceService
    {
        private readonly IAsakiResStrategy _strategy;
        private readonly IAsakiAsyncService _asyncService;
        private readonly IAsakiResDependencyLookup _asakiResDependencyLookup;

        private class ResRecord
        {
            public string Location;
            public Type AssetType; // [新增] 记录资源类型
            public int CacheKey; // [新增] 记录缓存Key
            public Object Asset;
            public int RefCount;

            // [修改] 使用 int 类型的 HashKey 防止重复依赖 (因为依赖也是通过 HashKey 索引的)
            public HashSet<int> DependencyKeys = new HashSet<int>();
            public TaskCompletionSource<Object> LoadingTcs = new TaskCompletionSource<Object>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

            public Action<float> ProgressCallbacks;

            public void ReportProgress(float progress)
            {
                ProgressCallbacks?.Invoke(progress);
            }
        }

        // [修改] Key 从 string 变为 int (Hash)
        private readonly Dictionary<int, ResRecord> _cache = new Dictionary<int, ResRecord>();
        private readonly object _lock = new object();
        private int _timeoutSeconds = DefaultTimeoutSeconds;
        private const int DefaultTimeoutSeconds = 10000;

        public AsakiResourceService(
            IAsakiResStrategy strategy,
            IAsakiAsyncService asyncService,
            IAsakiResDependencyLookup asakiResDependencyLookup
        )
        {
            _strategy = strategy;
            _asyncService = asyncService;
            _asakiResDependencyLookup = asakiResDependencyLookup;
        }

        // [新增] 核心 Hash 生成逻辑：Path + Type
        private int GetCacheKey(string location, Type type)
        {
            if (type == null)
                type = typeof(Object);
            // 拼接路径和类型全名，确保 Sprite 和 Texture2D 生成不同的 Key
            string combine = $"{location}_{type.FullName}";
            return combine.GetHashCode();
        }

        public async UniTask UnloadUnusedAssets(
            CancellationToken token = default(CancellationToken)
        )
        {
            await _strategy.UnloadUnusedAssets(token);
        }

        public void SetTimeoutSeconds(int timeoutSeconds)
        {
            _timeoutSeconds = Mathf.Max(DefaultTimeoutSeconds, timeoutSeconds);
        }

        public UniTask OnInitAsync()
        {
            return _strategy.InitializeAsync();
        }

        public void OnInit() { }

        public void OnDispose()
        {
            lock (_lock)
            {
                foreach (var kvp in _cache)
                {
                    if (kvp.Value.Asset != null)
                        _strategy.UnloadAssetInternal(kvp.Value.Location, kvp.Value.Asset);
                }
                _cache.Clear();
            }
        }

        // =========================================================
        // Load Interface
        // =========================================================

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
            // [修改] 传入 typeof(T) 进行 Key 计算
            ResRecord record = GetOrCreateRecord(location, typeof(T), token);

            // 进度回调注册
            if (onProgress != null)
            {
                if (record.Asset != null)
                    onProgress(1f);
                else
                    record.ProgressCallbacks += onProgress;
            }

            // 乐观锁引用计数
            Interlocked.Increment(ref record.RefCount);

            try
            {
                Object assetObj = await record.LoadingTcs.Task.WaitAsync(token);

                if (assetObj is T tAsset)
                {
                    return new ResHandle<T>(location, tAsset, this);
                }
                else
                {
                    // [注意] 由于现在 Key 包含了类型，理论上不会进这里，除非 Strategy 返回了错误类型
                    throw new InvalidCastException(
                        $"[Resources] Type mismatch for {location}. Expected {typeof(T)}, got {assetObj?.GetType()}"
                    );
                }
            }
            catch (Exception)
            {
                // 发生取消或错误时，回滚引用 (需传入类型)
                ReleaseInternal(location, typeof(T));
                throw;
            }
            finally
            {
                // 清理进度委托
                if (onProgress != null)
                {
                    record.ProgressCallbacks -= onProgress;
                }
            }
        }

        // =========================================================
        // Internal Logic
        // =========================================================

        private ResRecord GetOrCreateRecord(
            string location,
            Type type,
            CancellationToken token = default(CancellationToken)
        )
        {
            ResRecord record;
            bool isOwner = false;
            int key = GetCacheKey(location, type);

            lock (_lock)
            {
                if (!_cache.TryGetValue(key, out record))
                {
                    // [修改] 初始化记录时存储 Type 和 Key
                    record = new ResRecord
                    {
                        Location = location,
                        AssetType = type,
                        CacheKey = key,
                    };
                    _cache.Add(key, record);
                    isOwner = true;
                }
            }

            if (isOwner)
            {
                SafeStartLoadTask(record, token);
            }

            return record;
        }

        private async void SafeStartLoadTask(
            ResRecord record,
            CancellationToken token = default(CancellationToken)
        )
        {
            try
            {
                await LoadTaskInternal(record, token);
            }
            catch (Exception ex)
            {
                if (!record.LoadingTcs.Task.IsCompleted)
                {
                    record.LoadingTcs.TrySetException(ex);
                }

                // [修改] 使用 CacheKey 移除
                lock (_lock)
                {
                    _cache.Remove(record.CacheKey);
                }

                // 错误回滚：释放已加载的依赖
                // [修改] 遍历 Int Key
                lock (record.DependencyKeys)
                {
                    foreach (int depKey in record.DependencyKeys)
                        ReleaseInternalByKey(depKey);
                }
            }
        }

        private async UniTask LoadTaskInternal(
            ResRecord record,
            CancellationToken token = default(CancellationToken)
        )
        {
            try
            {
                // --- 1. 依赖加载 ---
                var deps = _asakiResDependencyLookup.GetDependencies(record.Location);
                if (deps != null)
                {
                    foreach (string depLoc in deps)
                    {
                        Type depType = typeof(Object);
                        ResRecord depRecord = GetOrCreateRecord(depLoc, depType, token);
                        int depKey = depRecord.CacheKey;

                        Interlocked.Increment(ref depRecord.RefCount);

                        bool isValid = false;
                        lock (_lock)
                        {
                            // [修改] 使用 CacheKey 检查
                            if (_cache.ContainsKey(record.CacheKey))
                            {
                                lock (record.DependencyKeys)
                                {
                                    record.DependencyKeys.Add(depKey);
                                }
                                isValid = true;
                            }
                        }

                        if (!isValid)
                        {
                            // [修改] 使用 Key 释放
                            ReleaseInternalByKey(depKey);
                            throw new OperationCanceledException(
                                $"[Resources] Loading aborted for {record.Location}"
                            );
                        }

                        var dependencyTask = depRecord
                            .LoadingTcs.Task.AsUniTask()
                            .AttachExternalCancellation(token);
                        UniTask timeoutTask = UniTask.Delay(
                            _timeoutSeconds,
                            false,
                            PlayerLoopTiming.Update,
                            token,
                            false
                        );
                        (bool hasResultLeft, Object result) finishedIndex = await UniTask.WhenAny(
                            dependencyTask,
                            timeoutTask
                        );

                        if (finishedIndex.hasResultLeft)
                        {
                            throw new TimeoutException($"[Resources] Dependency Timeout: {depLoc}");
                        }

                        // 等待依赖任务完成，这样任何异常都会被正确传播
                        await dependencyTask;
                    }
                }

                // --- 2. 自身加载 ---

                // [关键修改] 将 record.AssetType 传递给 Strategy
                // 这样 Unity Resources.Load 就能收到正确的 Sprite 类型
                Object asset = await _asyncService.RunTask(
                    async () =>
                        await _strategy.LoadAssetInternalAsync(
                            record.Location,
                            record.AssetType,
                            record.ReportProgress,
                            token
                        ),
                    token
                );

                if (asset == null)
                    throw new Exception(
                        $"[Resources] Asset not found: {record.Location} (Type: {record.AssetType.Name})"
                    );

                record.Asset = asset;
                record.LoadingTcs.TrySetResult(asset);
                record.ReportProgress(1f);
            }
            catch (Exception ex)
            {
                record.LoadingTcs.TrySetException(ex);

                lock (_lock)
                {
                    _cache.Remove(record.CacheKey);
                }

                lock (record.DependencyKeys)
                {
                    foreach (int depKey in record.DependencyKeys)
                        ReleaseInternalByKey(depKey);
                }
            }
        }

        // =========================================================
        // Release Logic
        // =========================================================

        /// <summary>
        /// [API变更] 释放资源现在需要类型来定位准确的缓存
        /// </summary>
        public void Release(string location, Type type)
        {
            ReleaseInternal(location, type);
        }

        /// <summary>
        /// [兼容重载] 默认为 Object，但在 Sprite/Texture 混用时可能不准确，建议使用带 Type 的版本
        /// </summary>
        public void Release(string location)
        {
            ReleaseInternal(location, typeof(Object));
        }

        private void ReleaseInternal(string location, Type type)
        {
            int key = GetCacheKey(location, type);
            ReleaseInternalByKey(key);
        }

        // 内部递归核心，使用 Key 操作
        private void ReleaseInternalByKey(int rootKey)
        {
            var pendingRelease = new Stack<int>();
            pendingRelease.Push(rootKey);

            lock (_lock)
            {
                while (pendingRelease.Count > 0)
                {
                    int currentKey = pendingRelease.Pop();

                    if (!_cache.TryGetValue(currentKey, out ResRecord record))
                        continue;
                    record.RefCount--;

                    if (record.RefCount > 0)
                        continue;

                    // 引用归零，卸载
                    if (record.Asset != null)
                    {
                        try
                        {
                            _strategy.UnloadAssetInternal(record.Location, record.Asset);
                        }
                        catch (Exception e)
                        {
                            ALog.Error("[Resources] Unload Asset Failed", e);
                        }
                    }

                    _cache.Remove(currentKey);

                    if (record.DependencyKeys == null)
                        continue;

                    foreach (int depKey in record.DependencyKeys)
                    {
                        pendingRelease.Push(depKey);
                    }
                }
            }
        }

        // =========================================================
        // Batch Operations
        // =========================================================

        public async UniTask<List<ResHandle<T>>> LoadBatchAsync<T>(
            IEnumerable<string> locations,
            Action<float> onProgress,
            CancellationToken token
        )
            where T : class
        {
            var locList = locations.ToList();
            if (locList.Count == 0)
            {
                onProgress?.Invoke(1f);
                return new List<ResHandle<T>>();
            }

            float[] progresses = new float[locList.Count];

            Action<float> GetProgressHandler(int index)
            {
                return (p) =>
                {
                    progresses[index] = p;
                    {
                        float total = 0f;
                        for (int i = 0; i < progresses.Length; i++)
                            total += progresses[i];
                        onProgress(total / progresses.Length);
                    }
                };
            }

            var tasks = new UniTask<ResHandle<T>>[locList.Count];
            for (int i = 0; i < locList.Count; i++)
            {
                tasks[i] = LoadAsync<T>(
                    locList[i],
                    onProgress == null ? null : GetProgressHandler(i),
                    token
                );
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

        /// <summary>
        /// [API变更] 批量释放现在建议显式指定类型，或者修改接口
        /// 这里为了兼容 Interface 暂时回退到 Object 类型，如果有问题请改为 LoadBatchAsync<T> 对应的 ReleaseBatch<T>
        /// </summary>
        public void ReleaseBatch(IEnumerable<string> locations)
        {
            foreach (string location in locations)
                Release(location, typeof(Object));
        }

        // 建议新增的泛型批量释放
        public void ReleaseBatch<T>(IEnumerable<string> locations)
        {
            foreach (string location in locations)
                Release(location, typeof(T));
        }
    }
}
