using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.UI;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.UI
{
    public static class AsakiUIScaffolder
    {
        // [修改] 指向主配置
        private const string CONFIG_PATH =
            "Assets/Resources/Asaki/DataTable/AsakiFrameworkSetting.asset";

        // =========================================================
        // 入口 1: 从 Project 窗口的脚本直接生成 (新需求)
        // =========================================================
        [MenuItem("Assets/Asaki/Scaffold UI Object", false, 10)]
        public static void CreateUIFromScript()
        {
            // 1. 校验选中项是否为 C# 脚本
            MonoScript script = Selection.activeObject as MonoScript;
            if (script == null)
            {
                Debug.LogWarning("[AsakiUI] Please select a C# script.");
                return;
            }

            System.Type type = script.GetClass();
            if (type == null || !type.IsSubclassOf(typeof(MonoBehaviour)))
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Selected script must inherit from MonoBehaviour (or AsakiUIWindow).",
                    "OK"
                );
                return;
            }

            // 2. 寻找放置环境 (Canvas)
            Transform canvasTransform = GetActiveCanvasTransform();

            // 3. 创建根对象
            GameObject root = new GameObject(type.Name);
            if (canvasTransform != null)
            {
                root.transform.SetParent(canvasTransform, false);
            }

            // 4. 挂载脚本
            root.AddComponent(type);

            // 注册撤销
            Undo.RegisterCreatedObjectUndo(root, "Create UI from Script");

            // 5. 执行核心生成逻辑
            ProcessScaffolding(root);

            // 6. 选中新生成的对象
            Selection.activeGameObject = root;
        }

        // =========================================================
        // 入口 2: 从 Hierarchy 中选中的 GameObject 生成 (旧功能)
        // =========================================================
        [MenuItem("Asaki/UI/Scaffold UI", false, 12)]
        public static void ScaffoldFromScene()
        {
            GameObject root = Selection.activeGameObject;
            if (root == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a UI Root GameObject.", "OK");
                return;
            }
            ProcessScaffolding(root);
        }

        // =========================================================
        // 入口 3: 从指定 MonoBehaviour 生成
        // =========================================================
        public static void ScaffoldFromTarget(MonoBehaviour target)
        {
            ProcessScaffolding(target.gameObject);
        }

        // =========================================================
        // 核心生成逻辑 (复用)
        // =========================================================
        private static void ProcessScaffolding(GameObject root)
        {
            var scripts = root.GetComponents<MonoBehaviour>();
            if (scripts.Length == 0)
                return;
            MonoBehaviour targetScript = scripts[0];
            System.Type type = targetScript.GetType();

            // [修改] 加载主配置
            AsakiFrameworkSetting mainFrameworkSetting =
                AssetDatabase.LoadAssetAtPath<AsakiFrameworkSetting>(CONFIG_PATH);
            if (mainFrameworkSetting == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    $"AsakiFrameworkSetting not found at {CONFIG_PATH}",
                    "OK"
                );
                return;
            }

            // 确保 Root 名称一致性
            if (root.name != type.Name)
            {
                Undo.RecordObject(root, "Rename UI Root");
                root.name = type.Name;
            }

            // 确保有 RectTransform
            if (root.GetComponent<RectTransform>() == null)
                root.AddComponent<RectTransform>();

            // 1. 收集字段与属性
            var fields = type.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            var buildList = new List<BuildItem>();

            foreach (FieldInfo field in fields)
            {
                AsakiUIBuilderAttribute attr = field.GetCustomAttribute<AsakiUIBuilderAttribute>();
                if (attr != null)
                {
                    buildList.Add(new BuildItem { Field = field, Attr = attr });
                }
            }

            // 2. 排序 (Order 越小越先生成 -> 被压在下面; Order 越大越后生成 -> 浮在上面)
            var sortedList = buildList.OrderBy(x => x.Attr.Order).ToList();

            // 3. 循环生成
            foreach (BuildItem item in sortedList)
            {
                FieldInfo field = item.Field;
                AsakiUIBuilderAttribute attr = item.Attr;

                // 解析名称
                string nodeName = !string.IsNullOrEmpty(attr.Name)
                    ? attr.Name
                    : SanitizeFieldName(field.Name);

                // 解析容器
                Transform container = root.transform;
                if (!string.IsNullOrEmpty(attr.Parent))
                {
                    container = GetOrCreateContainer(root.transform, attr.Parent);
                }

                // 检查已存在
                Transform existing = container.Find(nodeName);
                if (existing != null)
                {
                    // 仅更新顺序和引用
                    existing.SetAsLastSibling();
                    AssignReference(field, targetScript, existing.gameObject);
                    continue;
                }

                // 实例化
                GameObject prefab = null;
                if (attr.Type == AsakiUIWidgetType.Custom)
                {
                    Debug.LogWarning(
                        $"[AsakiUI] Custom prefab loading not implemented for {nodeName}"
                    );
                    continue;
                }
                else
                {
                    // [修改] 从主配置的 UIConfig 中获取模板
                    // 假设 AsakiUIConfig 已实现 GetTemplate 方法
                    prefab = mainFrameworkSetting.UIConfig.GetTemplate(attr.Type);
                }

                if (prefab != null)
                {
                    GameObject instance = (GameObject)
                        PrefabUtility.InstantiatePrefab(prefab, container);
                    instance.name = nodeName;

                    RectTransform rect = instance.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.anchoredPosition3D = Vector3.zero;
                        rect.localScale = Vector3.one;
                    }

                    instance.transform.SetAsLastSibling(); // 确保 Order 生效
                    AssignReference(field, targetScript, instance);

                    // 注册撤销
                    Undo.RegisterCreatedObjectUndo(instance, "Create UI Widget");
                }
            }

            Debug.Log($"[AsakiUI] Scaffolding complete for {root.name}");
        }

        // =========================================================
        // 辅助方法
        // =========================================================

        private class BuildItem
        {
            public FieldInfo Field;
            public AsakiUIBuilderAttribute Attr;
        }

        private static Transform GetActiveCanvasTransform()
        {
            // 优先找当前选中的 Canvas
            if (Selection.activeGameObject != null)
            {
                Canvas c = Selection.activeGameObject.GetComponentInParent<Canvas>();
                if (c != null)
                    return c.transform;
            }
            // 否则找场景里第一个 Canvas
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            return canvas != null ? canvas.transform : null;
        }

        private static string SanitizeFieldName(string fieldName)
        {
            string name = fieldName;
            if (name.StartsWith("_"))
                name = name.Substring(1);
            if (name.Length > 0)
                name = char.ToUpper(name[0]) + name.Substring(1);
            return name;
        }

        private static Transform GetOrCreateContainer(Transform root, string path)
        {
            string[] parts = path.Split('/');
            Transform current = root;

            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;

                Transform child = current.Find(part);
                if (child == null)
                {
                    GameObject go = new GameObject(part, typeof(RectTransform));
                    go.transform.SetParent(current, false);

                    RectTransform rect = go.GetComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    rect.localScale = Vector3.one;

                    child = go.transform;
                    Undo.RegisterCreatedObjectUndo(go, "Create UI Container");
                }
                current = child;
            }
            return current;
        }

        private static void AssignReference(
            FieldInfo field,
            MonoBehaviour script,
            GameObject instance
        )
        {
            if (IsSerialized(field))
            {
                Component comp = instance.GetComponent(field.FieldType);
                if (comp != null)
                {
                    field.SetValue(script, comp);
                    EditorUtility.SetDirty(script);
                }
                else if (field.FieldType == typeof(GameObject))
                {
                    field.SetValue(script, instance);
                    EditorUtility.SetDirty(script);
                }
            }
        }

        private static bool IsSerialized(FieldInfo field)
        {
            return field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
        }
    }
}
