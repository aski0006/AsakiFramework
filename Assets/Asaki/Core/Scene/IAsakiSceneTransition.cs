using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Scene
{
    public interface IAsakiSceneTransition : IDisposable
    {
        UniTask EnterAsync(CancellationToken ct);
        void OnProgress(float normalizedProgress);
        UniTask ExitAsync(CancellationToken ct);
    }
}
