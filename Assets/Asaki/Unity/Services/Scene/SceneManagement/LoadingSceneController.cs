using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Asaki.Core.Resources;
using Asaki.Core.Scene;
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
            IAsakiInit<IAsakiResourceService, IAsakiSceneManagerService>
    {
        [Header("Configuration")]
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

            // 初始化视图接口
            InitializeView();

            _payload = SceneLoadStateService.GetPayload();

            if (_payload == null)
            {
                ALog.Error(
                    "[LoadingSceneController] No payload found, cannot proceed with loading"
                );
                UpdateProgress(0f, null);
                ShowError("加载参数丢失");
                return;
            }

            ALog.Info(
                $"[LoadingSceneController] Starting preload for scene: {_payload.TargetSceneName}"
            );
            StartLoading().Forget();
        }

        /// <summary>
        /// 初始化视图接口
        /// </summary>
        private void InitializeView()
        {
            if (_loadingView == null)
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
        }

        private async UniTaskVoid StartLoading()
        {
            _loadingCts = new CancellationTokenSource();
            var token = _loadingCts.Token;

            try
            {
                UpdateProgress(0f, "准备加载...");

                var config = GetPreloadConfig();

                if (config != null && config.Resources.Count > 0)
                {
                    await PreloadResourcesAsync(config, token);
                }

                UpdateProgress(1f, "加载完成");

                if (_payload.UsePreload && config != null && config.AutoTransition)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(0.3f), cancellationToken: token);
                    await LoadTargetSceneAsync(token);
                }
            }
            catch (OperationCanceledException)
            {
                ALog.Info("[LoadingSceneController] Loading cancelled");
            }
            catch (Exception ex)
            {
                ALog.Error($"[LoadingSceneController] Loading failed: {ex}");
                ShowError("加载失败，请重试");
            }
        }

        private ScenePreloadConfig GetPreloadConfig()
        {
            if (_preloadDatabase == null)
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
            var handles = new List<ResHandle<Object>>();
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

                UpdateProgress(progresses.Average() * 0.9f, $"加载资源 ({i + 1}/{totalCount})...");

                try
                {
                    var type = entry.ResourceType?.GetResourceType() ?? typeof(Object);
                    var handle = await LoadResourceAsync(entry.Location, type, token);

                    if (handle != null)
                    {
                        handles.Add(handle);
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

            ALog.Info($"[LoadingSceneController] Preloaded {handles.Count} resources successfully");
        }

        private async UniTask<ResHandle<Object>> LoadResourceAsync(
            string location,
            Type type,
            CancellationToken token
        )
        {
            var method = _resourceService.GetType().GetMethod("LoadAsync");
            var genericMethod = method.MakeGenericMethod(type);

            Action<float> progressCallback = (p) => { };

            var task = (UniTask)
                genericMethod.Invoke(
                    _resourceService,
                    new object[] { location, progressCallback, token }
                );

            await task;

            var resultProperty = task.GetType().GetProperty("Result");
            var handle = resultProperty?.GetValue(task);

            return handle as ResHandle<Object>;
        }

        private async UniTask LoadTargetSceneAsync(CancellationToken token)
        {
            ALog.Info($"[LoadingSceneController] Loading target scene: {_payload.TargetSceneName}");

            var result = await _sceneManager.LoadSceneAsync(
                _payload.TargetSceneName,
                _payload.LoadMode,
                _payload.Activation,
                null,
                token
            );

            if (!result.IsSuccess)
            {
                ALog.Error($"[LoadingSceneController] Failed to load scene: {result.ErrorMessage}");
                ShowError("场景加载失败");
            }
        }

        private void UpdateProgress(float progress, string message = null)
        {
            _currentProgress = progress;

            // 通过接口更新视图，解耦具体 UI 实现
            _loadingSceneView?.UpdateProgress(progress);

            if (!string.IsNullOrEmpty(message))
            {
                _loadingSceneView?.UpdateTip(message);
            }
        }

        private void ShowError(string message)
        {
            _loadingSceneView?.ShowError(message);
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
