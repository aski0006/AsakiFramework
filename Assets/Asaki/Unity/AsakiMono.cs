using System;
using System.Collections.Generic;
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
    /// </remarks>
    public abstract class AsakiMono : MonoBehaviour, IAsakiHandler<OnAsakiFrameworkReadyEvent>
    {
        protected bool IsActivated { get; private set; }
        public bool IsInitialized => IsActivated;
        public bool IsPendingInitialization => !IsActivated && _lifecycleTrackingId > 0;

        private int _lifecycleTrackingId;
        private bool _isRegisteredWithLifecycleManager;
        private bool _enableComponentCalled;
        private readonly Dictionary<Type, Component> _componentCache = new();

        protected virtual void Awake()
        {
            IsActivated = false;
            OnAwake();
            RegisterWithLifecycleManager();
        }

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

        protected virtual void OnEnable()
        {
            this.AsakiRegister();
            if (IsActivated && !_enableComponentCalled)
            {
                _enableComponentCalled = true;
                EnableComponent();
            }
        }

        protected virtual void EnableComponent() { }

        protected virtual void OnDisable()
        {
            this.AsakiUnregister();
            _enableComponentCalled = false;
            DisableComponent();
        }

        protected virtual void DisableComponent() { }

        protected virtual void OnAwake() { }

        protected virtual void OnStart() { }

        public void OnEvent(in OnAsakiFrameworkReadyEvent e)
        {
            ActivateFrameworkReady();
        }

        internal void ActivateFrameworkReady()
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

        protected virtual void Update()
        {
            if (!IsActivated)
                return;
            OnUpdate();
        }

        protected virtual void FixedUpdate()
        {
            if (!IsActivated)
                return;
            OnFixedUpdate();
        }

        protected virtual void LateUpdate()
        {
            if (!IsActivated)
                return;
            OnLateUpdate();
        }

        protected virtual void OnUpdate() { }

        protected virtual void OnFixedUpdate() { }

        protected virtual void OnLateUpdate() { }

        protected virtual void OnDestroy()
        {
            // 强制取消订阅所有事件，防止内存泄漏
            try
            {
                this.AsakiUnregister();
            }
            catch (Exception ex)
            {
                ALog.Error($"[{GetType().Name}] Error during forced unsubscribe on destroy: {ex}");
            }

            // 原有逻辑
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

        protected virtual void Cleanup() { }

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

        protected void ClearComponentCache()
        {
            _componentCache.Clear();
        }

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

        protected bool IsNullOrDestroyed(UnityEngine.Object obj)
        {
            return obj == null || obj.Equals(null);
        }

        protected void DestroySafely(UnityEngine.Object obj, float delay = 0f)
        {
            if (IsNullOrDestroyed(obj))
                return;

            if (delay > 0)
                Destroy(obj, delay);
            else
                Destroy(obj);
        }

        protected void Activate()
        {
            if (IsActivated)
                return;
            IsActivated = true;
            OnActivated();
        }

        protected void Deactivate()
        {
            if (!IsActivated)
                return;
            IsActivated = false;
            OnDeactivated();
        }

        protected virtual void OnActivated() { }

        protected virtual void OnDeactivated() { }

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
    }
}
