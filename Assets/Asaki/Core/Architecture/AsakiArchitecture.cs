using System;
using System.Collections.Generic;
using Asaki.Core.Context;
using Asaki.Core.Context.Resolvers;
using Asaki.Core.Logging;
using Asaki.Core.Simulation;

namespace Asaki.Core.Architecture
{
    public abstract partial class AsakiArchitecture : IAsakiArchitecture, IAsakiInit
    {
        private readonly Dictionary<Type, IAsakiModel> _models =
            new Dictionary<Type, IAsakiModel>();
        private readonly Dictionary<Type, IAsakiSystem> _systems =
            new Dictionary<Type, IAsakiSystem>();
        private IAsakiSimulationService _simulationService;
        protected IAsakiResolver Resolver { get; private set; }
        private bool _isInited = false;

        public void Init(IAsakiResolver resolver)
        {
            if (_isInited)
                return;
            Resolver = resolver ?? AsakiGlobalResolver.Instance;
            Resolver.TryGet(out _simulationService);
            OnSetup();
            foreach (IAsakiModel model in _models.Values)
            {
                model.Create();
            }

            foreach (IAsakiSystem system in _systems.Values)
            {
                system.Setup();
                BindSimulation(system);
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
                return;
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
                return;
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
            ALog.Error(
                $"[AsakiArchitecture] Failed to get System: {typeof(T).Name}. Make sure it is registered in OnSetup()."
            );
            return null;
        }

        public T GetModel<T>()
            where T : class, IAsakiModel
        {
            if (_models.TryGetValue(typeof(T), out IAsakiModel model))
            {
                return (T)model;
            }

            ALog.Error(
                $"[AsakiArchitecture] Failed to get Model: {typeof(T).Name}. Make sure it is registered in OnSetup()."
            );
            return null;
        }

        public void Dispose()
        {
            if (!_isInited)
                return;
            foreach (IAsakiSystem system in _systems.Values)
            {
                UnbindSimulation(system); // 停止心跳
                system.Dispose(); // 释放资源
            }
            _systems.Clear();
            foreach (IAsakiModel model in _models.Values)
            {
                model.Dispose();
            }
            _models.Clear();
            Resolver = null;
            _simulationService = null;
            _isInited = false;
            ALog.Info($"[AsakiArchitecture] Disposed {GetType().Name}");
        }

        private void BindSimulation(IAsakiSystem system)
        {
            if (_simulationService == null)
                return;
            switch (system)
            {
                case IAsakiTickable tickable:
                    _simulationService.Register(tickable);
                    break;
                case IAsakiLateTickable lateTickable:
                    _simulationService.Register(lateTickable);
                    break;
                case IAsakiFixedTickable fixedTickable:
                    _simulationService.Register(fixedTickable);
                    break;
            }
        }

        private void UnbindSimulation(IAsakiSystem system)
        {
            if (_simulationService == null)
                return;

            switch (system)
            {
                case IAsakiTickable tickable:
                    _simulationService.Unregister(tickable);
                    break;
                case IAsakiFixedTickable fixedTickable:
                    _simulationService.Unregister(fixedTickable);
                    break;
                case IAsakiLateTickable lateTickable:
                    _simulationService.Unregister(lateTickable);
                    break;
            }
        }
    }
}
