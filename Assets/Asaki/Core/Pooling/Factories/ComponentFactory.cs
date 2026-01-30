// 文件: Assets/Asaki/Core/Pooling/V2/Factories/ComponentFactory.cs
using System;
using System.Threading;
using Asaki.Core.Logging;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.Resources;
using Cysharp.Threading.Tasks;
using UnityEngine;
// 使用 IAsakiPoolable 的旧命名空间兼容性导入
using IAsakiPoolable = Asaki.Core.Pooling.Interfaces.IAsakiPoolable;
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

        public async UniTask<T> CreateAsync(CancellationToken token = default)
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
                _isHandleLoaded = true;

                if (_prefabHandle == null || !_prefabHandle.IsValid)
                {
                    ALog.Error(
                        $"[AsakiPool] ComponentFactory failed to load prefab: {_prefabPath}"
                    );
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

            if (component is IAsakiPoolable poolable)
            {
                try
                {
                    poolable.OnSpawn();
                }
                catch (Exception ex)
                {
                    ALog.Error($"[AsakiPool] ComponentFactory OnSpawn failed: {ex.Message}", ex);
                }
            }
        }

        public void OnReturn(T component)
        {
            if (!component)
                return;

            if (component is IAsakiPoolable poolable)
            {
                try
                {
                    poolable.OnDespawn();
                }
                catch (Exception ex)
                {
                    ALog.Error($"[AsakiPool] ComponentFactory OnDespawn failed: {ex.Message}", ex);
                }
            }

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
