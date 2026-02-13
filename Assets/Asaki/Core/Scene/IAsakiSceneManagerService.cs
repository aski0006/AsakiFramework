﻿using System;
using System.Threading;
using Asaki.Core.Context;
using Asaki.Core.Scene.SceneManagement;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Scene
{
    public interface IAsakiSceneManagerService : IAsakiService, IDisposable
    {
        string LastLoadedSceneName { get; }

        /// <summary>
        /// 当前待处理的场景加载参数
        /// </summary>
        SceneLoadPayload CurrentPayload { get; }

        void PerBuildScene();
        UniTask<AsakiSceneResult> LoadSceneAsync(
            string sceneName,
            AsakiLoadSceneMode mode = AsakiLoadSceneMode.Single,
            AsakiSceneActivation activation = AsakiSceneActivation.Immediate,
            IAsakiSceneTransition transition = null,
            CancellationToken token = default(CancellationToken)
        );

        void ActivateScene();

        /// <summary>
        /// 带预加载的场景切换
        /// A->C(Loading)->B 流程：先加载过渡场景，在过渡场景中预加载目标场景资源，最后切换到目标场景
        /// </summary>
        /// <param name="targetSceneName">目标场景名称</param>
        /// <param name="loadingSceneName">过渡场景名称，默认为"LoadingScene"</param>
        /// <param name="token">取消令牌</param>
        /// <returns>场景加载结果（等待整个流程完成）</returns>
        UniTask<AsakiSceneResult> LoadSceneWithPreloadAsync(
            string targetSceneName,
            string loadingSceneName = "LoadingScene",
            CancellationToken token = default(CancellationToken)
        );

        /// <summary>
        /// 通知预加载流程已完成（由 LoadingSceneController 调用）
        /// </summary>
        /// <param name="success">是否成功</param>
        /// <param name="sceneName">场景名称</param>
        void NotifyPreloadFinished(bool success, string sceneName);
    }
}
