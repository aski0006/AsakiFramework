using System;
using System.Collections.Generic;
using UnityEngine;

namespace Asaki.Core.Reactive
{
    /// <summary>
    /// 绑定生命周期追踪器，用于自动管理 AsakiProperty 订阅的生命周期。
    /// </summary>
    /// <remarks>
    /// <para>该组件自动附加到 MonoBehaviour 所在的游戏对象上，当 MonoBehaviour 被销毁时，
    /// 自动取消所有相关的 AsakiProperty 订阅，防止内存泄漏。</para>
    /// <para>使用方式：</para>
    /// <code>
    /// public class PlayerView : MonoBehaviour
    /// {
    ///     void Start()
    ///     {
    ///         // 自动绑定到生命周期，无需手动取消订阅
    ///         GameState.Health.Subscribe(this, value => UpdateHealthUI(value));
    ///     }
    /// }
    /// </code>
    /// </remarks>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public class AsakiBindingTracker : MonoBehaviour
    {
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private bool _isDestroyed;

        /// <summary>
        /// 追踪一个订阅，使其在 MonoBehaviour 销毁时自动释放。
        /// </summary>
        /// <param name="subscription">要追踪的订阅</param>
        public void Track(IDisposable subscription)
        {
            if (subscription == null)
                return;

            if (_isDestroyed)
            {
                subscription.Dispose();
                return;
            }

            _subscriptions.Add(subscription);
        }

        /// <summary>
        /// 停止追踪并释放所有订阅。
        /// </summary>
        public void ReleaseAll()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                try
                {
                    subscription?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AsakiBindingTracker] 释放订阅时发生错误: {ex.Message}");
                }
            }
            _subscriptions.Clear();
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
            ReleaseAll();
        }
    }
}
