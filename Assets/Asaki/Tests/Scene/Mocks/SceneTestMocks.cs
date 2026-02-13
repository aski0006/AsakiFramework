// File: Assets/Asaki/Tests/Scene/Mocks/SceneTestMocks.cs
// 场景管理测试专用的Mock服务，使用全限定名避免与其他测试冲突

using System;
using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Async;
using Asaki.Core.Broker;
using Asaki.Core.Resources;
using Asaki.Core.Scene;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

namespace Asaki.Tests.Scene.Mocks
{
    /// <summary>
    /// 场景管理测试专用的Mock异步服务
    /// </summary>
    public class SceneTest_MockAsakiAsyncService : IAsakiAsyncService
    {
        public int WaitFrameCallCount { get; private set; }
        public int WaitSecondsCallCount { get; private set; }
        public int RunTaskCallCount { get; private set; }
        public int RunningTaskCount => 0;

        public UniTask WaitFrame(CancellationToken token = default)
        {
            WaitFrameCallCount++;
            return UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        public UniTask WaitSeconds(float seconds, CancellationToken token = default)
        {
            WaitSecondsCallCount++;
            if (seconds <= 0)
                return UniTask.CompletedTask;
            return UniTask.Delay(
                TimeSpan.FromSeconds(seconds),
                false,
                PlayerLoopTiming.Update,
                token
            );
        }

        public UniTask WaitSecondsUnscaled(float seconds, CancellationToken token = default)
        {
            if (seconds <= 0)
                return UniTask.CompletedTask;
            return UniTask.Delay(
                TimeSpan.FromSeconds(seconds),
                true,
                PlayerLoopTiming.Update,
                token
            );
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

        public async UniTask<bool> WaitUntil(
            Func<bool> predicate,
            float timeoutSeconds,
            CancellationToken token = default
        )
        {
            try
            {
                await UniTask
                    .WaitUntil(predicate, PlayerLoopTiming.Update, token)
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

        public UniTask<bool> WaitWhile(
            Func<bool> predicate,
            float timeoutSeconds,
            CancellationToken token = default
        )
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

        public async UniTask DelayedCall(
            float delaySeconds,
            Action action,
            CancellationToken token = default,
            bool unscaledTime = false
        )
        {
            if (delaySeconds <= 0)
            {
                action?.Invoke();
                return;
            }

            await UniTask.Delay(
                TimeSpan.FromSeconds(delaySeconds),
                unscaledTime,
                PlayerLoopTiming.Update,
                token
            );

            if (!token.IsCancellationRequested)
                action?.Invoke();
        }

        public async UniTask NextFrameCall(Action action, CancellationToken token = default)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);

            if (!token.IsCancellationRequested)
                action?.Invoke();
        }

        public async UniTask When(
            Func<bool> condition,
            Action action,
            CancellationToken token = default
        )
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

        public async UniTask Sequence(
            IEnumerable<Func<UniTask>> actions,
            CancellationToken token = default
        )
        {
            foreach (var action in actions)
            {
                if (token.IsCancellationRequested)
                    break;
                await action();
            }
        }

        public async UniTask Parallel(
            IEnumerable<Func<UniTask>> actions,
            CancellationToken token = default
        )
        {
            var tasks = new List<UniTask>();
            foreach (var action in actions)
            {
                tasks.Add(action());
            }
            await UniTask.WhenAll(tasks).AttachExternalCancellation(token);
        }

        public async UniTask Retry(
            Func<UniTask> action,
            int maxRetries = 3,
            float retryDelay = 1f,
            CancellationToken token = default
        )
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
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(retryDelay),
                        true,
                        PlayerLoopTiming.Update,
                        token
                    );
                }
            }
        }

        public async UniTask WaitCustom(
            IAsakiWaitSource waitSource,
            CancellationToken token = default
        )
        {
            while (!waitSource.IsCompleted)
            {
                waitSource.Update();
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }

        public IWaitBuilder CreateWaitBuilder()
        {
            return new SceneTest_MockWaitBuilder(this);
        }

        public void CancelAllTasks() { }

        public CancellationToken CreateLinkedToken(CancellationToken externalToken = default)
        {
            return externalToken;
        }

        public void Reset()
        {
            WaitFrameCallCount = 0;
            WaitSecondsCallCount = 0;
            RunTaskCallCount = 0;
        }

        private class SceneTest_MockWaitBuilder : IWaitBuilder
        {
            private readonly IAsakiAsyncService _service;
            private readonly List<Func<CancellationToken, UniTask>> _steps = new();

            public SceneTest_MockWaitBuilder(IAsakiAsyncService service)
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

    /// <summary>
    /// 场景管理测试专用的Mock事件服务
    /// </summary>
    public class SceneTest_MockEventService : IAsakiEventService
    {
        public int PublishCallCount { get; private set; }
        public object LastPublishedEvent { get; private set; }
        public List<object> PublishedEvents { get; } = new();

        public void Publish<T>(T eventData)
            where T : struct, IAsakiEvent
        {
            PublishCallCount++;
            LastPublishedEvent = eventData;
            PublishedEvents.Add(eventData);
        }

        public void Subscribe<T>(IAsakiHandler<T> handler)
            where T : struct, IAsakiEvent { }

        public void Unsubscribe<T>(IAsakiHandler<T> handler)
            where T : struct, IAsakiEvent { }

        public void Dispose() { }

        public void Reset()
        {
            PublishCallCount = 0;
            LastPublishedEvent = null;
            PublishedEvents.Clear();
        }
    }

    /// <summary>
    /// 场景管理测试专用的Mock资源服务
    /// </summary>
    public class SceneTest_MockResourceService : IAsakiResourceService
    {
        public int LoadAsyncCallCount { get; private set; }
        public int ReleaseCallCount { get; private set; }
        public int UnloadUnusedAssetsCallCount { get; private set; }

        public UniTask<ResHandle<T>> LoadAsync<T>(
            string location,
            Action<float> onProgress,
            CancellationToken token
        )
            where T : class
        {
            LoadAsyncCallCount++;
            onProgress?.Invoke(1.0f);
            return UniTask.FromResult(new ResHandle<T>(location, null, this));
        }

        public UniTask<ResHandle<T>> LoadAsync<T>(string location, CancellationToken token)
            where T : class
        {
            LoadAsyncCallCount++;
            return UniTask.FromResult(new ResHandle<T>(location, null, this));
        }

        public async UniTask<ResHandle<Object>> LoadAsync(
            string location,
            Type type,
            Action<float> onProgress,
            CancellationToken token
        )
        {
            throw new NotImplementedException();
        }

        public void Release(string location, Type type)
        {
            ReleaseCallCount++;
        }

        public UniTask<List<ResHandle<T>>> LoadBatchAsync<T>(
            IEnumerable<string> locations,
            Action<float> onProgress,
            CancellationToken token
        )
            where T : class
        {
            LoadAsyncCallCount++;
            onProgress?.Invoke(1.0f);
            return UniTask.FromResult(new List<ResHandle<T>>());
        }

        public UniTask<List<ResHandle<T>>> LoadBatchAsync<T>(
            IEnumerable<string> locations,
            CancellationToken token
        )
            where T : class
        {
            LoadAsyncCallCount++;
            return UniTask.FromResult(new List<ResHandle<T>>());
        }

        public void ReleaseBatch(IEnumerable<string> locations) { }

        public void ReleaseBatch<T>(IEnumerable<string> locations)
            where T : class { }

        public UniTask UnloadUnusedAssets(CancellationToken token = default)
        {
            UnloadUnusedAssetsCallCount++;
            return UniTask.CompletedTask;
        }

        public void SetTimeoutSeconds(int timeoutSeconds) { }

        public void Reset()
        {
            LoadAsyncCallCount = 0;
            ReleaseCallCount = 0;
            UnloadUnusedAssetsCallCount = 0;
        }

        public void OnInit() { }

        public async UniTask OnInitAsync()
        {
            await UniTask.CompletedTask;
        }

        public void OnDispose() { }
    }

    /// <summary>
    /// 场景管理测试专用的Mock场景过渡
    /// </summary>
    public class SceneTest_MockSceneTransition : IAsakiSceneTransition
    {
        public bool EnterAsyncCalled { get; private set; }
        public bool ExitAsyncCalled { get; private set; }
        public bool Disposed { get; private set; }
        public List<float> ProgressValues { get; } = new();
        public float EnterDelayMs { get; set; } = 0;
        public float ExitDelayMs { get; set; } = 0;
        public bool ShouldThrowOnEnter { get; set; }
        public bool ShouldThrowOnExit { get; set; }

        public async UniTask EnterAsync(CancellationToken ct)
        {
            EnterAsyncCalled = true;
            if (ShouldThrowOnEnter)
                throw new InvalidOperationException("Mock enter exception");
            if (EnterDelayMs > 0)
                await UniTask.Delay(TimeSpan.FromMilliseconds(EnterDelayMs), cancellationToken: ct);
        }

        public void OnProgress(float normalizedProgress)
        {
            ProgressValues.Add(normalizedProgress);
        }

        public async UniTask ExitAsync(CancellationToken ct)
        {
            ExitAsyncCalled = true;
            if (ShouldThrowOnExit)
                throw new InvalidOperationException("Mock exit exception");
            if (ExitDelayMs > 0)
                await UniTask.Delay(TimeSpan.FromMilliseconds(ExitDelayMs), cancellationToken: ct);
        }

        public void Dispose()
        {
            Disposed = true;
        }

        public void Reset()
        {
            EnterAsyncCalled = false;
            ExitAsyncCalled = false;
            Disposed = false;
            ProgressValues.Clear();
            EnterDelayMs = 0;
            ExitDelayMs = 0;
            ShouldThrowOnEnter = false;
            ShouldThrowOnExit = false;
        }
    }
}
