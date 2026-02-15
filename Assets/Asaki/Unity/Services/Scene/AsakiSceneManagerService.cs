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
        private enum LoadingState
        {
            Idle,
            DirectLoading,
            PreloadLoadingScene,
            PreloadWaitingTarget,
        }

        private readonly IAsakiEventService _asakiEventService;
        private readonly IAsakiAsyncService _asakiAsyncService;
        private readonly IAsakiResourceService _asakiResourceService;
        private HashSet<string> _validSceneNames;
        private LoadingState _loadingState = LoadingState.Idle;
        private bool _isDisposed;
        public string LastLoadedSceneName { get; private set; }

        public SceneLoadPayload CurrentPayload { get; private set; }

        private UniTaskCompletionSource<bool> _activationCompletionSource;
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
            _validSceneNames ??= new HashSet<string>();

            if (_validSceneNames.Contains(sceneName))
                return true;

            int count = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
            {
                try
                {
                    string path = SceneUtility.GetScenePathByBuildIndex(i);
                    if (!string.IsNullOrEmpty(path))
                    {
                        string name = System.IO.Path.GetFileNameWithoutExtension(path);
                        _validSceneNames.Add(name);
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
            _validSceneNames ??= new HashSet<string>();
            ALog.Info("[SceneService] PerBuildScene called - using on-demand validation");
        }

        private bool TryStartLoading(LoadingState newState)
        {
            if (_loadingState != LoadingState.Idle)
                return false;
            _loadingState = newState;
            return true;
        }

        private void FinishLoading()
        {
            _loadingState = LoadingState.Idle;
        }

        public async UniTask<AsakiSceneResult> LoadSceneAsync(
            string targetScene,
            AsakiLoadSceneMode mode = AsakiLoadSceneMode.Single,
            AsakiSceneActivation activation = AsakiSceneActivation.Immediate,
            IAsakiSceneTransition transition = null,
            CancellationToken token = default(CancellationToken)
        )
        {
            if (!TryStartLoading(LoadingState.DirectLoading))
            {
                transition?.Dispose();
                return AsakiSceneResult.Failed(targetScene, "Another scene load is in progress");
            }

            return await ExecuteLoadSceneAsync(
                targetScene,
                mode,
                activation,
                transition,
                token,
                timeoutSeconds: 0
            );
        }

        private async UniTask<AsakiSceneResult> ExecuteLoadSceneAsync(
            string targetScene,
            AsakiLoadSceneMode mode,
            AsakiSceneActivation activation,
            IAsakiSceneTransition transition,
            CancellationToken token,
            int timeoutSeconds = 0
        )
        {
            if (!IsSceneValid(targetScene))
            {
                transition?.Dispose();
                FinishLoading();
                return AsakiSceneResult.Failed(
                    targetScene,
                    $"Scene '{targetScene}' not found in BuildSettings."
                );
            }

            CancellationToken effectiveToken = token;
            CancellationTokenSource timeoutCts = null;
            CancellationTokenSource linkedCts = null;

            if (timeoutSeconds > 0)
            {
                timeoutCts = new CancellationTokenSource();
                timeoutCts.CancelAfterSlim(TimeSpan.FromSeconds(timeoutSeconds));
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    token,
                    timeoutCts.Token
                );
                effectiveToken = linkedCts.Token;
            }

            try
            {
                return await ExecuteLoadSceneCoreAsync(
                    targetScene,
                    mode,
                    activation,
                    transition,
                    effectiveToken
                );
            }
            catch (OperationCanceledException)
                when (timeoutCts != null && timeoutCts.IsCancellationRequested)
            {
                ALog.Warn(
                    $"[SceneService] Scene load timed out after {timeoutSeconds} seconds: {targetScene}"
                );
                _asakiEventService.Publish(
                    new AsakiSceneStateEvent(
                        targetScene,
                        AsakiSceneStateEvent.State.Failed,
                        "Load timeout"
                    )
                );
                return AsakiSceneResult.Failed(
                    targetScene,
                    $"Scene load timed out after {timeoutSeconds} seconds"
                );
            }
            finally
            {
                timeoutCts?.Dispose();
                linkedCts?.Dispose();
            }
        }

        private async UniTask<AsakiSceneResult> ExecuteLoadSceneCoreAsync(
            string targetScene,
            AsakiLoadSceneMode mode,
            AsakiSceneActivation activation,
            IAsakiSceneTransition transition,
            CancellationToken token
        )
        {
            string previousSceneName = SceneManager.GetActiveScene().name;
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
                }

                LoadSceneMode unityMode =
                    mode == AsakiLoadSceneMode.Single
                        ? LoadSceneMode.Single
                        : LoadSceneMode.Additive;
                AsyncOperation op = SceneManager.LoadSceneAsync(targetScene, unityMode);
                if (op == null)
                {
                    FinishLoading();
                    return AsakiSceneResult.Failed(
                        targetScene,
                        "Unity internal error : AsyncOperation is null"
                    );
                }
                op.allowSceneActivation = false;
                float lastProgress = 0f;
                float lastReportTime = UnityEngine.Time.realtimeSinceStartup;

                const float UNITY_SCENE_LOAD_READY_THRESHOLD = 0.9f;
                while (op.progress < UNITY_SCENE_LOAD_READY_THRESHOLD)
                {
                    if (token.IsCancellationRequested)
                        return CancelSceneLoadOperation(targetScene);
                    float raw = op.progress;
                    float normalized = Mathf.Clamp01(raw / UNITY_SCENE_LOAD_READY_THRESHOLD);
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
                    _activationCompletionSource = new UniTaskCompletionSource<bool>();
                    ALog.Info($"[SceneService] Waiting for manual scene activation: {targetScene}");

                    try
                    {
                        await _activationCompletionSource.Task.AttachExternalCancellation(token);
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
                if (mode == AsakiLoadSceneMode.Single)
                {
                    _asakiEventService.Publish(
                        new AsakiActiveSceneChangedEvent(previousSceneName, targetScene)
                    );
                }
                return AsakiSceneResult.Ok(targetScene);
            }
            catch (Exception e)
            {
                ALog.Error("[SceneService] SceneLoad Failed.", e);
                _asakiEventService.Publish(
                    new AsakiSceneStateEvent(
                        targetScene,
                        AsakiSceneStateEvent.State.Failed,
                        e.Message
                    )
                );
                return AsakiSceneResult.Failed(targetScene, e.Message);
            }
            finally
            {
                if (transition != null)
                    transition.Dispose();
                FinishLoading();
                _activationCompletionSource = null;
            }
        }

        public void ActivateScene()
        {
            _activationCompletionSource?.TrySetResult(true);
        }

        public async UniTask<AsakiSceneResult> LoadSceneWithPreloadAsync(
            string targetSceneName,
            string loadingSceneName = "LoadingScene",
            CancellationToken token = default(CancellationToken)
        )
        {
            if (!TryStartLoading(LoadingState.PreloadLoadingScene))
            {
                ALog.Error(
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
                FinishLoading();
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
                FinishLoading();
                return AsakiSceneResult.Failed(
                    loadingSceneName,
                    $"Loading scene '{loadingSceneName}' not found in BuildSettings."
                );
            }

            try
            {
                ALog.Info(
                    $"[SceneService] Starting preload scene transition: Current -> {loadingSceneName} -> {targetSceneName}"
                );

                CurrentPayload = SceneLoadPayload.Create(targetSceneName, loadingSceneName);
                _preloadFlowSource = new UniTaskCompletionSource<AsakiSceneResult>();

                ALog.Info(
                    $"[SceneService] Payload set: Target={CurrentPayload.TargetSceneName}, Loading={CurrentPayload.LoadingSceneName}"
                );

                _loadingState = LoadingState.PreloadLoadingScene;
                var result = await ExecuteLoadSceneAsync(
                    loadingSceneName,
                    AsakiLoadSceneMode.Single,
                    AsakiSceneActivation.Immediate,
                    null,
                    token,
                    timeoutSeconds: 0
                );

                if (!result.IsSuccess)
                {
                    ALog.Error(
                        $"[SceneService] Failed to load loading scene: {result.ErrorMessage}"
                    );
                    CurrentPayload = null;
                    _preloadFlowSource = null;
                    FinishLoading();
                    return AsakiSceneResult.Failed(
                        targetSceneName,
                        $"Failed to load loading scene: {result.ErrorMessage}"
                    );
                }

                _loadingState = LoadingState.PreloadWaitingTarget;
                ALog.Info(
                    $"[SceneService] Loading scene '{loadingSceneName}' loaded successfully, waiting for target scene load..."
                );

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
                FinishLoading();
                ALog.Info("[SceneService] LoadSceneWithPreloadAsync completed");
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
            _loadingState = LoadingState.Idle;
            _validSceneNames?.Clear();
            _validSceneNames = null;
            _activationCompletionSource?.TrySetCanceled();
            _activationCompletionSource = null;
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
