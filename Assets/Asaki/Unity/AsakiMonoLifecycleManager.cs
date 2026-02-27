using System;
using System.Collections.Generic;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.Context.Resolvers;
using Asaki.Core.Logging;
using Asaki.Generated;
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
            Pending, // 等待注入
            Injected, // 注入成功
            Activated, // 激活成功
            InjectionFailed, // 注入失败
            Destroyed, // 已销毁
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

        /// <summary>
        /// 立即处理组件的注入和激活流程。
        /// <para>提供给 AsakiMono.Awake 调用的快速通道，在框架就绪时立即执行注入。</para>
        /// <para>状态机转换：Pending -> Injected -> Activated（成功）或 Pending -> InjectionFailed（失败）</para>
        /// </summary>
        /// <param name="component">要处理的 AsakiMono 组件</param>
        /// <returns>注入是否成功。true 表示注入成功且组件已激活；false 表示注入失败或参数无效。</returns>
        public bool ProcessComponentImmediately(AsakiMono component)
        {
            if (!_isFrameworkReady || component == null)
                return false;

            bool injectionSuccess = false;

            try
            {
                // 检查是否为 IAsakiGlobalService - 这些服务由 Bootstrapper 统一注入
                if (component is IAsakiGlobalService globalService)
                {
                    ALog.Info(
                        $"[Lifecycle] Skipping injection for {component.GetType().Name} - already injected by Bootstrapper as IAsakiGlobalService"
                    );
                    injectionSuccess = true;
                }
                else if (component is IAsakiAutoInject)
                {
                    // 获取 Resolver (优化：缓存 SceneContext 查找)
                    var resolver = GetResolverForComponent(component);
                    AsakiGlobalInjector.Inject(component, resolver);
                    injectionSuccess = true;
                }
                else
                {
                    // 既不是 IAsakiGlobalService 也不是 IAsakiAutoInject，视为成功
                    injectionSuccess = true;
                }

                // 只有注入成功才激活组件
                if (injectionSuccess)
                {
                    component.ActivateFrameworkReady();
                }
            }
            catch (Exception ex)
            {
                ALog.Error($"[Lifecycle] Failed to process {component.name}: {ex}");
                injectionSuccess = false;
            }

            return injectionSuccess;
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

        /// <summary>
        /// 处理待注入队列中的所有组件。
        /// <para>根据注入结果更新组件状态：成功则 Activated，失败则 InjectionFailed。</para>
        /// </summary>
        private void ProcessQueue()
        {
            while (_pendingInjection.Count > 0)
            {
                var state = _pendingInjection.Dequeue();
                if (state.Component == null)
                    continue; // 已销毁

                bool success = ProcessComponentImmediately(state.Component);
                state.State = success ? InitState.Activated : InitState.InjectionFailed;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_isFrameworkReady)
                return;

            ProcessQueue();
            ReinjectGlobalServices(scene);
        }

        /// <summary>
        /// 重新注入持久化组件的全局服务依赖。
        /// <para>当新场景加载时，持久化组件需要获取新场景的 Resolver 来注入场景特定的服务。</para>
        /// <para>状态机转换：已激活的组件重新注入后会更新为 Injected 状态，激活后恢复为 Activated。</para>
        /// </summary>
        /// <param name="newScene">新加载的场景</param>
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
                            // 记录注入前的状态用于日志
                            var previousState = state.State;

                            AsakiGlobalInjector.Inject(state.Component, newResolver);

                            // 更新状态机：注入成功后状态更新为 Injected
                            state.State = InitState.Injected;
                            ALog.Info(
                                $"[Lifecycle] Re-injected persistent component: {state.Component.GetType().Name}, state: {previousState} -> Injected"
                            );

                            if (!state.Component.IsActivated)
                            {
                                state.Component.ActivateFrameworkReady();
                                // 激活成功后状态更新为 Activated
                                state.State = InitState.Activated;
                                ALog.Info(
                                    $"[Lifecycle] Activated persistent component: {state.Component.GetType().Name}, state: Injected -> Activated"
                                );
                            }
                        }
                        catch (Exception ex)
                        {
                            ALog.Error(
                                $"[Lifecycle] Failed to re-inject {state.Component.name}: {ex}"
                            );
                            // 注入失败时更新状态为 InjectionFailed，允许后续重试
                            state.State = InitState.InjectionFailed;
                            ALog.Warn(
                                $"[Lifecycle] Persistent component injection failed: {state.Component.GetType().Name}, state: -> InjectionFailed"
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
                if (!context.IsInitializingServices)
                {
                    context.Build();
                }
                return context;
            }
            return AsakiGlobalResolver.Instance;
        }
    }
}
