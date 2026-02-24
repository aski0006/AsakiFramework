using System;
using System.Collections.Generic;
using Asaki.Core.Attributes;
using Asaki.Core.Broker;
using Asaki.Core.Logging;
using UnityEngine;

namespace Asaki.Core.Context.Resolvers
{
    /// <summary>
    /// 构建状态枚举，表示场景上下文的构建生命周期状态。
    /// </summary>
    public enum BuildState
    {
        /// <summary>
        /// 未构建：初始状态或已销毁重置后的状态。
        /// </summary>
        NotBuilt,

        /// <summary>
        /// 构建中：正在执行服务注入和初始化。
        /// </summary>
        Building,

        /// <summary>
        /// 已构建：构建成功完成。
        /// </summary>
        Built,

        /// <summary>
        /// 构建失败：构建过程中发生异常。
        /// </summary>
        Failed
    }

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
    ///
    /// [v3.3 修复] 延迟 Build() 到框架就绪事件后执行，避免时序问题。
    /// </remarks>
    [DefaultExecutionOrder(-100)]
    public class AsakiSceneContext
        : MonoBehaviour,
            IAsakiResolver,
            IAsakiHandler<OnAsakiFrameworkReadyEvent>
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

        /// <summary>
        /// 当前构建状态
        /// </summary>
        private BuildState _buildState = BuildState.NotBuilt;

        /// <summary>
        /// 构建失败时捕获的异常
        /// </summary>
        private Exception _buildException;

        /// <summary>
        /// 获取当前构建状态
        /// </summary>
        public BuildState State => _buildState;

        /// <summary>
        /// 获取构建失败时的异常信息
        /// </summary>
        public Exception BuildException => _buildException;

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

            ALog.Info(
                $"[AsakiSceneContext] Initialization complete. Waiting for framework ready..."
            );
        }

        /// <summary>
        /// 启用时订阅框架就绪事件
        /// </summary>
        private void OnEnable()
        {
            AsakiBroker.Subscribe<OnAsakiFrameworkReadyEvent>(this);
        }

        /// <summary>
        /// 禁用时取消订阅框架就绪事件
        /// </summary>
        private void OnDisable()
        {
            AsakiBroker.Unsubscribe<OnAsakiFrameworkReadyEvent>(this);
        }

        /// <summary>
        /// 框架就绪事件处理：当收到此事件时才执行 Build()
        /// </summary>
        public void OnEvent(in OnAsakiFrameworkReadyEvent e)
        {
            if (!_isBuilt)
            {
                Build();
            }
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
        /// <remarks>
        /// 状态转换：
        /// - NotBuilt -> Building -> Built (成功)
        /// - NotBuilt -> Building -> Failed (异常)
        /// - Built/Failed -> 直接返回（不重复构建）
        /// </remarks>
        public void Build()
        {
            // 快速路径检查 (无锁)：已构建或已失败时直接返回
            if (_buildState == BuildState.Built || _buildState == BuildState.Failed)
                return;

            lock (_buildLock)
            {
                // 双重检查锁定 (Double-Check Lock)
                if (_buildState == BuildState.Built || _buildState == BuildState.Failed)
                    return;

                // 设置状态为构建中
                _buildState = BuildState.Building;

                ALog.Info(
                    $"[AsakiSceneContext] Building {_pendingInitServices.Count} pending services..."
                );

                int successCount = 0;
                int failCount = 0;
                Exception caughtException = null;

                try
                {
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

                    // 构建成功，设置状态为已构建
                    _buildState = BuildState.Built;

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
                catch (Exception ex)
                {
                    caughtException = ex;
                    _buildException = ex;
                    _buildState = BuildState.Failed;

                    ALog.Error($"[AsakiSceneContext] Build failed with exception: {ex}");
                }
            }
        }

        /// <summary>
        /// 销毁时清理场景服务并重置构建状态。
        /// </summary>
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

            // 重置构建状态
            _isBuilt = false;
            _buildState = BuildState.NotBuilt;
            _buildException = null;
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

        /// <summary>
        /// 获取指定类型的服务实例。
        /// </summary>
        /// <typeparam name="T">服务类型，必须是实现了<see cref="IAsakiService"/>接口的类类型。</typeparam>
        /// <returns>请求的服务实例。</returns>
        /// <exception cref="KeyNotFoundException">当指定类型的服务未找到时抛出。</exception>
        /// <exception cref="CircularDependencyException">当检测到循环依赖时抛出。</exception>
        public T Get<T>()
            where T : class, IAsakiService
        {
            AsakiResolveContext.BeginResolve(typeof(T));
            try
            {
                if (_localServices.TryGetValue(typeof(T), out IAsakiService service))
                    return (T)service;

                return AsakiContext.Get<T>();
            }
            finally
            {
                AsakiResolveContext.EndResolve(typeof(T));
            }
        }

        /// <summary>
        /// 尝试获取指定类型的服务实例，如果找到则返回true，否则返回false。
        /// </summary>
        /// <typeparam name="T">服务类型，必须是实现了<see cref="IAsakiService"/>接口的类类型。</typeparam>
        /// <param name="service">如果找到服务，将返回的服务实例赋值给此参数；否则为null。</param>
        /// <returns>如果找到服务则返回true，否则返回false。</returns>
        /// <exception cref="CircularDependencyException">当检测到循环依赖时抛出。</exception>
        public bool TryGet<T>(out T service)
            where T : class, IAsakiService
        {
            AsakiResolveContext.BeginResolve(typeof(T));
            try
            {
                if (_localServices.TryGetValue(typeof(T), out IAsakiService s))
                {
                    service = (T)s;
                    return true;
                }
                return AsakiContext.TryGet(out service);
            }
            finally
            {
                AsakiResolveContext.EndResolve(typeof(T));
            }
        }
    }
}
