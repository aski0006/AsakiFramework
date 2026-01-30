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
    /// GameObject 预制体实例化工厂（集成资源服务）
    /// 特性：
    /// - 支持设置父节点
    /// - 支持 IAsakiPoolable 生命周期回调
    /// - 自动管理资源句柄
    /// </summary>
    public class PrefabInstanceFactory : IAsakiPoolObjectFactory<GameObject>, IDisposable
    {
        private readonly IAsakiResourceService _resourceService;
        private readonly string _prefabPath;
        private readonly Transform _parent;
        private readonly bool _worldPositionStays;
        private ResHandle<GameObject> _prefabHandle;
        private bool _isHandleLoaded;

        public PrefabInstanceFactory(
            IAsakiResourceService resourceService,
            string prefabPath,
            Transform parent = null,
            bool worldPositionStays = false
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
            _worldPositionStays = worldPositionStays;
        }

        public async UniTask<GameObject> CreateAsync(
            CancellationToken token = default(CancellationToken)
        )
        {
            await EnsurePrefabLoadedAsync(token);
            return CreateGameObjectInstance();
        }

        public GameObject CreateSync()
        {
            if (!_isHandleLoaded)
            {
                throw new InvalidOperationException(
                    "Prefab not loaded. Call CreateAsync first or ensure async initialization is complete."
                );
            }
            return CreateGameObjectInstance();
        }

        private async UniTask EnsurePrefabLoadedAsync(CancellationToken token)
        {
            if (!_isHandleLoaded)
            {
                try
                {
                    _prefabHandle = await _resourceService.LoadAsync<GameObject>(
                        _prefabPath,
                        token
                    );

                    if (_prefabHandle == null || !_prefabHandle.IsValid)
                    {
                        ALog.Error(
                            $"[AsakiPool] PrefabInstanceFactory failed to load prefab: {_prefabPath}"
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
                        $"[AsakiPool] PrefabInstanceFactory failed to load prefab: {_prefabPath}, {ex.Message}",
                        ex
                    );
                }
            }
        }

        private GameObject CreateGameObjectInstance()
        {
            if (_prefabHandle == null || !_prefabHandle.IsValid)
            {
                return null;
            }

            GameObject instance = _parent
                ? Object.Instantiate(_prefabHandle.Asset, _parent, _worldPositionStays)
                : Object.Instantiate(_prefabHandle.Asset);

            instance.SetActive(false);
            return instance;
        }

        public void OnGet(GameObject obj)
        {
            if (!obj)
                return;

            obj.SetActive(true);

            IAsakiPoolable poolable = obj.GetComponent<IAsakiPoolable>();
            if (poolable != null)
            {
                try
                {
                    poolable.OnSpawn();
                }
                catch (Exception ex)
                {
                    ALog.Error(
                        $"[AsakiPool] PrefabInstanceFactory OnSpawn failed: {ex.Message}",
                        ex
                    );
                }
            }
        }

        public void OnReturn(GameObject obj)
        {
            if (!obj)
                return;

            IAsakiPoolable poolable = obj.GetComponent<IAsakiPoolable>();
            if (poolable != null)
            {
                try
                {
                    poolable.OnDespawn();
                }
                catch (Exception ex)
                {
                    ALog.Error(
                        $"[AsakiPool] PrefabInstanceFactory OnDespawn failed: {ex.Message}",
                        ex
                    );
                }
            }

            obj.SetActive(false);

            if (_parent && obj.transform.parent != _parent)
            {
                obj.transform.SetParent(_parent, _worldPositionStays);
            }
        }

        public void OnDestroy(GameObject obj)
        {
            if (obj)
                Object.Destroy(obj);
        }

        public bool Validate(GameObject obj)
        {
            return obj != null;
        }

        public void Dispose()
        {
            if (_prefabHandle != null && _prefabHandle.IsValid)
            {
                _prefabHandle.Dispose();
                _prefabHandle = null;
                _isHandleLoaded = false;
                ALog.Info($"[AsakiPool] Released prefab handle: {_prefabPath}");
            }
        }
    }
}
