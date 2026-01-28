using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Services.Async
{
    public partial class AsakiAsyncProvider
    {
        public UniTask WaitSeconds(
            float seconds,
            CancellationToken token = default(CancellationToken)
        )
        {
            CancellationToken linkedToken = CreateLinkedToken(token);
            if (seconds <= 0)
                return UniTask.CompletedTask;
            return UniTask.Delay(
                TimeSpan.FromSeconds(seconds),
                false,
                PlayerLoopTiming.Update,
                linkedToken
            );
        }

        public UniTask WaitSecondsUnscaled(
            float seconds,
            CancellationToken token = default(CancellationToken)
        )
        {
            CancellationToken linkedToken = CreateLinkedToken(token);
            if (seconds <= 0)
                return UniTask.CompletedTask;
            return UniTask.Delay(
                TimeSpan.FromSeconds(seconds),
                true,
                PlayerLoopTiming.Update,
                linkedToken
            );
        }

        public UniTask WaitFrame(CancellationToken token = default(CancellationToken))
        {
            CancellationToken linkedToken = CreateLinkedToken(token);
            return UniTask.Yield(PlayerLoopTiming.Update, linkedToken);
        }

        public UniTask WaitFrames(int count, CancellationToken token = default(CancellationToken))
        {
            CancellationToken linkedToken = CreateLinkedToken(token);
            if (count <= 0)
                return UniTask.CompletedTask;
            return UniTask.DelayFrame(count, PlayerLoopTiming.Update, linkedToken);
        }

        public UniTask WaitFixedFrame(CancellationToken token = default(CancellationToken))
        {
            CancellationToken linkedToken = CreateLinkedToken(token);
            return UniTask.Yield(PlayerLoopTiming.FixedUpdate, linkedToken);
        }

        public UniTask WaitFixedFrames(
            int count,
            CancellationToken token = default(CancellationToken)
        )
        {
            CancellationToken linkedToken = CreateLinkedToken(token);
            if (count <= 0)
                return UniTask.CompletedTask;
            return UniTask.DelayFrame(count, PlayerLoopTiming.FixedUpdate, linkedToken);
        }

        public UniTask WaitUntil(
            Func<bool> predicate,
            CancellationToken token = default(CancellationToken)
        )
        {
            CancellationToken linkedToken = CreateLinkedToken(token);
            return UniTask.WaitUntil(predicate, PlayerLoopTiming.Update, linkedToken);
        }

        public UniTask WaitWhile(
            Func<bool> predicate,
            CancellationToken token = default(CancellationToken)
        )
        {
            CancellationToken linkedToken = CreateLinkedToken(token);
            return UniTask.WaitWhile(predicate, PlayerLoopTiming.Update, linkedToken);
        }

        public async UniTask<bool> WaitUntil(
            Func<bool> predicate,
            float timeoutSeconds,
            CancellationToken token = default(CancellationToken)
        )
        {
            CancellationToken linkedToken = CreateLinkedToken(token);
            try
            {
                await UniTask
                    .WaitUntil(predicate, PlayerLoopTiming.Update, linkedToken)
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
            CancellationToken token = default(CancellationToken)
        )
        {
            return WaitUntil(() => !predicate(), timeoutSeconds, token);
        }
    }
}
