using System.Collections;
using Asaki.Unity.Services.SafeCoroutine;
using UnityEngine;

namespace Game.Examples.SafeCoroutine
{
    /// <summary>
    /// 示例4: 协程生命周期控制 - 暂停、恢复、停止
    /// </summary>
    public class SafeCoroutineLifecycleExample : MonoBehaviour
    {
        private SafeCoroutineHandle _longRunningCoroutine;

        void Start()
        {
            // 启动一个长时间运行的协程
            _longRunningCoroutine = this.StartSafeCoroutine(
                LongRunningTask(),
                null,
                (id, result) => Debug.Log($"任务完成: {result.State}")
            );
        }

        void Update()
        {
            // 按P暂停
            if (Input.GetKeyDown(KeyCode.P))
            {
                _longRunningCoroutine?.Pause();
                Debug.Log("协程已暂停");
            }

            // 按R恢复
            if (Input.GetKeyDown(KeyCode.R))
            {
                _longRunningCoroutine?.Resume();
                Debug.Log("协程已恢复");
            }

            // 按S停止
            if (Input.GetKeyDown(KeyCode.S))
            {
                _longRunningCoroutine?.Stop();
                Debug.Log("协程已停止");
            }
        }

        private IEnumerator LongRunningTask()
        {
            int step = 0;
            while (true)
            {
                Debug.Log($"执行任务步骤 {step++}");
                yield return new WaitForSeconds(1f);
            }
        }
    }
}
