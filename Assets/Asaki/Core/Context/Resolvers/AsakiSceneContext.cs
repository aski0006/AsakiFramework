using System;
using System.Collections.Generic;
using System.Linq;
using Asaki.Core.Attributes;
using Asaki.Core.Logging;
using UnityEngine;

namespace Asaki.Core.Context.Resolvers
{
    /// <summary>
    /// Asaki场景上下文组件，用于管理场景级别的服务和依赖注入。
    /// </summary>
    /// <remarks>
    /// [v2.0 修复] 采用两阶段初始化：
    /// 1. Awake: 仅注册服务到字典，不调用 Init。
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

        [Header("MonoBehaviour Services")]
        [Tooltip("MonoBehaviour 场景服务（通过 Unity 原生引用）")]
        [SerializeField]
        private List<MonoBehaviour> _behaviourServices = new List<MonoBehaviour>();

        // ========================================================================
        // 运行时数据
        // ========================================================================

        private readonly Dictionary<Type, IAsakiService> _localServices =
            new Dictionary<Type, IAsakiService>();

        // [新增] 缓存待初始化的服务列表
        private readonly List<IAsakiInit> _pendingInitServices = new List<IAsakiInit>();
        private bool _isBuilt = false;

        public bool IsBuilt => _isBuilt;

#if UNITY_EDITOR
        public Dictionary<Type, IAsakiService> GetRuntimeServices()
        {
            return _localServices;
        }
#endif

        // ========================================================================
        // 生命周期
        // ========================================================================

        private void Awake()
        {
            ALog.Info(
                $"[AsakiSceneContext] Registering services in scene: {gameObject.scene.name}"
            );

            // 1. 注册纯 C# 服务 (仅注册，暂不 Init)
            RegisterPureCSharpServices();

            // 2. 注册 MonoBehaviour 服务 (仅注册，暂不 Init)
            RegisterBehaviourServices();

            ALog.Info(
                $"[AsakiSceneContext] Registered {_localServices.Count} services. Waiting for Build()..."
            );
        }

        /// <summary>
        /// [新增] 构建上下文。
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
                    // 此时传入 this (AsakiSceneContext)，服务可以通过它获取全局服务
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

            foreach (var kvp in _localServices)
            {
                if (kvp.Value is IDisposable disposable && !(kvp.Value is MonoBehaviour))
                {
                    disposable.Dispose();
                }
            }

            _localServices.Clear();
            _pendingInitServices.Clear();
            _isBuilt = false;
        }

        // ========================================================================
        // 服务注册
        // ========================================================================

        private void RegisterPureCSharpServices()
        {
            if (_pureCSharpServices == null || _pureCSharpServices.Count == 0)
                return;

            foreach (IAsakiSceneService service in _pureCSharpServices.Where(s => s != null))
            {
                RegisterServiceWithInterfaces(service.GetType(), service);
            }
        }

        private void RegisterBehaviourServices()
        {
            if (_behaviourServices == null || _behaviourServices.Count == 0)
                return;

            foreach (MonoBehaviour behaviour in _behaviourServices.Where(b => b != null))
            {
                if (behaviour is not IAsakiSceneService service)
                {
                    ALog.Error(
                        $"[SceneContext] {behaviour.GetType().Name} does not implement IAsakiSceneService! Skipped."
                    );
                    continue;
                }
                RegisterServiceWithInterfaces(behaviour.GetType(), service);
            }
        }

        private void RegisterServiceWithInterfaces(Type concreteType, IAsakiService service)
        {
            // 1. 注册具体类型
            RegisterInternal(concreteType, service);

            // 2. 注册接口
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

            // 3. [关键修改] 不立即 Init，而是加入待处理列表
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
