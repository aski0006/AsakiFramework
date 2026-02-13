using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Asaki.Core.Attributes;
using Asaki.Core.Broker;
using Asaki.Core.DataTable;
using Asaki.Core.Logging;
using Asaki.Unity.Services.Serialization;
using Asaki.Unity.Utils;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Unity.Services.DataTable
{
    public class AsakiConfigService : IAsakiConfigService
    {
        private const string SYSTEM_PERMISSION_KEY = "ASAKI_SYS_KEY_9482_ACCESS";

        private readonly Dictionary<Type, Dictionary<int, IAsakiDataTable>> _configStore =
            new Dictionary<Type, Dictionary<int, IAsakiDataTable>>();
        private readonly Dictionary<Type, object> _listStore = new Dictionary<Type, object>();

        private readonly SemaphoreSlim _loadSemaphore = new SemaphoreSlim(1, 1);
        private string _csvRootPath;
        private string _binaryCachePath;
        private bool _isEditor;
        private readonly IAsakiEventService _asakiEventService;

        private readonly Dictionary<Type, ConfigMetadata> _metadataCache =
            new Dictionary<Type, ConfigMetadata>();
        private readonly Dictionary<Type, UniTask> _loadingTasks = new Dictionary<Type, UniTask>();
        private readonly Dictionary<Type, ConfigStats> _statsCache =
            new Dictionary<Type, ConfigStats>();

        private class ConfigMetadata
        {
            public Type ConfigType;
            public AsakiConfigLoadStrategy Strategy;
            public int Priority;
            public bool Unloadable;
            public Type[] Dependencies;
            public long EstimatedSize;
        }

        private class ConfigStats
        {
            public int AccessCount;
            public DateTime LastAccessTime;
            public DateTime LoadTime;
        }

        public AsakiConfigService(IAsakiEventService asakiEventService)
        {
            _asakiEventService = asakiEventService;
        }

        public void OnInit()
        {
            _csvRootPath = Path.Combine(Application.streamingAssetsPath, "FrameworkSettings");
            _binaryCachePath = Path.Combine(Application.persistentDataPath, "ConfigCache");
            _isEditor = Application.isEditor;

            if (!Directory.Exists(_binaryCachePath))
                Directory.CreateDirectory(_binaryCachePath);

            ScanConfigTypes();

            if (!_isEditor || !Application.isPlaying)
                return;
            GameObject go = new GameObject("[AsakiConfigHotReloader]");
            go.AddComponent<AsakiConfigHotReloader>();
            Object.DontDestroyOnLoad(go);
        }

        public async UniTask OnInitAsync()
        {
            var preloadTypes = _metadataCache
                .Where(kvp => kvp.Value.Strategy == AsakiConfigLoadStrategy.Preload)
                .OrderByDescending(kvp => kvp.Value.Priority)
                .Select(kvp => kvp.Key)
                .ToList();
            if (preloadTypes.Count > 0)
            {
                ALog.Info($"[AsakiFrameworkSetting] Preloading {preloadTypes.Count} core configs...");

                var tasks = preloadTypes.Select(LoadConfigInternalAsync).ToList();

                await UniTask.WhenAll(tasks);
            }
            ALog.Info($"[AsakiFrameworkSetting] Service Ready.  Preloaded {_configStore.Count} tables.");
#if UNITY_EDITOR
            if (_isEditor)
            {
                await ValidateAllConfigsAsync();
            }
#endif
        }

        public void OnDispose()
        {
            _configStore.Clear();
            _listStore.Clear();
            _loadSemaphore?.Dispose();
        }

        // ==================== 修改点1：LoadAllInternal 重构 ====================
        public UniTask LoadAllAsync()
        {
            return LoadAllInternal();
        }

        private async UniTask LoadAllInternal()
        {
            // 不再通过扫描 CSV 文件 + 字符串注册表加载
            // 改为直接加载所有已扫描到的配置类型（无论策略）
            var allTypes = _metadataCache.Keys.ToList();
            if (allTypes.Count == 0)
                return;

            ALog.Info($"[AsakiFrameworkSetting] Loading all {allTypes.Count} config types...");
            var tasks = allTypes.Select(LoadConfigInternalAsync).ToArray();
            await UniTask.WhenAll(tasks);
        }
        // ==================== 修改点结束 ====================

        public UniTask ReloadAsync<T>()
            where T : class, IAsakiDataTable, new()
        {
            return ReloadInternal<T>();
        }

        public T Get<T>(int id)
            where T : class, IAsakiDataTable, new()
        {
            if (!IsLoaded<T>())
            {
                ConfigMetadata metadata = GetMetadata<T>();

                if (metadata.Strategy == AsakiConfigLoadStrategy.Manual)
                {
                    ALog.Error(
                        $"[AsakiFrameworkSetting] {typeof(T).Name} requires manual loading.  Call LoadAsync<{typeof(T).Name}>() first."
                    );
                    return null;
                }

                ALog.Warn(
                    $"[AsakiFrameworkSetting] {typeof(T).Name} not loaded, blocking load on main thread.  Consider using GetAsync or Preload."
                );
                LoadConfigInternalAsync(typeof(T)).GetAwaiter().GetResult();
            }

            RecordAccess<T>();

            if (_configStore.TryGetValue(typeof(T), out var dict))
            {
                if (dict.TryGetValue(id, out IAsakiDataTable val))
                    return (T)val.CloneConfig();
            }
            return null;
        }

        public IReadOnlyList<T> GetAll<T>()
            where T : class, IAsakiDataTable, new()
        {
            if (_listStore.TryGetValue(typeof(T), out object list))
            {
                return (IReadOnlyList<T>)list;
            }
            return Array.Empty<T>();
        }

        public async IAsyncEnumerable<T> GetAllStreamAsync<T>()
            where T : class, IAsakiDataTable, new()
        {
            if (!IsLoaded<T>())
            {
                string csvPath = Path.Combine(_csvRootPath, typeof(T).Name + ".csv");
                if (File.Exists(csvPath))
                {
                    await LoadInternalAsync<T>(csvPath);
                }
                else
                {
                    yield break;
                }
            }
            if (!_listStore.TryGetValue(typeof(T), out object list))
                yield break;
            if (list is not List<T> typedList)
                yield break;
            foreach (T item in typedList)
            {
                yield return item;
            }
        }

        public T Find<T>(Predicate<T> predicate)
            where T : class, IAsakiDataTable, new()
        {
            if (predicate == null)
            {
                ALog.Warn("[AsakiFrameworkSetting] Find predicate cannot be null");
                return null;
            }

            if (!_listStore.TryGetValue(typeof(T), out object list))
            {
                return null;
            }

            var typedList = list as List<T>;
            if (typedList == null)
                return null;

            foreach (T item in typedList)
            {
                if (predicate(item))
                {
                    return item;
                }
            }

            return null;
        }

        public IReadOnlyList<T> Where<T>(Func<T, bool> predicate)
            where T : class, IAsakiDataTable, new()
        {
            if (predicate == null)
            {
                ALog.Warn("[AsakiFrameworkSetting] Where predicate cannot be null");
                return Array.Empty<T>();
            }

            if (!_listStore.TryGetValue(typeof(T), out object list))
            {
                return Array.Empty<T>();
            }

            var typedList = list as List<T>;
            if (typedList == null)
                return Array.Empty<T>();

            var result = new List<T>();
            foreach (T item in typedList)
            {
                if (predicate(item))
                {
                    result.Add(item);
                }
            }

            return result;
        }

        public bool Exists<T>(Predicate<T> predicate)
            where T : class, IAsakiDataTable, new()
        {
            if (predicate == null)
            {
                ALog.Warn("[AsakiFrameworkSetting] Exists predicate cannot be null");
                return false;
            }

            if (!_listStore.TryGetValue(typeof(T), out object list))
            {
                return false;
            }

            var typedList = list as List<T>;
            if (typedList == null)
                return false;

            foreach (T item in typedList)
            {
                if (predicate(item))
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<T> GetBatch<T>(IEnumerable<int> ids)
            where T : class, IAsakiDataTable, new()
        {
            if (ids == null)
            {
                ALog.Warn("[AsakiFrameworkSetting] GetBatch ids cannot be null");
                return Array.Empty<T>();
            }

            if (!_configStore.TryGetValue(typeof(T), out var dict))
            {
                return Array.Empty<T>();
            }

            var result = new List<T>();
            foreach (int id in ids)
            {
                if (dict.TryGetValue(id, out IAsakiDataTable config))
                {
                    result.Add((T)config);
                }
                else
                {
                    ALog.Warn($"[AsakiFrameworkSetting] ID {id} not found in {typeof(T).Name}");
                }
            }

            return result;
        }

        public int GetCount<T>()
            where T : class, IAsakiDataTable, new()
        {
            if (_listStore.TryGetValue(typeof(T), out object list))
            {
                return (list as List<T>)?.Count ?? 0;
            }
            return 0;
        }

        public bool IsLoaded<T>()
            where T : class, IAsakiDataTable, new()
        {
            return _configStore.ContainsKey(typeof(T));
        }

        public bool IsLoaded(Type configType)
        {
            return _configStore.ContainsKey(configType);
        }

        public string GetSourcePath<T>()
            where T : class, IAsakiDataTable, new()
        {
            string fileName = typeof(T).Name + ".csv";
            return Path.Combine(_csvRootPath, fileName);
        }

        public DateTime GetLastModifiedTime<T>()
            where T : class, IAsakiDataTable, new()
        {
            string sourcePath = GetSourcePath<T>();
            try
            {
                return File.Exists(sourcePath)
                    ? File.GetLastWriteTime(sourcePath)
                    : DateTime.MinValue;
            }
            catch (Exception ex)
            {
                ALog.Error(
                    $"[AsakiFrameworkSetting] Failed to get modified time for {typeof(T).Name}: {ex.Message}",
                    ex
                );
                return DateTime.MinValue;
            }
        }

        public async UniTask<T> GetAsync<T>(int id)
            where T : class, IAsakiDataTable, new()
        {
            await EnsureLoadedAsync<T>();
            return Get<T>(id);
        }

        public async UniTask PreloadAsync<T>()
            where T : class, IAsakiDataTable, new()
        {
            await LoadConfigInternalAsync(typeof(T));
        }

        public async UniTask PreloadAsync(Type configType)
        {
            await LoadConfigInternalAsync(configType);
        }

        public async UniTask PreloadBatchAsync(params Type[] configTypes)
        {
            var tasks = configTypes.Select(LoadConfigInternalAsync).ToArray();
            await UniTask.WhenAll(tasks);
        }

        public void Unload<T>()
            where T : class, IAsakiDataTable, new()
        {
            Type type = typeof(T);
            ConfigMetadata metadata = GetMetadata<T>();

            if (!metadata.Unloadable)
            {
                ALog.Warn($"[AsakiFrameworkSetting] {type.Name} is marked as non-unloadable.");
                return;
            }

            if (_configStore.Remove(type))
            {
                _listStore.Remove(type);
                ALog.Info($"[AsakiFrameworkSetting] Unloaded {type.Name}");
            }
        }

        public void Unload(Type configType)
        {
            ConfigMetadata metadata = GetMetadata(configType);

            if (!metadata.Unloadable)
            {
                ALog.Warn($"[AsakiFrameworkSetting] {configType.Name} is non-unloadable.");
                return;
            }

            if (_configStore.Remove(configType))
            {
                _listStore.Remove(configType);
                ALog.Info($"[AsakiFrameworkSetting] Unloaded {configType.Name}");
            }
        }

        public AsakiConfigLoadInfo GetLoadInfo<T>()
            where T : class, IAsakiDataTable, new()
        {
            Type type = typeof(T);
            ConfigMetadata metadata = GetMetadata<T>();

            return new AsakiConfigLoadInfo
            {
                ConfigName = type.Name,
                IsLoaded = IsLoaded(type),
                Strategy = metadata.Strategy,
                Priority = metadata.Priority,
                Unloadable = metadata.Unloadable,
                EstimatedSize = metadata.EstimatedSize,
                AccessCount = _statsCache.TryGetValue(type, out ConfigStats stats)
                    ? stats.AccessCount
                    : 0,
                LastAccessTime = stats?.LastAccessTime ?? DateTime.MinValue,
            };
        }

        // ==================== 修改点2：LoadInternalAsync<T> 保持不变（由生成器调用）====================
        public async UniTask LoadInternalAsync<T>(string csvPath)
            where T : class, IAsakiDataTable, new()
        {
            string fileName = Path.GetFileNameWithoutExtension(csvPath);
            string binaryPath = Path.Combine(_binaryCachePath, fileName + ".bin");
            List<T> results = null;
            bool shouldLoadBinary = false;

            if (File.Exists(binaryPath))
            {
                if (_isEditor)
                {
                    DateTime binTime = File.GetLastWriteTime(binaryPath);
                    DateTime csvTime = File.GetLastWriteTime(csvPath);
                    if (binTime >= csvTime)
                    {
                        shouldLoadBinary = true;
                    }
                    else
                    {
                        ALog.Warn(
                            $"[AsakiFrameworkSetting] Detected stale binary for '{fileName}'. Re-baking from CSV..."
                        );
                    }
                }
                else
                {
                    shouldLoadBinary = true;
                }
            }

            if (shouldLoadBinary)
            {
                try
                {
                    results = await LoadFromBinaryAsync<T>(binaryPath);
                }
                catch (Exception ex)
                {
                    ALog.Error(
                        $"[AsakiFrameworkSetting] Failed to load binary '{fileName}', falling back to CSV. Error : {ex.Message}",
                        ex
                    );
                    results = null;
                }
            }

            if (results == null)
            {
                await UniTask.SwitchToThreadPool();
                string csvContent = await File.ReadAllTextAsync(csvPath)
                    .AsUniTask(useCurrentSynchronizationContext: false);
                await UniTask.SwitchToMainThread();
                results = await ParseCsvAsync<T>(csvContent);
                await SaveToBinaryAsync(binaryPath, results);
            }
            BuildIndex(results);
        }

        private async UniTask<List<T>> ParseCsvAsync<T>(string csvContent)
            where T : class, IAsakiDataTable, new()
        {
            return await UniTask.RunOnThreadPool(() =>
            {
                string[] lines = csvContent.Replace("\r\n", "\n").Split('\n');
                if (lines.Length < 2)
                    return new List<T>();

                string[] headers = AsakiCsvUtils.ParseLine(lines[0]);
                var headerMap = new Dictionary<string, int>();
                for (int i = 0; i < headers.Length; i++)
                {
                    headerMap[headers[i].Trim()] = i;
                }

                var result = new List<T>(lines.Length);
                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                        continue;

                    string[] rowData = AsakiCsvUtils.ParseLine(lines[i]);
                    AsakiCsvReader reader = new AsakiCsvReader(rowData, headerMap);
                    T obj = new T();
                    obj.Deserialize(reader);
                    result.Add(obj);
                }
                return result;
            });
        }

        private async UniTask<List<T>> LoadFromBinaryAsync<T>(string path)
            where T : class, IAsakiDataTable, new()
        {
            await UniTask.SwitchToThreadPool();
            byte[] bytes = await File.ReadAllBytesAsync(path)
                .AsUniTask(useCurrentSynchronizationContext: false);
            await UniTask.SwitchToMainThread();
            return DeserializeBytes<T>(bytes);
        }

        private async UniTask SaveToBinaryAsync<T>(string path, List<T> data)
            where T : class, IAsakiDataTable
        {
            byte[] bytes = SerializeBytes(data);
            await UniTask.SwitchToThreadPool();
            await File.WriteAllBytesAsync(path, bytes)
                .AsUniTask(useCurrentSynchronizationContext: false);
            await UniTask.SwitchToMainThread();
        }

        private async UniTask ReloadInternal<T>()
            where T : class, IAsakiDataTable, new()
        {
            string csvPath = Path.Combine(_csvRootPath, typeof(T).Name + ".csv");
            if (File.Exists(csvPath))
            {
                ALog.Info($"[AsakiFrameworkSetting] Hot Reloading: {typeof(T).Name}...");

                await UniTask.SwitchToThreadPool();
                string content = await File.ReadAllTextAsync(csvPath)
                    .AsUniTask(useCurrentSynchronizationContext: false);
                await UniTask.SwitchToMainThread();

                var list = await ParseCsvAsync<T>(content);
                BuildIndex(list);

                string fileName = typeof(T).Name;
                string binaryPath = Path.Combine(_binaryCachePath, fileName + ".bin");
                await SaveToBinaryAsync(binaryPath, list);

                _asakiEventService.Publish(new AsakiConfigReloadedEvent { ConfigType = typeof(T) });
            }
        }

        private async UniTask EnsureLoadedAsync<T>()
            where T : class, IAsakiDataTable, new()
        {
            if (IsLoaded<T>())
                return;

            ConfigMetadata metadata = GetMetadata<T>();

            if (metadata.Strategy == AsakiConfigLoadStrategy.Manual)
            {
                throw new InvalidOperationException(
                    $"Config {typeof(T).Name} requires manual loading. Call LoadAsync<{typeof(T).Name}>() first."
                );
            }

            await LoadConfigInternalAsync(typeof(T));
        }

        private async UniTask LoadConfigInternalAsync(Type configType)
        {
            if (IsLoaded(configType))
                return;

            await _loadSemaphore.WaitAsync().AsUniTask();

            UniTask loadTask;
            try
            {
                if (IsLoaded(configType))
                    return;

                if (!_loadingTasks.TryGetValue(configType, out loadTask))
                {
                    loadTask = LoadConfigCoreAsync(configType);
                    _loadingTasks[configType] = loadTask;
                }
            }
            finally
            {
                _loadSemaphore.Release();
            }

            await loadTask;
        }

        // ==================== 修改点3：GetLoader 调用改为 Type 参数 ====================
        private async UniTask LoadConfigCoreAsync(Type configType)
        {
            try
            {
                if (!_metadataCache.TryGetValue(configType, out ConfigMetadata metadata))
                {
                    throw new InvalidOperationException(
                        $"Config type {configType.Name} not registered."
                    );
                }

                if (metadata.Dependencies is { Length: > 0 })
                {
                    ALog.Info($"[AsakiFrameworkSetting] Loading dependencies for {configType.Name}...");

                    var depTasks = metadata.Dependencies.Select(LoadConfigInternalAsync).ToArray();

                    await UniTask.WhenAll(depTasks);
                }

                string csvPath = Path.Combine(_csvRootPath, configType.Name + ".csv");
                if (!File.Exists(csvPath))
                {
                    throw new FileNotFoundException($"Config file not found: {csvPath}");
                }

                // [关键修改] 现在 GetLoader 接收 Type 而不是 string
                var loadTask = AsakiConfigRegistry.GetLoader(this, configType, csvPath);
                if (!loadTask.HasValue)
                {
                    throw new InvalidOperationException(
                        $"No loader registered for {configType.Name}"
                    );
                }

                await loadTask.Value;

                if (!_statsCache.ContainsKey(configType))
                {
                    _statsCache[configType] = new ConfigStats();
                }
                _statsCache[configType].LoadTime = DateTime.Now;

                ALog.Info($"[AsakiFrameworkSetting] ✅ Loaded {configType.Name} ({metadata.Strategy})");
            }
            catch (Exception ex)
            {
                ALog.Error($"[AsakiFrameworkSetting] ❌ Failed to load {configType.Name}: {ex.Message}", ex);
                throw;
            }
            finally
            {
                await _loadSemaphore.WaitAsync().AsUniTask(useCurrentSynchronizationContext: false);
                try
                {
                    _loadingTasks.Remove(configType);
                }
                finally
                {
                    _loadSemaphore.Release();
                }
            }
        }
        // ==================== 修改点结束 ====================

        private List<T> DeserializeBytes<T>(byte[] bytes)
            where T : class, IAsakiDataTable, new()
        {
            using MemoryStream ms = new MemoryStream(bytes);
            AsakiBinaryReader reader = new AsakiBinaryReader(ms);
            int count = reader.ReadInt(null);
            var list = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                T obj = new T();
                obj.Deserialize(reader);
                list.Add(obj);
            }
            return list;
        }

        private byte[] SerializeBytes<T>(List<T> data)
            where T : class, IAsakiDataTable
        {
            using MemoryStream ms = new MemoryStream();
            AsakiBinaryWriter writer = new AsakiBinaryWriter(ms);
            writer.WriteInt(null, data.Count);

            foreach (T item in data)
            {
                item.AllowConfigSerialization(SYSTEM_PERMISSION_KEY);
                item.Serialize(writer);
            }

            return ms.ToArray();
        }

        private void BuildIndex<T>(List<T> list)
            where T : class, IAsakiDataTable
        {
            var dict = new Dictionary<int, IAsakiDataTable>(list.Count);
            foreach (T item in list)
            {
                dict.TryAdd(item.Id, item);
            }
            _configStore[typeof(T)] = dict;
            _listStore[typeof(T)] = list;
        }

        private void ScanConfigTypes()
        {
            var allTypes = TypeCache
                .GetTypesDerivedFrom<IAsakiDataTable>()
                .Where(t => !t.IsAbstract && !t.IsInterface);

            foreach (Type type in allTypes)
            {
                AsakiConfigAttribute attr = type.GetCustomAttribute<AsakiConfigAttribute>();

                ConfigMetadata metadata = new ConfigMetadata
                {
                    ConfigType = type,
                    Strategy = attr?.LoadStrategy ?? AsakiConfigLoadStrategy.Auto,
                    Priority = attr?.Priority ?? 0,
                    Unloadable = attr?.Unloadable ?? true,
                    Dependencies = attr?.Dependencies ?? Array.Empty<Type>(),
                    EstimatedSize = EstimateConfigSize(type),
                };

                if (metadata.Strategy == AsakiConfigLoadStrategy.Auto)
                {
                    metadata.Strategy =
                        metadata.EstimatedSize < 100 * 1024
                            ? AsakiConfigLoadStrategy.Preload
                            : AsakiConfigLoadStrategy.OnDemand;
                }

                _metadataCache[type] = metadata;
            }
        }

        private long EstimateConfigSize(Type type)
        {
            string csvPath = Path.Combine(_csvRootPath, type.Name + ".csv");
            if (File.Exists(csvPath))
                return new FileInfo(csvPath).Length;

            string binPath = Path.Combine(_binaryCachePath, type.Name + ".bin");
            if (File.Exists(binPath))
                return new FileInfo(binPath).Length;

            return 0;
        }

        private ConfigMetadata GetMetadata<T>()
            where T : IAsakiDataTable
        {
            if (_metadataCache.TryGetValue(typeof(T), out ConfigMetadata metadata))
                return metadata;

            return new ConfigMetadata
            {
                ConfigType = typeof(T),
                Strategy = AsakiConfigLoadStrategy.OnDemand,
                Priority = 0,
                Unloadable = true,
                Dependencies = Array.Empty<Type>(),
            };
        }

        private void RecordAccess<T>()
        {
            Type type = typeof(T);
            if (!_statsCache.ContainsKey(type))
            {
                _statsCache[type] = new ConfigStats();
            }

            _statsCache[type].AccessCount++;
            _statsCache[type].LastAccessTime = DateTime.Now;
        }

        private async UniTask ValidateAllConfigsAsync()
        {
            Stopwatch sw = Stopwatch.StartNew();
            ALog.Info("[AsakiFrameworkSetting] 🔍 Validating all configs in editor mode...");

            var allTypes = _metadataCache.Keys.ToList();

            var errors = new System.Collections.Concurrent.ConcurrentBag<string>();
            var warnings = new System.Collections.Concurrent.ConcurrentBag<string>();

            await UniTask.RunOnThreadPool(() =>
            {
                Parallel.ForEach(
                    allTypes,
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    type =>
                    {
                        try
                        {
                            string csvPath = Path.Combine(_csvRootPath, type.Name + ".csv");
                            if (!File.Exists(csvPath))
                            {
                                warnings.Add($"Missing CSV: {type.Name}.csv");
                                return;
                            }

                            FileInfo fileInfo = new FileInfo(csvPath);
                            if (fileInfo.Length < 10)
                            {
                                warnings.Add(
                                    $"{type.Name}. csv is too small ({fileInfo.Length} bytes), might be empty."
                                );
                            }

                            if (_metadataCache.TryGetValue(type, out ConfigMetadata metadata))
                            {
                                if (
                                    metadata.Dependencies != null
                                    && metadata.Dependencies.Length > 0
                                )
                                {
                                    foreach (Type depType in metadata.Dependencies)
                                    {
                                        string depCsvPath = Path.Combine(
                                            _csvRootPath,
                                            depType.Name + ". csv"
                                        );
                                        if (!File.Exists(depCsvPath))
                                        {
                                            errors.Add(
                                                $"{type.Name} depends on {depType.Name}, but CSV not found!"
                                            );
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Validation failed for {type.Name}: {ex.Message}");
                        }
                    }
                );
            });

            sw.Stop();

            foreach (string error in errors)
            {
                ALog.Error($"[AsakiFrameworkSetting] ❌ {error}");
            }

            foreach (string warning in warnings)
            {
                ALog.Warn($"[AsakiFrameworkSetting] ⚠️ {warning}");
            }

            if (errors.Count > 0)
            {
                ALog.Error(
                    $"[AsakiFrameworkSetting] ❌ Validation completed with {errors.Count} errors and {warnings.Count} warnings in {sw.ElapsedMilliseconds}ms."
                );
            }
            else if (warnings.Count > 0)
            {
                ALog.Warn(
                    $"[AsakiFrameworkSetting] ⚠️ Validation completed with {warnings.Count} warnings in {sw.ElapsedMilliseconds}ms."
                );
            }
            else
            {
                ALog.Info(
                    $"[AsakiFrameworkSetting] ✅ All {allTypes.Count} configs validated successfully in {sw.ElapsedMilliseconds}ms."
                );
            }
        }

        private ConfigMetadata GetMetadata(Type configType)
        {
            return _metadataCache.TryGetValue(configType, out ConfigMetadata metadata)
                ? metadata
                : new ConfigMetadata
                {
                    ConfigType = configType,
                    Strategy = AsakiConfigLoadStrategy.OnDemand,
                    Priority = 0,
                    Unloadable = true,
                    Dependencies = Array.Empty<Type>(),
                };
        }
    }
}
