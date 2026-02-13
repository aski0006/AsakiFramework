using System;

namespace Asaki.Core.UI
{
    public interface IAsakiUIResourceHandle : IDisposable
    {
        bool IsValid { get; }
    }
}
