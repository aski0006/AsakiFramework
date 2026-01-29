using System.Threading;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Pooling.Interfaces
{
    public interface IAsakiPoolObjectFactory<T>
        where T : class
    {
        UniTask<T> CreateAsync(CancellationToken token = default);
        void OnGet(T obj);
        void OnReturn(T obj);
        void OnDestroy(T obj);
        bool Validate(T obj);
    }
}
