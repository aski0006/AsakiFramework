using Asaki.Core.Context;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Asaki.Core.Async
{
	public interface IAsakiAsyncService : IAsakiService
	{
		UniTask WaitSeconds(float seconds, CancellationToken token = default);
		UniTask WaitSecondsUnscaled(float seconds, CancellationToken token = default);
		UniTask WaitFrame(CancellationToken token = default);
		UniTask WaitFrames(int count, CancellationToken token = default);
		UniTask WaitFixedFrame(CancellationToken token = default);
		UniTask WaitFixedFrames(int count, CancellationToken token = default);
		UniTask WaitUntil(Func<bool> predicate, CancellationToken token = default);
		UniTask WaitWhile(Func<bool> predicate, CancellationToken token = default);
		UniTask<bool> WaitUntil(Func<bool> predicate, float timeoutSeconds, CancellationToken token = default);
		UniTask<bool> WaitWhile(Func<bool> predicate, float timeoutSeconds, CancellationToken token = default);
		UniTask RunTask(Func<UniTask> taskFunc, CancellationToken token = default);
		UniTask<T> RunTask<T>(Func<UniTask<T>> taskFunc, CancellationToken token = default);
		UniTask DelayedCall(float delaySeconds, Action action, CancellationToken token = default, bool unscaledTime = false);
		UniTask NextFrameCall(Action action, CancellationToken token = default);
		UniTask When(Func<bool> condition, Action action, CancellationToken token = default);
		UniTask WaitAll(IEnumerable<UniTask> tasks, CancellationToken token = default);
		UniTask<int> WaitAny(IEnumerable<UniTask> tasks, CancellationToken token = default);
		UniTask Sequence(IEnumerable<Func<UniTask>> actions, CancellationToken token = default);
		UniTask Parallel(IEnumerable<Func<UniTask>> actions, CancellationToken token = default);
		UniTask Retry(Func<UniTask> action, int maxRetries = 3, float retryDelay = 1f, CancellationToken token = default);
		UniTask WaitCustom(IAsakiWaitSource waitSource, CancellationToken token = default);
		IWaitBuilder CreateWaitBuilder();
		int RunningTaskCount { get; }
		void CancelAllTasks();
		CancellationToken CreateLinkedToken(CancellationToken externalToken = default);
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
		UniTask Build(CancellationToken token = default);
	}
}