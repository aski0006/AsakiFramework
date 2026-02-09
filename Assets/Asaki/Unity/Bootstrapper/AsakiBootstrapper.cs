using System;
using Asaki.Core.Broker;
using Asaki.Core.Configs;
using Asaki.Core.Context;
using Asaki.Core.Context.Resolvers;
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
        [Tooltip("是否自动扫描场景中的 MonoBehaviour 进行依赖注入")]
        [SerializeField]
        private bool _autoScanOnSceneLoad = true;

        [Tooltip("手动指定需要注入的 MonoBehaviour（高性能模式，仅在首场景使用）")]
        [SerializeField]
        private MonoBehaviour[] _manualTargets;

        [Header("Global MonoBehaviour Services")]
        [Tooltip("全局 MonoBehaviour 服务（DontDestroyOnLoad，贯穿整个游戏生命周期）")]
        [SerializeField]
        private MonoBehaviour[] _globalBehaviourServices;

        [Header("Configuration")]
        [SerializeField]
        private AsakiConfig _config;

        [Header("Performance")]
        [Tooltip("每帧最大注入数量，0表示无限制")]
        [SerializeField]
#pragma warning disable CS0414
        private int _maxInjectionsPerFrame = 0;
#pragma warning restore CS0414

        private static AsakiBootstrapper _instance;
        private static bool _isInitializing;
        private static bool _isReady;
        private IAsakiLoggingService _logService;

        /// <summary>
        /// 获取 Bootstrapper 实例，如果不存在则自动创建
        /// </summary>
        public static AsakiBootstrapper Instance
        {
            get
            {
                if (_instance == null)
                {
                    EnsureInstance();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 框架是否已准备就绪
        /// </summary>
        public static bool IsReady => _isReady;

        /// <summary>
        /// 确保 Bootstrapper 实例存在（运行时自动创建入口）
        /// </summary>
        public static void EnsureInstance()
        {
            if (_instance != null)
                return;

            if (_isInitializing)
                return;

            // 尝试查找现有实例
            _instance = FindFirstObjectByType<AsakiBootstrapper>();
            if (_instance != null)
                return;

#if UNITY_EDITOR
            // 编辑器模式下，检查是否需要自动创建
            if (!Application.isPlaying)
                return;
#endif

            // 自动创建 Bootstrapper
            _isInitializing = true;
            GameObject go = new GameObject("[AsakiBootstrapper]");
            _instance = go.AddComponent<AsakiBootstrapper>();
            Debug.Log("[AsakiBootstrapper] Auto-created instance.");
        }

        /// <summary>
        /// 等待框架准备就绪
        /// </summary>
        public static async UniTask WaitForReadyAsync()
        {
            if (_isReady)
                return;

            EnsureInstance();

            // 等待框架就绪事件
            await UniTask.WaitUntil(() => _isReady);
        }

        private void Awake()
        {
            // 单例检查
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // 如果未设置 Config，尝试自动加载
            if (_config == null)
            {
                _config = LoadConfigAsset();
            }

            AsakiContext.ClearAll();
            Application.targetFrameRate = _config ? _config.TickRate : 60;

            _logService = new AsakiLoggingService();
            AsakiContext.Register(_logService);

            // 应用日志配置 - 确保配置不为null
            AsakiLogConfig logConfig = _config?.LogConfig;
            if (logConfig == null)
            {
                logConfig = new AsakiLogConfig(); // 使用默认配置
                Debug.Log("[AsakiBootstrapper] Using default log config.");
            }
            _logService.ApplyConfig(logConfig);

            ALog.Info("=======================================");
            ALog.Info("== ASAKI FRAMEWORK V2 BOOT START ==");
            ALog.Info("=======================================");

            if (_config != null)
                AsakiContext.Register(_config);

            RegisterGlobalBehaviourServices();
        }

        /// <summary>
        /// 自动加载 Config 资源
        /// </summary>
        private AsakiConfig LoadConfigAsset()
        {
            // 尝试从 Resources 加载
            var config = Resources.Load<AsakiConfig>("AsakiConfig");
            if (config != null)
            {
                ALog.Info("[AsakiBootstrapper] Auto-loaded config from Resources.");
                return config;
            }

#if UNITY_EDITOR
            // 编辑器模式下，尝试从 AssetDatabase 查找
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AsakiConfig");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                config = UnityEditor.AssetDatabase.LoadAssetAtPath<AsakiConfig>(path);
                if (config != null)
                {
                    ALog.Info($"[AsakiBootstrapper] Auto-loaded config from AssetDatabase: {path}");
                    return config;
                }
            }
#endif

            ALog.Warn("[AsakiBootstrapper] Config asset not found. Using default settings.");
            return null;
        }

        private void Start()
        {
            StartAsync().Forget();
        }

        private async UniTaskVoid StartAsync()
        {
            try
            {
                ALog.Info("Starting module discovery...");
                AsakiStaticModuleDiscovery discovery = new AsakiStaticModuleDiscovery();

                ALog.Info("Initializing modules (DAG)...");
                await AsakiModuleLoader.Startup(discovery);

                ALog.Info("Freezing context...");
                AsakiContext.Freeze();

                InitializeGlobalBehaviourServices();
                RegisterSceneLoadEvents();

                ALog.Info("Performing initial scene injection...");
                InjectCurrentScene();

                ALog.Info("Broadcasting ready event...");
                _isReady = true;
                _isInitializing = false;
                AsakiBroker.Publish(new OnAsakiFrameworkReadyEvent());

                ALog.Info("=======================================");
                ALog.Info("== ASAKI FRAMEWORK READY ==");
                ALog.Info("=======================================");
            }
            catch (Exception ex)
            {
                _isInitializing = false;
                ALog.Fatal("Framework boot failed!", ex);
                throw;
            }
        }

        private void RegisterGlobalBehaviourServices()
        {
            if (_globalBehaviourServices == null)
                return;
            foreach (MonoBehaviour behaviour in _globalBehaviourServices)
            {
                if (behaviour == null)
                    continue;
                if (behaviour is not IAsakiGlobalService service)
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
        }

        private void InitializeGlobalBehaviourServices()
        {
            if (_globalBehaviourServices == null)
                return;
            foreach (MonoBehaviour behaviour in _globalBehaviourServices)
            {
                if (behaviour is IAsakiGlobalService service)
                {
                    AsakiGlobalInjector.Inject(service);
                    service.OnBootstrapInit();
                }
            }
        }

        // ===================================================================
        // 场景注入系统
        // ===================================================================

        private void RegisterSceneLoadEvents()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ALog.Info($"Scene '{scene.name}' loaded ({mode}). Performing injection...");
            InjectScene(scene);
        }

        private void InjectCurrentScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    InjectScene(scene);
                }
            }
        }

        /// <summary>
        /// 注入指定场景中的所有 MonoBehaviour
        /// </summary>
        private void InjectScene(Scene scene)
        {
            ALog.Info($"[SceneInjector] Processing scene: {scene.name}");

            // 1. 查找场景上下文
            IAsakiResolver resolver = null;

            // [v2.0 修复] 尝试获取具体类型的 AsakiSceneContext 以便调用 Build
            AsakiSceneContext sceneContext = FindSceneContext(scene);

            if (sceneContext != null)
            {
                ALog.Info($"  → Scene Context found. Igniting services...");
                // [关键修复] 在开始注入 View 之前，强制构建场景上下文
                // 这确保了 Architecture 在 View Init 之前已经完成了自己的 Init (包括 Simulation 注册)
                sceneContext.Build();

                resolver = sceneContext;
            }
            else
            {
                ALog.Info($"  → No Scene Context. Using Global-Only resolution.");
                resolver = AsakiGlobalResolver.Instance;
            }

            // 2. 执行注入
            if (_autoScanOnSceneLoad)
            {
                InjectSceneAutoScan(scene, resolver);
            }
            else if (scene.buildIndex == 0 && _manualTargets != null)
            {
                InjectSceneManual(resolver);
            }

            ALog.Info($"[SceneInjector] Scene '{scene.name}' injection complete.");
        }

        /// <summary>
        /// 查找场景中的 AsakiSceneContext
        /// </summary>
        private AsakiSceneContext FindSceneContext(Scene scene)
        {
            var rootObjects = scene.GetRootGameObjects();
            foreach (GameObject rootObj in rootObjects)
            {
                AsakiSceneContext context = rootObj.GetComponentInChildren<AsakiSceneContext>(true);
                if (context != null)
                {
                    return context;
                }
            }
            return null;
        }

        private void InjectSceneAutoScan(Scene scene, IAsakiResolver resolver)
        {
            var rootObjects = scene.GetRootGameObjects();
            int injectedCount = 0;

            foreach (GameObject rootObj in rootObjects)
            {
                var behaviours = rootObj.GetComponentsInChildren<MonoBehaviour>(true);

                foreach (MonoBehaviour behaviour in behaviours)
                {
                    if (behaviour == null)
                        continue;
                    if (behaviour is AsakiBootstrapper)
                        continue;
                    if (behaviour is AsakiSceneContext)
                        continue;

                    if (behaviour is IAsakiAutoInject)
                    {
                        AsakiGlobalInjector.Inject(behaviour, resolver);
                        injectedCount++;
                    }
                }
            }

            ALog.Info($"  → Injected {injectedCount} MonoBehaviour(s) in scene '{scene.name}'");
        }

        private void InjectSceneManual(IAsakiResolver resolver)
        {
            if (_manualTargets == null || _manualTargets.Length == 0)
                return;

            ALog.Info($"  → Manual injection mode:  {_manualTargets.Length} target(s)");

            foreach (MonoBehaviour target in _manualTargets)
            {
                if (target != null)
                {
                    AsakiGlobalInjector.Inject(target, resolver);
                }
            }
        }

        private void OnDestroy()
        {
            if (_instance != this)
                return;
            ALog.Info("Asaki Framework shutting down...");
            SceneManager.sceneLoaded -= OnSceneLoaded;
            AsakiContext.ClearAll();
            ALog.Reset();
            _instance = null;
            _isReady = false;
            _isInitializing = false;
        }
    }
}
