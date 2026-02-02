using System;
using System.Collections.Generic;
using Asaki.Core.Broker;
using Asaki.Core.Logging;
using Asaki.Unity.Bootstrapper;
using UnityEngine;

namespace Asaki.Unity
{
    public abstract class AsakiMono : MonoBehaviour, IAsakiHandler<OnAsakiFrameworkReadyEvent>
    {
        protected bool IsActivated { get; private set; }

        protected virtual void Awake()
        {
            IsActivated = false;
            OnAwake();
        }

        protected virtual void OnEnable()
        {
            this.AsakiRegister();
            EnableComponent();
        }

        protected virtual void EnableComponent() { }

        protected virtual void OnDisable()
        {
            this.AsakiUnregister();
            DisableComponent();
        }

        protected virtual void DisableComponent() { }

        protected virtual void OnAwake() { }

        protected virtual void OnStart() { }

        public void OnEvent(OnAsakiFrameworkReadyEvent e)
        {
            IsActivated = true;
            OnStart();
        }

        protected virtual void Update()
        {
            if (!IsActivated)
            {
                return;
            }

            OnUpdate();
        }

        protected virtual void FixedUpdate()
        {
            if (!IsActivated)
            {
                return;
            }

            OnFixedUpdate();
        }

        protected virtual void LateUpdate()
        {
            if (!IsActivated)
            {
                return;
            }
            OnLateUpdate();
        }

        protected virtual void OnUpdate() { }

        protected virtual void OnFixedUpdate() { }

        protected virtual void OnLateUpdate() { }

        #region Lifecycle Extensions

        protected virtual void OnDestroy()
        {
            Cleanup();
        }

        protected virtual void Cleanup() { }

        protected virtual void OnApplicationPause(bool pauseStatus) { }

        protected virtual void OnApplicationFocus(bool hasFocus) { }

        protected virtual void OnApplicationQuit() { }

        #endregion

        #region Component Cache

        private readonly Dictionary<Type, Component> _componentCache = new();

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
            {
                return cached as T;
            }

            var component = GetComponentInChildren<T>();
            if (component != null)
            {
                _componentCache[type] = component;
            }

            return component;
        }

        protected void ClearComponentCache()
        {
            _componentCache.Clear();
        }

        #endregion

        #region Utility Methods

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
            {
                return;
            }

            if (delay > 0)
            {
                Destroy(obj, delay);
            }
            else
            {
                Destroy(obj);
            }
        }

        protected void DestroyImmediateSafely(UnityEngine.Object obj)
        {
            if (!IsNullOrDestroyed(obj))
            {
                DestroyImmediate(obj);
            }
        }

        #endregion

        #region Activation Control

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

        #endregion
    }
}
