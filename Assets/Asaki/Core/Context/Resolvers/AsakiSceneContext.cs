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
    /// 1. Awake: 注册纯C#服务 → 实例化预制体 → 扫描服务
    /// 2. Build: 由 Bootstrapper 在全局环境就绪后显式调用，执行 Init。
    ///
    /// [v3.1 修复] 调整执行顺序，确保纯C#服务在预制体实例化前注册，
    ///             避免预制体Awake触发Build()时pending services为空的问题。
    ///
    /// [v3.2 修复] 添加初始化状态标志，防止 AsakiMonoLifecycleManager 提前触发 Build()，
    ///             确保所有服务（包括预制体服务）注册完成后才执行注入。
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

        private readonly List<IAsakiInject> _pendingInitServices = new List<IAsakiInject>();

        private readonly List<GameObject> _instantiatedPrefabs = new List<GameObject>();

        private readonly List<(Type Type, IAsakiSceneService Service)> _pendingPrefabServices =
            new List<(Type, IAsakiSceneService)>();

        private volatile bool _isBuilt = false;

        private readonly object _buildLock = new object();

        private bool _isInitializing = false;

        public bool IsBuilt => _isBuilt;

        public bool IsInitializingServices => _isInitializing;

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
            ALog.Info($"[AsakiSceneContext] Initializing in scene: {gameObject.scene.name}");

            _isInitializing = true;

            RegisterPureCSharpServices();

            ALog.Info($"[AsakiSceneContext] Registered {_localServices.Count} pure C# services.");

            InstantiateServicePrefabs();

            ScanAndRegisterPrefabServices();

            _isInitializing = false;

            ALog.Info($"[AsakiSceneContext] Total {_localServices.Count} services registered.");

            Build();

            ALog.Info(
                $"[AsakiSceneContext] Initialization complete. Waiting for framework ready..."
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
                    // 限制最大递归深度为 10 层，防止过深遍历
                    CollectServicesRecursive(instance.transform, 0, 10);
                }
            }

            foreach ((Type type, IAsakiSceneService service) in _pendingPrefabServices)
            {
                RegisterServiceWithInterfaces(type, service);
            }

            _pendingPrefabServices.Clear();
        }

        /// <summary>
        /// 递归收集服务组件，带深度限制
        /// </summary>
        /// <param name="parent">当前遍历的 Transform</param>
        /// <param name="currentDepth">当前深度</param>
        /// <param name="maxDepth">最大深度限制</param>
        private void CollectServicesRecursive(Transform parent, int currentDepth, int maxDepth)
        {
            // 获取组件 (使用非分配方式)
            var services = parent.GetComponents<IAsakiSceneService>();
            foreach (IAsakiSceneService service in services)
            {
                if (service is MonoBehaviour behaviour)
                {
                    _pendingPrefabServices.Add((behaviour.GetType(), service));
                }
            }

            // 检查深度限制
            if (currentDepth >= maxDepth)
            {
                if (currentDepth == maxDepth)
                {
                    ALog.Warn(
                        $"[AsakiSceneContext] Max recursion depth ({maxDepth}) reached at {parent.name}. Stopping scan."
                    );
                }
                return;
            }

            // 遍历子对象 (使用反向迭代器避免 enumerator 分配)
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                CollectServicesRecursive(parent.GetChild(i), currentDepth + 1, maxDepth);
            }
        }

        /// <summary>
        /// 构建上下文。
        /// 此方法必须由 Bootstrapper 在确认全局环境（如 SimulationService）就绪后调用。
        /// </summary>
        public void Build()
        {
            // 快速路径检查 (无锁)
            if (_isBuilt)
                return;

            lock (_buildLock)
            {
                // 双重检查锁定 (Double-Check Lock)
                if (_isBuilt)
                    return;

                ALog.Info(
                    $"[AsakiSceneContext] Building {_pendingInitServices.Count} pending services..."
                );

                int successCount = 0;
                int failCount = 0;

                foreach (IAsakiInject service in _pendingInitServices)
                {
                    try
                    {
                        service.Inject(this);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        ALog.Error(
                            $"[AsakiSceneContext] Failed to init service {service.GetType().Name}: {ex}"
                        );
                    }
                }

                _pendingInitServices.Clear();
                _isBuilt = true;

                if (failCount > 0)
                {
                    ALog.Warn(
                        $"[AsakiSceneContext] Build completed with {failCount} failures, {successCount} successes."
                    );
                }
                else
                {
                    ALog.Info("[AsakiSceneContext] Build Complete.");
                }
            }
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

            if (service is IAsakiInject initable)
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
