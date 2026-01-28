using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Asaki.Core.Logging;

namespace Asaki.Unity.Services.Async
{
    [AsyncMethodBuilder(typeof(AsakiTaskVoidMethodBuilder))]
    public readonly struct AsakiTaskVoid
    {
        public void Forget() { }
    }

    public struct AsakiTaskVoidMethodBuilder
    {
        public static AsakiTaskVoidMethodBuilder Create()
        {
            return default(AsakiTaskVoidMethodBuilder);
        }

        public AsakiTaskVoid Task => default(AsakiTaskVoid);

        [DebuggerHidden]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        public void SetResult() { }

        public void SetException(Exception exception)
        {
            AsakiTaskExceptionLogger.Log(exception);
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter,
            ref TStateMachine stateMachine
        )
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter,
            ref TStateMachine stateMachine
        )
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }
    }

    internal static class AsakiTaskExceptionLogger
    {
        public static void Log(Exception ex)
        {
            ALog.Error($"[AsakiAsync] Task error: {ex.Message}", ex);
        }
    }
}
