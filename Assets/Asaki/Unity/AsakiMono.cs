using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Asaki.Generated;
using Asaki.Unity.Bootstrapper;
using UnityEngine;

namespace Asaki.Unity
{
    /// <summary>
    /// Asaki 框架 MonoBehaviour 基类，提供统一的生命周期管理、组件缓存和实用工具方法。
    /// </summary>
    /// <remarks>
    /// <para>【全局服务设计规范】</para>
    /// <para>全局服务应该继承 AsakiMono 并同时实现 IAsakiGlobalService 接口。</para>
    /// <para>AsakiMonoLifecycleManager 会自动检测 IAsakiGlobalService 并跳过重复注入。</para>
    /// <para>【生命周期设计规范】</para>
    /// <para>子类应通过 OnXxx 虚方法接入生命周期，而非重写 Unity 原生方法。</para>
    /// </remarks>
    public abstract class AsakiMono : MonoBehaviour, IAsakiHandler<OnAsakiFrameworkReadyEvent>
    {
        #region 字段和属性

        public bool IsActivated { get; private set; }
        public bool IsInitialized => IsActivated;
        public bool IsPendingInitialization => !IsActivated && _lifecycleTrackingId > 0;

        private int _lifecycleTrackingId;
        private bool _isRegisteredWithLifecycleManager;
        private bool _enableComponentCalled;
        private readonly Dictionary<Type, Component> _componentCache = new();

        #endregion

