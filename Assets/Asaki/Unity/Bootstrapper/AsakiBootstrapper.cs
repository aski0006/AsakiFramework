using Asaki.Core.Broker;
using Asaki.Core.Configs;
using Asaki.Core.Context;
using Asaki.Core.Context.Resolvers;
using Asaki.Core.Logging;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using AsakiBroker = Asaki.Core.Broker.AsakiBroker;

namespace Asaki.Unity.Bootstrapper
{
	public struct FrameworkReadyEvent : IAsakiEvent { }

	[DefaultExecutionOrder(-9999)]
	public class AsakiBootstrapper : MonoBehaviour
	{
		[Header("Settings")]
		[Tooltip("是否自动扫描场景中的 MonoBehaviour 进行依赖注入")]
		[SerializeField] private bool _autoScanOnSceneLoad = true;

		[Tooltip("手动指定需要注入的 MonoBehaviour（高性能模式，仅在首场景使用）")]
		[SerializeField] private MonoBehaviour[] _manualTargets;

		[Header("Global MonoBehaviour Services")]
		[Tooltip("全局 MonoBehaviour 服务（DontDestroyOnLoad，贯穿整个游戏生命周期）")]
		[SerializeField] private MonoBehaviour[] _globalBehaviourServices;

		[Header("Configuration")]
		[SerializeField] private AsakiConfig _config;

		private static AsakiBootstrapper _instance;
		private IAsakiLoggingService _logService;

		// ===================================================================
		// 生命周期：Awake - 注册阶段
		// ===================================================================
		private void Awake()
		{
			// 单例检查
			if (_instance != null)
			{
				Destroy(gameObject);
				return;
			}
			_instance = this;
			DontDestroyOnLoad(gameObject);

			// ============================================
			// 第0阶段：极早期初始化 - 上下文清理
			// ============================================
			AsakiContext.ClearAll();
			Application.targetFrameRate = _config ? _config.TickRate : 60;

			// ============================================
			// 第1阶段：日志服务 V2 启动 (直连模式)
			// ============================================
			_logService = new AsakiLoggingService();
			AsakiContext.Register(_logService);

			if (_config != null)
			{
				_logService.ApplyConfig(_config.LogConfig);
			}

			// ============================================
			// 第2阶段：核心驱动与自我诊断
			// ============================================
			ALog.Info("=======================================");
			ALog.Info("== ASAKI FRAMEWORK V2 BOOT START ==");
			ALog.Info("=======================================");
			ALog.Info($"Bootstrapper ready.  Platform: {Application.platform}");

			// 注册全局配置
			if (_config != null)
			{
				AsakiContext.Register(_config);
				ALog.Info($"Configuration loaded: {_config.name}");
			}
			else
			{
				ALog.Warn("No configuration assigned in inspector!");
			}

			// ============================================
			// 第3阶段：注册全局 MonoBehaviour 服务
			// ============================================
			RegisterGlobalBehaviourServices();
		}

		// ===================================================================
		// 生命周期：Start - 初始化阶段
		// ===================================================================
		private async void Start()
		{
			try
			{
				// ============================================
				// 第4阶段：模块 DAG 系统启动
				// ============================================
				ALog.Info("Starting module discovery.. .");
				AsakiStaticModuleDiscovery discovery = new AsakiStaticModuleDiscovery();

				ALog.Info("Initializing modules (DAG)...");
				await AsakiModuleLoader.Startup(discovery);

				ALog.Info("Freezing context...");
				AsakiContext.Freeze();

				// ============================================
				// 第5阶段：初始化全局 MonoBehaviour 服务
				// ============================================
				InitializeGlobalBehaviourServices();

				// ============================================
				// 第6阶段：注册场景加载事件
				// ============================================
				RegisterSceneLoadEvents();

				// ============================================
				// 第7阶段：首场景注入（当前已加载的场景）
				// ============================================
				ALog.Info("Performing initial scene injection...");
				InjectCurrentScene();

				// ============================================
				// 完成启动
				// ============================================
				ALog.Info("Broadcasting ready event...");
				AsakiBroker.Publish(new FrameworkReadyEvent());

				ALog.Info("=======================================");
				ALog.Info("== ASAKI FRAMEWORK READY ==");
				ALog.Info("=======================================");
			}
			catch (Exception ex)
			{
				ALog.Fatal("Framework boot failed!", ex);
				throw;
			}
		}

		// ===================================================================
		// 全局 MonoBehaviour 服务管理
		// ===================================================================

		/// <summary>
		/// 注册全局 MonoBehaviour 服务到 AsakiContext
		/// <para>时机：Awake，在 Module 系统启动之前</para>
		/// </summary>
		private void RegisterGlobalBehaviourServices()
		{
			if (_globalBehaviourServices == null || _globalBehaviourServices.Length == 0)
			{
				ALog.Info("No global MonoBehaviour services configured.");
				return;
			}

			ALog.Info($"Registering {_globalBehaviourServices.Length} global MonoBehaviour services...");

			foreach (MonoBehaviour behaviour in _globalBehaviourServices)
			{
				if (behaviour == null)
				{
					ALog.Warn("Null reference in global behaviour services list, skipping.");
					continue;
				}

				// 验证接口实现
				if (behaviour is not IAsakiGlobalService service)
				{
					ALog.Error($"{behaviour.GetType().Name} does not implement IAsakiGlobalMonoBehaviourService!  Skipped.");
					continue;
				}

				// 注册所有服务接口
				Type behaviourType = behaviour.GetType();
				var serviceInterfaces = behaviourType.GetInterfaces()
				                                     .Where(i => typeof(IAsakiService).IsAssignableFrom(i) &&
				                                                 i != typeof(IAsakiService) &&
				                                                 i != typeof(IAsakiGlobalService));

				foreach (Type interfaceType in serviceInterfaces)
				{
					AsakiContext.Register(interfaceType, service);
					ALog.Info($"  → Registered {behaviourType.Name} as {interfaceType.Name}");
				}

				// 注册具体类型
				AsakiContext.Register(behaviourType, service);
			}
		}

