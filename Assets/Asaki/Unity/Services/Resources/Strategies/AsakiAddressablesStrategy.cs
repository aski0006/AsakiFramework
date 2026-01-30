#if ASAKI_USE_ADDRESSABLES
using Asaki.Core.Async;
using Asaki.Core.Logging;
using Asaki.Core.Resources;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine; // 引用 UnityEngine 以使用 Sprite
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Asaki.Unity.Services.Resources.Strategies
{
    public class AsakiAddressablesStrategy : IAsakiResStrategy
    {
        public string StrategyName => "Addressables (Pro)";

        private readonly IAsakiAsyncService _async;

        public AsakiAddressablesStrategy(IAsakiAsyncService async)
        {
            _async = async;
        }

        public async UniTask InitializeAsync()
        {
            var handle = Addressables.InitializeAsync();
            await handle.Task;
        }

        public async UniTask<Object> LoadAssetInternalAsync(
            string location,
            Type type,
            Action<float> onProgress,
            CancellationToken token
        )
        {
            // [关键修复] 根据请求的类型，分发到正确的泛型方法
            // Addressables 必须显式调用 LoadAssetAsync<Sprite> 才能加载出 Sprite 子资源
            if (type == typeof(Sprite))
            {
                return await LoadAssetGenericAsync<Sprite>(location, onProgress, token);
            }

            // 默认情况 (包括 Texture2D, GameObject, ScriptableObject 等)
            return await LoadAssetGenericAsync<Object>(location, onProgress, token);
        }

        /// <summary>
        /// [新增] 泛型加载核心逻辑，复用进度处理代码
        /// </summary>
        private async UniTask<Object> LoadAssetGenericAsync<T>(
            string location,
            Action<float> onProgress,
            CancellationToken token
        )
            where T : Object
        {
            // 使用泛型 T 发起加载
            var handle = Addressables.LoadAssetAsync<T>(location);

            try
            {
                // 1. 无进度回调：直接使用 Task 包装器
                if (onProgress == null)
                {
                    return await WrapTask(handle, token);
                }

                // 2. 有进度回调：轮询模式
                while (!handle.IsDone)
                {
                    if (token.IsCancellationRequested)
                    {
                        Addressables.Release(handle);
                        throw new OperationCanceledException(token);
                    }

                    onProgress.Invoke(handle.PercentComplete);
                    await _async.WaitFrame(token);
                }

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    onProgress.Invoke(1f);
                    return handle.Result;
                }
                else
                {
                    Exception exception =
                        handle.OperationException
                        ?? new Exception($"[Addressables] Failed to load: {location}");
                    Addressables.Release(handle);
                    throw exception;
                }
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

        public void UnloadAssetInternal(string location, Object asset)
        {
            if (asset != null)
            {
                // Addressables 能够通过实例反查 Handle 并释放
                Addressables.Release(asset);
            }
        }

        public async UniTask UnloadUnusedAssets(CancellationToken token)
        {
            // 注意：Addressables 自身没有 UnloadUnusedAssets 概念，它依赖引用计数。
            // 但底层仍是 Unity 资源，所以调用 Resources.UnloadUnusedAssets 依然有助于清理无引用的原生资源
            AsyncOperation op = UnityEngine.Resources.UnloadUnusedAssets();
            if (_async != null)
            {
                while (!op.isDone)
                {
                    if (token.IsCancellationRequested)
                        return;
                    await _async.WaitFrame(token);
                }
            }
            else
            {
                while (!op.isDone)
                {
                    await Task.Yield();
                }
            }
        }

        // [修改] 泛型化 WrapTask 以适配不同的 Handle 类型
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
                    // 等待泛型 Task 完成
                    T result = await handle.Task;

                    return result;
                }
                catch (Exception ex)
                {
                    if (handle.IsValid())
                        Addressables.Release(handle);
                    ALog.Error("Addressables failed to load", ex);
                    throw;
                }
            }
        }
    }
}
#endif
