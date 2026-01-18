using Asaki.Core.Async;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace Asaki.Unity.Services.Async
{
	public static class AsakiAsyncServiceExtensions
	{
		public static UniTask WaitAll(this IAsakiAsyncService service, params UniTask[] tasks)
			=> service.WaitAll(tasks, CancellationToken.None);

		public static UniTask<int> WaitAny(this IAsakiAsyncService service, params UniTask[] tasks)
			=> service.WaitAny(tasks, CancellationToken.None);

		public static UniTask Sequence(this IAsakiAsyncService service, params Func<UniTask>[] actions)
			=> service.Sequence(actions, CancellationToken.None);

		public static UniTask Parallel(this IAsakiAsyncService service, params Func<UniTask>[] actions)
			=> service.Parallel(actions, CancellationToken.None);
	}
}
