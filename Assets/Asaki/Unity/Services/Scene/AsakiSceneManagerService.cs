using System;
using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Async;
using Asaki.Core.Broker;
using Asaki.Core.Logging;
using Asaki.Core.Resources;
using Asaki.Core.Scene;
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
        private UniTaskCompletionSource<bool> _activationTaskSignal;

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
            _validScene ??= new HashSet<string>();
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
                    }
                }
                catch (Exception e)
                {
                    ALog.Error($"Failed to get scene path at index {i}", e);
                }
            }
            return _validScene.Contains(sceneName);
        }

        public void PerBuildScene()
        {
            _validScene ??= new HashSet<string>();
            int count = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
            {
                try
                {
                    string path = SceneUtility.GetScenePathByBuildIndex(i);
                    if (string.IsNullOrEmpty(path))
                        continue;
                    string name = System.IO.Path.GetFileNameWithoutExtension(path);
                    _validScene.Add(name);
                }
                catch (Exception e)
                {
                    ALog.Error($"Failed to get scene path at index {i}, Message : {e.Message}", e);
                }
            }
        }

        public async UniTask<AsakiSceneResult> LoadSceneAsync(
            string targetScene,
            AsakiLoadSceneMode mode = AsakiLoadSceneMode.Single,
            AsakiSceneActivation activation = AsakiSceneActivation.Immediate,
            IAsakiSceneTransition transition = null,
            CancellationToken token = default(CancellationToken)
        )
        {
            if (_isLoading)
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
                    GC.Collect();
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

                while (Mathf.Approximately(op.progress, 0.899f))
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
                    UniTask signalTask = _activationTaskSignal.Task.AttachExternalCancellation(
                        token
                    );
                    UniTask waitTask = UniTask.Delay(
                        TimeSpan.MaxValue,
                        false,
                        PlayerLoopTiming.Update,
                        token,
                        false
                    );
                    int completedIndex = await UniTask.WhenAny(signalTask, waitTask);
                    if (completedIndex == 0) // signalTask 先完成 (索引 0)
                        return CancelSceneLoadOperation(targetScene);
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
                return AsakiSceneResult.Failed(
                    targetSceneName,
                    "Another scene load is in progress"
                );

            if (!IsSceneValid(targetSceneName))
                return AsakiSceneResult.Failed(
                    targetSceneName,
                    $"Scene '{targetSceneName}' not found in BuildSettings."
                );

            if (!IsSceneValid(loadingSceneName))
                return AsakiSceneResult.Failed(
                    loadingSceneName,
                    $"Loading scene '{loadingSceneName}' not found in BuildSettings."
                );

            _isLoading = true;

            try
            {
                ALog.Info(
                    $"[SceneService] Starting preload scene transition: Current -> {loadingSceneName} -> {targetSceneName}"
                );

                var payload = SceneLoadPayload.Create(targetSceneName, loadingSceneName);
                SceneLoadStateService.SetPayload(payload);

                var result = await LoadSceneAsync(
                    loadingSceneName,
                    AsakiLoadSceneMode.Single,
                    AsakiSceneActivation.Immediate,
                    null,
                    token
                );

                if (!result.IsSuccess)
                {
                    SceneLoadStateService.ClearPayload();
                    return AsakiSceneResult.Failed(
                        targetSceneName,
                        $"Failed to load loading scene: {result.ErrorMessage}"
                    );
                }

                return AsakiSceneResult.Ok(targetSceneName);
            }
            catch (Exception e)
            {
                SceneLoadStateService.ClearPayload();
                ALog.Error("[SceneService] Preload scene transition failed.", e);
                return AsakiSceneResult.Failed(targetSceneName, e.Message);
            }
            finally
            {
                _isLoading = false;
            }
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
        }

        private AsakiSceneResult CancelSceneLoadOperation(string targetSceneName)
        {
            _asakiEventService.Publish(
                new AsakiSceneStateEvent(targetSceneName, AsakiSceneStateEvent.State.Cancelled)
            );
            ;
            return AsakiSceneResult.OperationCanceled(targetSceneName);
        }
    }
}
