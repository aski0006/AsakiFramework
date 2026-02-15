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
            public int SceneHandle;

            public void Reset()
            {
                Component = null;
                State = InitState.Destroyed;
                SceneHandle = 0;
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

        private ComponentState RentState(AsakiMono component, int sceneHandle)
        {
            ComponentState state = _statePool.Count > 0 ? _statePool.Pop() : new ComponentState();
            state.Component = component;
            state.SceneHandle = sceneHandle;
            state.State = InitState.Pending;
            return state;
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

            int sceneHandle = component.gameObject.scene.handle;

            lock (_lock)
            {
                // 状态池获取
                var state = RentState(component, sceneHandle);
                int trackingId = _nextTrackingId++;
                _trackedComponents[trackingId] = state;

                if (_isFrameworkReady)
                {
                    _pendingInjection.Enqueue(state);
                    // 只有在框架Ready时才触发立即处理，避免在Awake中进行过重的操作
                    // 实际处理放在 ProcessPendingComponents 或下一次 Update 钩子中
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
                if (component is IAsakiAutoInject)
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
            // 场景加载后，再次检查队列（处理那些在 Loading 期间创建的对象）
            if (_isFrameworkReady)
                ProcessQueue();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            lock (_lock)
            {
                var toRemove = new List<int>();
                foreach (var kvp in _trackedComponents)
                {
                    if (kvp.Value.SceneHandle == scene.handle)
                        toRemove.Add(kvp.Key);
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
            // 简单查找，可进一步优化缓存
            var context = component.GetComponentInParent<AsakiSceneContext>();
            if (context)
                return context;
            return AsakiGlobalResolver.Instance;
        }
    }
}
