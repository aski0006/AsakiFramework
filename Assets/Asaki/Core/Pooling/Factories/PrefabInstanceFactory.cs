// 文件: Assets/Asaki/Core/Pooling/V2/Factories/PrefabInstanceFactory.cs
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

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="resourceService">资源服务</param>
        /// <param name="prefabPath">预制体资源路径</param>
        /// <param name="parent">实例化父节点（可选）</param>
        /// <param name="worldPositionStays">实例化时是否保持世界坐标</param>
        public PrefabInstanceFactory(
            IAsakiResourceService resourceService,
            string prefabPath,
            Transform parent = null,
            bool worldPositionStays = false
        )
        {
            _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
            _prefabPath = !string.IsNullOrEmpty(prefabPath)
                ? prefabPath
                : throw new ArgumentException("Prefab path cannot be null or empty", nameof(prefabPath));
            _parent = parent;
            _worldPositionStays = worldPositionStays;
        }

        public async UniTask<GameObject> CreateAsync(CancellationToken token = default)
        {
            // 延迟加载预制体
            if (!_isHandleLoaded)
            {
                try
                {
                    _prefabHandle = await _resourceService.LoadAsync<GameObject>(_prefabPath, token);
                    _isHandleLoaded = true;

                    if (_prefabHandle == null || !_prefabHandle.IsValid)
                    {
                        ALog.Error($"[AsakiPoolService] PrefabInstanceFactory Prefab load failure: {_prefabPath}");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    ALog.Error($"[AsakiPoolService] PrefabInstanceFactory Prefab load failure: {_prefabPath}, {ex.Message}", ex);
                    return null;
                }
            }

            // 实例化
            GameObject instance = _parent
                ? Object.Instantiate(_prefabHandle.Asset, _parent, _worldPositionStays)
                : Object.Instantiate(_prefabHandle.Asset);

            instance.SetActive(false);  // 默认禁用，等待池激活

            return instance;
        }

        public void OnGet(GameObject obj)
        {
            if (!obj) return;

            obj.SetActive(true);

            // ✅ 支持 IAsakiPoolable 接口
            var poolable = obj.GetComponent<IAsakiPoolable>();
            if (poolable != null)
            {
                try
                {
                    poolable.OnSpawn();
                }
                catch (Exception ex)
                {
                    ALog.Error($"[AsakiPoolService] PrefabInstanceFactory OnSpawn callback failed: {ex.Message}", ex);
                }
            }
        }

        public void OnReturn(GameObject obj)
        {
            if (!obj) return;

            // ✅ 支持 IAsakiPoolable 接口
            var poolable = obj.GetComponent<IAsakiPoolable>();
            if (poolable != null)
            {
                try
                {
                    poolable.OnDespawn();
                }
                catch (Exception ex)
                {
                    ALog.Error($"[AsakiPoolService] PrefabInstanceFactory OnDespawn callback failed: {ex.Message}", ex);
                }
            }

            obj.SetActive(false);

            // 归位到父节点
            if (_parent && obj.transform.parent != _parent)
            {
                obj.transform.SetParent(_parent, _worldPositionStays);
            }
        }

        public void OnDestroy(GameObject obj)
        {
            if (obj) Object.Destroy(obj);
        }

        public bool Validate(GameObject obj)
        {
            return obj;  // Unity 对象的隐式 bool 转换
        }

        public void Dispose()
        {
            if (_prefabHandle != null && _prefabHandle.IsValid)
            {
                _prefabHandle.Dispose();
                _prefabHandle = null;
                _isHandleLoaded = false;
                ALog.Info($"[AsakiPoolService] Released prefab handle: {_prefabPath}");
            }
        }
    }
}