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
    /// Unity 组件工厂（直接池化组件而非 GameObject）
    /// 示例：池化 ParticleSystem, AudioSource, Rigidbody 等
    /// </summary>
    /// <typeparam name="T">组件类型（必须继承 Component）</typeparam>
    public class ComponentFactory<T> : IAsakiPoolObjectFactory<T>, IDisposable
        where T : Component
    {
        private readonly IAsakiResourceService _resourceService;
        private readonly string _prefabPath;
        private readonly Transform _parent;
        private ResHandle<GameObject> _prefabHandle;
        private bool _isHandleLoaded;

        public ComponentFactory(
            IAsakiResourceService resourceService,
            string prefabPath,
            Transform parent = null
        )
        {
            _resourceService =
                resourceService ?? throw new ArgumentNullException(nameof(resourceService));
            _prefabPath = !string.IsNullOrEmpty(prefabPath)
                ? prefabPath
                : throw new ArgumentException(
                    "Prefab path cannot be null or empty",
                    nameof(prefabPath)
                );
            _parent = parent;
        }

        public async UniTask<T> CreateAsync(CancellationToken token = default(CancellationToken))
        {
            await EnsurePrefabLoadedAsync(token);
            return CreateComponentInstance();
        }

        public T CreateSync()
        {
            if (!_isHandleLoaded)
            {
                throw new InvalidOperationException(
                    "Prefab not loaded. Call CreateAsync first or ensure async initialization is complete."
                );
            }
            return CreateComponentInstance();
        }

        private async UniTask EnsurePrefabLoadedAsync(CancellationToken token)
        {
            if (!_isHandleLoaded)
            {
                _prefabHandle = await _resourceService.LoadAsync<GameObject>(_prefabPath, token);

                if (_prefabHandle == null || !_prefabHandle.IsValid)
                {
                    ALog.Error(
                        $"[AsakiPool] ComponentFactory failed to load prefab: {_prefabPath}"
                    );
                }
                else
                {
                    _isHandleLoaded = true;
                }
            }
        }

        private T CreateComponentInstance()
        {
            if (_prefabHandle == null || !_prefabHandle.IsValid)
            {
                return null;
            }

            GameObject instance = _parent
                ? Object.Instantiate(_prefabHandle.Asset, _parent)
                : Object.Instantiate(_prefabHandle.Asset);

            instance.SetActive(false);

            T component = instance.GetComponent<T>();
            if (component == null)
            {
                ALog.Error(
                    $"[AsakiPool] ComponentFactory missing component {typeof(T).Name} in {_prefabPath}"
                );
                Object.Destroy(instance);
                return null;
            }

            return component;
        }

        public void OnGet(T component)
        {
            if (!component)
                return;

            component.gameObject.SetActive(true);
            PoolObjectLifecycleHelper.InvokeOnSpawnForComponent(component);
        }

        public void OnReturn(T component)
        {
            if (!component)
                return;

            PoolObjectLifecycleHelper.InvokeOnDespawnForComponent(component);
            component.gameObject.SetActive(false);

            if (_parent && component.transform.parent != _parent)
            {
                component.transform.SetParent(_parent);
            }
        }

        public void OnDestroy(T component)
        {
            if (component)
                Object.Destroy(component.gameObject);
        }

        public bool Validate(T component)
        {
            return component != null && component.gameObject != null;
        }

        public void Dispose()
        {
            if (_prefabHandle != null && _prefabHandle.IsValid)
            {
                _prefabHandle.Dispose();
                _prefabHandle = null;
                _isHandleLoaded = false;
            }
        }
    }
}
