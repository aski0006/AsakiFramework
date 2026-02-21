using System;
using System.Collections.Generic;
using Asaki.Core.Context;
using Asaki.Core.Context.Resolvers;
using Asaki.Core.Logging;
using Asaki.Core.Simulation;

namespace Asaki.Core.Architecture
{
    public abstract partial class AsakiArchitecture : IAsakiArchitecture, IAsakiInject, IDisposable
    {
        private readonly Dictionary<Type, IAsakiModel> _models =
            new Dictionary<Type, IAsakiModel>();
        private readonly Dictionary<Type, IAsakiSystem> _systems =
            new Dictionary<Type, IAsakiSystem>();
        private IAsakiSimulationService _simulationService;
        private ArchitectureRegister _architectureRegister;
        protected IAsakiResolver Resolver { get; private set; }
        private bool _isInited;

        public void Inject(IAsakiResolver resolver)
        {
            if (_isInited)
            {
                ALog.Warn(
                    $"[AsakiArchitecture] {GetType().Name}.Inject() was called multiple times. Ignoring subsequent calls."
                );
                return;
            }

            Resolver = resolver ?? AsakiGlobalResolver.Instance;
            Resolver.TryGet(out _simulationService);
            Resolver.TryGet(out _architectureRegister);
            OnSetup();

            // Phase 1: 创建所有 Model
            foreach (IAsakiModel model in _models.Values)
            {
                AsakiGlobalInjector.Inject(model, Resolver);
                model.Create();
            }

            // Phase 2: 创建所有 System（但不绑定 Simulation）
            foreach (IAsakiSystem system in _systems.Values)
            {
                AsakiGlobalInjector.Inject(system, Resolver);
                if (system is AsakiSystemBase sysBase)
                    sysBase.Create(this);
                else
                    system.Create();
            }

            // Phase 3: 调用所有 System 的 Start（此时所有 System 已就绪）
            foreach (IAsakiSystem system in _systems.Values)
            {
                system.Start();
                BindSimulation(system); // Start 之后再绑定 Simulation
            }

            if (_architectureRegister == null)
            {
                ALog.Error(
                    $"[AsakiArchitecture] {GetType().Name} is not initialized. Register it in AsakiArchitectureModule."
                );
            }
            else
            {
                _architectureRegister.RegisterArchitecture((IAsakiArchitecture)this);
            }

            _isInited = true;
            ALog.Info(
                $"[AsakiArchitecture] {GetType().Name} initialized. (M:{_models.Count}, S:{_systems.Count})"
            );
        }

        protected abstract void OnSetup();

        protected void RegisterModel<T>(T model)
            where T : class, IAsakiModel
        {
            if (model == null)
            {
                return;
            }

            Type type = typeof(T);
            if (!_models.TryAdd(type, model))
            {
                ALog.Warn(
                    $"[AsakiArchitecture] Model {type.Name} is already registered in {GetType().Name}."
                );
            }
        }

        protected void RegisterSystem<T>(T system)
            where T : class, IAsakiSystem
        {
            if (system == null)
            {
                return;
            }

            Type type = typeof(T);
            if (!_systems.TryAdd(type, system))
            {
                ALog.Warn(
                    $"[AsakiArchitecture] System {type.Name} is already registered in {GetType().Name}."
                );
            }
        }

        public T GetSystem<T>()
            where T : class, IAsakiSystem
        {
            if (_systems.TryGetValue(typeof(T), out IAsakiSystem system))
            {
                return (T)system;
            }
            throw new KeyNotFoundException(
                $"[AsakiArchitecture] System not registered: {typeof(T).Name}. Register it in OnSetup()."
            );
        }

        public T GetModel<T>()
            where T : class, IAsakiModel
        {
            if (_models.TryGetValue(typeof(T), out IAsakiModel model))
            {
                return (T)model;
            }
            throw new KeyNotFoundException(
                $"[AsakiArchitecture] Model not registered: {typeof(T).Name}. Register it in OnSetup()."
            );
        }

        /// <summary>
        /// 获取实体世界（便捷方法）
        /// </summary>
        Entities.IEntityWorld IAsakiArchitecture.GetEntityWorld()
        {
            return GetModel<EntityModel>().World;
        }

