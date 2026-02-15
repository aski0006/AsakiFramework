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
    public struct OnAsakiFrameworkReadyEvent : IAsakiEvent { }

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

        [Header("Global Service Prefabs")]
        [Tooltip("全局服务预制体列表，框架启动时自动实例化并注册所有 IAsakiGlobalService 组件")]
        [SerializeField]
        private GameObject[] _globalServicePrefabs;

        [Header("DataTable")]
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
                AsakiBroker.Publish(new OnAsakiFrameworkReadyEvent());
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
            if (_globalServicePrefabs == null || _globalServicePrefabs.Length == 0)
                return;

            foreach (GameObject prefab in _globalServicePrefabs)
            {
                if (prefab == null)
                    continue;

                var instance = Instantiate(prefab, transform);
                instance.name = prefab.name;
                _instantiatedServiceObjects.Add(instance);
                ALog.Info($"[AsakiBootstrapper] Instantiated global service prefab: {prefab.name}");
            }
        }

        private void CollectAndRegisterGlobalServices()
        {
            _allGlobalServices.Clear();

            foreach (var obj in _instantiatedServiceObjects)
            {
                if (!obj)
                    continue;
                CollectGlobalServicesRecursive(obj.transform, _allGlobalServices);
            }

            foreach (var service in _allGlobalServices)
            {
                if (service is not MonoBehaviour behaviour)
                    continue;

                Type type = behaviour.GetType();
                AsakiContext.Register(type, service);

                foreach (Type i in type.GetInterfaces())
                {
                    if (
                        typeof(IAsakiService).IsAssignableFrom(i)
                        && i != typeof(IAsakiGlobalService)
                        && i != typeof(IAsakiService)
                    )
                    {
                        AsakiContext.Register(i, service);
                    }
                }
            }

            ALog.Info($"[AsakiBootstrapper] Registered {_allGlobalServices.Count} global services");
        }

        private void CollectGlobalServicesRecursive(
            Transform parent,
            List<IAsakiGlobalService> results
        )
        {
            var services = parent.GetComponents<IAsakiGlobalService>();
            foreach (var service in services)
            {
                if (service != null)
                    results.Add(service);
            }

            foreach (Transform child in parent)
            {
                CollectGlobalServicesRecursive(child, results);
            }
        }

        private void InitializeGlobalServices()
        {
            foreach (var service in _allGlobalServices)
            {
                AsakiGlobalInjector.Inject(service);
                service.OnBootstrapInit();
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
