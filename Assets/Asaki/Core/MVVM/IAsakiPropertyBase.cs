using System;

namespace Asaki.Core.Reactive
{
    public interface IAsakiPropertyBase : IDisposable
    {
        void InvokeCallback(object value);
        Type ValueType { get; }
    }
}