        #region Unity 生命周期

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Awake()
        {
            IsActivated = false;
            OnAwake();
            RegisterWithLifecycleManager();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnEnable()
        {
            this.AsakiRegister();
            if (IsActivated && !_enableComponentCalled)
            {
                _enableComponentCalled = true;
                EnableComponent();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnDisable()
        {
            this.AsakiUnregister();
            _enableComponentCalled = false;
            DisableComponent();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Update()
        {
            if (!IsActivated)
                return;
            OnUpdate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FixedUpdate()
        {
            if (!IsActivated)
                return;
            OnFixedUpdate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LateUpdate()
        {
            if (!IsActivated)
                return;
            OnLateUpdate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnDestroy()
        {
            try
            {
                this.AsakiUnregister();
            }
            catch (Exception ex)
            {
                ALog.Error($"[{GetType().Name}] Error during forced unsubscribe on destroy: {ex}");
            }

            if (_isRegisteredWithLifecycleManager && _lifecycleTrackingId > 0)
            {
                try
                {
                    AsakiMonoLifecycleManager.Instance.UnregisterComponent(_lifecycleTrackingId);
                }
                catch (Exception ex)
                {
                    ALog.Error(
                        $"[{GetType().Name}] Error unregistering from lifecycle manager: {ex}"
                    );
                }
            }
            Cleanup();
        }

        #endregion

        #region 框架生命周期虚方法

        /// <summary>
        /// Awake 阶段调用，用于子类初始化逻辑。
        /// </summary>
        protected virtual void OnAwake() { }

        /// <summary>
        /// 框架就绪后调用，相当于 Start 阶段。
        /// </summary>
        protected virtual void OnStart() { }

        /// <summary>
        /// 组件启用时调用（首次启用或从禁用状态恢复）。
        /// </summary>
        protected virtual void EnableComponent() { }

        /// <summary>
        /// 组件禁用时调用。
        /// </summary>
        protected virtual void DisableComponent() { }

        /// <summary>
        /// Update 阶段调用，每帧执行。
        /// </summary>
        protected virtual void OnUpdate() { }

        /// <summary>
        /// FixedUpdate 阶段调用，固定时间步长执行。
        /// </summary>
        protected virtual void OnFixedUpdate() { }

        /// <summary>
        /// LateUpdate 阶段调用，在所有 Update 之后执行。
        /// </summary>
        protected virtual void OnLateUpdate() { }

        /// <summary>
        /// 组件销毁时调用，用于清理资源。
        /// </summary>
        protected virtual void Cleanup() { }

        /// <summary>
        /// 组件激活时调用。
        /// </summary>
        protected virtual void OnActivated() { }

        /// <summary>
        /// 组件停用时调用。
        /// </summary>
        protected virtual void OnDeactivated() { }

        #endregion

        #region 框架核心方法

        private void RegisterWithLifecycleManager()
        {
            if (_isRegisteredWithLifecycleManager)
                return;

            try
            {
                _lifecycleTrackingId = AsakiMonoLifecycleManager.Instance.RegisterComponent(this);
                _isRegisteredWithLifecycleManager = true;

                if (AsakiBootstrapper.IsReady)
                {
                    AsakiMonoLifecycleManager.Instance.ProcessComponentImmediately(this);
                }
            }
            catch (Exception ex)
            {
                ALog.Error($"[{GetType().Name}] Failed to register with lifecycle manager: {ex}");
            }
        }

        public void OnEvent(in OnAsakiFrameworkReadyEvent e)
        {
            ActivateFrameworkReady();
        }

        /// <summary>
        /// 激活框架就绪状态，触发 OnStart 和 EnableComponent。
        /// </summary>
        public void ActivateFrameworkReady()
        {
            if (IsActivated)
                return;

            try
            {
                IsActivated = true;
                OnStart();

                if (gameObject.activeInHierarchy && !_enableComponentCalled)
                {
                    _enableComponentCalled = true;
                    EnableComponent();
                }
            }
            catch (Exception ex)
            {
                ALog.Error($"[{GetType().Name}] Error during framework activation: {ex}");
                IsActivated = false;
            }
        }

        /// <summary>
        /// 手动激活组件。
        /// </summary>
        protected void Activate()
        {
            if (IsActivated)
                return;
            IsActivated = true;
            OnActivated();
        }

        /// <summary>
        /// 手动停用组件。
        /// </summary>
        protected void Deactivate()
        {
            if (!IsActivated)
                return;
            IsActivated = false;
            OnDeactivated();
        }

        #endregion

        #region 组件缓存方法

        /// <summary>
        /// 获取自身对象上的组件（带缓存）。
        /// </summary>
        /// <typeparam name="T">组件类型，必须继承自 Component</typeparam>
        /// <returns>缓存的组件实例，如果不存在则返回 null</returns>
        protected T GetCachedComponent<T>()
            where T : Component
        {
            var type = typeof(T);
            if (_componentCache.TryGetValue(type, out var cached))
                return cached as T;

            var component = GetComponent<T>();
            if (component != null)
                _componentCache[type] = component;

            return component;
        }

        /// <summary>
        /// 获取子级对象上的组件（带缓存）。
        /// </summary>
        /// <typeparam name="T">组件类型，必须继承自 Component</typeparam>
        /// <returns>缓存的子级组件实例，如果不存在则返回 null</returns>
        protected T GetCachedComponentInChildren<T>()
            where T : Component
        {
            var type = typeof(T);
            if (_componentCache.TryGetValue(type, out var cached))
                return cached as T;

            var component = GetComponentInChildren<T>();
            if (component != null)
                _componentCache[type] = component;

            return component;
        }

        /// <summary>
        /// 获取父级对象上的组件（带缓存）。
        /// </summary>
        /// <typeparam name="T">组件类型，必须继承自 Component</typeparam>
        /// <returns>缓存的父级组件实例，如果不存在则返回 null</returns>
        protected T GetCachedComponentInParent<T>()
            where T : Component
        {
            var type = typeof(T);
            if (_componentCache.TryGetValue(type, out var cached))
                return cached as T;

            var component = GetComponentInParent<T>();
            if (component != null)
                _componentCache[type] = component;

            return component;
        }

        /// <summary>
        /// 获取自身或父级对象上的组件（带缓存）。
        /// <para>优先检查自身，如果自身不存在则检查父级。</para>
        /// </summary>
        /// <typeparam name="T">组件类型，必须继承自 Component</typeparam>
        /// <returns>缓存的组件实例，如果自身和父级都不存在则返回 null</returns>
        protected T GetCachedComponentInSelfOrParent<T>()
            where T : Component
        {
            var type = typeof(T);
            if (_componentCache.TryGetValue(type, out var cached))
                return cached as T;

            var component = GetComponent<T>() ?? GetComponentInParent<T>();
            if (component != null)
                _componentCache[type] = component;

            return component;
        }

        /// <summary>
        /// 精确获取父级对象上的组件（带缓存），排除自身组件。
        /// <para>用于需要明确从父级获取组件的场景，避免获取到自身组件。</para>
        /// </summary>
        /// <typeparam name="T">组件类型，必须继承自 Component</typeparam>
        /// <returns>缓存的父级组件实例，如果父级不存在则返回 null</returns>
        protected T GetCachedComponentExact<T>()
            where T : Component
        {
            var type = typeof(T);
            if (_componentCache.TryGetValue(type, out var cached))
                return cached as T;

            var component = GetComponentInParent<T>();
            if (component != null && component != this)
                _componentCache[type] = component;
            else
                component = null;

            return component;
        }

        /// <summary>
        /// 清空组件缓存。
        /// </summary>
        protected void ClearComponentCache()
        {
            _componentCache.Clear();
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 安全执行操作，捕获并记录异常。
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="context">上下文信息，用于日志</param>
        protected void SafeExecute(Action action, string context = null)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                ALog.Error(
                    $"[{GetType().Name}] {(context != null ? $"[{context}] " : "")}Error: {e.Message}\n{e.StackTrace}",
                    e
                );
            }
        }

        /// <summary>
        /// 安全执行函数并返回结果，捕获并记录异常。
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要执行的函数</param>
        /// <param name="defaultValue">异常时的默认返回值</param>
        /// <param name="context">上下文信息，用于日志</param>
        /// <returns>函数执行结果或默认值</returns>
        protected T SafeExecute<T>(Func<T> func, T defaultValue = default, string context = null)
        {
            try
            {
                return func != null ? func.Invoke() : defaultValue;
            }
            catch (Exception e)
            {
                ALog.Error(
                    $"[{GetType().Name}] {(context != null ? $"[{context}] " : "")}Error: {e.Message}\n{e.StackTrace}",
                    e
                );
                return defaultValue;
            }
        }

        /// <summary>
        /// 检查对象是否为 null 或已销毁。
        /// </summary>
        /// <param name="obj">要检查的对象</param>
        /// <returns>如果对象为 null 或已销毁则返回 true</returns>
        protected bool IsNullOrDestroyed(UnityEngine.Object obj)
        {
            return obj == null || obj.Equals(null);
        }

        /// <summary>
        /// 安全销毁对象。
        /// </summary>
        /// <param name="obj">要销毁的对象</param>
        /// <param name="delay">延迟销毁时间（秒）</param>
        protected void DestroySafely(UnityEngine.Object obj, float delay = 0f)
        {
            if (IsNullOrDestroyed(obj))
                return;

            if (delay > 0)
                Destroy(obj, delay);
            else
                Destroy(obj);
        }

        #endregion

        #region 静态方法

        /// <summary>
        /// 当框架就绪时执行操作，如果已就绪则立即执行。
        /// </summary>
        /// <param name="action">要执行的操作</param>
        public static void WhenReady(Action action)
        {
            if (AsakiBootstrapper.IsReady)
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    ALog.Error($"[AsakiMono] Error in WhenReady callback: {ex}");
                }
            }
            else
            {
                var handler = new TemporaryReadyHandler(action);
                handler.Register();
            }
        }

        #endregion

        #region 内部类型

        internal class TemporaryReadyHandler : IAsakiHandler<OnAsakiFrameworkReadyEvent>
        {
            private Action _action;

            public TemporaryReadyHandler(Action action)
            {
                _action = action;
            }

            public void Register()
            {
                this.AsakiRegister();
            }

            public void OnEvent(in OnAsakiFrameworkReadyEvent e)
            {
                try
                {
                    _action?.Invoke();
                }
                catch (Exception ex)
                {
                    ALog.Error($"[AsakiMono] Error in WhenReady callback: {ex}");
                }
                finally
                {
                    this.AsakiUnregister();
                }
            }
        }

        #endregion
    }
}
