using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Unity.Utils
{
    /// <summary>
    /// [异步桥接器]
    /// 屏蔽 UniTask 与原生 Task/Coroutine 的差异。
    /// </summary>
    public static class AsakiResKitAsyncBridge
    {
        /// <summary>
        /// 将 ResourceRequest (Unity 异步加载) 转换为标准 Task。
        /// </summary>
        public static Task<Object> ToTask(
            this ResourceRequest request,
            CancellationToken token = default(CancellationToken)
        )
        {
#if ASAKI_USE_UNITASK
            // === 方案 A: 有 UniTask (高性能，零分配等待) ===
            // UniTask 能够直接 await ResourceRequest，并且支持 Token 取消
            return request.ToUniTask(cancellationToken: token).AsTask();
#else
            // === 方案 B: 原生兼容 (使用 TCS 包装) ===
            var tcs = new TaskCompletionSource<Object>();

            // 1. 注册取消回调
            if (token.IsCancellationRequested)
            {
                tcs.SetCanceled();
                return tcs.Task;
            }

            // 注册 Token 取消时的行为
            CancellationTokenRegistration registration = token.Register(() =>
            {
                tcs.TrySetCanceled();
                // 注意：ResourceRequest 很难真正从底层取消，这里只是断开 Task 连接
            });

            // 2. 监听 Unity 完成事件
            request.completed += _ =>
            {
                registration.Dispose();
                if (token.IsCancellationRequested)
                {
                    tcs.TrySetCanceled();
                    return;
                }

                if (request.asset == null)
                {
                    // 可以在这里决定是返回 null 还是抛出异常
                    tcs.TrySetResult(null);
                }
                else
                {
                    tcs.TrySetResult(request.asset);
                }
            };

            return tcs.Task;
#endif
        }

        /// <summary>
        /// 将 Addressables 的 Handle 或其他 Task 统一化
        /// </summary>
        public static Task<T> ConvertTask<T>(Task<T> task)
        {
            // 如果已经是 Task，直接返回
            return task;
        }

#if ASAKI_USE_UNITASK
        // 专门用于将 UniTask 转回 Task 的重载
        public static Task<T> ConvertTask<T>(UniTask<T> task)
        {
            return task.AsTask();
        }
#endif
    }
}
