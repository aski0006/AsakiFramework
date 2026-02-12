using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Asaki.Core.Resources;
using Asaki.Core.Scene;
using Asaki.Core.Scene.SceneManagement;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Unity.Services.Scene.SceneManagement
{
    /// <summary>
    /// 过渡场景控制器
    /// 负责在Loading场景中执行资源预加载并切换到目标场景
    /// 通过 ILoadingSceneView 接口与 UI 解耦，便于框架打包为 UPM
    /// </summary>
    public class LoadingSceneController
        : AsakiMono,
            IAsakiAutoInject,
            IAsakiInit<IAsakiResourceService, IAsakiSceneManagerService>
    {
        [Header("DataTable")]
        [Tooltip("场景预加载配置数据库")]
        [SerializeField]
        private ScenePreloadDatabase _preloadDatabase;

        [Tooltip("默认过渡场景名称")]
        [SerializeField]
        private string _defaultLoadingSceneName = "LoadingScene";

        [Header("Loading UI")]
        [Tooltip("加载场景视图组件（可选，实现 ILoadingSceneView 接口）")]
        [SerializeField]
        private MonoBehaviour _loadingView;

        private IAsakiResourceService _resourceService;
        private IAsakiSceneManagerService _sceneManager;
        private ILoadingSceneView _loadingSceneView;
        private SceneLoadPayload _payload;
        private CancellationTokenSource _loadingCts;
        private float _currentProgress;

        /// <summary>
        /// 预加载资源句柄列表 - 用于防止资源泄漏
        /// </summary>
        private readonly List<ResHandle<Object>> _preloadHandles = new List<ResHandle<Object>>();

        [AsakiInject]
        public void Init(
            IAsakiResourceService resourceService,
            IAsakiSceneManagerService sceneManager
        )
        {
            _resourceService = resourceService;
            _sceneManager = sceneManager;
            ALog.Info("[LoadingSceneController] ResourceService and SceneManager injected");
        }

        protected override void OnStart()
        {
            base.OnStart();

            ALog.Info("[LoadingSceneController] OnStart called");

            // 初始化视图接口
            InitializeView();

            // 使用 Service 实例获取 Payload，替代静态 SceneLoadStateService
            _payload = _sceneManager?.CurrentPayload;

            if (_payload == null)
            {
                ALog.Error(
                    "[LoadingSceneController] No payload found, cannot proceed with loading. "
                        + "Make sure LoadSceneWithPreloadAsync was called before loading this scene."
                );
                return;
            }

            ALog.Info(
                $"[LoadingSceneController] Payload received: TargetScene={_payload.TargetSceneName}, "
                    + $"LoadMode={_payload.LoadMode}, Activation={_payload.Activation}, UsePreload={_payload.UsePreload}"
            );
            StartLoading().Forget();
        }

        /// <summary>
        /// 初始化视图接口
        /// </summary>
        private void InitializeView()
        {
            if (!_loadingView)
            {
                ALog.Warn(
                    "[LoadingSceneController] No loading view assigned, UI updates will be ignored"
                );
                return;
            }

            _loadingSceneView = _loadingView as ILoadingSceneView;

            if (_loadingSceneView == null)
            {
                ALog.Error(
                    $"[LoadingSceneController] Assigned view '{_loadingView.name}' does not implement ILoadingSceneView"
                );
                return;
            }

            _loadingSceneView.Show();
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            _loadingCts?.Cancel();
            _loadingCts?.Dispose();
            _loadingCts = null;

            // 修复：释放所有预加载的资源句柄，防止资源泄漏
            ReleasePreloadedResources();
        }

        /// <summary>
        /// 释放所有预加载的资源句柄
        /// </summary>
        private void ReleasePreloadedResources()
        {
            if (_preloadHandles.Count == 0)
                return;

            ALog.Info($"[LoadingSceneController] Releasing {_preloadHandles.Count} preloaded resources");

            foreach (var handle in _preloadHandles)
            {
                if (handle != null && handle.IsValid)
                {
                    handle.Dispose();
                }
            }

            _preloadHandles.Clear();
            ALog.Info("[LoadingSceneController] All preloaded resources released");
        }

        private async UniTaskVoid StartLoading()
        {
            _loadingCts = new CancellationTokenSource();
            var token = _loadingCts.Token;

            try
            {
                UpdateProgress(0f);

                var config = GetPreloadConfig();
                ALog.Info(
                    $"[LoadingSceneController] Preload config: {(config != null ? "found" : "not found")}, Resources count: {(config?.Resources.Count ?? 0)}"
                );

                if (config != null && config.Resources.Count > 0)
                {
                    await PreloadResourcesAsync(config, token);
                }

                UpdateProgress(1f);

                if (_payload.UsePreload)
                {
                    ALog.Info(
                        $"[LoadingSceneController] UsePreload=true, config.AutoTransition={(config?.AutoTransition ?? false)}"
                    );
                    if (config != null && config.AutoTransition)
                    {
                        ALog.Info(
                            "[LoadingSceneController] AutoTransition enabled, loading target scene..."
                        );
                        await UniTask.Delay(TimeSpan.FromSeconds(0.3f), cancellationToken: token);
                        // 使用 CancellationToken.None 加载目标场景，因为场景切换时当前对象会被销毁
                        await LoadTargetSceneAsync(CancellationToken.None);
                    }
                    else
                    {
                        ALog.Info(
                            "[LoadingSceneController] AutoTransition disabled, waiting for manual transition"
                        );
                    }
                }
                else
                {
                    ALog.Info(
                        "[LoadingSceneController] UsePreload=false, loading target scene directly..."
                    );
                    // 使用 CancellationToken.None 加载目标场景，因为场景切换时当前对象会被销毁
                    await LoadTargetSceneAsync(CancellationToken.None);
                }
            }
            catch (OperationCanceledException)
            {
                ALog.Info("[LoadingSceneController] Loading cancelled");
            }
            catch (Exception ex)
            {
                ALog.Error($"[LoadingSceneController] Loading failed: {ex}");
            }
        }

        private ScenePreloadConfig GetPreloadConfig()
        {
            if (!_preloadDatabase)
            {
                ALog.Warn("[LoadingSceneController] PreloadDatabase not assigned");
                return null;
            }

            return _preloadDatabase.GetConfig(_payload.TargetSceneName);
        }

        private async UniTask PreloadResourcesAsync(
            ScenePreloadConfig config,
            CancellationToken token
        )
        {
            var resources = config.Resources;
            int totalCount = resources.Count;
            float[] progresses = new float[totalCount];

            ALog.Info($"[LoadingSceneController] Preloading {totalCount} resources...");

            if (_payload.TimeoutSeconds > 0)
            {
                _resourceService.SetTimeoutSeconds(_payload.TimeoutSeconds);
            }

            for (int i = 0; i < totalCount; i++)
            {
                int index = i;
                var entry = resources[i];

                if (string.IsNullOrEmpty(entry.Location))
                {
                    progresses[index] = 1f;
                    continue;
                }

                UpdateProgress(progresses.Average() * 0.9f);

                try
                {
                    var type = entry.ResourceType?.GetResourceType() ?? typeof(Object);
                    var handle = await LoadResourceAsync(entry.Location, type, token);

                    if (handle != null)
                    {
                        // 保存句柄以便后续释放
                        _preloadHandles.Add(handle);
                    }

                    progresses[index] = 1f;
                    UpdateProgress(progresses.Average() * 0.9f);
                }
                catch (Exception ex)
                {
                    ALog.Error(
                        $"[LoadingSceneController] Failed to load resource '{entry.Location}': {ex}"
                    );
                    throw;
                }
            }

            ALog.Info($"[LoadingSceneController] Preloaded {_preloadHandles.Count} resources successfully");
        }

        private async UniTask<ResHandle<Object>> LoadResourceAsync(
            string location,
            Type type,
            CancellationToken token
        )
        {
            // 使用非泛型接口方法，避免运行时反射
            return await _resourceService.LoadAsync(location, type, null, token);
        }

        private async UniTask LoadTargetSceneAsync(CancellationToken token)
        {
            if (_sceneManager == null)
            {
                ALog.Error(
                    "[LoadingSceneController] _sceneManager is null! Make sure IAsakiInit is properly implemented and injection is complete."
                );
                // 通知失败
                _sceneManager?.NotifyPreloadFinished(false, _payload?.TargetSceneName ?? "Unknown");
                return;
            }

            if (_payload == null)
            {
                ALog.Error(
                    "[LoadingSceneController] _payload is null! This should not happen after OnStart."
                );
                _sceneManager.NotifyPreloadFinished(false, "Unknown");
                return;
            }

            ALog.Info($"[LoadingSceneController] Loading target scene: {_payload.TargetSceneName}");

            AsakiSceneResult result;
            try
            {
                result = await _sceneManager.LoadSceneAsync(
                    _payload.TargetSceneName,
                    _payload.LoadMode,
                    _payload.Activation,
                    null,
                    token
                );
            }
            catch (Exception ex)
            {
                ALog.Error($"[LoadingSceneController] Exception during scene load: {ex}");
                _sceneManager.NotifyPreloadFinished(false, _payload.TargetSceneName);
                return;
            }

            if (!result.IsSuccess)
            {
                ALog.Error($"[LoadingSceneController] Failed to load scene: {result.ErrorMessage}");
                _sceneManager.NotifyPreloadFinished(false, _payload.TargetSceneName);
            }
            else
            {
                ALog.Info($"[LoadingSceneController] Target scene loaded successfully: {_payload.TargetSceneName}");
                // 修复：通知预加载流程成功完成
                _sceneManager.NotifyPreloadFinished(true, _payload.TargetSceneName);
            }
        }

        private void UpdateProgress(float progress)
        {
            _currentProgress = progress;
            _loadingSceneView?.UpdateProgress(progress);
        }

        /// <summary>
        /// 手动触发场景切换（用于非自动过渡模式）
        /// </summary>
        public void TriggerSceneTransition()
        {
            if (_payload != null)
            {
                LoadTargetSceneAsync(CancellationToken.None).Forget();
            }
        }

        /// <summary>
        /// 取消加载
        /// </summary>
        public void CancelLoading()
        {
            _loadingCts?.Cancel();
        }
    }
}