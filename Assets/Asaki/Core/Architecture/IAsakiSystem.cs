using System;

namespace Asaki.Core.Architecture
{
    public interface IAsakiSystem : IDisposable
    {
        void Setup();
    }
}
