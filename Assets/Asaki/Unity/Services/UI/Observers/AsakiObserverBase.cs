using Asaki.Core.Reactive;

namespace Asaki.Unity.Services.UI.Observers
{
    /// <summary>
    /// Observer 泛型基类，提供脏检查和空值保护。
    /// </summary>
    /// <typeparam name="T">观察的值类型</typeparam>
    /// <typeparam name="TTarget">目标组件类型</typeparam>
    public abstract class AsakiObserverBase<T, TTarget> : IAsakiObserver<T>
        where TTarget : class
    {
        protected readonly TTarget _target;
        protected T _lastValue;

        protected AsakiObserverBase(TTarget target)
        {
            _target = target;
            _lastValue = GetDefaultValue();
        }

        public void OnValueChange(T value)
        {
            if (_target == null)
                return;

            if (!ShouldUpdate(value))
                return;

            _lastValue = value;
            ApplyValue(value);
        }

        protected virtual bool ShouldUpdate(T value)
        {
            return !Equals(value, _lastValue);
        }

        protected abstract void ApplyValue(T value);

        protected virtual T GetDefaultValue()
        {
            return default;
        }
    }
}
