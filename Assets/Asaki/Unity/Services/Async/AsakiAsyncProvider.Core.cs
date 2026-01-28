using Asaki.Core.Async;
using Asaki.Core.Logging;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace Asaki.Unity.Services.Async
{
	public partial class AsakiAsyncProvider : IAsakiAsyncService, IDisposable
	{
		private CancellationTokenSource _serviceCts = new CancellationTokenSource();
		private int _runningTaskCount = 0;

		public int RunningTaskCount => _runningTaskCount;

		public void CancelAllTasks()
		{
			if (_serviceCts != null)
			{
				_serviceCts.Cancel();
				_serviceCts.Dispose();
			}
			_serviceCts = new CancellationTokenSource();
		}

		public CancellationToken CreateLinkedToken(CancellationToken externalToken = default(CancellationToken))
		{
			if (_serviceCts == null || _serviceCts.IsCancellationRequested) return CancellationToken.None;
			if (externalToken == CancellationToken.None) return _serviceCts.Token;
			return CancellationTokenSource.CreateLinkedTokenSource(_serviceCts.Token, externalToken).Token;
		}

		private async UniTask Track(Func<UniTask> taskFunc)
		{
			Interlocked.Increment(ref _runningTaskCount);
			try
			{
				await taskFunc();
			}
			catch (OperationCanceledException) { }
			catch (Exception e)
			{
				ALog.Error($"[AsakiAsync] Task error: {e.Message}", e);
				throw;
			}
			finally
			{
				Interlocked.Decrement(ref _runningTaskCount);
			}
		}

		private async UniTask<T> Track<T>(Func<UniTask<T>> taskFunc)
		{
			Interlocked.Increment(ref _runningTaskCount);
			try
			{
				return await taskFunc();
			}
			finally
			{
				Interlocked.Decrement(ref _runningTaskCount);
			}
		}

		public void Dispose()
		{
			if (_serviceCts != null)
			{
				_serviceCts.Cancel();
				_serviceCts.Dispose();
				_serviceCts = null;
			}
		}
	}
}
