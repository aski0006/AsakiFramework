using UnityEngine;

namespace Asaki.Unity.Services.Scene.SceneManagement.Scripts.Examples.SafeCoroutine
{
    /// <summary>
    /// 示例8: 测试入口
    /// </summary>
    public class SafeCoroutineTestRunner : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("=== SafeCoroutine 测试开始 ===");

            // 测试1: 非MonoBehaviour类
            var nonMonoService = new NonMonoBehaviourExample();
            nonMonoService.StartProcessing();

            // 测试2: 游戏任务服务
            var questService = new GameQuestService();
            questService.StartQuest("Quest_001");

            Debug.Log("=== SafeCoroutine 测试已启动 ===");
        }
    }
}
