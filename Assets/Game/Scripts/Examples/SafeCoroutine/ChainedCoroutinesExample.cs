using System;
using System.Collections;
using Asaki.Unity.Services.SafeCoroutine;
using UnityEngine;

namespace Game.Scripts.Examples.SafeCoroutine
{
    /// <summary>
    /// 示例6: 链式协程 - 按顺序执行多个协程
    /// </summary>
    public class ChainedCoroutinesExample : MonoBehaviour
    {
        void Start()
        {
            StartChainedCoroutines();
        }

        private void StartChainedCoroutines()
        {
            this.StartSafeCoroutine(ChainRunner(), OnException, OnCompleted);
        }

        private IEnumerator ChainRunner()
        {
            // 顺序执行多个协程
            yield return RunCoroutineWithTracking(Phase1());
            yield return RunCoroutineWithTracking(Phase2());
            yield return RunCoroutineWithTracking(Phase3());

            Debug.Log("所有阶段完成!");
        }

        private IEnumerator RunCoroutineWithTracking(IEnumerator coroutine)
        {
            bool isDone = false;
            Exception capturedException = null;

            SafeCoroutineRunner.Instance.StartSafeCoroutine(
                coroutine,
                (id, ex) =>
                {
                    capturedException = ex;
                    isDone = true;
                },
                (id, result) =>
                {
                    isDone = true;
                }
            );

            // 等待子协程完成
            while (!isDone)
            {
                yield return null;
            }

            // 如果子协程异常，向上传播
            if (capturedException != null)
            {
                throw new Exception("子协程执行失败", capturedException);
            }
        }

        private IEnumerator Phase1()
        {
            Debug.Log("阶段1: 加载资源");
            yield return new WaitForSeconds(1f);
            Debug.Log("阶段1完成");
        }

        private IEnumerator Phase2()
        {
            Debug.Log("阶段2: 初始化系统");
            yield return new WaitForSeconds(1.5f);
            Debug.Log("阶段2完成");
        }

        private IEnumerator Phase3()
        {
            Debug.Log("阶段3: 启动游戏");
            yield return new WaitForSeconds(1f);
            Debug.Log("阶段3完成");
        }

        private void OnException(string id, Exception ex)
        {
            Debug.LogError($"链式协程异常: {ex.Message}");
        }

        private void OnCompleted(string id, SafeCoroutineResult result)
        {
            Debug.Log($"链式协程完成: {result.State}");
        }
    }
}
