using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Asaki.Core.Resources;
using Asaki.Unity.Bootstrapper;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Unity.Services.Resources
{
    /// <summary>
    /// 资源预加载组件
    /// <para>用于在场景中批量预加载资源，并持有资源引用防止自动卸载。</para>
    /// <para>适用于长期使用、无需销毁的资源（如全局配置、常用UI素材等）。</para>
    /// </summary>
    /// <remarks>
    /// <para>核心功能：</para>
    /// <list type="bullet">
    /// <item><description>批量资源加载：支持按组组织资源，分批或并行加载</description></item>
    /// <item><description>进度反馈：提供0-1范围的加载进度回调</description></item>
    /// <item><description>资源持有：组件持有ResHandle，防止资源被自动卸载</description></item>
    /// <item><description>类型安全：支持通过泛型方法获取特定类型的资源</description></item>
    /// <item><description>生命周期集成：与AsakiMono生命周期管理正确集成</description></item>
    /// </list>
    /// </remarks>
    public class AsakiResourcePreloader
        : AsakiMono,
            IAsakiAutoInject,
            IAsakiInit<IAsakiResourceService>
    {
        #region Enums

        /// <summary>
        /// 资源加载状态
        /// </summary>
        public enum PreloadState
        {
            /// <summary>未开始加载</summary>
            Idle,

            /// <summary>正在加载中</summary>
            Loading,

            /// <summary>加载完成</summary>
            Completed,

            /// <summary>加载失败</summary>
            Failed,
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// 预加载资源配置项
        /// </summary>
        [Serializable]
        public class PreloadResourceEntry
        {
            [Tooltip("资源加载路径")]
            public string Location;

            [Tooltip("资源类型")]
            [SerializeReference]
            [AsakiResourceType]
            public SerializableResourceType ResourceType = new GameObjectResourceType();
        }

        /// <summary>
        /// 资源组配置
        /// </summary>
        [Serializable]
        public class ResourceGroup
        {
            [Tooltip("资源组名称")]
            public string GroupName = "New Group";

            [Tooltip("该组包含的资源")]
            public List<PreloadResourceEntry> Resources = new();
        }

        #endregion

        #region Serialized Fields

        [Header("Resource DataTable")]
        [SerializeField]
        [Tooltip("预加载的资源组列表")]
        private List<ResourceGroup> _resourceGroups = new();

        [Header("Loading Options")]
        [SerializeField]
        [Tooltip("框架就绪后自动开始加载")]
        private bool _autoStartOnFrameworkReady = true;

        [SerializeField]
        [Tooltip("各组并行加载（加载更快但内存峰值更高）")]
        private bool _loadGroupsInParallel = true;

        #endregion

        #region Private Fields

        // 依赖注入的服务
        private IAsakiResourceService _resourceService;

        private readonly List<ResHandle<Object>> _loadedHandles = new();
        private readonly Dictionary<(string Location, Type Type), ResHandle<Object>> _resourceMap =
            new();
        private readonly Dictionary<string, List<ResHandle<Object>>> _groupHandlesMap = new();

        // 取消令牌源
        private CancellationTokenSource _loadingCts;

        #endregion

        #region Properties

        /// <summary>
        /// 当前加载状态
        /// </summary>
        public PreloadState State { get; private set; } = PreloadState.Idle;

        /// <summary>
        /// 当前加载进度 (0-1)
        /// </summary>
        public float Progress { get; private set; } = 0f;

        /// <summary>
        /// 是否加载完成
        /// </summary>
        public bool IsCompleted => State == PreloadState.Completed;

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading => State == PreloadState.Loading;

        /// <summary>
        /// 已加载的资源数量
        /// </summary>
        public int LoadedResourceCount => _loadedHandles.Count;

        /// <summary>
        /// 配置的资源组列表（只读）
        /// </summary>
        public IReadOnlyList<ResourceGroup> ResourceGroups => _resourceGroups;

        #endregion

        #region Events

        /// <summary>
        /// 进度变化事件
        /// </summary>
        public event Action<float> OnProgressChanged;

        /// <summary>
        /// 加载完成事件
        /// </summary>
        public event Action OnCompleted;

        /// <summary>
        /// 加载失败事件
        /// </summary>
        public event Action<Exception> OnFailed;

        /// <summary>
        /// 单个资源组加载完成事件
        /// </summary>
        public event Action<string> OnGroupCompleted;

        #endregion

        #region Dependency Injection

        /// <summary>
        /// 依赖注入点 - 由框架自动调用
        /// </summary>
        [AsakiInject]
        public void Init(IAsakiResourceService resourceService)
        {
            _resourceService = resourceService;
            ALog.Info($"[{nameof(AsakiResourcePreloader)}] ResourceService injected successfully.");
        }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// 框架就绪后的初始化
        /// </summary>
        protected override void OnStart()
        {
            base.OnStart();

            if (_autoStartOnFrameworkReady && _resourceService != null)
            {
                if (_resourceGroups.Count > 0 && _resourceGroups.Any(g => g.Resources.Count > 0))
                {
                    StartPreload().Forget();
                }
                else
                {
                    ALog.Warn(
                        $"[{nameof(AsakiResourcePreloader)}] No resources configured for preloading."
                    );
                }
            }
        }

        protected override void Cleanup()
        {
            base.Cleanup();

            var cts = _loadingCts;
            _loadingCts = null;
            cts?.Cancel();
            cts?.Dispose();

            ReleaseAllResources();
        }

        #endregion

        #region Public API - Loading

        /// <summary>
        /// 开始预加载所有配置的资源
        /// </summary>
        /// <param name="onProgress">进度回调 (0-1)</param>
        /// <returns>加载任务</returns>
        public async UniTask StartPreload(Action<float> onProgress = null)
        {
            if (State == PreloadState.Loading)
            {
                ALog.Warn($"[{nameof(AsakiResourcePreloader)}] Preload already in progress.");
                return;
            }

            if (_resourceService == null)
            {
                State = PreloadState.Failed;
                var ex = new InvalidOperationException("ResourceService not available.");
                OnFailed?.Invoke(ex);
                throw ex;
            }

            // 创建新的取消令牌
            _loadingCts?.Dispose();
            _loadingCts = new CancellationTokenSource();
            var token = _loadingCts.Token;

            State = PreloadState.Loading;
            Progress = 0f;

            try
            {
                ALog.Info($"[{nameof(AsakiResourcePreloader)}] Starting preload...");

                if (_loadGroupsInParallel)
                {
                    await LoadGroupsInParallel(onProgress, token);
                }
                else
                {
                    await LoadGroupsSequentially(onProgress, token);
                }

                State = PreloadState.Completed;
                Progress = 1f;

                ALog.Info(
                    $"[{nameof(AsakiResourcePreloader)}] Preload completed. Total resources: {LoadedResourceCount}"
                );

                OnProgressChanged?.Invoke(1f);
                OnCompleted?.Invoke();
            }
            catch (OperationCanceledException)
            {
                ALog.Info($"[{nameof(AsakiResourcePreloader)}] Preload cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                State = PreloadState.Failed;
                ALog.Error($"[{nameof(AsakiResourcePreloader)}] Preload failed: {ex}");
                OnFailed?.Invoke(ex);
                throw;
            }
        }

        /// <summary>
        /// 加载指定的资源组
        /// </summary>
        /// <param name="groupName">资源组名称</param>
        /// <param name="onProgress">进度回调</param>
        /// <returns>加载任务</returns>
        public async UniTask LoadGroupAsync(string groupName, Action<float> onProgress = null)
        {
            if (_resourceService == null)
            {
                throw new InvalidOperationException("ResourceService not available.");
            }

            var group = _resourceGroups.FirstOrDefault(g => g.GroupName == groupName);
            if (group == null)
            {
                throw new ArgumentException($"Resource group '{groupName}' not found.");
            }

            var token = _loadingCts?.Token ?? CancellationToken.None;

            await LoadGroupInternalAsync(group, onProgress, token);
        }

        /// <summary>
        /// 取消正在进行的加载
        /// </summary>
        public void CancelLoading()
        {
            _loadingCts?.Cancel();
        }

        #endregion

        #region Public API - Resource Access

        /// <summary>
        /// 获取已加载的资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="location">资源路径</param>
        /// <returns>资源实例，如果未找到则返回null</returns>
        public T GetResource<T>(string location)
            where T : class
        {
            var key = (location, typeof(T));
            if (!_resourceMap.TryGetValue(key, out var handle))
            {
                ALog.Warn(
                    $"[{nameof(AsakiResourcePreloader)}] Resource not found: {location} (Type: {typeof(T).Name})"
                );
                return null;
            }

            if (!handle.IsValid)
            {
                ALog.Warn(
                    $"[{nameof(AsakiResourcePreloader)}] Resource handle is invalid: {location}"
                );
                return null;
            }

            return handle.Asset as T;
        }

        /// <summary>
        /// 尝试获取已加载的资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="location">资源路径</param>
        /// <param name="resource">输出资源实例</param>
        /// <returns>是否成功获取</returns>
        public bool TryGetResource<T>(string location, out T resource)
            where T : class
        {
            var key = (location, typeof(T));
            if (_resourceMap.TryGetValue(key, out var handle) && handle.IsValid)
            {
                resource = handle.Asset as T;
                return resource != null;
            }
            resource = null;
            return false;
        }

        /// <summary>
        /// 检查资源是否已加载
        /// </summary>
        /// <param name="location">资源路径</param>
        /// <param name="type">资源类型 (可选，默认检查任意类型)</param>
        /// <returns>是否已加载</returns>
        public bool IsResourceLoaded(string location, Type type = null)
        {
            if (type != null)
            {
                var key = (location, type);
                return _resourceMap.TryGetValue(key, out var handle) && handle.IsValid;
            }

            foreach (var kvp in _resourceMap)
            {
                if (kvp.Key.Location == location && kvp.Value.IsValid)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 获取指定组的所有资源路径
        /// </summary>
        /// <param name="groupName">资源组名称</param>
        /// <returns>资源路径列表</returns>
        public IReadOnlyList<string> GetGroupResourceLocations(string groupName)
        {
            var group = _resourceGroups.FirstOrDefault(g => g.GroupName == groupName);
            if (group == null)
                return new List<string>();

            return group.Resources.Select(r => r.Location).ToList();
        }

        #endregion

        #region Public API - Resource Release

        /// <summary>
        /// 释放指定的资源
        /// </summary>
        /// <param name="location">资源路径</param>
        /// <param name="type">资源类型 (可选，默认释放所有匹配 location 的资源)</param>
        public void ReleaseResource(string location, Type type = null)
        {
            if (type != null)
            {
                var key = (location, type);
                if (_resourceMap.TryGetValue(key, out var handle))
                {
                    handle.Dispose();
                    _resourceMap.Remove(key);
                    _loadedHandles.Remove(handle);

                    foreach (var groupList in _groupHandlesMap.Values)
                    {
                        groupList.Remove(handle);
                    }

                    ALog.Info(
                        $"[{nameof(AsakiResourcePreloader)}] Released resource: {location} (Type: {type.Name})"
                    );
                }
            }
            else
            {
                var keysToRemove = _resourceMap
                    .Where(kvp => kvp.Key.Location == location)
                    .Select(kvp => kvp.Key)
                    .ToList();
                foreach (var key in keysToRemove)
                {
                    if (_resourceMap.TryGetValue(key, out var handle))
                    {
                        handle.Dispose();
                        _loadedHandles.Remove(handle);

                        foreach (var groupList in _groupHandlesMap.Values)
                        {
                            groupList.Remove(handle);
                        }
                    }
                    _resourceMap.Remove(key);
                }

                if (keysToRemove.Count > 0)
                {
                    ALog.Info(
                        $"[{nameof(AsakiResourcePreloader)}] Released {keysToRemove.Count} resource(s): {location}"
                    );
                }
            }
        }

        /// <summary>
        /// 释放指定资源组的所有资源
        /// </summary>
        /// <param name="groupName">资源组名称</param>
        public void ReleaseGroup(string groupName)
        {
            if (!_groupHandlesMap.TryGetValue(groupName, out var handles))
                return;

            foreach (var handle in handles)
            {
                var key = (handle.Location, handle.Asset?.GetType() ?? typeof(Object));
                if (_resourceMap.TryGetValue(key, out var mapHandle) && mapHandle == handle)
                {
                    _resourceMap.Remove(key);
                }
                handle.Dispose();
                _loadedHandles.Remove(handle);
            }

            handles.Clear();
            _groupHandlesMap.Remove(groupName);

            ALog.Info($"[{nameof(AsakiResourcePreloader)}] Released group: {groupName}");
        }

        /// <summary>
        /// 释放所有持有的资源
        /// </summary>
        public void ReleaseAllResources()
        {
            foreach (var handle in _loadedHandles)
            {
                handle.Dispose();
            }

            _loadedHandles.Clear();
            _resourceMap.Clear();
            _groupHandlesMap.Clear();

            State = PreloadState.Idle;
            Progress = 0f;

            ALog.Info($"[{nameof(AsakiResourcePreloader)}] All resources released.");
        }

        #endregion

        #region Private Methods - Loading Implementation

        private async UniTask LoadGroupsSequentially(
            Action<float> onProgress,
            CancellationToken token
        )
        {
            int totalGroups = _resourceGroups.Count;
            float[] groupProgresses = new float[totalGroups];

            for (int i = 0; i < totalGroups; i++)
            {
                var group = _resourceGroups[i];
                int groupIndex = i;

                if (group.Resources.Count == 0)
                {
                    groupProgresses[i] = 1f;
                    continue;
                }

                await LoadGroupInternalAsync(
                    group,
                    (p) =>
                    {
                        groupProgresses[groupIndex] = p;
                        UpdateOverallProgress(groupProgresses, onProgress);
                    },
                    token
                );

                groupProgresses[i] = 1f;
                UpdateOverallProgress(groupProgresses, onProgress);
            }
        }

        private async UniTask LoadGroupsInParallel(
            Action<float> onProgress,
            CancellationToken token
        )
        {
            int totalGroups = _resourceGroups.Count;
            float[] groupProgresses = new float[totalGroups];

            var tasks = new List<UniTask>();

            for (int i = 0; i < totalGroups; i++)
            {
                var group = _resourceGroups[i];
                int groupIndex = i;

                if (group.Resources.Count == 0)
                {
                    groupProgresses[i] = 1f;
                    continue;
                }

                var task = LoadGroupInternalAsync(
                    group,
                    (p) =>
                    {
                        groupProgresses[groupIndex] = p;
                        UpdateOverallProgress(groupProgresses, onProgress);
                    },
                    token
                );

                tasks.Add(task);
            }

            if (tasks.Count > 0)
            {
                await UniTask.WhenAll(tasks);
            }
        }

        private async UniTask LoadGroupInternalAsync(
            ResourceGroup group,
            Action<float> onProgress,
            CancellationToken token
        )
        {
            var entries = group.Resources.Where(r => !string.IsNullOrEmpty(r.Location)).ToList();

            if (entries.Count == 0)
            {
                onProgress?.Invoke(1f);
                return;
            }

            ALog.Info(
                $"[{nameof(AsakiResourcePreloader)}] Loading group '{group.GroupName}' with {entries.Count} resources..."
            );

            var handles = new List<ResHandle<Object>>();
            float[] progresses = new float[entries.Count];

            for (int i = 0; i < entries.Count; i++)
            {
                int index = i;
                var entry = entries[i];
                var type = entry.ResourceType?.GetResourceType() ?? typeof(Object);

                try
                {
                    Action<float> itemProgress = (p) =>
                    {
                        progresses[index] = p;
                        float overall = progresses.Average();
                        onProgress?.Invoke(overall);
                    };

                    var handle = await _resourceService.LoadAsync(
                        entry.Location,
                        type,
                        itemProgress,
                        token
                    );

                    handles.Add(handle);
                    _resourceMap[(entry.Location, type)] = handle;

                    progresses[i] = 1f;
                }
                catch (Exception ex)
                {
                    ALog.Error(
                        $"[{nameof(AsakiResourcePreloader)}] Failed to load resource '{entry.Location}': {ex}"
                    );
                    throw;
                }
            }

            if (!_groupHandlesMap.TryGetValue(group.GroupName, out var groupHandles))
            {
                groupHandles = new List<ResHandle<Object>>();
                _groupHandlesMap[group.GroupName] = groupHandles;
            }
            groupHandles.AddRange(handles);
            _loadedHandles.AddRange(handles);

            onProgress?.Invoke(1f);
            OnGroupCompleted?.Invoke(group.GroupName);

            ALog.Info(
                $"[{nameof(AsakiResourcePreloader)}] Group '{group.GroupName}' loaded successfully."
            );
        }

        private void UpdateOverallProgress(float[] groupProgresses, Action<float> onProgress)
        {
            float overall = groupProgresses.Average();
            Progress = overall;
            onProgress?.Invoke(overall);
            OnProgressChanged?.Invoke(overall);
        }

        #endregion

        #region Static Factory

        /// <summary>
        /// 创建预加载组件实例
        /// </summary>
        /// <param name="host">宿主GameObject</param>
        /// <returns>预加载组件实例</returns>
        public static AsakiResourcePreloader Create(GameObject host)
        {
            if (host == null)
            {
                ALog.Error(
                    $"[{nameof(AsakiResourcePreloader)}] Cannot create - host GameObject is null."
                );
                return null;
            }

            var preloader = host.AddComponent<AsakiResourcePreloader>();

            // 如果框架已就绪，手动触发初始化
            if (AsakiBootstrapper.IsReady)
            {
                AsakiMonoLifecycleManager.Instance.ProcessComponentImmediately(preloader);
            }

            return preloader;
        }

        #endregion
    }
}
