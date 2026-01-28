using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Asaki.Unity.Extensions
{
    public static class AsakiTaskExtensions
    {
        /// <summary>
        /// 安全等待任务完成或取消令牌触发。
        /// <para>解决问题：</para>
        /// <para>1. 即使底层任务无法取消（如资源加载中），也能立即响应用户取消。</para>
        /// <para>2. 避免 CancellationTokenRegistration 的内存泄漏。</para>
        /// </summary>
        public static async Task<T> WaitAsync<T>(this Task<T> task, CancellationToken token)
        {
            if (task.IsCompleted)
                return await task;

            token.ThrowIfCancellationRequested();

            // 使用 bool 作为占位符，RunContinuationsAsynchronously 防止死锁
            var tcs = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

            // 注册取消回调：当 token 取消时，让 tcs 完成
            using (token.Register(() => tcs.TrySetResult(true)))
            {
                // 等待 原始任务 OR 取消信号
                Task completedTask = await Task.WhenAny(task, tcs.Task);

                if (completedTask == task)
                {
                    return await task; // 原始任务完成
                }
                else
                {
                    throw new OperationCanceledException(token); // 取消信号触发
                }
            }
        }

        public static void FireAndForget(this Task task)
        {
            if (task.IsFaulted)
                Debug.LogException(task.Exception);
        }
    }
}
