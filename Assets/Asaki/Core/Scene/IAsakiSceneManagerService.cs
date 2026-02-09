using System;
using System.Threading;
using Asaki.Core.Context;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Scene
{
    public interface IAsakiSceneManagerService : IAsakiService, IDisposable
    {
        string LastLoadedSceneName { get; }
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
        /// <returns>场景加载结果</returns>
        UniTask<AsakiSceneResult> LoadSceneWithPreloadAsync(
            string targetSceneName,
            string loadingSceneName = "LoadingScene",
            CancellationToken token = default(CancellationToken)
        );
    }
}
