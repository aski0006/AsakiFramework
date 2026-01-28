using System;
using System.Threading;
using System.Threading.Tasks;

namespace Asaki.Core.Scene
{
    public interface IAsakiSceneTransition : IDisposable
    {
        Task EnterAsync(CancellationToken ct);
        void OnProgress(float normalizedProgress);
        Task ExitAsync(CancellationToken ct);
    }
}
