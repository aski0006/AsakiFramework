using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Asaki.Core.Context.Resolvers
{
	/// <summary>
	/// Asaki场景上下文组件，用于管理场景级别的服务和依赖注入。
	/// </summary>
	/// <remarks>
	/// 此组件是场景中服务的容器，实现了<see cref="IAsakiResolver"/>接口，提供了服务解析功能。
	/// 支持两种类型的服务：纯C#服务和MonoBehaviour服务。
	/// 纯C#服务通过SerializeReference序列化，在Awake时实例化并注册。
	/// MonoBehaviour服务通过Unity原生引用，仅作为服务注册，由Bootstrapper负责注入。
	/// 使用DefaultExecutionOrder属性设置为-100，确保在大多数其他MonoBehaviour之前执行。
	/// </remarks>
	[DefaultExecutionOrder(-100)]
	public class AsakiSceneContext : MonoBehaviour, IAsakiResolver
	{
		// ========================================================================
		// 配置字段
		// ========================================================================

		/// <summary>
		/// 纯C#场景服务列表。
		/// </summary>
		/// <remarks>
		/// 通过SerializeReference序列化，在Awake时实例化并注册。
		/// 必须实现<see cref="IAsakiSceneService"/>接口。
		/// 使用<see cref="AsakiInterfaceAttribute"/>限制可分配的类型。
		/// </remarks>
		[Header("Pure C# Services")]
		[Tooltip("纯 C# 场景服务（通过 SerializeReference 序列化）\n在 Awake 时实例化并注册")]
		[SerializeReference]
		[AsakiInterface(typeof(IAsakiSceneService))]
		private List<IAsakiSceneService> _pureCSharpServices = new List<IAsakiSceneService>();

		/// <summary>
		/// MonoBehaviour场景服务列表。
		/// </summary>
		/// <remarks>
		/// 通过Unity原生引用，仅作为服务注册，不会被注入（由Bootstrapper负责）。
		/// 必须实现<see cref="IAsakiSceneService"/>接口。
		/// </remarks>
		[Header("MonoBehaviour Services")]
		[Tooltip("MonoBehaviour 场景服务（通过 Unity 原生引用）\n仅作为服务注册，不会被注入（由 Bootstrapper 负责）")]
		[SerializeField]
		private List<MonoBehaviour> _behaviourServices = new List<MonoBehaviour>();

		// ========================================================================
		// 运行时数据
		// ========================================================================

		/// <summary>
		/// 本地服务字典，用于存储场景级别的服务实例。
		/// </summary>
		private readonly Dictionary<Type, IAsakiService> _localServices = new Dictionary<Type, IAsakiService>();

		#if UNITY_EDITOR
		/// <summary>
		/// 在编辑器模式下获取运行时服务字典，用于调试和测试。
		/// </summary>
		/// <returns>运行时服务字典。</returns>
		public Dictionary<Type, IAsakiService> GetRuntimeServices()
		{
			return _localServices;
		}
		#endif

		// ========================================================================
		// 生命周期
		// ========================================================================

		/// <summary>
		/// MonoBehaviour的Awake方法，在游戏对象实例化时调用。
		/// </summary>
		/// <remarks>
		/// 在此方法中初始化场景上下文，注册纯C#服务和MonoBehaviour服务。
		/// 执行顺序由DefaultExecutionOrder属性控制，确保在大多数其他MonoBehaviour之前执行。
		/// </remarks>
		private void Awake()
		{
			ALog.Info($"[AsakiSceneContext] Initializing in scene: {gameObject.scene.name}");

			// 1. 注册纯 C# 服务（立即创建并注册）
			RegisterPureCSharpServices();

			// 2. 注册 MonoBehaviour 服务（仅注册引用，不注入）
			RegisterBehaviourServices();

			ALog.Info($"[AsakiSceneContext] Registered {_localServices.Count} scene services");
		}

		/// <summary>
		/// MonoBehaviour的OnDestroy方法，在游戏对象销毁时调用。
		/// </summary>
		/// <remarks>
		/// 在此方法中清理场景服务，释放纯C#服务的资源（MonoBehaviour服务由Unity管理）。
		/// </remarks>
		private void OnDestroy()
		{
			ALog.Info($"[AsakiSceneContext] Cleaning up scene services...");

			// 清理本地服务（只清理纯 C# 服务，MonoBehaviour 由 Unity 管理）
			foreach (var kvp in _localServices)
			{
				// 只 Dispose 纯 C# 对象
				if (kvp.Value is IDisposable disposable && !(kvp.Value is MonoBehaviour))
				{
					disposable.Dispose();
				}
			}

			_localServices.Clear();
		}

		// ========================================================================
		// 服务注册
		// ========================================================================

		/// <summary>
		/// 注册纯C#服务。
		/// </summary>
		/// <remarks>
		/// 遍历_pureCSharpServices列表，注册每个纯C#服务及其所有接口。
		/// </remarks>
		private void RegisterPureCSharpServices()
		{
			if (_pureCSharpServices == null || _pureCSharpServices.Count == 0)
				return;

			ALog.Info($"  Registering {_pureCSharpServices.Count} pure C# service(s)...");

			foreach (IAsakiSceneService service in _pureCSharpServices.Where(s => s != null))
			{
				RegisterServiceWithInterfaces(service.GetType(), service);
			}
		}

		/// <summary>
		/// 注册MonoBehaviour服务。
		/// </summary>
		/// <remarks>
		/// 遍历_behaviourServices列表，验证每个服务是否实现了IAsakiSceneContextService接口，
		/// 然后注册每个有效服务及其所有接口。
		/// 仅注册引用，不进行注入（由Bootstrapper负责）。
		/// </remarks>
		private void RegisterBehaviourServices()
		{
			if (_behaviourServices == null || _behaviourServices.Count == 0)
				return;

			ALog.Info($"  Registering {_behaviourServices.Count} MonoBehaviour service(s)...");

			foreach (MonoBehaviour behaviour in _behaviourServices.Where(b => b != null))
			{
				// 验证接口实现
				if (behaviour is not IAsakiSceneService service)
				{
					ALog.Error($"[SceneContext] {behaviour.GetType().Name} does not implement IAsakiSceneService! Skipped.");
					continue;
				}

				// 只注册，不注入
				// 注入由 AsakiBootstrapper 在场景加载后统一处理
				RegisterServiceWithInterfaces(behaviour.GetType(), service);
			}
		}

		/// <summary>
		/// 注册服务并自动注册所有服务接口。
		/// </summary>
		/// <param name="concreteType">服务的具体类型。</param>
		/// <param name="service">服务实例。</param>
		/// <remarks>
		/// 首先注册服务的具体类型，然后注册所有实现的服务接口（排除基础标记接口）。
		/// 排除的接口包括：IAsakiService、IAsakiSceneContextService和IAsakiGlobalMonoBehaviourService。
		/// </remarks>
		private void RegisterServiceWithInterfaces(Type concreteType, IAsakiService service)
		{
			// 1. 注册具体类型
			RegisterInternal(concreteType, service);

			// 2. 注册所有服务接口（排除基础标记接口）
			foreach (Type interfaceType in concreteType.GetInterfaces())
			{
				if (typeof(IAsakiService).IsAssignableFrom(interfaceType) &&
				    interfaceType != typeof(IAsakiService) &&
				    interfaceType != typeof(IAsakiSceneService) &&
				    interfaceType != typeof(IAsakiGlobalService))
				{
					RegisterInternal(interfaceType, service);
				}
			}
		}

		/// <summary>
		/// 公共注册接口，允许运行时动态注册服务。
		/// </summary>
		/// <typeparam name="T">服务类型，必须是实现了IAsakiService接口的类类型。</typeparam>
		/// <param name="service">要注册的服务实例。</param>
		/// <remarks>
		/// 此方法仅注册服务的具体类型，不会自动注册其接口。
		/// 如果需要注册接口，请使用RegisterServiceWithInterfaces方法。
		/// </remarks>
		public void Register<T>(T service) where T : class, IAsakiService
		{
			RegisterInternal(typeof(T), service);
		}

		/// <summary>
		/// 内部注册实现，将服务添加到本地服务字典。
		/// </summary>
		/// <param name="type">服务类型。</param>
		/// <param name="service">服务实例。</param>
		/// <remarks>
		/// 如果服务类型已存在于字典中，会覆盖现有服务并记录警告。
		/// </remarks>
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
		/// 解析指定类型的服务实例。
		/// </summary>
		/// <typeparam name="T">服务类型，必须是实现了IAsakiService接口的类类型。</typeparam>
		/// <returns>请求的服务实例。</returns>
		/// <exception cref="KeyNotFoundException">当指定类型的服务未找到时抛出。</exception>
		/// <remarks>
		/// 首先在本地服务字典中查找服务，如果找到则返回。
		/// 如果本地未找到，降级到全局服务容器<see cref="AsakiContext"/>查找。
		/// </remarks>
		public T Get<T>() where T : class, IAsakiService
		{
			// 1. 优先查找本地场景服务
			if (_localServices.TryGetValue(typeof(T), out IAsakiService service))
				return (T)service;

			// 2. 降级到全局服务
			return AsakiContext.Get<T>();
		}

		/// <summary>
		/// 尝试解析指定类型的服务实例。
		/// </summary>
		/// <typeparam name="T">服务类型，必须是实现了IAsakiService接口的类类型。</typeparam>
		/// <param name="service">如果找到服务，将返回的服务实例赋值给此参数；否则为null。</param>
		/// <returns>如果找到服务则返回true，否则返回false。</returns>
		/// <remarks>
		/// 首先在本地服务字典中查找服务，如果找到则返回true。
		/// 如果本地未找到，降级到全局服务容器<see cref="AsakiContext"/>查找。
		/// </remarks>
		public bool TryGet<T>(out T service) where T : class, IAsakiService
		{
			// 1. 优先查找本地场景服务
			if (_localServices.TryGetValue(typeof(T), out IAsakiService s))
			{
				service = (T)s;
				return true;
			}

			// 2. 降级到全局服务
			return AsakiContext.TryGet(out service);
		}
	}
}
