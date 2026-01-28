using System;

namespace Asaki.Core.MVVM
{
    public interface IAsakiPropertyBase : IDisposable
    {
        void InvokeCallback(object value);
        Type ValueType { get; }
    }
}
