using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Resources
{
    public interface IAsakiResStrategy
    {
        string StrategyName { get; }
        UniTask InitializeAsync();

        /// <summary>
        /// 加载资源 (支持进度回调)
        /// </summary>
        /// <param name="onProgress">进度回调 (0.0 ~ 1.0)</param>
        UniTask<UnityEngine.Object> LoadAssetInternalAsync(
            string location,
            Type type,
            Action<float> onProgress,
            CancellationToken token
        );

        void UnloadAssetInternal(string location, UnityEngine.Object asset);

        UniTask UnloadUnusedAssets(CancellationToken token);
    }
}
