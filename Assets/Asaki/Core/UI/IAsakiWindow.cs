using System.Threading;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.UI
{
    public interface IAsakiWindow
    {
        UniTask OnOpenAsync(object args, CancellationToken token);
        UniTask OnCloseAsync(CancellationToken token);
        void OnCover();
        void OnReveal();
    }

    public interface IAsakiWindowWithResult : IAsakiWindow
    {
        void OnReturnValue(object value);
    }
}
