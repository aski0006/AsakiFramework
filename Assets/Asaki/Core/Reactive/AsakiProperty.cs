using System;
using System.Collections.Generic;
using System.ComponentModel;
using Asaki.Core.Logging;
using UnityEngine;

namespace Asaki.Core.Reactive
{
    /// <summary>
    /// Asaki Reactive框架的核心可观察属性实现，提供值变化通知机制。
    /// </summary>
    /// <typeparam name="T">属性值的类型</typeparam>
    /// <remarks>
    /// <para>AsakiProperty是Asaki Reactive框架的基础组件，实现了观察者设计模式，
    /// 允许对象订阅值变化事件或通过<see cref="IAsakiObserver{T}"/>接口接收通知。</para>
    /// <para>主要功能包括：</para>
    /// <list type="bullet">
    /// <item>值变化时自动通知订阅者</item>
    /// <item>支持委托订阅和接口绑定两种方式</item>
    /// <item>提供丰富的相等性比较和运算符重载</item>
    /// <item>支持序列化</item>
    /// <item>线程安全的值更新（单线程环境下）</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 创建可观察属性
    /// /// var count = new AsakiProperty&lt;int&gt;(0);
    ///
    /// // 使用委托订阅值变化
    /// count.Subscribe(value => Debug.Log($"Count changed to: {value}"));
    ///
    /// // 使用接口绑定
    /// var observer = new CountObserver();
    /// count.Bind(observer);
    ///
    /// // 更新值，会自动通知所有订阅者
    /// count.Value = 10;
    ///
    /// // 自动类型转换
    /// int currentValue = count;
    /// Debug.Log($"Current value: {currentValue}");
    /// </code>
    /// </example>
    /// <seealso cref="IAsakiObserver{T}"/>
    [Serializable]
    public class AsakiProperty<T> : IEquatable<AsakiProperty<T>>, IAsakiPropertyBase
    {
        public Type ValueType => typeof(T);

        /// <summary>
        /// 存储属性的实际值。
        /// </summary>
        /// <remarks>
        /// 该字段标记为EditorBrowsableState.Never，不建议直接访问，应通过<see cref="Value"/>属性操作。
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public T _value;

        /// <summary>
        /// 存储值变化时要调用的委托。
        /// </summary>
        /// <remarks>
        /// 通过<see cref="Subscribe(Action{T})"/>和<see cref="Unsubscribe(Action{T})"/>方法管理订阅。
        /// 该字段标记为NonSerialized，不会被序列化。
        /// </remarks>
        [NonSerialized]
        private Action<T> _onValueChangedAction;

        /// <summary>
        /// 存储实现了<see cref="IAsakiObserver{T}"/>接口的观察者列表。
        /// </summary>
        /// <remarks>
        /// 通过<see cref="Bind(IAsakiObserver{T})"/>和<see cref="Unbind(IAsakiObserver{T})"/>方法管理绑定。
        /// 该字段标记为NonSerialized，不会被序列化。
        /// 使用延迟初始化属性 <see cref="Observers"/> 访问，确保反序列化后正确初始化。
        /// </remarks>
        [NonSerialized]
        private List<IAsakiObserver<T>> _observers;

        /// <summary>
        /// 线程同步锁对象。
        /// </summary>
        /// <remarks>
        /// 使用延迟初始化属性 <see cref="SyncRoot"/> 访问，确保反序列化后正确初始化。
        /// </remarks>
        [NonSerialized]
        private object _syncRoot;

        /// <summary>
        /// 获取观察者列表（延迟初始化，支持序列化恢复）。
        /// </summary>
        private List<IAsakiObserver<T>> Observers => _observers ??= new List<IAsakiObserver<T>>();

        /// <summary>
        /// 获取同步锁对象（延迟初始化，支持序列化恢复）。
        /// </summary>
        private object SyncRoot => _syncRoot ??= new object();

        /// <summary>
        /// 订阅凭证，实现IDisposable接口，用于自动取消订阅或解绑。
        /// </summary>
        private class Subscription : IDisposable
        {
            private Action _unsubscribeAction;
            private bool _disposed;

            /// <summary>
            /// 初始化订阅凭证。
            /// </summary>
            /// <param name="property">AsakiProperty实例（保留用于未来扩展）</param>
            /// <param name="unsubscribeAction">取消订阅的委托</param>
            public Subscription(AsakiProperty<T> property, Action unsubscribeAction)
            {
                _unsubscribeAction = unsubscribeAction;
                _disposed = false;
            }

