using System;

namespace Asaki.Core.UI
{
    public interface IUIResourceHandle : IDisposable
    {
        bool IsValid { get; }
    }
}
