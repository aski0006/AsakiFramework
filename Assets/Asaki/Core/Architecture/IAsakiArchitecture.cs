using System;
using Asaki.Core.Context;

namespace Asaki.Core.Architecture
{
    public interface IAsakiArchitecture : IAsakiSceneService, IDisposable
    {
        T GetSystem<T>()
            where T : class, IAsakiSystem;
        T GetModel<T>()
            where T : class, IAsakiModel;
    }
}