            /// <summary>
            /// 执行取消订阅操作。
            /// </summary>
            public void Dispose()
            {
                if (!_disposed)
                {
                    try
                    {
                        _unsubscribeAction?.Invoke();
                    }
                    catch (ObjectDisposedException) { }
                    _disposed = true;
                    _unsubscribeAction = null;
                }
            }
        }

        /// <summary>
        /// 使用指定的初始值初始化AsakiProperty实例。
        /// </summary>
        /// <param name="initialValue">属性的初始值，默认为类型的默认值</param>
        /// <remarks>
        /// 构造函数会直接设置初始值，但不会触发值变化通知，因为此时还没有订阅者。
        /// 对于引用类型，默认值为null；对于值类型，默认值为该类型的默认值（如int为0，bool为false）。
        /// </remarks>
        public AsakiProperty(T initialValue = default(T))
        {
            _value = initialValue;
        }

        /// <summary>
        /// 获取或设置属性的值，设置时会自动通知所有订阅者。
        /// </summary>
        /// <value>属性的当前值</value>
        /// <remarks>
        /// <para>设置值时，会先比较新旧值是否相等（使用<see cref="EqualityComparer{T}.Default"/>），
        /// 如果相等则不会触发通知，以避免不必要的性能开销。</para>
        /// <para>如果值发生变化，会立即通知所有通过<see cref="Subscribe(Action{T})"/>和
        /// <see cref="Bind(IAsakiObserver{T})"/>注册的订阅者。</para>
        /// </remarks>
        public T Value
        {
            get => _value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(_value, value))
                    return;
                _value = value;
                Notify();
            }
        }

        /// <summary>
        /// 订阅属性值的变化事件。
        /// </summary>
        /// <param name="action">值变化时要调用的委托</param>
        /// <remarks>
        /// <para>订阅后，每当属性值发生变化时，都会调用指定的委托。</para>
        /// <para>订阅时会立即用当前值调用一次委托，确保订阅者获得最新状态。</para>
        /// <para>可以通过<see cref="Unsubscribe(Action{T})"/>方法取消订阅。</para>
        /// <para>⚠️ 注意：如果忘记取消订阅，可能导致内存泄漏。建议在 MonoBehaviour 中使用 <see cref="Subscribe(MonoBehaviour, Action{T})"/> 重载。</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var property = new AsakiProperty&lt;string&gt;("initial");
        ///
        /// // 订阅值变化（会立即输出 "Value: initial"）
        /// using (var subscription = property.Subscribe(value => Debug.Log($"Value: {value}")))
        /// {
        ///     // 更新值（会输出 "Value: updated"）
        ///     property.Value = "updated";
        /// }
        /// // 超出using块后自动取消订阅
        /// property.Value = "changed";
        /// </code>
        /// </example>
        public IDisposable Subscribe(Action<T> action)
        {
            lock (SyncRoot)
            {
                _onValueChangedAction += action;
            }
            action?.Invoke(_value);
            return new Subscription(
                this,
                () =>
                {
                    lock (SyncRoot)
                    {
                        _onValueChangedAction -= action;
                    }
                }
            );
        }

        /// <summary>
        /// 订阅属性值的变化事件，并自动绑定到 MonoBehaviour 的生命周期。
        /// </summary>
        /// <param name="owner">订阅者所属的 MonoBehaviour，销毁时自动取消订阅</param>
        /// <param name="action">值变化时要调用的委托</param>
        /// <remarks>
        /// <para>订阅后，每当属性值发生变化时，都会调用指定的委托。</para>
        /// <para>订阅时会立即用当前值调用一次委托，确保订阅者获得最新状态。</para>
        /// <para>当 MonoBehaviour 被销毁时，会自动取消订阅，防止内存泄漏。</para>
        /// <para>这是 Unity 场景对象订阅属性的推荐方式。</para>
        /// </remarks>
        /// <example>
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
        /// </example>
        public IDisposable Subscribe(MonoBehaviour owner, Action<T> action)
        {
            if (owner == null)
            {
                ALog.Warn(
                    "[AsakiProperty] Subscribe 的 owner 参数为 null，将使用普通订阅方式。建议传入有效的 MonoBehaviour 以启用自动生命周期管理。"
                );
                return Subscribe(action);
            }

            IDisposable subscription = Subscribe(action);

            AsakiBindingTracker tracker = owner.gameObject.GetComponent<AsakiBindingTracker>();
            if (tracker == null)
            {
                tracker = owner.gameObject.AddComponent<AsakiBindingTracker>();
            }
            tracker.Track(subscription);

            return subscription;
        }

        /// <summary>
        /// 取消订阅属性值的变化事件。
        /// </summary>
        /// <param name="action">要取消的委托</param>
        /// <remarks>
        /// 取消订阅后，当属性值发生变化时，不会再调用指定的委托。
        /// </remarks>
        /// <example>
        /// <code>
        /// var property = new AsakiProperty&lt;int&gt;(5);
        /// Action&lt;int&gt; onChanged = value => Debug.Log($"Value: {value}");
        ///
        /// property.Subscribe(onChanged);
        /// property.Value = 10; // 输出 "Value: 10"
        ///
        /// // 取消订阅
        /// property.Unsubscribe(onChanged);
        /// property.Value = 15; // 不会输出任何内容
        /// </code>
        /// </example>
        public void Unsubscribe(Action<T> action)
        {
            lock (SyncRoot)
            {
                _onValueChangedAction -= action;
            }
        }

        /// <summary>
        /// 将观察者绑定到属性，当值变化时会通知观察者。
        /// </summary>
        /// <param name="observer">实现了<see cref="IAsakiObserver{T}"/>接口的观察者</param>
        /// <remarks>
        /// <para>绑定后，每当属性值发生变化时，都会调用观察者的<see cref="IAsakiObserver{T}.OnValueChange(T)"/>方法。</para>
        /// <para>绑定前会检查观察者是否已存在，避免重复绑定。</para>
        /// <para>绑定时会立即用当前值调用一次观察者的OnValueChange方法。</para>
        /// <para>可以通过<see cref="Unbind(IAsakiObserver{T})"/>方法解除绑定。</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// public class MyObserver : IAsakiObserver&lt;int&gt;
        /// {
        ///     public void OnValueChange(int value)
        ///     {
        ///         Debug.Log($"Value changed to: {value}");
        ///     }
        /// }
        ///
        /// var property = new AsakiProperty&lt;int&gt;(5);
        /// var observer = new MyObserver();
        ///
        /// // 绑定观察者（会立即输出 "Value changed to: 5"）
        /// using (var binding = property.Bind(observer))
        /// {
        ///     property.Value = 10; // 输出 "Value changed to: 10"
        /// }
        /// // 超出using块后自动解除绑定
        /// property.Value = 15; // 不会输出任何内容
        /// </code>
        /// </example>
        public IDisposable Bind(IAsakiObserver<T> observer)
        {
            lock (SyncRoot)
            {
                if (Observers.Contains(observer))
                {
                    return new Subscription(this, () => { });
                }
                Observers.Add(observer);
            }
            observer.OnValueChange(_value);
            return new Subscription(
                this,
                () =>
                {
                    lock (SyncRoot)
                    {
                        Observers.Remove(observer);
                    }
                }
            );
        }

        /// <summary>
        /// 解除观察者与属性的绑定。
        /// </summary>
        /// <param name="observer">要解除绑定的观察者</param>
        /// <remarks>
        /// 解除绑定后，当属性值发生变化时，不会再通知该观察者。
        /// </remarks>
        /// <example>
        /// <code>
        /// var property = new AsakiProperty&lt;int&gt;(5);
        /// var observer = new MyObserver();
        ///
        /// property.Bind(observer);
        /// property.Value = 10; // 输出 "Value changed to: 10"
        ///
        /// // 解除绑定
        /// property.Unbind(observer);
        /// property.Value = 15; // 不会输出任何内容
        /// </code>
        /// </example>
        public void Unbind(IAsakiObserver<T> observer)
        {
            lock (SyncRoot)
            {
                Observers.Remove(observer);
            }
        }

        /// <summary>
        /// 通知所有订阅者和观察者值已变化。
        /// </summary>
        /// <remarks>
        /// <para>该方法是内部实现，不建议直接调用。</para>
        /// <para>通知顺序：先调用所有通过<see cref="Subscribe(Action{T})"/>注册的委托，
        /// 然后调用所有通过<see cref="Bind(IAsakiObserver{T})"/>注册的观察者。</para>
        /// <para>遍历观察者列表时从后往前遍历，以支持在通知过程中解除绑定。</para>
        /// <para>使用快照机制确保在通知过程中集合被修改时不会出错。</para>
        /// </remarks>
        private void Notify()
        {
            NotifyCore(_value);
        }

        /// <summary>
        /// 核心通知逻辑，避免代码重复。
        /// </summary>
        /// <param name="value">要通知的值</param>
        private void NotifyCore(T value)
        {
            Action<T> actionSnapshot;
            IAsakiObserver<T>[] observerSnapshot;
            int observerCount;

            lock (SyncRoot)
            {
                actionSnapshot = _onValueChangedAction;
                observerCount = Observers.Count;
                if (observerCount > 0)
                {
                    if (_observerBuffer == null || _observerBuffer.Length < observerCount)
                    {
                        _observerBuffer = new IAsakiObserver<T>[observerCount * 2];
                    }
                    Observers.CopyTo(_observerBuffer, 0);
                    observerSnapshot = _observerBuffer;
                }
                else
                {
                    observerSnapshot = null;
                }
            }

            actionSnapshot?.Invoke(value);

            if (observerSnapshot != null)
            {
                for (int i = observerCount - 1; i >= 0; i--)
                {
                    try
                    {
                        observerSnapshot[i]?.OnValueChange(value);
                    }
                    catch (Exception ex)
                    {
                        ALog.Error(
                            $"[AsakiProperty] An error occurred while notifying the observer: {ex.Message}"
                        );
                    }
                }
            }
        }

        /// <summary>
        /// 观察者快照缓冲区，用于减少 GC 压力。
        /// </summary>
        [NonSerialized]
        private IAsakiObserver<T>[] _observerBuffer;

        public void InvokeCallback(object value)
        {
            if (value is not T typedValue)
                return;

            _value = typedValue;
            NotifyCore(typedValue);
        }

        public void Dispose()
        {
            lock (SyncRoot)
            {
                _onValueChangedAction = null;
                Observers.Clear();
            }
        }

        // ========================================================================
        // 相等性实现
        // ========================================================================

        /// <summary>
        /// 获取对象的哈希码。
        /// </summary>
        /// <returns>对象值的哈希码</returns>
        /// <remarks>
        /// <para>哈希码基于当前值计算。由于AsakiProperty是可变对象，
        /// 其哈希码会随值变化而变化，因此不建议作为字典键或哈希集合的元素。</para>
        /// <para>如果需要在字典中使用，建议使用值的引用或不可变的标识符作为键。</para>
        /// </remarks>
        public override int GetHashCode()
        {
            return _value?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// 比较当前对象与另一个AsakiProperty对象是否相等。
        /// </summary>
        /// <param name="other">要比较的另一个AsakiProperty对象</param>
        /// <returns>如果两个对象的值相等则返回true，否则返回false</returns>
        /// <remarks>
        /// 相等性比较基于属性的当前值，而不是引用相等。
        /// </remarks>
        public bool Equals(AsakiProperty<T> other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return EqualityComparer<T>.Default.Equals(_value, other._value);
        }

        /// <summary>
        /// 比较当前对象与另一个对象是否相等。
        /// </summary>
        /// <param name="obj">要比较的另一个对象</param>
        /// <returns>如果两个对象相等则返回true，否则返回false</returns>
        /// <remarks>
        /// <para>支持三种比较场景：
        /// <list type="bullet">
        /// <item>与另一个AsakiProperty对象比较：比较两者的值</item>
        /// <item>与T类型的值比较：比较当前对象的值与该值</item>
        /// <item>与其他类型比较：始终返回false</item>
        /// </list>
        /// </para>
        /// </remarks>
        public override bool Equals(object obj)
        {
            return obj switch
            {
                // 相同类型，使用类型安全的 Equals 方法
                AsakiProperty<T> other => Equals(other),
                // T 类型，直接比较值
                T val => EqualityComparer<T>.Default.Equals(_value, val),
                // 其他类型不相等
                _ => false,
            };
        }

        // ========================================================================
        // 运算符重载
        // ========================================================================

        /// <summary>
        /// 比较两个AsakiProperty对象是否相等。
        /// </summary>
        /// <param name="left">左侧的AsakiProperty对象</param>
        /// <param name="right">右侧的AsakiProperty对象</param>
        /// <returns>如果两个对象的值相等则返回true，否则返回false</returns>
        /// <remarks>
        /// 相等性比较基于属性的当前值，而不是引用相等。
        /// 支持null安全比较。
        /// </remarks>
        public static bool operator ==(AsakiProperty<T> left, AsakiProperty<T> right)
        {
            // 处理两个都为 null 的情况
            if (ReferenceEquals(left, right))
                return true;

            // 处理其中一个为 null 的情况
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;

            return EqualityComparer<T>.Default.Equals(left._value, right._value);
        }

        /// <summary>
        /// 比较两个AsakiProperty对象是否不相等。
        /// </summary>
        /// <param name="left">左侧的AsakiProperty对象</param>
        /// <param name="right">右侧的AsakiProperty对象</param>
        /// <returns>如果两个对象的值不相等则返回true，否则返回false</returns>
        /// <remarks>
        /// 相等性比较基于属性的当前值，而不是引用相等。
        /// 支持null安全比较。
        /// </remarks>
        public static bool operator !=(AsakiProperty<T> left, AsakiProperty<T> right)
        {
            return !(left == right);
        }

        /// <summary>
        /// 比较T类型的值与AsakiProperty对象是否相等（T在左侧）。
        /// </summary>
        /// <param name="left">左侧的T类型值</param>
        /// <param name="right">右侧的AsakiProperty对象</param>
        /// <returns>如果值相等则返回true，否则返回false</returns>
        /// <remarks>
        /// 支持null安全比较。如果右侧的AsakiProperty为null，则将其视为默认值。
        /// </remarks>
        public static bool operator ==(T left, AsakiProperty<T> right)
        {
            // 如果 right 为 null，只有 left 为 null 时才相等（对于引用类型）
            if (ReferenceEquals(right, null))
                return EqualityComparer<T>.Default.Equals(left, default(T));

            return EqualityComparer<T>.Default.Equals(left, right._value);
        }

        /// <summary>
        /// 比较T类型的值与AsakiProperty对象是否不相等（T在左侧）。
        /// </summary>
        /// <param name="left">左侧的T类型值</param>
        /// <param name="right">右侧的AsakiProperty对象</param>
        /// <returns>如果值不相等则返回true，否则返回false</returns>
        /// <remarks>
        /// 支持null安全比较。如果右侧的AsakiProperty为null，则将其视为默认值。
        /// </remarks>
        public static bool operator !=(T left, AsakiProperty<T> right)
        {
            return !(left == right);
        }

        /// <summary>
        /// 比较AsakiProperty对象与T类型的值是否相等（AsakiProperty在左侧）。
        /// </summary>
        /// <param name="left">左侧的AsakiProperty对象</param>
        /// <param name="right">右侧的T类型值</param>
        /// <returns>如果值相等则返回true，否则返回false</returns>
        /// <remarks>
        /// 支持null安全比较。如果左侧的AsakiProperty为null，则将其视为默认值。
        /// </remarks>
        public static bool operator ==(AsakiProperty<T> left, T right)
        {
            // 如果 left 为 null，只有 right 为 null 时才相等（对于引用类型）
            if (ReferenceEquals(left, null))
                return EqualityComparer<T>.Default.Equals(default(T), right);

            return EqualityComparer<T>.Default.Equals(left._value, right);
        }

        /// <summary>
        /// 比较AsakiProperty对象与T类型的值是否不相等（AsakiProperty在左侧）。
        /// </summary>
        /// <param name="left">左侧的AsakiProperty对象</param>
        /// <param name="right">右侧的T类型值</param>
        /// <returns>如果值不相等则返回true，否则返回false</returns>
        /// <remarks>
        /// 支持null安全比较。如果左侧的AsakiProperty为null，则将其视为默认值。
        /// </remarks>
        public static bool operator !=(AsakiProperty<T> left, T right)
        {
            return !(left == right);
        }

        // ========================================================================
        // 其他方法
        // ========================================================================

        /// <summary>
        /// 返回属性值的字符串表示。
        /// </summary>
        /// <returns>属性值的字符串表示，如果值为null则返回"null"</returns>
        /// <remarks>
        /// 直接调用值的ToString()方法，如果值为null则返回"null"。
        /// </remarks>
        public override string ToString()
        {
            return _value?.ToString() ?? "null";
        }

        /// <summary>
        /// 隐式将AsakiProperty对象转换为T类型的值。
        /// </summary>
        /// <param name="property">要转换的AsakiProperty对象</param>
        /// <returns>AsakiProperty对象的值</returns>
        /// <remarks>
        /// 支持null安全转换。如果property为null，则返回T类型的默认值。
        /// </remarks>
        /// <example>
        /// <code>
        /// var property = new AsakiProperty&lt;int&gt;(10);
        /// int value = property; // 隐式转换，value = 10
        /// </code>
        /// </example>
        public static implicit operator T(AsakiProperty<T> property)
        {
            if (property == null)
                return default(T);
            return property._value;
        }
    }
}
