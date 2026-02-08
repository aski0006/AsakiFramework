// File: Assets/Asaki/Tests/Resources/Mocks/MockAsakiAsyncService.cs
// 扩展的模拟异步服务，专门用于资源测试

using System;
using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Async;
using Cysharp.Threading.Tasks;

namespace Asaki.Tests.Resources.Mocks
{
    /// <summary>
    /// 扩展的模拟异步服务，用于资源加载测试
    /// </summary>
    public class MockAsakiAsyncService : IAsakiAsyncService
    {
        public int WaitFrameCallCount { get; private set; }
        public int RunTaskCallCount { get; private set; }
        public int RunningTaskCount => 0;

        public UniTask WaitFrame(CancellationToken token = default)
        {
            WaitFrameCallCount++;
            return UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        public UniTask WaitSeconds(float seconds, CancellationToken token = default)
        {
            if (seconds <= 0)
                return UniTask.CompletedTask;
            return UniTask.Delay(TimeSpan.FromSeconds(seconds), false, PlayerLoopTiming.Update, token);
        }

        public UniTask WaitSecondsUnscaled(float seconds, CancellationToken token = default)
        {
            if (seconds <= 0)
                return UniTask.CompletedTask;
            return UniTask.Delay(TimeSpan.FromSeconds(seconds), true, PlayerLoopTiming.Update, token);
        }

        public UniTask WaitFrames(int count, CancellationToken token = default)
        {
            if (count <= 0)
                return UniTask.CompletedTask;
            return UniTask.DelayFrame(count, PlayerLoopTiming.Update, token);
        }

        public UniTask WaitFixedFrame(CancellationToken token = default)
        {
            return UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);
        }

        public UniTask WaitFixedFrames(int count, CancellationToken token = default)
        {
            if (count <= 0)
                return UniTask.CompletedTask;
            return UniTask.DelayFrame(count, PlayerLoopTiming.FixedUpdate, token);
        }

        public UniTask WaitUntil(Func<bool> predicate, CancellationToken token = default)
        {
            return UniTask.WaitUntil(predicate, PlayerLoopTiming.Update, token);
        }

        public UniTask WaitWhile(Func<bool> predicate, CancellationToken token = default)
        {
            return UniTask.WaitWhile(predicate, PlayerLoopTiming.Update, token);
        }

        public async UniTask<bool> WaitUntil(Func<bool> predicate, float timeoutSeconds, CancellationToken token = default)
        {
            try
            {
                await UniTask.WaitUntil(predicate, PlayerLoopTiming.Update, token)
                    .Timeout(TimeSpan.FromSeconds(timeoutSeconds));
                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        public UniTask<bool> WaitWhile(Func<bool> predicate, float timeoutSeconds, CancellationToken token = default)
        {
            return WaitUntil(() => !predicate(), timeoutSeconds, token);
        }

        public UniTask RunTask(Func<UniTask> taskFunc, CancellationToken token = default)
        {
            RunTaskCallCount++;
            if (token.IsCancellationRequested)
                return UniTask.FromCanceled(token);
            return taskFunc();
        }

        public UniTask<T> RunTask<T>(Func<UniTask<T>> taskFunc, CancellationToken token = default)
        {
            RunTaskCallCount++;
            if (token.IsCancellationRequested)
                return UniTask.FromCanceled<T>(token);
            return taskFunc();
        }

        public async UniTask DelayedCall(float delaySeconds, Action action, CancellationToken token = default, bool unscaledTime = false)
        {
            if (delaySeconds <= 0)
            {
                action?.Invoke();
                return;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), unscaledTime, PlayerLoopTiming.Update, token);

            if (!token.IsCancellationRequested)
                action?.Invoke();
        }

        public async UniTask NextFrameCall(Action action, CancellationToken token = default)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);

            if (!token.IsCancellationRequested)
                action?.Invoke();
        }

        public async UniTask When(Func<bool> condition, Action action, CancellationToken token = default)
        {
            await UniTask.WaitUntil(condition, PlayerLoopTiming.Update, token);

            if (!token.IsCancellationRequested)
                action?.Invoke();
        }

        public UniTask WaitAll(IEnumerable<UniTask> tasks, CancellationToken token = default)
        {
            return UniTask.WhenAll(tasks).AttachExternalCancellation(token);
        }

        public UniTask<int> WaitAny(IEnumerable<UniTask> tasks, CancellationToken token = default)
        {
            return UniTask.WhenAny(tasks).AttachExternalCancellation(token);
        }

        public async UniTask Sequence(IEnumerable<Func<UniTask>> actions, CancellationToken token = default)
        {
            foreach (var action in actions)
            {
                if (token.IsCancellationRequested)
                    break;
                await action();
            }
        }

        public async UniTask Parallel(IEnumerable<Func<UniTask>> actions, CancellationToken token = default)
        {
            var tasks = new List<UniTask>();
            foreach (var action in actions)
            {
                tasks.Add(action());
            }
            await UniTask.WhenAll(tasks).AttachExternalCancellation(token);
        }

        public async UniTask Retry(Func<UniTask> action, int maxRetries = 3, float retryDelay = 1f, CancellationToken token = default)
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
                    if (i == maxRetries - 1)
                        throw;
                    if (token.IsCancellationRequested)
                        return;
                    await UniTask.Delay(TimeSpan.FromSeconds(retryDelay), true, PlayerLoopTiming.Update, token);
                }
            }
        }

        public async UniTask WaitCustom(IAsakiWaitSource waitSource, CancellationToken token = default)
        {
            while (!waitSource.IsCompleted)
            {
                waitSource.Update();
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }

        public IWaitBuilder CreateWaitBuilder()
        {
            return new MockWaitBuilder(this);
        }

        public void CancelAllTasks()
        {
        }

        public CancellationToken CreateLinkedToken(CancellationToken externalToken = default)
        {
            return externalToken;
        }

        public void Reset()
        {
            WaitFrameCallCount = 0;
            RunTaskCallCount = 0;
        }

        private class MockWaitBuilder : IWaitBuilder
        {
            private readonly IAsakiAsyncService _service;
            private readonly List<Func<CancellationToken, UniTask>> _steps = new();

            public MockWaitBuilder(IAsakiAsyncService service)
            {
                _service = service;
            }

            public IWaitBuilder Seconds(float seconds, bool unscaled = false)
            {
                _steps.Add(ct =>
                    unscaled
                        ? _service.WaitSecondsUnscaled(seconds, ct)
                        : _service.WaitSeconds(seconds, ct)
                );
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
                foreach (var step in _steps)
                {
                    if (token.IsCancellationRequested)
                        break;
                    await step(token);
                }
            }
        }
    }
}
