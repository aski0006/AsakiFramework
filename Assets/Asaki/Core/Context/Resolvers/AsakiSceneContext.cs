using System;
using System.Collections.Generic;
using Asaki.Core.Attributes;
using Asaki.Core.Logging;
using UnityEngine;

namespace Asaki.Core.Context.Resolvers
{
    /// <summary>
    /// Asaki场景上下文组件，用于管理场景级别的服务和依赖注入。
    /// </summary>
    /// <remarks>
    /// [v3.0 重构] 采用预制体服务注册模式：
    /// 1. Awake: 实例化预制体 → 扫描服务 → 注册服务
    /// 2. Build: 由 Bootstrapper 在全局环境就绪后显式调用，执行 Init。
    /// </remarks>
    [DefaultExecutionOrder(-100)]
    public class AsakiSceneContext : MonoBehaviour, IAsakiResolver
    {
        // ========================================================================
        // 配置字段
        // ========================================================================

        [Header("Pure C# Services")]
        [Tooltip("纯 C# 场景服务（通过 SerializeReference 序列化）")]
        [SerializeReference]
        [AsakiInterface(typeof(IAsakiSceneService))]
        private List<IAsakiSceneService> _pureCSharpServices = new List<IAsakiSceneService>();

        [Header("Scene Service Prefabs")]
        [Tooltip("场景服务预制体。运行时将实例化预制体并扫描注册所有 IAsakiSceneService")]
        [SerializeField]
        private GameObject[] _servicePrefabs;

        [Tooltip("实例化后的父对象（可选），为空则挂载到 SceneContext 所在对象下")]
        [SerializeField]
        private Transform _instanceParent;

        // ========================================================================
        // 运行时数据
        // ========================================================================

        private readonly Dictionary<Type, IAsakiService> _localServices =
            new Dictionary<Type, IAsakiService>();

        private readonly List<IAsakiInit> _pendingInitServices = new List<IAsakiInit>();

        private readonly List<GameObject> _instantiatedPrefabs = new List<GameObject>();

        private readonly List<(Type Type, IAsakiSceneService Service)> _pendingPrefabServices =
            new List<(Type, IAsakiSceneService)>();

        private bool _isBuilt = false;

        public bool IsBuilt => _isBuilt;

#if UNITY_EDITOR
        public Dictionary<Type, IAsakiService> GetRuntimeServices()
        {
            return _localServices;
        }

        public List<GameObject> GetInstantiatedPrefabs()
        {
            return _instantiatedPrefabs;
        }
#endif

        // ========================================================================
        // 生命周期
        // ========================================================================

        private void Awake()
        {
            ALog.Info(
                $"[AsakiSceneContext] Initializing in scene: {gameObject.scene.name}"
            );

            InstantiateServicePrefabs();

            ScanAndRegisterPrefabServices();

            RegisterPureCSharpServices();

            ALog.Info(
                $"[AsakiSceneContext] Registered {_localServices.Count} services. Waiting for Build()..."
            );
        }

        private void InstantiateServicePrefabs()
        {
            if (_servicePrefabs == null || _servicePrefabs.Length == 0)
                return;

            Transform parent = _instanceParent != null ? _instanceParent : transform;

            foreach (GameObject prefab in _servicePrefabs)
            {
                if (prefab == null)
                    continue;

                GameObject instance = Instantiate(prefab, parent);
                instance.name = prefab.name;
                _instantiatedPrefabs.Add(instance);
                ALog.Info($"[AsakiSceneContext] Instantiated service prefab: {prefab.name}");
            }
        }

        private void ScanAndRegisterPrefabServices()
        {
            foreach (GameObject instance in _instantiatedPrefabs)
            {
                if (instance != null)
                {
                    CollectServicesRecursive(instance.transform);
                }
            }

            foreach ((Type type, IAsakiSceneService service) in _pendingPrefabServices)
            {
                RegisterServiceWithInterfaces(type, service);
            }

            _pendingPrefabServices.Clear();
        }