        /// <summary>
        /// 获取实体世界（内部访问）
        /// </summary>
        protected Entities.IEntityWorld GetEntityWorld()
        {
            return GetModel<EntityModel>().World;
        }

        #region IAsakiServiceProvider Implementation

        public T GetService<T>()
            where T : class, IAsakiService
        {
            // 处理 Model 类型
            if (typeof(T) == typeof(IAsakiModel) || typeof(T).IsSubclassOf(typeof(IAsakiModel)))
            {
                throw new InvalidOperationException(
                    "Use GetModel<T>() to retrieve models, not GetService<T>()"
                );
            }

            // 处理 System 类型
            if (typeof(T) == typeof(IAsakiSystem) || typeof(T).IsSubclassOf(typeof(IAsakiSystem)))
            {
                throw new InvalidOperationException(
                    "Use GetSystem<T>() to retrieve systems, not GetService<T>()"
                );
            }

            // 处理 IAsakiArchitecture 自身
            if (typeof(T) == typeof(IAsakiArchitecture))
            {
                return (T)(object)this;
            }

            // 尝试从 Resolver 获取服务
            if (Resolver != null && Resolver.TryGet<T>(out T service))
            {
                return service;
            }

            throw new KeyNotFoundException($"Service not found: {typeof(T).Name}");
        }

        public bool TryGetService<T>(out T service)
            where T : class, IAsakiService
        {
            try
            {
                service = GetService<T>();
                return true;
            }
            catch
            {
                service = null;
                return false;
            }
        }

        #endregion

        public void Dispose()
        {
            if (!_isInited)
            {
                return;
            }

            // 分别处理每个 System，避免一个失败影响其他
            foreach (IAsakiSystem system in _systems.Values)
            {
                try
                {
                    UnbindSimulation(system); // 停止心跳
                    system.Dispose(); // 释放资源
                }
                catch (System.Exception ex)
                {
                    ALog.Error(
                        $"[AsakiArchitecture] Error disposing system {system.GetType().Name}: {ex}"
                    );
                }
            }
            _systems.Clear();

            // 分别处理每个 Model，避免一个失败影响其他
            foreach (IAsakiModel model in _models.Values)
            {
                try
                {
                    model.Dispose();
                }
                catch (Exception ex)
                {
                    ALog.Error(
                        $"[AsakiArchitecture] Error disposing model {model.GetType().Name}: {ex}"
                    );
                }
            }
            _models.Clear();

            Resolver = null;
            _simulationService = null;
            _isInited = false;
            if (_architectureRegister != null)
            {
                _architectureRegister.RemoveArchitecture(this);
            }
            ALog.Info($"[AsakiArchitecture] Disposed {GetType().Name}");
        }

        private void BindSimulation(IAsakiSystem system)
        {
            if (_simulationService == null)
            {
                return;
            }

            if (system is IAsakiTickable tickable)
            {
                _simulationService.Register(tickable);
            }

            if (system is IAsakiLateTickable lateTickable)
            {
                _simulationService.Register(lateTickable);
            }

            if (system is IAsakiFixedTickable fixedTickable)
            {
                _simulationService.Register(fixedTickable);
            }
        }

        private void UnbindSimulation(IAsakiSystem system)
        {
            if (_simulationService == null)
            {
                return;
            }

            if (system is IAsakiTickable tickable)
            {
                _simulationService.Unregister(tickable);
            }

            if (system is IAsakiFixedTickable fixedTickable)
            {
                _simulationService.Unregister(fixedTickable);
            }

            if (system is IAsakiLateTickable lateTickable)
            {
                _simulationService.Unregister(lateTickable);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器专用：获取所有已注册的Model
        /// </summary>
        public IReadOnlyDictionary<Type, IAsakiModel> GetModelsForEditor()
        {
            return _models;
        }

        /// <summary>
        /// 编辑器专用：获取所有已注册的System
        /// </summary>
        public IReadOnlyDictionary<Type, IAsakiSystem> GetSystemsForEditor()
        {
            return _systems;
        }
#endif
    }
}
