using System;
using System.Collections.Generic;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.Context.Resolvers;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.Logging;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using AsakiBroker = Asaki.Core.Broker.AsakiBroker;

namespace Asaki.Unity.Bootstrapper
{

    [DefaultExecutionOrder(-9999)]
    public class AsakiBootstrapper : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("是否在场景加载时处理手动注入列表 (高性能模式，不再自动扫描全场景)")]
        [SerializeField]
        private bool _handleManualInjection = true;

        [Tooltip("手动指定需要注入的非 AsakiMono 脚本")]
        [SerializeField]
        private MonoBehaviour[] _manualTargets;

        [Header("Global Service Prefabs (Legacy)")]
        [Tooltip("[已弃用] 请使用 AsakiFrameworkSetting 中的 GlobalServiceRegistry 配置全局服务")]
        [SerializeField]
        [Obsolete("Use GlobalServiceRegistry in AsakiFrameworkSetting instead")]
        private GameObject[] _globalServicePrefabs;

        [Header("Framework Setting")]
        [SerializeField]
        private AsakiFrameworkSetting frameworkSetting;

        private static AsakiBootstrapper _instance;
        private static bool _isReady;
        private IAsakiLoggingService _logService;
        private readonly List<GameObject> _instantiatedServiceObjects = new();
        private readonly List<IAsakiGlobalService> _allGlobalServices = new();

        public static bool IsReady => _isReady;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (frameworkSetting == null)
                frameworkSetting = LoadConfigAsset();

            AsakiContext.ClearAll();
            Application.targetFrameRate = frameworkSetting ? frameworkSetting.TickRate : 60;

            _logService = new AsakiLoggingService();
            AsakiContext.Register(_logService);

            var logConfig = frameworkSetting?.LogConfig ?? new AsakiLogConfig();
            _logService.ApplyConfig(logConfig);

            ALog.Info("== ASAKI FRAMEWORK V2 BOOT START ==");

            if (frameworkSetting != null)
                AsakiContext.Register(frameworkSetting);

            InstantiateGlobalServicePrefabs();
            CollectAndRegisterGlobalServices();
        }

        private AsakiFrameworkSetting LoadConfigAsset()
        {
            var config = Resources.Load<AsakiFrameworkSetting>("AsakiFrameworkSetting");
#if UNITY_EDITOR
            if (config == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AsakiFrameworkSetting");
                if (guids.Length > 0)
                    config = UnityEditor.AssetDatabase.LoadAssetAtPath<AsakiFrameworkSetting>(
                        UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0])
                    );
            }