        private void CollectServicesRecursive(Transform parent)
        {
            IAsakiSceneService[] services = parent.GetComponents<IAsakiSceneService>();
            foreach (IAsakiSceneService service in services)
            {
                if (service is MonoBehaviour behaviour)
                {
                    _pendingPrefabServices.Add((behaviour.GetType(), service));
                }
            }

            foreach (Transform child in parent)
            {
                CollectServicesRecursive(child);
            }
        }

        /// <summary>
        /// 构建上下文。
        /// 此方法必须由 Bootstrapper 在确认全局环境（如 SimulationService）就绪后调用。
        /// </summary>
        public void Build()
        {
            if (_isBuilt)
                return;

            ALog.Info(
                $"[AsakiSceneContext] Building {_pendingInitServices.Count} pending services..."
            );

            foreach (IAsakiInit service in _pendingInitServices)
            {
                try
                {
                    service.Init(this);
                }
                catch (Exception ex)
                {
                    ALog.Error(
                        $"[AsakiSceneContext] Failed to init service {service.GetType().Name}: {ex}"
                    );
                }
            }

            _pendingInitServices.Clear();
            _isBuilt = true;
            ALog.Info("[AsakiSceneContext] Build Complete.");
        }

        private void OnDestroy()
        {
            ALog.Info($"[AsakiSceneContext] Cleaning up scene services...");

            foreach (KeyValuePair<Type, IAsakiService> kvp in _localServices)
            {
                if (kvp.Value is IDisposable disposable && !(kvp.Value is MonoBehaviour))
                {
                    disposable.Dispose();
                }
            }

            _localServices.Clear();
            _pendingInitServices.Clear();
            _pendingPrefabServices.Clear();

            foreach (GameObject instance in _instantiatedPrefabs)
            {
                if (instance != null)
                    Destroy(instance);
            }
            _instantiatedPrefabs.Clear();

            _isBuilt = false;
        }

        // ========================================================================
        // 服务注册
        // ========================================================================

        private void RegisterPureCSharpServices()
        {
            if (_pureCSharpServices == null || _pureCSharpServices.Count == 0)
                return;

            foreach (IAsakiSceneService service in _pureCSharpServices)
            {
                if (service != null)
                {
                    RegisterServiceWithInterfaces(service.GetType(), service);
                }
            }
        }

        private void RegisterServiceWithInterfaces(Type concreteType, IAsakiService service)
        {
            RegisterInternal(concreteType, service);

            foreach (Type interfaceType in concreteType.GetInterfaces())
            {
                if (
                    typeof(IAsakiService).IsAssignableFrom(interfaceType)
                    && interfaceType != typeof(IAsakiService)
                    && interfaceType != typeof(IAsakiSceneService)
                    && interfaceType != typeof(IAsakiGlobalService)
                )
                {
                    RegisterInternal(interfaceType, service);
                }
            }

            if (service is IAsakiInit initable)
            {
                _pendingInitServices.Add(initable);
            }
        }

        public void Register<T>(T service)
            where T : class, IAsakiService
        {
            RegisterServiceWithInterfaces(typeof(T), service);
        }

        private void RegisterInternal(Type type, IAsakiService service)
        {
            if (_localServices.ContainsKey(type))
            {
                ALog.Warn($"[AsakiSceneContext] Service {type.Name} is being overwritten.");
            }
            _localServices[type] = service;
        }

        // ========================================================================
        // 服务解析（IAsakiResolver 实现）
        // ========================================================================

        public T Get<T>()
            where T : class, IAsakiService
        {
            if (_localServices.TryGetValue(typeof(T), out IAsakiService service))
                return (T)service;

            return AsakiContext.Get<T>();
        }

        public bool TryGet<T>(out T service)
            where T : class, IAsakiService
        {
            if (_localServices.TryGetValue(typeof(T), out IAsakiService s))
            {
                service = (T)s;
                return true;
            }
            return AsakiContext.TryGet(out service);
        }
    }
}
