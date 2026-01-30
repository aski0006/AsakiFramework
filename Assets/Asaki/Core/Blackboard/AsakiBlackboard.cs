using System;
using System.Collections.Generic;
using Asaki.Core.Reactive;

namespace Asaki.Core.Blackboard
{
    public class AsakiBlackboard : IAsakiBlackboard, IDisposable
    {
        private readonly Dictionary<AsakiBlackboardKey, object> _data =
            new Dictionary<AsakiBlackboardKey, object>();
        private readonly Dictionary<AsakiBlackboardKey, IAsakiPropertyBase> _properties =
            new Dictionary<AsakiBlackboardKey, IAsakiPropertyBase>();
        private readonly IAsakiBlackboard _parentScope;

        private bool _isBatchMode = false;
        private HashSet<AsakiBlackboardKey> _pendingNotifications;
        private int _batchDepth = 0;

        public AsakiBlackboard(IAsakiBlackboard parentScope = null)
        {
            _parentScope = parentScope;
        }

        public IDisposable BeginBatch()
        {
            if (_batchDepth == 0)
            {
                _isBatchMode = true;
                _pendingNotifications = new HashSet<AsakiBlackboardKey>();
            }
            _batchDepth++;

            return new BatchScope(this);
        }

        private void EndBatch()
        {
            _batchDepth--;

            if (_batchDepth == 0)
            {
                _isBatchMode = false;

                if (_pendingNotifications != null && _pendingNotifications.Count > 0)
                {
                    foreach (AsakiBlackboardKey key in _pendingNotifications)
                    {
                        NotifyChange(key);
                    }
                    _pendingNotifications.Clear();
                }

                _pendingNotifications = null;
            }
        }

        private class BatchScope : IDisposable
        {
            private readonly AsakiBlackboard _owner;

            public BatchScope(AsakiBlackboard owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                _owner.EndBatch();
            }
        }

        public T GetValue<T>(AsakiBlackboardKey key)
        {
            if (_data.TryGetValue(key, out object value))
            {
                return (T)value;
            }

            if (_parentScope != null)
            {
                return _parentScope.GetValue<T>(key);
            }

            return default(T);
        }

        public void SetValue<T>(AsakiBlackboardKey key, T value)
        {
            _data[key] = value;

            if (_isBatchMode)
            {
                _pendingNotifications?.Add(key);
            }
            else
            {
                NotifyChange(key);
            }
        }

        private void NotifyChange(AsakiBlackboardKey key)
        {
            if (_properties.TryGetValue(key, out IAsakiPropertyBase propertyBase))
            {
                if (_data.TryGetValue(key, out object value))
                {
                    propertyBase.InvokeCallback(value);
                }
            }
        }

        public AsakiProperty<T> GetProperty<T>(AsakiBlackboardKey key)
        {
            if (!_properties.TryGetValue(key, out IAsakiPropertyBase propertyBase))
            {
                var typedProperty = new AsakiProperty<T>();

                if (_data.TryGetValue(key, out object existingValue))
                {
                    if (existingValue is T typedValue)
                    {
                        typedProperty._value = typedValue;
                    }
                }

                _properties[key] = typedProperty;
                return typedProperty;
            }

            if (propertyBase is AsakiProperty<T> existing)
            {
                return existing;
            }

            var newProperty = new AsakiProperty<T>();
            if (_data.TryGetValue(key, out object value) && value is T val)
            {
                newProperty._value = val;
            }

            _properties[key] = newProperty;
            return newProperty;
        }

        public bool HasKey(AsakiBlackboardKey key)
        {
            return _data.ContainsKey(key) || (_parentScope?.HasKey(key) ?? false);
        }

        public void Remove(AsakiBlackboardKey key)
        {
            _data.Remove(key);

            if (_properties.TryGetValue(key, out IAsakiPropertyBase property))
            {
                property?.Dispose();
                _properties.Remove(key);
            }
        }

        public void Clear()
        {
            foreach (IAsakiPropertyBase prop in _properties.Values)
            {
                prop?.Dispose();
            }
            _properties.Clear();
            _data.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
