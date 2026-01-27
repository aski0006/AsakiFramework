using Asaki.Core.Async;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace Asaki.Unity.Services.Async
{
	public static class AsakiAsyncServiceExtensions
	{
		public static UniTask WaitAll(this IAsakiAsyncService service, params UniTask[] tasks)
		{
			return service.WaitAll(tasks, CancellationToken.None);
		}

		public static UniTask<int> WaitAny(this IAsakiAsyncService service, params UniTask[] tasks)
		{
			return service.WaitAny(tasks, CancellationToken.None);
		}

		public static UniTask Sequence(this IAsakiAsyncService service, params Func<UniTask>[] actions)
		{
			return service.Sequence(actions, CancellationToken.None);
		}

		public static UniTask Parallel(this IAsakiAsyncService service, params Func<UniTask>[] actions)
		{
			return service.Parallel(actions, CancellationToken.None);
		}
	}
}
