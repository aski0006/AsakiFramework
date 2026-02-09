using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Asaki.Editor.Bootstrapper
{
    /// <summary>
    /// 场景切换处理器
    /// 处理编辑器中场景切换时的框架状态管理
    /// </summary>
    [InitializeOnLoad]
    public static class AsakiSceneSwitchHandler
    {
        // 记录上一个场景是否有 Bootstrapper
        private static bool _lastSceneHadBootstrapper;
        private static string _lastSceneName;

        static AsakiSceneSwitchHandler()
        {
            // 注册场景切换事件
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosing += OnSceneClosing;

            // 初始化状态
            _lastSceneName = SceneManager.GetActiveScene().name;
            _lastSceneHadBootstrapper =
                Object.FindFirstObjectByType<Asaki.Unity.Bootstrapper.AsakiBootstrapper>() != null;
        }

        /// <summary>
        /// 场景打开时
        /// </summary>
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            // 延迟一帧执行，确保场景完全加载
            EditorApplication.delayCall += () =>
            {
                CheckSceneConfiguration(scene);
            };
        }

        /// <summary>
        /// 场景关闭时
        /// </summary>
        private static void OnSceneClosing(Scene scene, bool removingScene)
        {
            _lastSceneName = scene.name;
            _lastSceneHadBootstrapper =
                Object.FindFirstObjectByType<Asaki.Unity.Bootstrapper.AsakiBootstrapper>() != null;
        }

        /// <summary>
        /// 检查场景配置
        /// </summary>
        private static void CheckSceneConfiguration(Scene scene)
        {
            // 只在 EditMode 下检查
            if (Application.isPlaying)
                return;

            var result = AsakiBootstrapperEditor.ValidateScene(scene);

            // 如果场景配置不正确，显示提示
            if (!result.IsValid)
            {
                ShowSceneWarning(scene, result);
            }
        }

        /// <summary>
        /// 显示场景警告
        /// </summary>
        private static void ShowSceneWarning(
            Scene scene,
            AsakiBootstrapperEditor.ValidationResult result
        )
        {
            // 使用延迟调用避免在场景加载过程中弹出对话框
            EditorApplication.delayCall += () =>
            {
                // 检查用户是否选择忽略此场景的警告
                string ignoreKey = $"Asaki_IgnoreSceneWarning_{scene.name}";
                if (EditorPrefs.GetBool(ignoreKey, false))
                    return;

                int option = EditorUtility.DisplayDialogComplex(
                    "Asaki Framework - 场景配置检查",
                    $"场景 '{scene.name}' 缺少必要的 Asaki 框架配置：\n\n"
                        + result.GetErrorMessage()
                        + "\n"
                        + "是否自动修复？",
                    "自动修复",
                    "忽略此场景",
                    "打开配置窗口"
                );

                switch (option)
                {
                    case 0: // 自动修复
                        AsakiBootstrapperEditor.FixSceneIssues(result);
                        EditorSceneManager.MarkSceneDirty(scene);
                        break;

                    case 1: // 忽略此场景
                        EditorPrefs.SetBool(ignoreKey, true);
                        Debug.Log(
                            $"[Asaki] 已忽略场景 '{scene.name}' 的配置警告。可以在 EditorPrefs 中删除键 '{ignoreKey}' 来恢复。"
                        );
                        break;

                    case 2: // 打开配置窗口
                        AsakiBootstrapperWindow.ShowWindow();
                        break;
                }
            };
        }

        /// <summary>
        /// 清除所有忽略的设置
        /// </summary>
        [MenuItem("Asaki/Bootstrapper/Clear Ignored Warnings", false, 300)]
        public static void ClearIgnoredWarnings()
        {
            // 查找所有 Asaki 相关的 EditorPrefs 并删除
            // 注意：Unity 没有提供枚举 EditorPrefs 的方法，这里只是示例
            // 实际项目中可能需要手动管理这些键

            EditorUtility.DisplayDialog(
                "Asaki Framework",
                "请在 EditorPrefs 中手动删除以 'Asaki_IgnoreSceneWarning_' 开头的键值。",
                "OK"
            );
        }
    }
}
