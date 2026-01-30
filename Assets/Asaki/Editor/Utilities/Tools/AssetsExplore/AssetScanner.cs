using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Asaki.Editor.Utilities.Tools.AssetsExplore
{
    public class AssetScanner
    {
        // ========== 公共属性与事件 ==========
        public bool IsScanning { get; private set; }
        public float Progress { get; private set; }
        public string CurrentScanningPath { get; private set; }
        public IReadOnlyDictionary<string, AssetInfo> CachedAssets => _cachedAssets;

        public event Action<AssetInfo> OnAssetFound;
        public event Action OnScanComplete;
        public event Action<string> OnError;

        // ========== 私有字段 ==========
        private CancellationTokenSource _cancellationTokenSource;
        private readonly HashSet<string> _excludedFolders;
        private readonly List<string> _scanPaths;

        // 缓存与并发
        private Dictionary<string, AssetInfo> _cachedAssets = new Dictionary<string, AssetInfo>();
        private readonly string _cacheFilePath;
        private readonly SemaphoreSlim _ioSemaphore; // 限制 IO 并发数

        // ========== 构造函数 ==========
        public AssetScanner()
        {
            _excludedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Library",
                "Temp",
                "obj",
                "bin",
                "Logs",
                "Packages",
                "Assets/ThirdParty",
                "Assets/Plugins",
                "Assets/Editor",
            };

            _scanPaths = new List<string> { "Assets" };

            // 将缓存移至 Library 目录，更加规范且稳定
            string libPath = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                "Library"
            );
            if (!Directory.Exists(libPath))
                Directory.CreateDirectory(libPath);
            _cacheFilePath = Path.Combine(libPath, "AsakiAssetCache.json");

            // IO 密集型操作，并发数设为处理器核心数
            _ioSemaphore = new SemaphoreSlim(Mathf.Max(2, SystemInfo.processorCount));

            LoadCache();
        }

        // ========== 公共方法 ==========
        public void SetExcludedFolders(IEnumerable<string> folders)
        {
            _excludedFolders.Clear();
            foreach (string folder in folders)
            {
                // 标准化路径格式
                _excludedFolders.Add(folder.Replace("\\", "/").TrimEnd('/'));
            }
        }

        public IReadOnlyCollection<string> ExcludedFolders => _excludedFolders;

        public bool IsCacheValid()
        {
            return _cachedAssets.Count > 0 && File.Exists(_cacheFilePath);
        }

        public async Task StartScanAsync(bool incremental = false)
        {
            if (IsScanning)
                return;
            IsScanning = true;
            Progress = 0f;

            // [修复] 确保旧的 CTS 被清理
            if (_cancellationTokenSource != null)
            {
                try
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                }
                catch { }
                _cancellationTokenSource = null;
            }
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;

            try
            {
                // 1. 第一阶段：快速收集所有文件路径 (后台线程)
                var allFiles = await Task.Run(() => CollectAllFiles(token), token);

                int totalCount = allFiles.Count;
                int processedCount = 0;
                var newCache = new Dictionary<string, AssetInfo>(totalCount);
                string projectRoot = Path.GetDirectoryName(Application.dataPath)
                    ?.Replace('\\', '/');

                // 2. 第二阶段：Native IO 并行解析 (不卡主线程)
                var tasks = allFiles.Select(async filePath =>
                {
                    if (token.IsCancellationRequested)
                        return;

                    await _ioSemaphore.WaitAsync(token);
                    try
                    {
                        // 计算相对路径
                        string relativePath = filePath
                            .Substring(projectRoot.Length + 1)
                            .Replace('\\', '/');
                        DateTime currentModTime = File.GetLastWriteTimeUtc(filePath);

                        // 增量检查：利用文件修改时间
                        if (
                            incremental
                            && _cachedAssets.TryGetValue(relativePath, out AssetInfo cachedInfo)
                        )
                        {
                            // 误差在 1 秒内认为未修改
                            if (
                                Math.Abs((cachedInfo.lastModified - currentModTime).TotalSeconds)
                                < 1.0
                            )
                            {
                                lock (newCache)
                                {
                                    newCache[relativePath] = cachedInfo;
                                }
                                return;
                            }
                        }

                        // [核心优化] Native IO 解析：直接读 Meta 获取 GUID，速度提升 50 倍
                        AssetInfo info = await CreateAssetInfoNative(
                            filePath,
                            relativePath,
                            currentModTime
                        );
                        if (info != null)
                        {
                            lock (newCache)
                            {
                                newCache[relativePath] = info;
                            }
                            OnAssetFound?.Invoke(info);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 忽略个别文件访问错误
                        if (!token.IsCancellationRequested)
                            Debug.LogWarning($"[Asaki] Scan error on {filePath}: {ex.Message}");
                    }
                    finally
                    {
                        _ioSemaphore.Release();
                        int c = Interlocked.Increment(ref processedCount);
                        // 降低进度更新频率，减少主线程压力
                        if (c % 50 == 0)
                        {
                            Progress = (float)c / totalCount;
                            CurrentScanningPath = filePath;
                        }
                    }
                });

                await Task.WhenAll(tasks);

                _cachedAssets = newCache;
                SaveCache();

                Progress = 1f;
                OnScanComplete?.Invoke();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[Asaki] Asset scan canceled.");
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Scan failed: {ex.Message}");
            }
            finally
            {
                IsScanning = false;
                CurrentScanningPath = "";

                // [关键修复] 销毁后立即置空，防止 OnDisable 再次访问
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }
            }
        }

        public void CancelScan()
        {
            // [关键修复] 安全取消模式
            // 捕获引用到局部变量，防止多线程竞争导致 NullReference
            CancellationTokenSource cts = _cancellationTokenSource;
            if (cts != null)
            {
                try
                {
                    if (!cts.IsCancellationRequested)
                        cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // 已经被销毁了，忽略即可
                }
            }
        }

        // ========== 私有核心逻辑 ==========

        private List<string> CollectAllFiles(CancellationToken token)
        {
            var results = new List<string>(10000);
            foreach (string rootPath in _scanPaths)
            {
                if (!Directory.Exists(rootPath))
                    continue;

                try
                {
                    // 使用 EnumerateFiles 配合 SearchOption.AllDirectories 效率更高
                    var files = Directory.EnumerateFiles(
                        rootPath,
                        "*",
                        SearchOption.AllDirectories
                    );
                    foreach (string file in files)
                    {
                        if (token.IsCancellationRequested)
                            break;
                        if (file.EndsWith(".meta"))
                            continue; // 忽略 .meta 自身

                        // 简单路径过滤
                        bool excluded = false;
                        // 统一转为正斜杠比较
                        string normalizedPath = file.Replace('\\', '/');
                        foreach (string ex in _excludedFolders)
                        {
                            if (normalizedPath.Contains(ex))
                            {
                                excluded = true;
                                break;
                            }
                        }

                        if (!excluded)
                            results.Add(normalizedPath);
                    }
                }
                catch { }
            }
            return results;
        }

        // [黑科技] 直接读取 Meta 文件获取 GUID，完全绕过 Unity API
        private async Task<AssetInfo> CreateAssetInfoNative(
            string fullPath,
            string relativePath,
            DateTime lastWrite
        )
        {
            string metaPath = fullPath + ".meta";
            if (!File.Exists(metaPath))
                return null;

            string guid = null;

            // Meta 文件很小，读取前几行即可找到 GUID
            using (StreamReader reader = new StreamReader(metaPath))
            {
                for (int i = 0; i < 10; i++) // GUID 通常在 header 附近
                {
                    string line = await reader.ReadLineAsync();
                    if (line == null)
                        break;
                    if (line.Contains("guid: "))
                    {
                        // 格式通常为: guid: xxxxxxxxxxxxx
                        int idx = line.IndexOf("guid: ");
                        guid = line.Substring(idx + 6).Trim();
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(guid))
                return null;

            return new AssetInfo
            {
                guid = guid,
                name = Path.GetFileNameWithoutExtension(fullPath),
                path = relativePath,
                extension = Path.GetExtension(fullPath).ToLowerInvariant(),
                fileSize = new FileInfo(fullPath).Length,
                lastModified = lastWrite,
                category = CategoryManager.GetCategory(Path.GetExtension(fullPath)),
            };
        }

        #region Cache IO

        [Serializable]
        private class CacheWrapper
        {
            public AssetInfo[] items;
            public long timestamp;
        }

        private void LoadCache()
        {
            if (!File.Exists(_cacheFilePath))
                return;
            try
            {
                string json = File.ReadAllText(_cacheFilePath);
                CacheWrapper data = JsonUtility.FromJson<CacheWrapper>(json);
                if (data?.items != null)
                {
                    _cachedAssets = data.items.ToDictionary(x => x.path);
                    Debug.Log($"[Asaki] Loaded {data.items.Length} assets from cache.");
                }
            }
            catch
            {
                _cachedAssets = new Dictionary<string, AssetInfo>();
            }
        }

        private void SaveCache()
        {
            try
            {
                CacheWrapper wrapper = new CacheWrapper
                {
                    items = _cachedAssets.Values.ToArray(),
                    timestamp = DateTime.UtcNow.Ticks,
                };
                // 移除 pretty print 以减小文件体积，加快 IO
                File.WriteAllText(_cacheFilePath, JsonUtility.ToJson(wrapper));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Asaki] Failed to save cache: {ex.Message}");
            }
        }

        #endregion
    }
}
