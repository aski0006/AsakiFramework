using System;
using System.Collections;
using Asaki.Unity.Services.SafeCoroutine;
using UnityEngine;

namespace Game.Examples.SafeCoroutine
{
    /// <summary>
    /// 示例2: 异常处理示例 - 展示如何捕获协程中的异常
    /// </summary>
    public class SafeCoroutineExceptionExample : MonoBehaviour
    {
        void Start()
        {
            // 启动一个会抛出异常的协程
            SafeCoroutineRunner.Instance.StartSafeCoroutine(
                CoroutineWithException(),
                OnException, // 异常会被这里捕获
                OnCompleted
            );

            // 启动一个嵌套异常协程
            this.StartSafeCoroutine(NestedCoroutineWithException(), OnException, OnCompleted);
        }

        private IEnumerator CoroutineWithException()
        {
            Debug.Log("步骤1: 正常执行");
            yield return new WaitForSeconds(0.5f);

            Debug.Log("步骤2: 准备抛出异常");
            yield return new WaitForSeconds(0.5f);

            // 抛出异常 - 会被SafeCoroutine捕获
            throw new InvalidOperationException("这是一个测试异常!");
        }

        private IEnumerator NestedCoroutineWithException()
        {
            Debug.Log("嵌套协程开始");
            yield return new WaitForSeconds(0.3f);

            // 调用可能出错的方法
            yield return DoSomethingRisky();

            Debug.Log("这行不会执行，因为上面已经异常");
        }

        private IEnumerator DoSomethingRisky()
        {
            yield return new WaitForSeconds(0.3f);

            // 模拟空引用异常
            string nullString = null;
            int length = nullString.Length; // NullReferenceException

            yield return null;
        }

        private void OnException(string coroutineId, Exception exception)
        {
            Debug.LogError($"[异常捕获] 协程 {coroutineId}");
            Debug.LogError($"异常类型: {exception.GetType().Name}");
            Debug.LogError($"异常消息: {exception.Message}");
            Debug.LogError($"堆栈跟踪: {exception.StackTrace}");

            // 在这里可以进行：
            // 1. 记录日志到文件
            // 2. 上报到服务器
            // 3. 显示错误UI
            // 4. 尝试恢复或重试
        }

        private void OnCompleted(string coroutineId, SafeCoroutineResult result)
        {
            if (result.State == SafeCoroutineState.Failed)
            {
                Debug.LogWarning($"协程 {coroutineId} 执行失败");
            }
            else if (result.State == SafeCoroutineState.Completed)
            {
                Debug.Log($"协程 {coroutineId} 成功完成");
            }
        }
    }
}
