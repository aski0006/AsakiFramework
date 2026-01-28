using Asaki.Core.Context.Resolvers;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Utilities.Tools
{
    public static class AsakiSceneContextCreator
    {
        // 快捷键：Ctrl+Shift+C (可根据喜好修改)
        // 菜单路径：GameObject -> Asaki -> Create Scene Context
        [MenuItem("GameObject/Asaki/Create Scene Context #%c", false, 10)]
        public static void CreateSceneContext()
        {
            // 1. 检查当前场景是否已存在 SceneContext
            // 使用 FindFirstObjectByType (Unity 2023+) 或 FindObjectOfType
#if UNITY_2023_1_OR_NEWER
            AsakiSceneContext existingCtx = Object.FindFirstObjectByType<AsakiSceneContext>();
#else
            var existingCtx = Object.FindObjectOfType<AsakiSceneContext>();
#endif

            if (existingCtx != null)
            {
                // 2. 如果存在，发出警告并选中它
                EditorUtility.DisplayDialog(
                    "Asaki Framework",
                    $"场景中已存在上下文组件：\n'{existingCtx.name}'\n\nAsaki 架构要求每个场景只能有一个 Scene Context。",
                    "OK"
                );

                Selection.activeGameObject = existingCtx.gameObject;
                EditorGUIUtility.PingObject(existingCtx.gameObject);
                return;
            }

            // 3. 创建新的 GameObject
            GameObject go = new GameObject("[SceneContext]");

            // 4. 注册 Undo (允许 Ctrl+Z 撤销)
            Undo.RegisterCreatedObjectUndo(go, "Create Asaki Scene Context");

            // 5. 添加组件
            go.AddComponent<AsakiSceneContext>();

            // 6. 设为选中状态并高亮
            Selection.activeGameObject = go;

            // 7. (可选) 自动将其移动到层级面板顶部，便于管理
            go.transform.SetSiblingIndex(0);

            Debug.Log("[Asaki] Scene Context created successfully.");
        }

        // 验证函数：确保只有在没有选中 Context 时才启用菜单（可选，目前始终启用以提供提示）
        // [MenuItem("GameObject/Asaki/Create Scene Context", true)]
        // private static bool ValidateCreateSceneContext() { ... }
    }
}
