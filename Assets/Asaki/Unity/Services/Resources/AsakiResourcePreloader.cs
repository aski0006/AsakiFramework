using System;
using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Asaki.Core.Resources;
using Asaki.Unity.Bootstrapper;
using Asaki.Unity.Services.Resources.Preloader;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Unity.Services.Resources
{
    /// <summary>
    /// 资源预加载组件
    /// <para>作为协调者整合配置管理、加载执行和资源注册三个子服务。</para>
    /// <para>用于在场景中批量预加载资源，并持有资源引用防止自动卸载。</para>
    /// <para>适用于长期使用、无需销毁的资源（如全局配置、常用UI素材等）。</para>
    /// </summary>
    /// <remarks>
    /// <para>架构设计：</para>
    /// <list type="bullet">
    /// <item><description>PreloadConfigProvider - 配置管理：管理资源组和资源配置</description></item>
    /// <item><description>PreloadExecutor - 加载执行：执行加载操作，管理状态和进度</description></item>
    /// <item><description>PreloadResourceRegistry - 资源注册：管理已加载资源的句柄和访问</description></item>
    /// </list>
    /// <para>使用方式：</para>
    /// <list type="bullet">
    /// <item><description>在Inspector中配置资源组</description></item>
    /// <item><description>框架就绪后自动加载（可配置）</item></item>
    /// <item><description>通过GetResource&lt;T&gt;获取已加载的资源</description></item>
    /// </list>
    /// </remarks>
    public class AsakiResourcePreloader
        : AsakiMono,
            IAsakiAutoInject,
            IAsakiInject<IAsakiResourceService>
    {
        #region Serialized Fields

        [Header("Resource Configuration")]
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

        private IAsakiResourceService _resourceService;

        private PreloadConfigProvider _configProvider;
        private PreloadResourceRegistry _registry;
        private PreloadExecutor _executor;

        #endregion

        #region Properties

        /// <summary>
        /// 当前加载状态
        /// </summary>
        public PreloadState State => _executor?.State ?? PreloadState.Idle;

        /// <summary>
        /// 当前加载进度 (0-1)
        /// </summary>
        public float Progress => _executor?.Progress ?? 0f;

        /// <summary>
        /// 是否加载完成
        /// </summary>
        public bool IsCompleted => _executor?.IsCompleted ?? false;

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading => _executor?.IsLoading ?? false;

        /// <summary>
        /// 已加载的资源数量
        /// </summary>
        public int LoadedResourceCount => _registry?.LoadedResourceCount ?? 0;

        /// <summary>
        /// 配置的资源组列表（只读）
        /// </summary>
        public IReadOnlyList<ResourceGroup> ResourceGroups => _configProvider?.ResourceGroups ?? new List<ResourceGroup>();

        #endregion

        #region Events

        /// <summary>
        /// 进度变化事件
        /// </summary>
        public event Action<float> OnProgressChanged
        {
            add
            {
                if (_executor != null)
                    _executor.OnProgressChanged += value;
            }
            remove
            {
                if (_executor != null)
                    _executor.OnProgressChanged -= value;
            }
        }

        /// <summary>
        /// 加载完成事件
        /// </summary>
        public event Action OnCompleted
        {
            add
            {
                if (_executor != null)
                    _executor.OnCompleted += value;
            }
            remove
            {
                if (_executor != null)
                    _executor.OnCompleted -= value;
            }
        }

        /// <summary>
        /// 加载失败事件
        /// </summary>
        public event Action<Exception> OnFailed
        {
            add
            {
                if (_executor != null)
                    _executor.OnFailed += value;
            }
            remove
            {
                if (_executor != null)
                    _executor.OnFailed -= value;
            }
        }

        /// <summary>
        /// 单个资源组加载完成事件
        /// </summary>
        public event Action<string> OnGroupCompleted
        {
            add
            {
                if (_executor != null)
                    _executor.OnGroupCompleted += value;
            }
            remove
            {
                if (_executor != null)
                    _executor.OnGroupCompleted -= value;
            }
        }

        #endregion

        #region Dependency Injection

        [AsakiInject]
        public void Inject(IAsakiResourceService resourceService)
        {
            _resourceService = resourceService;
            InitializeServices();
        }

        #endregion

        #region Lifecycle Methods

        protected override void OnStart()
        {
            base.OnStart();

            if (_autoStartOnFrameworkReady && _resourceService != null)
            {
                if (_configProvider.HasResourcesToLoad)
                {
                    StartPreload().Forget();
                }
            }
        }

        protected override void Cleanup()
        {
            base.Cleanup();

            _executor?.CancelLoading();
            _executor?.Dispose();
            _registry?.Dispose();
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
            EnsureServicesInitialized();

            if (_executor.IsLoading)
            {
                return;
            }

            await _executor.StartPreloadAsync(_loadGroupsInParallel, onProgress);
        }

        /// <summary>
        /// 加载指定的资源组
        /// </summary>
        /// <param name="groupName">资源组名称</param>
        /// <param name="onProgress">进度回调</param>
        /// <returns>加载任务</returns>
        public async UniTask LoadGroupAsync(string groupName, Action<float> onProgress = null)
        {
            EnsureServicesInitialized();

            await _executor.LoadGroupAsync(groupName, onProgress);
        }

        /// <summary>
        /// 取消正在进行的加载
        /// </summary>
        public void CancelLoading()
        {
            _executor?.CancelLoading();
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
            EnsureServicesInitialized();
            return _registry.GetResource<T>(location);
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
            EnsureServicesInitialized();
            return _registry.TryGetResource(location, out resource);
        }

        /// <summary>
        /// 检查资源是否已加载
        /// </summary>
        /// <param name="location">资源路径</param>
        /// <param name="type">资源类型 (可选，默认检查任意类型)</param>
        /// <returns>是否已加载</returns>
        public bool IsResourceLoaded(string location, Type type = null)
        {
            EnsureServicesInitialized();
            return _registry.IsResourceLoaded(location, type);
        }

        /// <summary>
        /// 获取指定组的所有资源路径
        /// </summary>
        /// <param name="groupName">资源组名称</param>
        /// <returns>资源路径列表</returns>
        public IReadOnlyList<string> GetGroupResourceLocations(string groupName)
        {
            return _configProvider?.GetGroupResourceLocations(groupName) ?? new List<string>();
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
            _registry?.ReleaseResource(location, type);
        }

        /// <summary>
        /// 释放指定资源组的所有资源
        /// </summary>
        /// <param name="groupName">资源组名称</param>
        public void ReleaseGroup(string groupName)
        {
            _registry?.ReleaseGroup(groupName);
            _executor?.Reset();
        }

        /// <summary>
        /// 释放所有持有的资源
        /// </summary>
        public void ReleaseAllResources()
        {
            _registry?.ReleaseAllResources();
            _executor?.Reset();
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
                return null;
            }

            var preloader = host.AddComponent<AsakiResourcePreloader>();

            if (AsakiBootstrapper.IsReady)
            {
                AsakiMonoLifecycleManager.Instance.ProcessComponentImmediately(preloader);
            }

            return preloader;
        }

        #endregion

        #region Private Methods

        private void InitializeServices()
        {
            if (_configProvider == null)
            {
                _configProvider = new PreloadConfigProvider();
            }

            _configProvider.SetResourceGroups(_resourceGroups);

            if (_registry == null)
            {
                _registry = new PreloadResourceRegistry();
            }

            if (_executor == null && _resourceService != null)
            {
                _executor = new PreloadExecutor(_resourceService, _configProvider, _registry);
            }
        }

        private void EnsureServicesInitialized()
        {
            if (_configProvider == null || _registry == null || _executor == null)
            {
                throw new InvalidOperationException(
                    $"[{nameof(AsakiResourcePreloader)}] Services not initialized. Ensure ResourceService is injected."
                );
            }
        }

        #endregion
    }
}