		/// <summary>
		/// 初始化全局 MonoBehaviour 服务
		/// <para>时机：Start，在 Module DAG 完成之后</para>
		/// </summary>
		private void InitializeGlobalBehaviourServices()
		{
			if (_globalBehaviourServices == null || _globalBehaviourServices.Length == 0)
				return;

			ALog.Info("Initializing global MonoBehaviour services...");

			foreach (MonoBehaviour behaviour in _globalBehaviourServices)
			{
				if (behaviour is not IAsakiGlobalService service)
					continue;

				try
				{
					// 先注入依赖
					AsakiGlobalInjector.Inject(service);

					// 再调用初始化
					service.OnBootstrapInit();

					ALog.Info($"  → {behaviour.GetType().Name} initialized");
				}
				catch (Exception ex)
				{
					ALog.Error($"Failed to initialize {behaviour.GetType().Name}: {ex}");
				}
			}
		}

		// ===================================================================
		// 场景注入系统
		// ===================================================================

		/// <summary>
		/// 注册场景加载事件监听
		/// </summary>
		private void RegisterSceneLoadEvents()
		{
			ALog.Info("Registering scene load callbacks...");
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		/// <summary>
		/// 场景加载完成回调
		/// </summary>
		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			ALog.Info($"Scene '{scene.name}' loaded ({mode}). Performing injection...");
			InjectScene(scene);
		}

		/// <summary>
		/// 注入当前所有已加载的场景（用于首场景）
		/// </summary>
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
			IAsakiResolver sceneResolver = FindSceneContext(scene);

			if (sceneResolver != null)
			{
				ALog.Info($"  → Scene Context found.  Using Scene+Global resolution.");
			}
			else
			{
				ALog.Info($"  → No Scene Context.  Using Global-Only resolution.");
			}

			// 2. 执行注入
			if (_autoScanOnSceneLoad)
			{
				InjectSceneAutoScan(scene, sceneResolver);
			}
			else if (scene.buildIndex == 0 && _manualTargets != null) // 仅首场景使用手动列表
			{
				InjectSceneManual(sceneResolver);
			}

			ALog.Info($"[SceneInjector] Scene '{scene.name}' injection complete.");
		}

		/// <summary>
		/// 查找场景中的 AsakiSceneContext
		/// </summary>
		private IAsakiResolver FindSceneContext(Scene scene)
		{
			// 获取场景根对象
			var rootObjects = scene.GetRootGameObjects();

			foreach (GameObject rootObj in rootObjects)
			{
				// 在根对象及其子对象中查找
				AsakiSceneContext context = rootObj.GetComponentInChildren<AsakiSceneContext>(true);
				if (context != null)
				{
					return context;
				}
			}

			return null;
		}

		/// <summary>
		/// 自动扫描场景中的 MonoBehaviour 并注入
		/// </summary>
		private void InjectSceneAutoScan(Scene scene, IAsakiResolver resolver)
		{
			// 获取场景根对象
			var rootObjects = scene.GetRootGameObjects();
			int injectedCount = 0;

			foreach (GameObject rootObj in rootObjects)
			{
				// 获取所有 MonoBehaviour（包括未激活的）
				var behaviours = rootObj.GetComponentsInChildren<MonoBehaviour>(true);

				foreach (MonoBehaviour behaviour in behaviours)
				{
					// 跳过特殊类型
					if (behaviour == null) continue;
					if (behaviour is IAsakiModule) continue;      // Module 由 DAG 管理
					if (behaviour is AsakiBootstrapper) continue; // 跳过自身
					if (behaviour is AsakiSceneContext) continue; // 上下文由自身管理

					// 只注入标记了 IAsakiAutoInject 的类型
					if (behaviour is IAsakiAutoInject)
					{
						AsakiGlobalInjector.Inject(behaviour, resolver);
						injectedCount++;
					}
				}
			}

			ALog.Info($"  → Injected {injectedCount} MonoBehaviour(s) in scene '{scene.name}'");
		}

		/// <summary>
		/// 手动注入指定的 MonoBehaviour 列表
		/// </summary>
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

		// ===================================================================
		// 清理
		// ===================================================================

		private void OnDestroy()
		{
			if (_instance != this) return;
			ALog.Info("Asaki Framework shutting down...");

			// 取消场景事件监听
			SceneManager.sceneLoaded -= OnSceneLoaded;

			// 清理上下文
			AsakiContext.ClearAll();

			ALog.Reset(); // 重置日志

			_instance = null;
		}
	}
}
