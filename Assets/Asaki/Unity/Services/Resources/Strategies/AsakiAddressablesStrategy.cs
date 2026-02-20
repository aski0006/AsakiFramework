#if ASAKI_USE_ADDRESSABLES
using Asaki.Core.Async;
using Asaki.Core.Logging;
using Asaki.Core.Resources;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Asaki.Unity.Services.Resources.Strategies
{
    /// <summary>
    /// Unity Addressables加载策略
    /// <para>使用Unity Addressables系统实现资源加载，适用于生产环境。</para>
    /// </summary>
    /// <remarks>
    /// <para>优点：</para>
    /// <list type="bullet">
    /// <item><description>内存管理优秀，支持细粒度卸载和引用计数</description></item>
    /// <item><description>自动依赖处理，无需手动管理资源依赖</description></item>
    /// <item><description>支持热更新，可远程更新资源</description></item>
    /// <item><description>支持异步加载和进度回调</description></item>
    /// </list>
    /// <para>缺点：</para>
    /// <list type="bullet">
    /// <item><description>需要Build Bundle步骤，开发流程更复杂</description></item>
    /// <item><description>需要配置Addressables Group和Label</description></item>
    /// </list>
    /// <para>适用场景：</para>
    /// <list type="bullet">
    /// <item><description>中大型项目</description></item>
    /// <item><description>需要热更新的项目</description></item>
    /// <item><description>对内存管理有严格要求的项目</description></item>
    /// </list>
    /// <para>使用前提：</para>
    /// <list type="bullet">
    /// <item><description>安装Unity Addressables包</description></item>
    /// <item><description>定义编译宏 ASAKI_USE_ADDRESSABLES</description></item>
    /// </list>
    /// </remarks>
    public class AsakiAddressablesStrategy : IAsakiResStrategy
    {
        /// <summary>
        /// 策略名称标识
        /// </summary>
        public string StrategyName => "Addressables (Pro)";

        private readonly IAsakiAsyncService _asyncService;

        private delegate UniTask<Object> LoadDelegate(
            string location,
            Action<float> onProgress,
            CancellationToken token
        );

        private readonly Dictionary<Type, LoadDelegate> _loadDelegates = new();

        /// <summary>
        /// 创建Addressables策略实例
        /// </summary>
        /// <param name="asyncService">异步驱动服务，用于协程调度</param>
        public AsakiAddressablesStrategy(IAsakiAsyncService asyncService)
        {
            _asyncService = asyncService ?? throw new ArgumentNullException(nameof(asyncService));
            RegisterDefaultLoaders();
        }

        /// <summary>
        /// 注册默认的资源类型加载器
        /// <para>为常用Unity类型注册泛型加载委托，避免运行时反射。</para>
        /// </summary>
        private void RegisterDefaultLoaders()
        {
            RegisterLoader<Sprite>();
            RegisterLoader<Texture2D>();
            RegisterLoader<GameObject>();
            RegisterLoader<AudioClip>();
            RegisterLoader<Material>();
            RegisterLoader<TextAsset>();
            RegisterLoader<AnimationClip>();
            RegisterLoader<Shader>();
            RegisterLoader<Mesh>();
            RegisterLoader<ScriptableObject>();
        }

        /// <summary>
        /// 注册指定类型的加载器
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        public void RegisterLoader<T>()
            where T : Object
        {
            _loadDelegates[typeof(T)] = (loc, prog, tok) =>
                LoadAssetGenericAsync<T>(loc, prog, tok);
        }

        /// <summary>
        /// 初始化策略
        /// <para>初始化Addressables系统，加载Catalog。</para>
        /// </summary>
        public async UniTask InitializeAsync()
        {
            var handle = Addressables.InitializeAsync();
            await handle.Task;
        }

        /// <summary>
        /// 异步加载资源
        /// <para>根据类型选择最优加载方式，优先使用预注册的泛型加载器。</para>
        /// </summary>
        /// <param name="location">Addressable地址或Label</param>
        /// <param name="type">资源类型</param>
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

            if (_loadDelegates.TryGetValue(type, out var loader))
            {
                return await loader(location, onProgress, token);
            }

            return await LoadAssetGenericAsync<Object>(location, onProgress, token);
        }

        /// <summary>
        /// 泛型资源加载实现
        /// <para>使用Addressables.LoadAssetAsync&lt;T&gt;加载指定类型资源。</para>
        /// </summary>
        private async UniTask<Object> LoadAssetGenericAsync<T>(
            string location,
            Action<float> onProgress,
            CancellationToken token
        )
            where T : Object
        {
            var handle = Addressables.LoadAssetAsync<T>(location);

            try
            {
                if (onProgress == null)
                {
                    return await WrapTask(handle, token);
                }

                while (!handle.IsDone)
                {
                    if (token.IsCancellationRequested)
                    {
                        Addressables.Release(handle);
                        throw new OperationCanceledException(token);
                    }

                    onProgress.Invoke(handle.PercentComplete);
                    await _asyncService.WaitFrame(token);
                }

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    onProgress.Invoke(1f);
                    return handle.Result;
                }

                Exception exception =
                    handle.OperationException
                    ?? new InvalidOperationException(
                        $"[Addressables] Failed to load: '{location}'"
                    );
                Addressables.Release(handle);
                throw exception;
            }
            catch (Exception)
            {
                if (handle.IsValid() && handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Addressables.Release(handle);
                }
                throw;
            }
        }

        /// <summary>
        /// 卸载单个资源
        /// <para>调用Addressables.Release减少引用计数。</para>
        /// </summary>
        /// <param name="location">资源地址（此策略中未使用）</param>
        /// <param name="asset">要卸载的资源对象</param>
        public void UnloadAssetInternal(string location, Object asset)
        {
            if (asset != null)
            {
                Addressables.Release(asset);
            }
        }

        /// <summary>
        /// 卸载未使用的资源
        /// <para>调用Resources.UnloadUnusedAssets触发垃圾回收。</para>
        /// <para>Addressables本身有引用计数管理，此方法主要用于清理非Addressables资源。</para>
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

        /// <summary>
        /// 包装AsyncOperationHandle为Task
        /// <para>用于无进度回调的快速加载路径。</para>
        /// </summary>
        private async Task<Object> WrapTask<T>(
            AsyncOperationHandle<T> handle,
            CancellationToken token
        )
            where T : Object
        {
            var tcs = new TaskCompletionSource<Object>();

            using (
                token.Register(() =>
                {
                    if (handle.IsValid())
                        Addressables.Release(handle);
                    tcs.TrySetCanceled();
                })
            )
            {
                try
                {
                    T result = await handle.Task;
                    return result;
                }
                catch (Exception ex)
                {
                    if (handle.IsValid())
                        Addressables.Release(handle);
                    ALog.Error($"[Addressables] Failed to load asset: {ex.Message}");
                    throw;
                }
            }
        }
    }
}
#endif
