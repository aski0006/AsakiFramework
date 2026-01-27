using Asaki.Core.Context;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Asaki.Core.Async
{
	public interface IAsakiAsyncService : IAsakiService
	{
		UniTask WaitSeconds(float seconds, CancellationToken token = default(CancellationToken));
		UniTask WaitSecondsUnscaled(float seconds, CancellationToken token = default(CancellationToken));
		UniTask WaitFrame(CancellationToken token = default(CancellationToken));
		UniTask WaitFrames(int count, CancellationToken token = default(CancellationToken));
		UniTask WaitFixedFrame(CancellationToken token = default(CancellationToken));
		UniTask WaitFixedFrames(int count, CancellationToken token = default(CancellationToken));
		UniTask WaitUntil(Func<bool> predicate, CancellationToken token = default(CancellationToken));
		UniTask WaitWhile(Func<bool> predicate, CancellationToken token = default(CancellationToken));
		UniTask<bool> WaitUntil(Func<bool> predicate, float timeoutSeconds, CancellationToken token = default(CancellationToken));
		UniTask<bool> WaitWhile(Func<bool> predicate, float timeoutSeconds, CancellationToken token = default(CancellationToken));
		UniTask RunTask(Func<UniTask> taskFunc, CancellationToken token = default(CancellationToken));
		UniTask<T> RunTask<T>(Func<UniTask<T>> taskFunc, CancellationToken token = default(CancellationToken));
		UniTask DelayedCall(float delaySeconds, Action action, CancellationToken token = default(CancellationToken), bool unscaledTime = false);
		UniTask NextFrameCall(Action action, CancellationToken token = default(CancellationToken));
		UniTask When(Func<bool> condition, Action action, CancellationToken token = default(CancellationToken));
		UniTask WaitAll(IEnumerable<UniTask> tasks, CancellationToken token = default(CancellationToken));
		UniTask<int> WaitAny(IEnumerable<UniTask> tasks, CancellationToken token = default(CancellationToken));
		UniTask Sequence(IEnumerable<Func<UniTask>> actions, CancellationToken token = default(CancellationToken));
		UniTask Parallel(IEnumerable<Func<UniTask>> actions, CancellationToken token = default(CancellationToken));
		UniTask Retry(Func<UniTask> action, int maxRetries = 3, float retryDelay = 1f, CancellationToken token = default(CancellationToken));
		UniTask WaitCustom(IAsakiWaitSource waitSource, CancellationToken token = default(CancellationToken));
		IWaitBuilder CreateWaitBuilder();
		int RunningTaskCount { get; }
		void CancelAllTasks();
		CancellationToken CreateLinkedToken(CancellationToken externalToken = default(CancellationToken));
	}

	public interface IAsakiWaitSource
	{
		bool IsCompleted { get; }
		float Progress { get; }
		void Update();
	}

	public interface IWaitBuilder
	{
		IWaitBuilder Seconds(float seconds, bool unscaled = false);
		IWaitBuilder Frames(int count);
		IWaitBuilder FixedFrames(int count);
		IWaitBuilder Until(Func<bool> condition);
		IWaitBuilder While(Func<bool> condition);
		UniTask Build(CancellationToken token = default(CancellationToken));
	}
}
