using System.Threading;
using Asaki.Core.Architecture;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Architecture.Queries
{
    public interface IAsakiQuery<TResult>
    {
        void Create(IAsakiServiceProvider serviceProvider);
        TResult Query();
    }

    public interface IAsakiQueryAsync<TResult>
    {
        void Create(IAsakiServiceProvider serviceProvider);
        UniTask<TResult> QueryAsync(CancellationToken token = default(CancellationToken));
    }
}
