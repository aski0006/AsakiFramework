using System;
using System.Threading;
using System.Threading.Tasks;
using Asaki.Core.Async;
using Asaki.Core.Resources;
using Asaki.Unity.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Unity.Services.Resources.Strategies
{
    /// <summary>
    /// Unity原生Resources加载策略
    /// <para>使用UnityEngine.Resources实现资源加载，适用于开发期和原型期。</para>
    /// </summary>
    /// <remarks>
    /// <para>优点：</para>
    /// <list type="bullet">
    /// <item><description>无需打包，即改即用，开发效率高</description></item>
    /// <item><description>API简单直观，学习成本低</description></item>
    /// </list>
    /// <para>缺点：</para>
    /// <list type="bullet">
    /// <item><description>构建包体大，所有Resources目录下资源都会被打包</description></item>
    /// <item><description>内存管理较差，不支持细粒度卸载</description></item>
    /// <item><description>不支持热更新</description></item>
    /// </list>
    /// <para>适用场景：</para>
    /// <list type="bullet">
    /// <item><description>快速原型开发</description></item>
    /// <item><description>小型项目</description></item>
    /// <item><description>无需热更新的项目</description></item>
    /// </list>
    /// </remarks>
    public class AsakiResourcesStrategy : IAsakiResStrategy
    {
        /// <summary>
        /// 策略名称标识
        /// </summary>
        public string StrategyName => "Resources (Native)";

        private readonly IAsakiAsyncService _asyncService;

        /// <summary>
        /// 创建Resources策略实例
        /// </summary>
        /// <param name="asyncService">异步驱动服务，用于协程调度</param>
        public AsakiResourcesStrategy(IAsakiAsyncService asyncService)
        {
            _asyncService = asyncService ?? throw new ArgumentNullException(nameof(asyncService));
        }

        /// <summary>
        /// 初始化策略
        /// <para>Resources模式无需初始化，直接返回完成。</para>
        /// </summary>
        public UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 异步加载资源
        /// <para>使用Resources.LoadAsync实现异步加载，支持进度回调和取消。</para>
        /// </summary>
        /// <param name="location">相对于Resources文件夹的路径，不含扩展名</param>
        /// <param name="type">资源类型，用于正确加载子资源</param>
        /// <param name="onProgress">进度回调，范围0.0~1.0</param>
        /// <param name="token">取消令牌</param>
        /// <returns>加载的资源对象</returns>
        public async UniTask<Object> LoadAssetInternalAsync(
            string location,
            Type type,
            Action<float> onProgress,
            CancellationToken token
        )
        {
            if (string.IsNullOrEmpty(location))
                throw new ArgumentNullException(nameof(location));

            ResourceRequest request = UnityEngine.Resources.LoadAsync(location, type);

            if (onProgress == null)
            {
                return await request.ToTask(token);
            }

            while (!request.isDone)
            {
                if (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException(token);
                }

                onProgress.Invoke(request.progress);
                await _asyncService.WaitFrame(token);
            }

            onProgress.Invoke(1f);
            return request.asset;
        }

        /// <summary>
        /// 卸载单个资源
        /// <para>非GameObject资源调用Resources.UnloadAsset释放内存。</para>
        /// <para>GameObject资源不执行卸载，应由Destroy处理。</para>
        /// </summary>
        /// <param name="location">资源路径（此策略中未使用）</param>
        /// <param name="asset">要卸载的资源对象</param>
        public void UnloadAssetInternal(string location, Object asset)
        {
            if (asset == null)
                return;

            if (asset is GameObject)
                return;

            UnityEngine.Resources.UnloadAsset(asset);
        }

        /// <summary>
        /// 卸载未使用的资源
        /// <para>调用Resources.UnloadUnusedAssets触发垃圾回收。</para>
        /// </summary>
        /// <param name="token">取消令牌</param>
        public async UniTask UnloadUnusedAssets(CancellationToken token)
        {
            AsyncOperation op = UnityEngine.Resources.UnloadUnusedAssets();

            while (!op.isDone)
            {
                if (token.IsCancellationRequested)
                    return;

                await _asyncService.WaitFrame(token);
            }
        }
    }
}
