using System.Threading;
using System.Threading.Tasks;

namespace Asaki.Core.UI
{
    public interface IAsakiWindow
    {
        Task OnOpenAsync(object args, CancellationToken token);
        Task OnCloseAsync(CancellationToken token);
        void OnCover();
        void OnReveal();
    }

    public interface IAsakiWindowWithResult : IAsakiWindow
    {
        void OnReturnValue(object value);
    }
}
