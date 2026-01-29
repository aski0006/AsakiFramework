// 文件: Assets/Asaki/Core/Pooling/V2/Factories/ResourcePoolFactory.cs
using System;
using System.Threading;
using Asaki.Core.Logging;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.Resources;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Core.Pooling
{
    /// <summary>
    /// 基于 IAsakiResourceService 的资源池工厂
    /// 特性：
    /// - 自动管理资源句柄生命周期
    /// - 支持 Addressable 和 Resources 双路径
    /// - 延迟加载（首次创建时才加载资源）
    /// </summary>
    /// <typeparam name="T">Unity 资源类型（GameObject, Sprite, AudioClip 等）</typeparam>
    public class ResourcePoolFactory<T> : IAsakiPoolObjectFactory<T>, IDisposable where T : Object
    {
        private readonly IAsakiResourceService _resourceService;
        private readonly string _resourceKey;
        private ResHandle<T> _handle;
        private bool _isHandleLoaded;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="resourceService">资源服务</param>
        /// <param name="resourceKey">资源路径或 Key</param>
        public ResourcePoolFactory(IAsakiResourceService resourceService, string resourceKey)
        {
            _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
            _resourceKey = !string.IsNullOrEmpty(resourceKey) 
                ? resourceKey 
                : throw new ArgumentException("Resource key cannot be null or empty", nameof(resourceKey));
        }

        /// <summary>
        /// 异步创建对象（首次调用时加载资源）
        /// </summary>
        public async UniTask<T> CreateAsync(CancellationToken token = default)
        {
            // 延迟加载资源
            if (!_isHandleLoaded)
            {
                try
                {
                    _handle = await _resourceService.LoadAsync<T>(_resourceKey, token);
                    _isHandleLoaded = true;

                    if (_handle is not { IsValid: true })
                    {
                        ALog.Error($"[AsakiPoolService] ResourcePoolFactory Resource load failure: {_resourceKey}");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    ALog.Error($"[AsakiPoolService] ResourcePoolFactory Resource load failure: {_resourceKey}", ex);
                    return null;
                }
            }

            // 对于 GameObject，需要实例化
            if (typeof(T) == typeof(GameObject))
            {
                GameObject instance = Object.Instantiate(_handle.Asset as GameObject);
                return instance as T;
            }

            // 对于其他资源类型（Sprite, AudioClip 等），直接返回引用
            // 注意：这些资源不应该被 Destroy，由资源服务管理生命周期
            return _handle.Asset;
        }

        public virtual void OnGet(T obj)
        {
            // GameObject 激活
            if (obj is GameObject go)
            {
                go.SetActive(true);
            }
        }

        public virtual void OnReturn(T obj)
        {
            // GameObject 禁用
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
            // 非实例化资源（Sprite, AudioClip）不销毁，由资源服务管理
        }

        public virtual bool Validate(T obj) => obj;

        /// <summary>
        /// 释放资源句柄
        /// </summary>
        public void Dispose()
        {
            if (_handle is { IsValid: true })
            {
                _handle.Dispose();
                _handle = null;
                _isHandleLoaded = false;
                ALog.Info($"[AsakiPoolService] ResourcePoolFactory Dispose: {_resourceKey}");
            }
        }
    }
}