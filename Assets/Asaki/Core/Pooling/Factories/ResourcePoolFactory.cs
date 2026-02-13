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
    /// - 仅支持 GameObject 类型（其他资源类型不支持池化）
    /// </summary>
    /// <typeparam name="T">Unity 资源类型（仅支持 GameObject）</typeparam>
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

            // 验证类型：仅支持 GameObject
            if (typeof(T) != typeof(GameObject))
            {
                throw new NotSupportedException(
                    $"ResourcePoolFactory only supports GameObject pooling. "
                        + $"Type {typeof(T).Name} is not supported. "
                        + "For shared assets like Sprite or AudioClip, use direct resource loading instead."
                );
            }
        }

        public async UniTask<T> CreateAsync(CancellationToken token = default(CancellationToken))
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

                    if (_handle is not { IsValid: true })
                    {
                        ALog.Error(
                            $"[AsakiPool] ResourcePoolFactory failed to load resource: {_resourceKey}"
                        );
                    }
                    else
                    {
                        _isHandleLoaded = true;
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

            // 实例化 GameObject
            GameObject instance = Object.Instantiate(_handle.Asset as GameObject);
            return instance as T;
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
            if (obj is GameObject go)
            {
                Object.Destroy(go);
            }
        }

        public virtual bool Validate(T obj)
        {
            if (obj == null)
                return false;

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