#endif
            return config;
        }

        private void Start() => StartAsync().Forget();

        private async UniTaskVoid StartAsync()
        {
            try
            {
                ALog.Info("Starting module discovery...");
                AsakiStaticModuleDiscovery discovery = new AsakiStaticModuleDiscovery();
                await AsakiModuleLoader.Startup(discovery);

                AsakiContext.Freeze();

                InitializeGlobalServices();
                RegisterSceneLoadEvents();

                // 初始场景注入：只处理手动目标，AsakiMono 会自动注册
                InjectCurrentSceneManualOnly();

                _isReady = true;
                AsakiContext.SetReady();
                AsakiBroker.Publish(new Asaki.Core.Context.OnAsakiFrameworkReadyEvent());
                ALog.Info("== ASAKI FRAMEWORK READY ==");
            }
            catch (Exception ex)
            {
                ALog.Fatal("Framework boot failed!", ex);
                throw;
            }
        }

        private void InstantiateGlobalServicePrefabs()
        {
            List<GameObject> prefabsToInstantiate = GetGlobalServicePrefabs();

            if (prefabsToInstantiate == null || prefabsToInstantiate.Count == 0)
                return;

            foreach (GameObject prefab in prefabsToInstantiate)
            {
                if (prefab == null)
                    continue;

                var instance = Instantiate(prefab, transform);
                instance.name = prefab.name;
                _instantiatedServiceObjects.Add(instance);
                ALog.Info($"[AsakiBootstrapper] Instantiated global service prefab: {prefab.name}");
            }
        }

        private List<GameObject> GetGlobalServicePrefabs()
        {
            if (frameworkSetting != null && frameworkSetting.GlobalServiceRegistry != null)
            {
                return frameworkSetting.GetGlobalServicePrefabs();
            }

#pragma warning disable CS0618
            if (_globalServicePrefabs is { Length: > 0 })
            {
                ALog.Warn(
                    "[AsakiBootstrapper] Using legacy _globalServicePrefabs field. Consider migrating to GlobalServiceRegistry."
                );
                return new List<GameObject>(_globalServicePrefabs);
            }
#pragma warning restore CS0618

            return new List<GameObject>();
        }

        private void CollectAndRegisterGlobalServices()
        {
            _allGlobalServices.Clear();
            var registeredTypes = new HashSet<Type>();

            foreach (var obj in _instantiatedServiceObjects)
            {
                if (!obj)
                    continue;
                CollectGlobalServicesRecursive(obj.transform, _allGlobalServices);
            }

            ALog.Info($"[AsakiBootstrapper] Collected {_allGlobalServices.Count} global service instances");

            int registeredCount = 0;
            int skippedCount = 0;

            foreach (var service in _allGlobalServices)
            {
                if (service is not MonoBehaviour behaviour)
                {
                    ALog.Warn($"[AsakiBootstrapper] Service {service.GetType().Name} is not a MonoBehaviour, skipping.");
                    skippedCount++;
                    continue;
                }

                Type type = behaviour.GetType();

                // 注册具体类型
                if (!registeredTypes.Contains(type))
                {
                    AsakiContext.Register(type, service);
                    registeredTypes.Add(type);
                    registeredCount++;
                }
                else
                {
                    ALog.Warn($"[AsakiBootstrapper] Service type {type.Name} already registered, skipping duplicate.");
                    skippedCount++;
                }

                // 注册接口
                foreach (Type i in type.GetInterfaces())
                {
                    if (
                        typeof(IAsakiService).IsAssignableFrom(i)
                        && i != typeof(IAsakiGlobalService)
                        && i != typeof(IAsakiService)
                    )
                    {
                        if (!registeredTypes.Contains(i))
                        {
                            AsakiContext.Register(i, service);
                            registeredTypes.Add(i);
                        }
                        else
                        {
                            ALog.Warn(
                                $"[AsakiBootstrapper] Interface {i.Name} already registered by another service, skipping."
                            );
                        }
                    }
                }
            }

            ALog.Info(
                $"[AsakiBootstrapper] Registered {registeredCount} services ({skippedCount} skipped due to duplicates)"
            );
        }

        private void CollectGlobalServicesRecursive(
            Transform parent,
            List<IAsakiGlobalService> results
        )
        {
            CollectGlobalServicesRecursive(parent, results, 0, 10);
        }

        /// <summary>
        /// 递归收集全局服务组件，带深度限制
        /// </summary>
        /// <param name="parent">当前遍历的 Transform</param>
        /// <param name="results">结果列表</param>
        /// <param name="currentDepth">当前深度</param>
        /// <param name="maxDepth">最大深度限制</param>
        private void CollectGlobalServicesRecursive(
            Transform parent,
            List<IAsakiGlobalService> results,
            int currentDepth,
            int maxDepth
        )
        {
            var services = parent.GetComponents<IAsakiGlobalService>();
            foreach (var service in services)
            {
                if (service != null)
                    results.Add(service);
            }

            // 检查深度限制
            if (currentDepth >= maxDepth)
            {
                if (currentDepth == maxDepth)
                {
                    ALog.Warn(
                        $"[AsakiBootstrapper] Max recursion depth ({maxDepth}) reached at {parent.name}. Stopping scan."
                    );
                }
                return;
            }

            // 遍历子对象
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                CollectGlobalServicesRecursive(parent.GetChild(i), results, currentDepth + 1, maxDepth);
            }
        }

        private void InitializeGlobalServices()
        {
            ALog.Info("== [GlobalServices] Phase 1: Dependency Injection ==");
            int injectSuccessCount = 0;
            int injectFailCount = 0;

            foreach (var service in _allGlobalServices)
            {
                try
                {
                    AsakiGlobalInjector.Inject(service);
                    injectSuccessCount++;
                }
                catch (Exception ex)
                {
                    injectFailCount++;
                    ALog.Error(
                        $"[AsakiBootstrapper] Failed to inject service {service.GetType().Name}: {ex}"
                    );
                }
            }

            if (injectFailCount > 0)
            {
                ALog.Warn(
                    $"[AsakiBootstrapper] Injection completed with {injectFailCount} failures, {injectSuccessCount} successes."
                );
            }

            ALog.Info("== [GlobalServices] Phase 2: Bootstrap Initialization ==");
            int initSuccessCount = 0;
            int initFailCount = 0;

            foreach (var service in _allGlobalServices)
            {
                try
                {
                    service.OnBootstrapInit();
                    initSuccessCount++;
                }
                catch (Exception ex)
                {
                    initFailCount++;
                    ALog.Error(
                        $"[AsakiBootstrapper] Failed to initialize service {service.GetType().Name}: {ex}"
                    );
                }
            }

            if (initFailCount > 0)
            {
                ALog.Warn(
                    $"[AsakiBootstrapper] Initialization completed with {initFailCount} failures, {initSuccessCount} successes."
                );
            }
            else
            {
                ALog.Info($"[AsakiBootstrapper] All {initSuccessCount} global services initialized successfully.");
            }
        }

        private void RegisterSceneLoadEvents()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // AsakiMono 已经通过 LifecycleManager 自动处理，这里只处理手动配置
            if (_handleManualInjection)
            {
                InjectSceneManual(scene);
            }
        }

        private void InjectCurrentSceneManualOnly()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                InjectSceneManual(SceneManager.GetSceneAt(i));
            }
        }

        private void InjectSceneManual(Scene scene)
        {
            // 查找场景 Context 并构建（始终执行，与是否有手动注入目标无关）
            IAsakiResolver resolver = AsakiGlobalResolver.Instance;
            var rootObjects = scene.GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                var ctx = root.GetComponentInChildren<AsakiSceneContext>(true);
                if (ctx != null)
                {
                    ctx.Build();
                    resolver = ctx;
                    break;
                }
            }

            // 只在有手动注入目标时才执行注入
            if (_manualTargets == null || _manualTargets.Length == 0)
                return;

            int count = 0;
            foreach (MonoBehaviour target in _manualTargets)
            {
                if (target != null && target.gameObject.scene == scene)
                {
                    AsakiGlobalInjector.Inject(target, resolver);
                    count++;
                }
            }
            if (count > 0)
                ALog.Info($"[AsakiBootstrapper] Manually injected {count} targets in {scene.name}");
        }

        private void OnDestroy()
        {
            if (_instance != this)
                return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            AsakiContext.ClearAll();
            ALog.Reset();
            _instance = null;
            _isReady = false;
        }
    }
}
