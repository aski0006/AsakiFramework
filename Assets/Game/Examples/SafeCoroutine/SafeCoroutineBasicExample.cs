// ============================================================================
// SafeCoroutine - 使用示例
// ============================================================================
// 本文件包含完整的SafeCoroutine使用示例，展示各种使用场景
// ============================================================================

using System;
using System.Collections;
using Asaki.Unity.Services.SafeCoroutine;
using UnityEngine;

namespace Game.Examples.SafeCoroutine
{
    /// <summary>
    /// 示例1: 基础用法 - 在MonoBehaviour中使用
    /// </summary>
    public class SafeCoroutineBasicExample : MonoBehaviour
    {
        private SafeCoroutineHandle _currentCoroutine;

        void Start()
        {
            // 方式1: 使用SafeCoroutineRunner直接启动
            _currentCoroutine = SafeCoroutineRunner.Instance.StartSafeCoroutine(
                SimpleCoroutine(),
                OnCoroutineException, // 异常回调
                OnCoroutineCompleted // 完成回调
            );

            // 方式2: 使用扩展方法启动（更简洁）
            this.StartSafeCoroutine(
                AnotherCoroutine(),
                (id, ex) => Debug.LogError($"协程{id}异常: {ex.Message}"),
                (id, result) => Debug.Log($"协程{id}完成，成功: {result.Success}")
            );
        }

        private IEnumerator SimpleCoroutine()
        {
            Debug.Log("协程开始");
            yield return new WaitForSeconds(1f);
            Debug.Log("等待1秒后");
            yield return new WaitForSeconds(1f);
            Debug.Log("协程结束");
        }

        private IEnumerator AnotherCoroutine()
        {
            for (int i = 0; i < 5; i++)
            {
                Debug.Log($"步骤 {i}");
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void OnCoroutineException(string coroutineId, Exception exception)
        {
            Debug.LogError($"[SafeCoroutine] {coroutineId} 发生异常: {exception}");
        }

        private void OnCoroutineCompleted(string coroutineId, SafeCoroutineResult result)
        {
            Debug.Log($"[SafeCoroutine] {coroutineId} 执行完成，状态: {result.State}");
        }

        void OnDestroy()
        {
            // 组件销毁时停止协程
            _currentCoroutine?.Stop();
        }
    }

    /// <summary>
    /// 示例3: 非MonoBehaviour类中使用 - 突破原生限制
    /// </summary>
    public class NonMonoBehaviourExample
    {
        private SafeCoroutineHandle _coroutineHandle;

        /// <summary>
        /// 普通类启动协程
        /// </summary>
        public void StartProcessing()
        {
            Debug.Log("[NonMonoBehaviour] 开始处理");

            // 非MonoBehaviour类也能启动协程！
            _coroutineHandle = SafeCoroutineRunner.Instance.StartSafeCoroutine(
                ProcessingCoroutine(),
                OnException,
                OnCompleted
            );
        }

        private IEnumerator ProcessingCoroutine()
        {
            Debug.Log("[NonMonoBehaviour] 步骤1: 初始化");
            yield return new WaitForSeconds(0.5f);

            Debug.Log("[NonMonoBehaviour] 步骤2: 处理数据");
            yield return new WaitForSeconds(1f);

            Debug.Log("[NonMonoBehaviour] 步骤3: 保存结果");
            yield return new WaitForSeconds(0.5f);

            Debug.Log("[NonMonoBehaviour] 处理完成!");
        }

        public void StopProcessing()
        {
            _coroutineHandle?.Stop();
        }

        private void OnException(string id, Exception ex)
        {
            Debug.LogError($"[NonMonoBehaviour] 处理异常: {ex.Message}");
        }

        private void OnCompleted(string id, SafeCoroutineResult result)
        {
            Debug.Log($"[NonMonoBehaviour] 处理完成，状态: {result.State}");
        }
    }

    /// <summary>
    /// 示例5: 服务类模式 - 游戏系统使用SafeCoroutine
    /// </summary>
    public class GameQuestService
    {
        private SafeCoroutineHandle _questCoroutine;

        /// <summary>
        /// 开始一个任务
        /// </summary>
        public void StartQuest(string questId)
        {
            _questCoroutine = SafeCoroutineRunner.Instance.StartSafeCoroutine(
                QuestFlow(questId),
                (id, ex) =>
                {
                    Debug.LogError($"任务 {questId} 执行异常: {ex.Message}");
                    // 可以在这里进行任务失败处理
                    OnQuestFailed(questId, ex);
                },
                (id, result) =>
                {
                    if (result.State == SafeCoroutineState.Completed)
                    {
                        OnQuestCompleted(questId);
                    }
                    else if (result.State == SafeCoroutineState.Cancelled)
                    {
                        OnQuestCancelled(questId);
                    }
                }
            );
        }

        private IEnumerator QuestFlow(string questId)
        {
            // 任务开始
            Debug.Log($"[Quest] {questId} 开始");
            yield return new WaitForSeconds(1f);

            // 阶段1: 收集物品
            Debug.Log($"[Quest] {questId} 阶段1: 收集物品");
            yield return CollectItems();

            // 阶段2: 击败敌人
            Debug.Log($"[Quest] {questId} 阶段2: 击败敌人");
            yield return DefeatEnemies();

            // 阶段3: 提交任务
            Debug.Log($"[Quest] {questId} 阶段3: 提交任务");
            yield return SubmitQuest();

            Debug.Log($"[Quest] {questId} 完成!");
        }

        private IEnumerator CollectItems()
        {
            int itemsCollected = 0;
            int itemsNeeded = 3;

            while (itemsCollected < itemsNeeded)
            {
                // 模拟等待玩家收集
                yield return new WaitForSeconds(2f);
                itemsCollected++;
                Debug.Log($"收集进度: {itemsCollected}/{itemsNeeded}");
            }
        }

        private IEnumerator DefeatEnemies()
        {
            Debug.Log("开始战斗...");
            yield return new WaitForSeconds(3f);
            Debug.Log("战斗胜利!");
        }

        private IEnumerator SubmitQuest()
        {
            Debug.Log("提交任务中...");
            yield return new WaitForSeconds(1f);
        }

        private void OnQuestCompleted(string questId)
        {
            Debug.Log($"[Quest] {questId} 成功完成，发放奖励!");
        }

        private void OnQuestFailed(string questId, Exception ex)
        {
            Debug.LogError($"[Quest] {questId} 失败: {ex.Message}");
        }

        private void OnQuestCancelled(string questId)
        {
            Debug.Log($"[Quest] {questId} 被取消");
        }

        public void CancelQuest()
        {
            _questCoroutine?.Stop();
        }
    }

}
