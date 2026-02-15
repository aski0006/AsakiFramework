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
    public class AsakiAddressablesStrategy : IAsakiResStrategy
    {
        public string StrategyName => "Addressables (Pro)";

        private readonly IAsakiAsyncService _async;

        private delegate UniTask<Object> LoadDelegate(
            string location,
            Action<float> onProgress,
            CancellationToken token
        );

        private readonly Dictionary<Type, LoadDelegate> _loadDelegates = new();

        public AsakiAddressablesStrategy(IAsakiAsyncService async)
        {
            _async = async;
            RegisterDefaultLoaders();
        }

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

        public void RegisterLoader<T>()
            where T : Object
        {
            _loadDelegates[typeof(T)] = (loc, prog, tok) =>
                LoadAssetGenericAsync<T>(loc, prog, tok);
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
            if (_loadDelegates.TryGetValue(type, out var loader))
            {
                return await loader(location, onProgress, token);
            }

            return await LoadAssetGenericAsync<Object>(location, onProgress, token);
        }

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
                Addressables.Release(asset);
            }
        }

        public async UniTask UnloadUnusedAssets(CancellationToken token)
        {
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
                    ALog.Error("Addressables failed to load", ex);
                    throw;
                }
            }
        }
    }
}
#endif
