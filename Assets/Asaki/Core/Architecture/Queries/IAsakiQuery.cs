using System.Threading;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Architecture.Queries
{
    public interface IAsakiQuery<TResult>
    {
        void Create(IAsakiArchitecture architecture);
        TResult Query();
    }

    public interface IAsakiQueryAsync<TResult>
    {
        void Create(IAsakiArchitecture architecture);
        UniTask<TResult> QueryAsync(CancellationToken token = default(CancellationToken));
    }
}
