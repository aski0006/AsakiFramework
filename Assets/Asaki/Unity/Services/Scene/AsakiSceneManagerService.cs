using System;
using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Async;
using Asaki.Core.Broker;
using Asaki.Core.Logging;
using Asaki.Core.Resources;
using Asaki.Core.Scene;
using Asaki.Core.Scene.SceneManagement;
using Asaki.Unity.Services.Scene.SceneManagement;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Asaki.Unity.Services.Scene
{
    public class AsakiSceneManagerService : IAsakiSceneManagerService
    {
        private readonly IAsakiEventService _asakiEventService;
        private readonly IAsakiAsyncService _asakiAsyncService;
        private readonly IAsakiResourceService _asakiResourceService;
        private HashSet<string> _validScene;
        private bool _isLoading;
        private bool _isDisposed;
        public string LastLoadedSceneName { get; private set; }

        /// <summary>
        /// 当前待处理的场景加载参数
        /// </summary>
        public SceneLoadPayload CurrentPayload { get; private set; }

        private UniTaskCompletionSource<bool> _activationTaskSignal;
        private UniTaskCompletionSource<AsakiSceneResult> _preloadFlowSource;

        public AsakiSceneManagerService(
            IAsakiEventService asakiEventService,
            IAsakiAsyncService asakiAsyncService,
            IAsakiResourceService asakiResourceService
        )
        {
            _asakiEventService = asakiEventService;
            _asakiAsyncService = asakiAsyncService;
            _asakiResourceService = asakiResourceService;
        }

        private bool IsSceneValid(string sceneName)
        {
            // 按需检查，避免启动时遍历所有场景
            _validScene ??= new HashSet<string>();

            // 如果已经验证过，直接返回
            if (_validScene.Contains(sceneName))
                return true;

            // 按需检查特定场景是否存在
            int count = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
            {
                try
                {
                    string path = SceneUtility.GetScenePathByBuildIndex(i);
                    if (!string.IsNullOrEmpty(path))
                    {
                        string name = System.IO.Path.GetFileNameWithoutExtension(path);
                        _validScene.Add(name);
                        if (name == sceneName)
                            return true;
                    }
                }
                catch (Exception e)
                {
                    ALog.Error($"Failed to get scene path at index {i}", e);
                }
            }
            return false;
        }

        public void PerBuildScene()
        {
            // 按需检查模式，不再在启动时遍历所有场景
            _validScene ??= new HashSet<string>();
            ALog.Info("[SceneService] PerBuildScene called - using on-demand validation");
        }

        public async UniTask<AsakiSceneResult> LoadSceneAsync(
            string targetScene,
            AsakiLoadSceneMode mode = AsakiLoadSceneMode.Single,
            AsakiSceneActivation activation = AsakiSceneActivation.Immediate,
            IAsakiSceneTransition transition = null,
            CancellationToken token = default(CancellationToken)
        )
        {
            return await LoadSceneInternalAsync(
                targetScene,
                mode,
                activation,
                transition,
                token,
                true
            );
        }

        /// <summary>
        /// 内部场景加载方法，支持跳过 _isLoading 检查（用于 LoadSceneWithPreloadAsync）
        /// </summary>
        private async UniTask<AsakiSceneResult> LoadSceneInternalAsync(
            string targetScene,
            AsakiLoadSceneMode mode,
            AsakiSceneActivation activation,
            IAsakiSceneTransition transition,
            CancellationToken token,
            bool checkIsLoading
        )
        {
            if (checkIsLoading && _isLoading)
            {
                transition?.Dispose();
                return AsakiSceneResult.Failed(targetScene, "Another scene load is in progress");
            }
            if (!IsSceneValid(targetScene))
            {
                transition?.Dispose();
                return AsakiSceneResult.Failed(
                    targetScene,
                    $"Scene '{targetScene}' not found in BuildSettings."
                );
            }
            _isLoading = true;
            _asakiEventService.Publish(
                new AsakiSceneStateEvent(targetScene, AsakiSceneStateEvent.State.Started)
            );
            Action<float> transitionProgress = null;
            if (transition != null)
                transitionProgress = transition.OnProgress;
            try
            {
                if (transition != null)
                    await transition.EnterAsync(token);
                if (mode == AsakiLoadSceneMode.Single)
                {
                    await _asakiAsyncService.WaitFrame(token);
                    await _asakiResourceService.UnloadUnusedAssets(token);
                    // 移除强制 GC.Collect()，避免在过渡动画期间造成卡顿
                    // 依赖 Unity 的自动垃圾回收机制
                }

                LoadSceneMode unityMode =
                    mode == AsakiLoadSceneMode.Single
                        ? LoadSceneMode.Single
                        : LoadSceneMode.Additive;
                AsyncOperation op = SceneManager.LoadSceneAsync(targetScene, unityMode);
                if (op == null)
                    return AsakiSceneResult.Failed(
                        targetScene,
                        "Unity internal error : AsyncOperation is null"
                    );
                op.allowSceneActivation = false;
                float lastProgress = 0f;
                float lastReportTime = UnityEngine.Time.realtimeSinceStartup;

                // 修复：使用 < 0.9f 而不是 Mathf.Approximately，避免浮点数精度问题
                while (op.progress < 0.9f)
                {
                    if (token.IsCancellationRequested)
                        return CancelSceneLoadOperation(targetScene);
                    float raw = op.progress;
                    float normalized = Mathf.Clamp01(raw / 0.9f);
                    float timeNow = UnityEngine.Time.realtimeSinceStartup;

                    if (normalized > lastProgress + 0.01f || timeNow - lastReportTime > 0.1f)
                    {
                        lastProgress = normalized;
                        lastReportTime = timeNow;

                        AsakiBroker.Publish(new AsakiSceneProgressEvent(targetScene, normalized));
                        transitionProgress?.Invoke(normalized);
                    }
                    await _asakiAsyncService.WaitFrame(token);
                }

                AsakiBroker.Publish(new AsakiSceneProgressEvent(targetScene, 1.0f));
                transitionProgress?.Invoke(1.0f);

                if (activation == AsakiSceneActivation.ManualConfirm)
                {
                    _activationTaskSignal = new UniTaskCompletionSource<bool>();

                    // 修复：移除 UniTask.Delay(TimeSpan.MaxValue) 的奇怪写法
                    // 直接等待激活信号，添加超时警告
                    ALog.Info($"[SceneService] Waiting for manual scene activation: {targetScene}");

                    try
                    {
                        await _activationTaskSignal.Task.AttachExternalCancellation(token);
                    }
                    catch (OperationCanceledException)
                    {
                        ALog.Warn(
                            $"[SceneService] Scene activation cancelled or timeout: {targetScene}"
                        );
                        return CancelSceneLoadOperation(targetScene);
                    }
                }

                op.allowSceneActivation = true;

                while (!op.isDone)
                {
                    if (token.IsCancellationRequested)
                        return CancelSceneLoadOperation(targetScene);
                    await _asakiAsyncService.WaitFrame(token);
                }

                LastLoadedSceneName = targetScene;

                if (transition != null)
                    await transition.ExitAsync(token);
                _asakiEventService.Publish(
                    new AsakiSceneStateEvent(targetScene, AsakiSceneStateEvent.State.Completed)
                );
                return AsakiSceneResult.Ok(targetScene);
            }
            catch (Exception e)
            {
                ALog.Error("[SceneService] SceneLoad Failed.", e);
                return AsakiSceneResult.Failed(targetScene, e.Message);
            }
            finally
            {
                if (transition != null)
                    transition.Dispose();
                _isLoading = false;
                _activationTaskSignal = null;
            }
        }

        public void ActivateScene()
        {
            _activationTaskSignal?.TrySetResult(true);
        }

        public async UniTask<AsakiSceneResult> LoadSceneWithPreloadAsync(
            string targetSceneName,
            string loadingSceneName = "LoadingScene",
            CancellationToken token = default(CancellationToken)
        )
        {
            if (_isLoading)
            {
                ALog.Warn(
                    $"[SceneService] Cannot start preload transition, another load is in progress"
                );
                return AsakiSceneResult.Failed(
                    targetSceneName,
                    "Another scene load is in progress"
                );
            }

            if (!IsSceneValid(targetSceneName))
            {
                ALog.Error(
                    $"[SceneService] Target scene '{targetSceneName}' not found in BuildSettings"
                );
                return AsakiSceneResult.Failed(
                    targetSceneName,
                    $"Scene '{targetSceneName}' not found in BuildSettings."
                );
            }

            if (!IsSceneValid(loadingSceneName))
            {
                ALog.Error(
                    $"[SceneService] Loading scene '{loadingSceneName}' not found in BuildSettings"
                );
                return AsakiSceneResult.Failed(
                    loadingSceneName,
                    $"Loading scene '{loadingSceneName}' not found in BuildSettings."
                );
            }

            _isLoading = true;

            try
            {
                ALog.Info(
                    $"[SceneService] Starting preload scene transition: Current -> {loadingSceneName} -> {targetSceneName}"
                );

                // 使用实例属性存储 Payload，替代静态 SceneLoadStateService
                CurrentPayload = SceneLoadPayload.Create(targetSceneName, loadingSceneName);
                _preloadFlowSource = new UniTaskCompletionSource<AsakiSceneResult>();

                ALog.Info(
                    $"[SceneService] Payload set: Target={CurrentPayload.TargetSceneName}, Loading={CurrentPayload.LoadingSceneName}"
                );

                // 使用内部方法，跳过 _isLoading 检查，因为已经在上面设置了
                var result = await LoadSceneInternalAsync(
                    loadingSceneName,
                    AsakiLoadSceneMode.Single,
                    AsakiSceneActivation.Immediate,
                    null,
                    token,
                    false // 跳过 _isLoading 检查
                );

                if (!result.IsSuccess)
                {
                    ALog.Error(
                        $"[SceneService] Failed to load loading scene: {result.ErrorMessage}"
                    );
                    CurrentPayload = null;
                    _preloadFlowSource = null;
                    return AsakiSceneResult.Failed(
                        targetSceneName,
                        $"Failed to load loading scene: {result.ErrorMessage}"
                    );
                }

                ALog.Info(
                    $"[SceneService] Loading scene '{loadingSceneName}' loaded successfully, waiting for target scene load..."
                );

                // 修复：等待整个流程完成（Loading -> Target）
                // 使用 AttachExternalCancellation 确保 token 取消时能正确处理
                var finalResult = await _preloadFlowSource.Task.AttachExternalCancellation(token);

                ALog.Info(
                    $"[SceneService] Preload flow completed with result: {finalResult.IsSuccess}"
                );
                return finalResult;
            }
            catch (OperationCanceledException)
            {
                CurrentPayload = null;
                _preloadFlowSource = null;
                ALog.Info("[SceneService] Preload scene transition cancelled");
                return AsakiSceneResult.OperationCanceled(targetSceneName);
            }
            catch (Exception e)
            {
                CurrentPayload = null;
                _preloadFlowSource = null;
                ALog.Error("[SceneService] Preload scene transition failed.", e);
                return AsakiSceneResult.Failed(targetSceneName, e.Message);
            }
            finally
            {
                _isLoading = false;
                ALog.Info("[SceneService] LoadSceneWithPreloadAsync completed, _isLoading reset");
            }
        }

        /// <summary>
        /// 通知预加载流程已完成（由 LoadingSceneController 调用）
        /// </summary>
        public void NotifyPreloadFinished(bool success, string sceneName)
        {
            if (_preloadFlowSource == null)
            {
                ALog.Warn(
                    "[SceneService] NotifyPreloadFinished called but no preload flow is active"
                );
                return;
            }

            if (success)
            {
                _preloadFlowSource.TrySetResult(AsakiSceneResult.Ok(sceneName));
            }
            else
            {
                _preloadFlowSource.TrySetResult(
                    AsakiSceneResult.Failed(
                        sceneName,
                        "Target scene load failed in LoadingSceneController"
                    )
                );
            }

            // 清理 Payload
            CurrentPayload = null;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            _isLoading = false;
            _validScene?.Clear();
            _validScene = null;
            _activationTaskSignal?.TrySetCanceled();
            _activationTaskSignal = null;
            _preloadFlowSource?.TrySetCanceled();
            _preloadFlowSource = null;
            CurrentPayload = null;
        }

        private AsakiSceneResult CancelSceneLoadOperation(string targetSceneName)
        {
            _asakiEventService.Publish(
                new AsakiSceneStateEvent(targetSceneName, AsakiSceneStateEvent.State.Cancelled)
            );
            return AsakiSceneResult.OperationCanceled(targetSceneName);
        }
    }
}
