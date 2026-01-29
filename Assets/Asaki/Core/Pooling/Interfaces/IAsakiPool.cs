using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace Asaki.Core.Pooling.Interfaces
{

	public interface IAsakiPoolBase : IDisposable
	{
		string Key { get; }
		AsakiPoolConfig Config { get; }

		IAsakiPoolStatistics Statistics { get; }

		Type ObjectType { get; }

		void Clear();
		void Shrink(int targetSize);
	}
	public interface IAsakiPool<T> : IAsakiPoolBase
		where T : class
	{
		UniTask PrewarmAsync(int count, int itemsPerFrame = 5, CancellationToken token = default);

		UniTask<T> GetAsync(CancellationToken token = default);
		T Get();
		bool Return(T item);
	}
}
