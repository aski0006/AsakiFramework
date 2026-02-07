using System.IO;
using Asaki.Core.Configs;
using Asaki.Core.Context.Resolvers;
using Asaki.Unity.Bootstrapper;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Asaki.Editor.Bootstrapper
{
    /// <summary>
    /// AsakiBootstrapper 编辑器工具
    /// 提供场景验证、自动修复、配置资源管理等功能
    /// </summary>
    public static class AsakiBootstrapperEditor
    {
        private const string CONFIG_PATH = "Assets/Resources/AsakiConfig.asset";
        private const string BOOTSTRAPPER_GO_NAME = "[AsakiBootstrapper]";

        // ===================================================================
        // 菜单项
        // ===================================================================

        [MenuItem("Asaki/Bootstrapper/Validate Current Scene", false, 100)]
        public static void ValidateCurrentScene()
        {
            var result = ValidateScene(SceneManager.GetActiveScene());
            if (result.IsValid)
            {
                EditorUtility.DisplayDialog(
                    "Asaki Framework",
                    "当前场景配置正确！\n\n" +
                    $"- Bootstrapper: {(result.HasBootstrapper ? "存在" : "不存在（运行时自动创建）")}\n" +
                    $"- Config Asset: {(result.HasConfig ? "存在" : "不存在")}\n" +
                    $"- Scene Context: {(result.HasSceneContext ? "存在" : "不存在（可选）")}",
                    "OK"
                );
            }
            else
            {
                bool fix = EditorUtility.DisplayDialog(
                    "Asaki Framework",
                    "检测到场景配置问题：\n\n" + result.GetErrorMessage(),
                    "自动修复",
                    "取消"
                );

                if (fix)
                {
                    FixSceneIssues(result);
                }
            }
        }

        [MenuItem("Asaki/Bootstrapper/Add Bootstrapper to Scene", false, 101)]
        public static void AddBootstrapperToScene()
        {
            var scene = SceneManager.GetActiveScene();

            // 检查是否已存在
            var existing = Object.FindFirstObjectByType<AsakiBootstrapper>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog(
                    "Asaki Framework",
                    $"场景中已存在 Bootstrapper：'{existing.gameObject.name}'",
                    "OK"
                );
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            // 创建 Bootstrapper GameObject
            var go = new GameObject(BOOTSTRAPPER_GO_NAME);
            var bootstrapper = go.AddComponent<AsakiBootstrapper>();

            // 确保配置资源存在
            var config = GetOrCreateConfigAsset();
            bootstrapper.GetType()
                .GetField("_config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(bootstrapper, config);

            Undo.RegisterCreatedObjectUndo(go, "Add AsakiBootstrapper");

            EditorUtility.DisplayDialog(
                "Asaki Framework",
                "Bootstrapper 已添加到当前场景！\n\n" +
                "注意：Bootstrapper 会在场景加载时自动设置为 DontDestroyOnLoad，\n" +
                "切换到其他场景时无需再次添加。",
                "OK"
            );

            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(scene);
        }

        [MenuItem("Asaki/Bootstrapper/Create Config Asset", false, 102)]
        public static AsakiConfig CreateConfigAsset()
        {
            var config = GetOrCreateConfigAsset();
            EditorUtility.DisplayDialog(
                "Asaki Framework",
                $"配置资源已创建/更新：\n{CONFIG_PATH}",
                "OK"
            );
            Selection.activeObject = config;
            return config;
        }

        [MenuItem("Asaki/Bootstrapper/Remove Bootstrapper from Scene", false, 200)]
        public static void RemoveBootstrapperFromScene()
        {
            var bootstrapper = Object.FindFirstObjectByType<AsakiBootstrapper>();
            if (bootstrapper == null)
            {
                EditorUtility.DisplayDialog(
                    "Asaki Framework",
                    "当前场景中没有 Bootstrapper。",
                    "OK"
                );
                return;
            }

            bool confirm = EditorUtility.DisplayDialog(
                "Asaki Framework",
                $"确定要删除 '{bootstrapper.gameObject.name}' 吗？\n\n" +
                "这将从场景中移除 Bootstrapper 组件。",
                "删除",
                "取消"
            );

            if (confirm)
            {
                Undo.DestroyObjectImmediate(bootstrapper.gameObject);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }

        // ===================================================================
        // PlayMode 自动初始化
        // ===================================================================

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // 进入 PlayMode 前自动确保配置资源存在
                EnsureConfigAssetExists();
            }
        }

        // ===================================================================
        // 场景验证
        // ===================================================================

        public struct ValidationResult
        {
            public bool IsValid;
            public bool HasBootstrapper;
            public bool HasConfig;
            public bool HasSceneContext;
            public string ErrorMessage;

            public string GetErrorMessage()
            {
                if (IsValid) return string.Empty;

                var sb = new System.Text.StringBuilder();
                if (!HasConfig)
                    sb.AppendLine("- 缺少 AsakiConfig 资源（必须）");

                return sb.ToString();
            }
        }

        public static ValidationResult ValidateScene(Scene scene)
        {
            var result = new ValidationResult
            {
                HasBootstrapper = Object.FindFirstObjectByType<AsakiBootstrapper>() != null,
                HasConfig = GetConfigAsset() != null,
                HasSceneContext = Object.FindFirstObjectByType<AsakiSceneContext>() != null
            };

            // 运行时只需要 Config 存在，Bootstrapper 可以自动创建
            result.IsValid = result.HasConfig;

            return result;
        }

        // ===================================================================
        // 自动修复
        // ===================================================================

        public static void FixSceneIssues(ValidationResult result)
        {
            if (!result.HasConfig)
            {
                GetOrCreateConfigAsset();
            }

            EditorUtility.DisplayDialog(
                "Asaki Framework",
                "场景问题已修复！\n\n" +
                "配置资源已创建。Bootstrapper 将在运行时自动创建。\n\n" +
                "如需手动添加 Bootstrapper，请使用菜单：\n" +
                "Asaki -> Bootstrapper -> Add Bootstrapper to Scene",
                "OK"
            );
        }

        // ===================================================================
        // 配置资源管理
        // ===================================================================

        /// <summary>
        /// 获取或创建 AsakiConfig 资源
        /// </summary>
        public static AsakiConfig GetOrCreateConfigAsset()
        {
            var config = GetConfigAsset();
            if (config != null)
                return config;

            // 确保目录存在
            string directory = Path.GetDirectoryName(CONFIG_PATH);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 创建资源
            config = ScriptableObject.CreateInstance<AsakiConfig>();
            AssetDatabase.CreateAsset(config, CONFIG_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[AsakiBootstrapperEditor] Created config asset at: {CONFIG_PATH}");
            return config;
        }

        /// <summary>
        /// 获取已存在的 AsakiConfig 资源
        /// </summary>
        public static AsakiConfig GetConfigAsset()
        {
            // 尝试从 Resources 加载
            var config = Resources.Load<AsakiConfig>("AsakiConfig");
            if (config != null)
                return config;

            // 尝试从 AssetDatabase 查找
            string[] guids = AssetDatabase.FindAssets("t:AsakiConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<AsakiConfig>(path);
            }

            return null;
        }

        /// <summary>
        /// 确保配置资源存在（用于 PlayMode 自动初始化）
        /// </summary>
        public static void EnsureConfigAssetExists()
        {
            if (GetConfigAsset() == null)
            {
                Debug.Log("[AsakiBootstrapperEditor] Auto-creating config asset before entering PlayMode...");
                GetOrCreateConfigAsset();
            }
        }
    }
}
