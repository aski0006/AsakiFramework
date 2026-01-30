// 文件: Assets/Asaki/Core/Pooling/V2/Factories/ResourcePoolFactory.cs
using System;
using System.Threading;
using Asaki.Core.Logging;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.Resources;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Core.Pooling.Factories
{
    /// <summary>
    /// 基于 IAsakiResourceService 的资源池工厂
    /// 特性：
    /// - 自动管理资源句柄生命周期
    /// - 支持 Addressable 和 Resources 双路径
    /// - 延迟加载（首次创建时才加载资源）
    /// </summary>
    /// <typeparam name="T">Unity 资源类型（GameObject, Sprite, AudioClip 等）</typeparam>
    public class ResourcePoolFactory<T> : IAsakiPoolObjectFactory<T>, IDisposable
        where T : Object
    {
        private readonly IAsakiResourceService _resourceService;
        private readonly string _resourceKey;
        private ResHandle<T> _handle;
        private bool _isHandleLoaded;

        public ResourcePoolFactory(IAsakiResourceService resourceService, string resourceKey)
        {
            _resourceService =
                resourceService ?? throw new ArgumentNullException(nameof(resourceService));
            _resourceKey = !string.IsNullOrEmpty(resourceKey)
                ? resourceKey
                : throw new ArgumentException(
                    "Resource key cannot be null or empty",
                    nameof(resourceKey)
                );
        }

        public async UniTask<T> CreateAsync(CancellationToken token = default)
        {
            await EnsureResourceLoadedAsync(token);
            return CreateResourceInstance();
        }

        public T CreateSync()
        {
            if (!_isHandleLoaded)
            {
                throw new InvalidOperationException(
                    "Resource not loaded. Call CreateAsync first or ensure async initialization is complete."
                );
            }
            return CreateResourceInstance();
        }

        private async UniTask EnsureResourceLoadedAsync(CancellationToken token)
        {
            if (!_isHandleLoaded)
            {
                try
                {
                    _handle = await _resourceService.LoadAsync<T>(_resourceKey, token);
                    _isHandleLoaded = true;

                    if (_handle is not { IsValid: true })
                    {
                        ALog.Error(
                            $"[AsakiPool] ResourcePoolFactory failed to load resource: {_resourceKey}"
                        );
                    }
                }
                catch (Exception ex)
                {
                    ALog.Error(
                        $"[AsakiPool] ResourcePoolFactory failed to load resource: {_resourceKey}",
                        ex
                    );
                }
            }
        }

        private T CreateResourceInstance()
        {
            if (_handle is not { IsValid: true })
            {
                return null;
            }

            // 对于 GameObject，需要实例化
            if (typeof(T) == typeof(GameObject))
            {
                GameObject instance = Object.Instantiate(_handle.Asset as GameObject);
                return instance as T;
            }

            // 对于其他资源类型（Sprite, AudioClip 等），直接返回引用
            return _handle.Asset;
        }

        public virtual void OnGet(T obj)
        {
            if (obj is GameObject go)
            {
                go.SetActive(true);
            }
        }

        public virtual void OnReturn(T obj)
        {
            if (obj is GameObject go)
            {
                go.SetActive(false);
            }
        }

        public virtual void OnDestroy(T obj)
        {
            // 只销毁实例化的对象（GameObject）
            if (obj is GameObject go)
            {
                Object.Destroy(go);
            }
        }

        public virtual bool Validate(T obj)
        {
            // 检查对象本身是否为空
            if (obj == null)
                return false;

            // 检查底层资源句柄是否仍然有效
            return _handle is { IsValid: true };
        }

        public void Dispose()
        {
            if (_handle is { IsValid: true })
            {
                _handle.Dispose();
                _handle = null;
                _isHandleLoaded = false;
                ALog.Info($"[AsakiPool] ResourcePoolFactory disposed: {_resourceKey}");
            }
        }
    }
}
