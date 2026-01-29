// 文件: Assets/Asaki/Core/Pooling/V2/Factories/ComponentFactory.cs
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
    /// Unity 组件工厂（直接池化组件而非 GameObject）
    /// 示例：池化 ParticleSystem, AudioSource, Rigidbody 等
    /// </summary>
    /// <typeparam name="T">组件类型（必须继承 Component）</typeparam>
    public class ComponentFactory<T> : IAsakiPoolObjectFactory<T>, IDisposable where T : Component
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
            _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
            _prefabPath = !string.IsNullOrEmpty(prefabPath)
                ? prefabPath
                : throw new ArgumentException("Prefab path cannot be null or empty", nameof(prefabPath));
            _parent = parent;
        }

        public async UniTask<T> CreateAsync(CancellationToken token = default)
        {
            if (!_isHandleLoaded)
            {
                _prefabHandle = await _resourceService.LoadAsync<GameObject>(_prefabPath, token);
                _isHandleLoaded = true;

                if (_prefabHandle == null || !_prefabHandle.IsValid)
                {
                    ALog.Error($"[AsakiPoolService] ComponentFactory Load Failure: {_prefabPath}");
                    return null;
                }
            }

            GameObject instance = _parent
                ? Object.Instantiate(_prefabHandle.Asset, _parent)
                : Object.Instantiate(_prefabHandle.Asset);

            instance.SetActive(false);

            // 获取组件
            T component = instance.GetComponent<T>();
            if (component == null)
            {
                ALog.Error($"[AsakiPoolService] ComponentFactory Lack of: {typeof(T).Name} in {_prefabPath}");
                Object.Destroy(instance);
                return null;
            }

            return component;
        }

        public void OnGet(T component)
        {
            if (!component) return;

            component.gameObject.SetActive(true);

            // 支持 IAsakiPoolable
            if (component is IAsakiPoolable poolable)
            {
                try { poolable.OnSpawn(); }
                catch (Exception ex) { ALog.Error($"[AsakiPoolService] ComponentFactory OnSpawn Failure: {ex.Message}", ex); }
            }
        }

        public void OnReturn(T component)
        {
            if (!component) return;

            // 支持 IAsakiPoolable
            if (component is IAsakiPoolable poolable)
            {
                try { poolable.OnDespawn(); }
                catch (Exception ex) { ALog.Error($"[ComponentFactory] OnDespawn 失败: {ex.Message}", ex); }
            }

            component.gameObject.SetActive(false);

            if (_parent && component.transform.parent != _parent)
            {
                component.transform.SetParent(_parent);
            }
        }

        public void OnDestroy(T component)
        {
            if (component) Object.Destroy(component.gameObject);
        }

        public bool Validate(T component)
        {
            return component;
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