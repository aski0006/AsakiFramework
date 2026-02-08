using System;
using System.Collections.Generic;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.Context.Resolvers;
using Asaki.Core.Logging;
using Asaki.Generated;
using Asaki.Unity.Bootstrapper;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Asaki.Unity
{
    /// <summary>
    /// AsakiMono 生命周期管理器
    /// <para>负责管理所有 AsakiMono 组件的初始化、激活和清理。</para>
    /// <para>解决动态附加组件和场景切换时的初始化问题。</para>
    /// </summary>
    /// <remarks>
    /// <para>核心职责：</para>
    /// <list type="bullet">
    /// <item><description>跟踪所有活跃的 AsakiMono 实例</description></item>
    /// <item><description>在框架就绪后自动激活等待中的组件</description></item>
    /// <item><description>为动态附加的组件提供延迟初始化</description></item>
    /// <item><description>处理场景切换时的组件生命周期</description></item>
    /// </list>
    /// </remarks>
    public sealed class AsakiMonoLifecycleManager : IAsakiHandler<OnAsakiFrameworkReadyEvent>, IDisposable
    {
        // ===================================================================
        // 单例模式
        // ===================================================================

        private static readonly Lazy<AsakiMonoLifecycleManager> _instance = new(() => new AsakiMonoLifecycleManager());
        public static AsakiMonoLifecycleManager Instance => _instance.Value;

        // ===================================================================
        // 状态跟踪
        // ===================================================================

        /// <summary>
        /// 组件初始化状态
        /// </summary>
        private enum InitState
        {
            /// <summary>刚创建，等待初始化</summary>
            Pending,

            /// <summary>依赖注入完成</summary>
            Injected,

            /// <summary>完全激活（OnStart已调用）</summary>
            Activated,

            /// <summary>已销毁</summary>
            Destroyed
        }

        /// <summary>
        /// 组件状态记录
        /// </summary>
        private class ComponentState
        {
            public AsakiMono Component;
            public InitState State;
            public int SceneHandle;
            public DateTime CreatedAt;
            public DateTime? ActivatedAt;

            public ComponentState(AsakiMono component, int sceneHandle)
            {
                Component = component;
                State = InitState.Pending;
                SceneHandle = sceneHandle;
                CreatedAt = DateTime.UtcNow;
            }
        }

        // 所有跟踪的组件 (使用 ConditionalWeakTable 避免内存泄漏)
        private readonly Dictionary<int, ComponentState> _trackedComponents = new();
        private readonly object _lock = new object();
        private int _nextTrackingId = 1;

        // 等待初始化的组件队列
        private readonly Queue<ComponentState> _pendingInjection = new();
        private readonly Queue<ComponentState> _pendingActivation = new();

        // 管理器状态
        private bool _isFrameworkReady;
        private bool _isDisposed;

        // ===================================================================
        // 初始化与销毁
        // ===================================================================

        private AsakiMonoLifecycleManager()
        {
            // 订阅框架就绪事件
            this.AsakiRegister();

            // 订阅场景加载事件
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            ALog.Info("[AsakiMonoLifecycleManager] Initialized.");
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
                _pendingActivation.Clear();
            }

            ALog.Info("[AsakiMonoLifecycleManager] Disposed.");
        }

        // ===================================================================
        // 核心 API
        // ===================================================================

        /// <summary>
        /// 注册 AsakiMono 组件以进行生命周期管理
        /// </summary>
        /// <param name="component">要注册的组件</param>
        /// <returns>跟踪ID，用于后续操作</returns>
        public int RegisterComponent(AsakiMono component)
        {
            if (_isDisposed)
            {
                ALog.Error("[AsakiMonoLifecycleManager] Cannot register component - manager is disposed.");
                return -1;
            }

            if (component == null)
                return -1;

            int sceneHandle = component.gameObject.scene.handle;
            var state = new ComponentState(component, sceneHandle);

            lock (_lock)
            {
                int trackingId = _nextTrackingId++;
                _trackedComponents[trackingId] = state;

                // 如果框架已就绪，立即处理
                if (_isFrameworkReady)
                {
                    _pendingInjection.Enqueue(state);
                }

                return trackingId;
            }
        }

        /// <summary>
        /// 注销组件
        /// </summary>
        public void UnregisterComponent(int trackingId)
        {
            lock (_lock)
            {
                if (_trackedComponents.TryGetValue(trackingId, out var state))
                {
                    state.State = InitState.Destroyed;
                    _trackedComponents.Remove(trackingId);
                }
            }
        }

        /// <summary>
        /// 立即处理指定组件的初始化和激活
        /// </summary>
        public void ProcessComponentImmediately(AsakiMono component)
        {
            if (!_isFrameworkReady || component == null)
                return;

            try
            {
                // 执行依赖注入
                if (component is IAsakiAutoInject)
                {
                    var resolver = GetResolverForComponent(component);
                    AsakiGlobalInjector.Inject(component, resolver);
                }

                // 激活组件
                component.ActivateFrameworkReady();
            }
            catch (Exception ex)
            {
                ALog.Error($"[AsakiMonoLifecycleManager] Failed to process component {component.GetType().Name}: {ex}");
            }
        }

        /// <summary>
        /// 批量处理等待中的组件
        /// </summary>
        /// <param name="maxPerFrame">每帧最大处理数量，0表示无限制</param>
        public void ProcessPendingComponents(int maxPerFrame = 0)
        {
            if (!_isFrameworkReady)
                return;

            ProcessPendingInjections(maxPerFrame);
            ProcessPendingActivations(maxPerFrame);
        }

        // ===================================================================
        // 框架就绪处理
        // ===================================================================

        public void OnEvent(OnAsakiFrameworkReadyEvent e)
        {
            ALog.Info("[AsakiMonoLifecycleManager] Framework ready - processing pending components...");

            _isFrameworkReady = true;

            // 将所有等待中的组件加入处理队列
            lock (_lock)
            {
                foreach (var kvp in _trackedComponents)
                {
                    var state = kvp.Value;
                    if (state.State == InitState.Pending)
                    {
                        _pendingInjection.Enqueue(state);
                    }
                }
            }

            // 立即处理所有等待中的组件
            ProcessPendingComponents(0);

            ALog.Info("[AsakiMonoLifecycleManager] All pending components processed.");
        }

        // ===================================================================
        // 场景事件处理
        // ===================================================================

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ALog.Info($"[AsakiMonoLifecycleManager] Scene loaded: {scene.name} ({mode})");

            if (!_isFrameworkReady)
                return;

            // 查找新场景中的所有 AsakiMono 组件
            var rootObjects = scene.GetRootGameObjects();
            int count = 0;

            foreach (var rootObj in rootObjects)
            {
                var components = rootObj.GetComponentsInChildren<AsakiMono>(true);
                foreach (var component in components)
                {
                    // 检查是否已在跟踪中
                    bool alreadyTracked = false;
                    lock (_lock)
                    {
                        foreach (var kvp in _trackedComponents)
                        {
                            if (kvp.Value.Component == component)
                            {
                                alreadyTracked = true;
                                break;
                            }
                        }
                    }

                    if (!alreadyTracked)
                    {
                        int id = RegisterComponent(component);
                        if (id > 0)
                        {
                            // 立即处理
                            ProcessComponentImmediately(component);
                            count++;
                        }
                    }
                }
            }

            if (count > 0)
            {
                ALog.Info($"[AsakiMonoLifecycleManager] Registered and processed {count} components from new scene.");
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            ALog.Info($"[AsakiMonoLifecycleManager] Scene unloaded: {scene.name}");

            lock (_lock)
            {
                var toRemove = new List<int>();

                foreach (var kvp in _trackedComponents)
                {
                    if (kvp.Value.SceneHandle == scene.handle)
                    {
                        kvp.Value.State = InitState.Destroyed;
                        toRemove.Add(kvp.Key);
                    }
                }

                foreach (var id in toRemove)
                {
                    _trackedComponents.Remove(id);
                }

                if (toRemove.Count > 0)
                {
                    ALog.Info($"[AsakiMonoLifecycleManager] Cleaned up {toRemove.Count} components from unloaded scene.");
                }
            }
        }

        // ===================================================================
        // 内部处理逻辑
        // ===================================================================

        private void ProcessPendingInjections(int maxCount)
        {
            int processed = 0;

            while (_pendingInjection.Count > 0)
            {
                if (maxCount > 0 && processed >= maxCount)
                    break;

                var state = _pendingInjection.Dequeue();

                // 检查组件是否仍然有效
                if (state.Component == null || state.State == InitState.Destroyed)
                    continue;

                try
                {
                    // 执行依赖注入
                    if (state.Component is IAsakiAutoInject)
                    {
                        var resolver = GetResolverForComponent(state.Component);
                        AsakiGlobalInjector.Inject(state.Component, resolver);
                    }

                    state.State = InitState.Injected;
                    _pendingActivation.Enqueue(state);
                    processed++;
                }
                catch (Exception ex)
                {
                    ALog.Error($"[AsakiMonoLifecycleManager] Injection failed for {state.Component.GetType().Name}: {ex}");
                }
            }
        }

        private void ProcessPendingActivations(int maxCount)
        {
            int processed = 0;

            while (_pendingActivation.Count > 0)
            {
                if (maxCount > 0 && processed >= maxCount)
                    break;

                var state = _pendingActivation.Dequeue();

                // 检查组件是否仍然有效
                if (state.Component == null || state.State == InitState.Destroyed)
                    continue;

                try
                {
                    // 激活组件
                    state.Component.ActivateFrameworkReady();
                    state.State = InitState.Activated;
                    state.ActivatedAt = DateTime.UtcNow;
                    processed++;
                }
                catch (Exception ex)
                {
                    ALog.Error($"[AsakiMonoLifecycleManager] Activation failed for {state.Component.GetType().Name}: {ex}");
                }
            }
        }

        private IAsakiResolver GetResolverForComponent(AsakiMono component)
        {
            // 尝试从组件所在场景获取 SceneContext
            var scene = component.gameObject.scene;
            if (scene.IsValid())
            {
                var rootObjects = scene.GetRootGameObjects();
                foreach (var rootObj in rootObjects)
                {
                    var context = rootObj.GetComponentInChildren<AsakiSceneContext>(true);
                    if (context != null)
                    {
                        return context;
                    }
                }
            }

            // 回退到全局解析器
            return AsakiGlobalResolver.Instance;
        }

        // ===================================================================
        // 诊断 API
        // ===================================================================

        /// <summary>
        /// 获取当前跟踪的组件统计信息
        /// </summary>
        public LifecycleStats GetStats()
        {
            lock (_lock)
            {
                var stats = new LifecycleStats
                {
                    TotalTracked = _trackedComponents.Count,
                    PendingInjection = 0,
                    PendingActivation = 0,
                    Activated = 0
                };

                foreach (var kvp in _trackedComponents)
                {
                    switch (kvp.Value.State)
                    {
                        case InitState.Pending:
                            stats.PendingInjection++;
                            break;
                        case InitState.Injected:
                            stats.PendingActivation++;
                            break;
                        case InitState.Activated:
                            stats.Activated++;
                            break;
                    }
                }

                stats.QueueInjection = _pendingInjection.Count;
                stats.QueueActivation = _pendingActivation.Count;

                return stats;
            }
        }

        /// <summary>
        /// 生命周期统计信息
        /// </summary>
        public struct LifecycleStats
        {
            public int TotalTracked;
            public int PendingInjection;
            public int PendingActivation;
            public int Activated;
            public int QueueInjection;
            public int QueueActivation;

            public override string ToString()
            {
                return $"[LifecycleStats] Total: {TotalTracked}, PendingInject: {PendingInjection}, " +
                       $"PendingActivate: {PendingActivation}, Activated: {Activated}, " +
                       $"QueueInject: {QueueInjection}, QueueActivate: {QueueActivation}";
            }
        }
    }
}
