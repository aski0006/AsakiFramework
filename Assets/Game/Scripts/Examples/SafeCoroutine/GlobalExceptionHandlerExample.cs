using System;
using System.Collections;
using Asaki.Unity.Services.SafeCoroutine;
using UnityEngine;

namespace Game.Scripts.Examples.SafeCoroutine
{
    /// <summary>
    /// 示例7: 全局异常处理
    /// </summary>
    public class GlobalExceptionHandlerExample : MonoBehaviour
    {
        void Awake()
        {
            // 设置全局异常处理器
            SafeCoroutineRunner.Instance.SetGlobalExceptionHandler(
                (coroutineId, exception) =>
                {
                    Debug.LogError($"[全局异常] 协程 {coroutineId}");
                    Debug.LogError($"异常: {exception}");

                    // 可以在这里进行：
                    // - 上报到错误追踪系统
                    // - 显示通用错误提示
                    // - 记录到本地日志文件
                }
            );
        }

        void Start()
        {
            // 这些协程如果没有单独设置异常处理，会触发全局处理器
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                this.StartSafeCoroutine(
                    RandomFailingCoroutine(index),
                    null, // 使用全局异常处理器
                    null // 使用全局完成处理
                );
            }
        }

        private IEnumerator RandomFailingCoroutine(int index)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 2f));

            if (UnityEngine.Random.value > 0.5f)
            {
                throw new Exception($"协程 {index} 随机失败!");
            }

            Debug.Log($"协程 {index} 成功完成");
        }
    }
}
