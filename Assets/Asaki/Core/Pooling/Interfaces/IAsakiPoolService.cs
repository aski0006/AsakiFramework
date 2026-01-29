using Asaki.Core.Context;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace Asaki.Core.Pooling.Interfaces
{
	public interface IAsakiPoolService : IAsakiService, IDisposable
	{
		UniTask<IAsakiPool<T>> CreatePoolAsync<T>(
			string key,
			IAsakiPoolObjectFactory<T> factory,
			AsakiPoolConfig config = null,
			CancellationToken token = default
		)
			where T : class;

		IAsakiPool<T> GetPool<T>(string key)
			where T : class;

		bool HasPool(string key);

		bool DestroyPool(string key);

		string GetStatisticsSummary();
	}
}
