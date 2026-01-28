using System;

namespace Asaki.Core.Architecture.Interfaces
{
    public interface IAsakiSystem : IDisposable
    {
        void Setup();
    }
}
