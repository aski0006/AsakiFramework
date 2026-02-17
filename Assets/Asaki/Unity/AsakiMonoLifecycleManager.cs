using System;
using System.Collections.Generic;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.Context.Resolvers;
using Asaki.Core.Logging;
using Asaki.Generated;
using Asaki.Unity.Bootstrapper;
using UnityEngine.SceneManagement;

namespace Asaki.Unity
{
    public sealed class AsakiMonoLifecycleManager
        : IAsakiHandler<OnAsakiFrameworkReadyEvent>,
            IDisposable
    {
        private static readonly Lazy<AsakiMonoLifecycleManager> _instance = new(() =>
            new AsakiMonoLifecycleManager()
        );
        public static AsakiMonoLifecycleManager Instance => _instance.Value;

        private enum InitState
        {
            Pending,
            Injected,
            Activated,
            Destroyed,
        }

        private class ComponentState
        {
            public AsakiMono Component;
            public InitState State;
            public bool IsPersistent;
            public bool IsGlobalService; // 标记是否为 IAsakiGlobalService

            public void Reset()
            {
                Component = null;
                State = InitState.Destroyed;
                IsPersistent = false;
                IsGlobalService = false;
            }
        }

        // 简单的对象池
        private readonly Stack<ComponentState> _statePool = new Stack<ComponentState>(128);
        private readonly Dictionary<int, ComponentState> _trackedComponents = new Dictionary<
            int,
            ComponentState
        >(512);
        private readonly Queue<ComponentState> _pendingInjection = new Queue<ComponentState>(64);

        private readonly object _lock = new object();
        private int _nextTrackingId = 1;
        private bool _isFrameworkReady;
        private bool _isDisposed;

        private AsakiMonoLifecycleManager()
        {
            this.AsakiRegister();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private ComponentState RentState(AsakiMono component)
        {
            ComponentState state = _statePool.Count > 0 ? _statePool.Pop() : new ComponentState();
            state.Component = component;
            state.State = InitState.Pending;
            state.IsPersistent = IsDontDestroyOnLoadScene(component.gameObject.scene);
            // 标记是否为 IAsakiGlobalService（由 Bootstrapper 统一管理注入）
            state.IsGlobalService = component is IAsakiGlobalService;
            return state;
        }

        private static bool IsDontDestroyOnLoadScene(Scene scene)
        {
            return scene.name == "DontDestroyOnLoad";
        }

        private void ReturnState(ComponentState state)
        {
            state.Reset();
            _statePool.Push(state);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            this.AsakiUnregister();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            lock (_lock)
            {
                _trackedComponents.Clear();
                _pendingInjection.Clear();
                _statePool.Clear();
            }
        }

        public int RegisterComponent(AsakiMono component)
        {
            if (_isDisposed || component == null)
                return -1;

            lock (_lock)
            {
                var state = RentState(component);
                int trackingId = _nextTrackingId++;
                _trackedComponents[trackingId] = state;

                if (_isFrameworkReady)
                {
                    _pendingInjection.Enqueue(state);
                }
                return trackingId;
            }
        }

        public void UnregisterComponent(int trackingId)
        {
            lock (_lock)
            {
                if (_trackedComponents.Remove(trackingId, out var state))
                {
                    ReturnState(state);
                }
            }
        }

        // 提供给 AsakiMono.Awake 调用的快速通道
        public void ProcessComponentImmediately(AsakiMono component)
        {
            if (!_isFrameworkReady || component == null)
                return;

            try
            {
                // 检查是否为 IAsakiGlobalService - 这些服务由 Bootstrapper 统一注入
                if (component is IAsakiGlobalService globalService)
                {
                    ALog.Info(
                        $"[Lifecycle] Skipping injection for {component.GetType().Name} - already injected by Bootstrapper as IAsakiGlobalService"
                    );
                }
                else if (component is IAsakiAutoInject)
                {
                    // 获取 Resolver (优化：缓存 SceneContext 查找)
                    var resolver = GetResolverForComponent(component);
                    AsakiGlobalInjector.Inject(component, resolver);
                }
                component.ActivateFrameworkReady();
            }
            catch (Exception ex)
            {
                ALog.Error($"[Lifecycle] Failed to process {component.name}: {ex}");
            }
        }

        public void OnEvent(in OnAsakiFrameworkReadyEvent e)
        {
            _isFrameworkReady = true;
            lock (_lock)
            {
                foreach (var kvp in _trackedComponents)
                {
                    if (kvp.Value.State == InitState.Pending)
                        _pendingInjection.Enqueue(kvp.Value);
                }
            }
            ProcessQueue();
        }

        private void ProcessQueue()
        {
            while (_pendingInjection.Count > 0)
            {
                var state = _pendingInjection.Dequeue();
                if (state.Component == null)
                    continue; // 已销毁

                ProcessComponentImmediately(state.Component);
                state.State = InitState.Activated;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_isFrameworkReady)
                return;

            ProcessQueue();
            ReinjectGlobalServices(scene);
        }

        private void ReinjectGlobalServices(Scene newScene)
        {
            IAsakiResolver newResolver = null;
            var rootObjects = newScene.GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                var ctx = root.GetComponentInChildren<AsakiSceneContext>(true);
                if (ctx != null)
                {
                    ctx.Build();
                    newResolver = ctx;
                    break;
                }
            }

            if (newResolver == null)
                newResolver = AsakiGlobalResolver.Instance;

            lock (_lock)
            {
                foreach (var kvp in _trackedComponents)
                {
                    var state = kvp.Value;
                    // 跳过 IAsakiGlobalService - 这些服务由 Bootstrapper 统一管理，不需要场景切换时重新注入
                    if (
                        state.IsPersistent
                        && state.Component != null
                        && state.Component is IAsakiAutoInject
                        && !state.IsGlobalService
                    )
                    {
                        try
                        {
                            AsakiGlobalInjector.Inject(state.Component, newResolver);
                            ALog.Info(
                                $"[Lifecycle] Re-injected persistent component: {state.Component.GetType().Name}"
                            );
                        }
                        catch (Exception ex)
                        {
                            ALog.Error(
                                $"[Lifecycle] Failed to re-inject {state.Component.name}: {ex}"
                            );
                        }
                    }
                }
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (IsDontDestroyOnLoadScene(scene))
                return;

            lock (_lock)
            {
                var toRemove = new List<int>();
                foreach (var kvp in _trackedComponents)
                {
                    if (!kvp.Value.IsPersistent && kvp.Value.Component != null)
                    {
                        if (kvp.Value.Component.gameObject.scene == scene)
                            toRemove.Add(kvp.Key);
                    }
                }

                foreach (var id in toRemove)
                {
                    var state = _trackedComponents[id];
                    _trackedComponents.Remove(id);
                    ReturnState(state);
                }
            }
        }

        private IAsakiResolver GetResolverForComponent(AsakiMono component)
        {
            var context = component.GetComponentInParent<AsakiSceneContext>();
            if (context == null)
            {
                var rootObjects = component.gameObject.scene.GetRootGameObjects();
                foreach (var root in rootObjects)
                {
                    context = root.GetComponentInChildren<AsakiSceneContext>(true);
                    if (context != null)
                        break;
                }
            }

            if (context != null)
            {
                context.Build();
                return context;
            }
            return AsakiGlobalResolver.Instance;
        }
    }
}
