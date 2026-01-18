using Asaki.Core.Async;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Asaki.Unity.Services.Async
{
	public partial class AsakiAsyncProvider
	{
		public UniTask RunTask(Func<UniTask> taskFunc, CancellationToken token = default)
		{
			return Track(async () =>
			{
				if (token.IsCancellationRequested) throw new OperationCanceledException(token);
				await taskFunc();
			});
		}

		public UniTask<T> RunTask<T>(Func<UniTask<T>> taskFunc, CancellationToken token = default)
		{
			return Track(async () =>
			{
				if (token.IsCancellationRequested) throw new OperationCanceledException(token);
				return await taskFunc();
			});
		}

		public UniTask DelayedCall(float delaySeconds, Action action, CancellationToken token = default, bool unscaledTime = false)
		{
			return Track(async () =>
			{
				CancellationToken linkedToken = CreateLinkedToken(token);
				if (unscaledTime) await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), true, PlayerLoopTiming.Update, linkedToken);
				else await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), false, PlayerLoopTiming.Update, linkedToken);
				if (!linkedToken.IsCancellationRequested) action?.Invoke();
			});
		}

		public UniTask NextFrameCall(Action action, CancellationToken token = default)
		{
			return Track(async () =>
			{
				CancellationToken linkedToken = CreateLinkedToken(token);
				await UniTask.Yield(PlayerLoopTiming.Update, linkedToken);
				if (!linkedToken.IsCancellationRequested) action?.Invoke();
			});
		}

		public UniTask When(Func<bool> condition, Action action, CancellationToken token = default)
		{
			return Track(async () =>
			{
				CancellationToken linkedToken = CreateLinkedToken(token);
				await UniTask.WaitUntil(condition, PlayerLoopTiming.Update, linkedToken);
				if (!linkedToken.IsCancellationRequested) action?.Invoke();
			});
		}

		public UniTask WaitAll(IEnumerable<UniTask> tasks, CancellationToken token = default)
		{
			return UniTask.WhenAll(tasks.ToArray()).AttachExternalCancellation(token);
		}

		public UniTask<int> WaitAny(IEnumerable<UniTask> tasks, CancellationToken token = default)
		{
			return UniTask.WhenAny(tasks.ToArray()).AttachExternalCancellation(token);
		}

		public UniTask Sequence(IEnumerable<Func<UniTask>> actions, CancellationToken token = default)
		{
			return Track(async () =>
			{
				CancellationToken linkedToken = CreateLinkedToken(token);
				foreach (var action in actions)
				{
					if (linkedToken.IsCancellationRequested) break;
					await action();
				}
			});
		}

		public UniTask Parallel(IEnumerable<Func<UniTask>> actions, CancellationToken token = default)
		{
			return Track(async () =>
			{
				CancellationToken linkedToken = CreateLinkedToken(token);
				var tasks = actions.Select(a => a()).ToArray();
				await UniTask.WhenAll(tasks).AttachExternalCancellation(token);
			});
		}

		public UniTask Retry(Func<UniTask> action, int maxRetries = 3, float retryDelay = 1f, CancellationToken token = default)
		{
			CancellationToken linkedToken = CreateLinkedToken(token);
			return Track(async () =>
			{
				for (int i = 0; i < maxRetries; i++)
				{
					try
					{
						await action();
						return;
					}
					catch (Exception)
					{
						if (i == maxRetries - 1) throw;
						if (linkedToken.IsCancellationRequested) return;
						await UniTask.Delay(TimeSpan.FromSeconds(retryDelay), true, PlayerLoopTiming.Update, linkedToken);
					}
				}
			});
		}

		public UniTask WaitCustom(IAsakiWaitSource waitSource, CancellationToken token = default)
		{
			CancellationToken linkedToken = CreateLinkedToken(token);
			return Track(async () =>
			{
				while (!waitSource.IsCompleted)
				{
					waitSource.Update();
					await UniTask.Yield(PlayerLoopTiming.Update, linkedToken);
				}
			});
		}

		public IWaitBuilder CreateWaitBuilder()
		{
			return new AsakiWaitBuilder(this);
		}

		private class AsakiWaitBuilder : IWaitBuilder
		{
			private readonly IAsakiAsyncService _service;
			private readonly List<Func<CancellationToken, UniTask>> _steps = new List<Func<CancellationToken, UniTask>>();

			public AsakiWaitBuilder(IAsakiAsyncService service)
			{
				_service = service;
			}

			public IWaitBuilder Seconds(float seconds, bool unscaled = false)
			{
				_steps.Add(ct => unscaled ? _service.WaitSecondsUnscaled(seconds, ct) : _service.WaitSeconds(seconds, ct));
				return this;
			}

			public IWaitBuilder Frames(int count)
			{
				_steps.Add(ct => _service.WaitFrames(count, ct));
				return this;
			}

			public IWaitBuilder FixedFrames(int count)
			{
				_steps.Add(ct => _service.WaitFixedFrames(count, ct));
				return this;
			}

			public IWaitBuilder Until(Func<bool> condition)
			{
				_steps.Add(ct => _service.WaitUntil(condition, ct));
				return this;
			}

			public IWaitBuilder While(Func<bool> condition)
			{
				_steps.Add(ct => _service.WaitWhile(condition, ct));
				return this;
			}

			public async UniTask Build(CancellationToken token = default)
			{
				CancellationToken linkedToken = _service.CreateLinkedToken(token);
				foreach (var step in _steps)
				{
					if (linkedToken.IsCancellationRequested) break;
					await step(linkedToken);
				}
			}
		}
	}
}