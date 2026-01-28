using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Asaki.Editor.Utilities.Tools.MissingFinder
{
    public class AsakiMissingFinderWindow : EditorWindow
    {
        // === 数据结构 ===
        private class MissingEntry
        {
            public Object ContextObject; // 出问题的对象 (GameObject 或 SO)
            public Component Component; // 出问题的组件 (如果是场景物体)
            public string PropertyPath; // 属性路径 (如 "m_MyData.Array.data[0]")
            public string ErrorType; // "Missing Script" 或 "Missing Reference"
        }

        private List<MissingEntry> _entries = new List<MissingEntry>();
        private Vector2 _scrollPos;

        [SerializeField]
        private bool _includeProject; // 是否扫描 Project (耗时操作)

        [MenuItem("Asaki/Tools/Missing References Finder")]
        public static void ShowWindow()
        {
            AsakiMissingFinderWindow wnd = GetWindow<AsakiMissingFinderWindow>("Missing Finder");
            wnd.minSize = new Vector2(600, 400);
            wnd.Show();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawToolbar();
            DrawList();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Asaki Missing Asset Scanner", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "检测两种错误：\n"
                    + "1. Missing Script: GameObject 上挂载的脚本文件被删除。\n"
                    + "2. Missing Reference: 属性引用的资源被删除 (显示为 Type Mismatch 或 Missing)。",
                MessageType.Info
            );
        }

        private void DrawToolbar()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Scan Current Scene", GUILayout.Height(30)))
            {
                ScanScene();
            }

            if (GUILayout.Button("Scan Entire Project (Prefabs & SOs)", GUILayout.Height(30)))
            {
                ScanProject();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            GUILayout.Label($"Found Issues: {_entries.Count}", EditorStyles.boldLabel);
        }

        private void DrawList()
        {
            if (_entries.Count == 0)
                return;

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            for (int i = 0; i < _entries.Count; i++)
            {
                MissingEntry entry = _entries[i];
                DrawEntry(entry, i);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawEntry(MissingEntry entry, int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            // 1. 图标状态
            GUI.color = Color.red;
            GUILayout.Label(
                EditorGUIUtility.IconContent("console.erroricon"),
                GUILayout.Width(20),
                GUILayout.Height(20)
            );
            GUI.color = Color.white;

            // 2. 对象引用 (点击可 Ping)
            EditorGUILayout.ObjectField(
                entry.ContextObject,
                typeof(Object),
                true,
                GUILayout.Width(200)
            );

            // 3. 错误详情
            EditorGUILayout.BeginVertical();
            GUILayout.Label(entry.ErrorType, EditorStyles.boldLabel);

            if (entry.Component != null)
            {
                GUILayout.Label(
                    $"Component: {entry.Component.GetType().Name}",
                    EditorStyles.miniLabel
                );
            }
            if (!string.IsNullOrEmpty(entry.PropertyPath))
            {
                GUI.color = new Color(1f, 0.6f, 0.6f);
                GUILayout.Label($"Property: {entry.PropertyPath}", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndVertical();

            // 4. 选择按钮
            if (GUILayout.Button("Select", GUILayout.Width(60), GUILayout.Height(30)))
            {
                Selection.activeObject = entry.ContextObject;
                EditorGUIUtility.PingObject(entry.ContextObject);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // =========================================================
        // 核心扫描逻辑
        // =========================================================

        private void ScanScene()
        {
            _entries.Clear();
            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

            EditorUtility.DisplayProgressBar("Scanning Scene", "Iterating GameObjects...", 0f);

            try
            {
                foreach (GameObject root in rootObjects)
                {
                    // 递归获取所有子物体
                    var allTransforms = root.GetComponentsInChildren<Transform>(true);
                    foreach (Transform t in allTransforms)
                    {
                        CheckGameObject(t.gameObject);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void ScanProject()
        {
            _entries.Clear();
            string[] guids = AssetDatabase.FindAssets("t:Prefab t:ScriptableObject");

            int count = 0;
            try
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    count++;
                    EditorUtility.DisplayProgressBar(
                        "Scanning Project",
                        path,
                        (float)count / guids.Length
                    );

                    Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);

                    if (obj is GameObject go)
                    {
                        // 这是一个 Prefab，需要检查它的所有组件
                        CheckGameObject(go);
                    }
                    else if (obj is ScriptableObject so)
                    {
                        // 这是一个 SO，检查它的序列化属性
                        CheckSerializedObject(obj, new SerializedObject(so), null);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void CheckGameObject(GameObject go)
        {
            // 1. 检查 Missing Script (脚本文件丢失)
            int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (missingCount > 0)
            {
                _entries.Add(
                    new MissingEntry
                    {
                        ContextObject = go,
                        ErrorType = $"Missing Script ({missingCount})",
                        PropertyPath = "Check Inspector (Yellow Warning)",
                    }
                );
            }

            // 2. 检查组件内的 Missing Reference
            var components = go.GetComponents<Component>();
            foreach (Component comp in components)
            {
                if (comp == null)
                    continue; // 已经被上面的 GetMonoBehavioursWithMissingScriptCount 捕获了

                // 跳过 Transform，通常没问题且浪费性能
                if (comp is Transform)
                    continue;

                SerializedObject so = new SerializedObject(comp);
                CheckSerializedObject(go, so, comp);
            }
        }

        private void CheckSerializedObject(Object context, SerializedObject so, Component comp)
        {
            SerializedProperty sp = so.GetIterator();

            // NextVisible(true) 会进入子属性，遍历整个树
            while (sp.NextVisible(true))
            {
                if (sp.propertyType == SerializedPropertyType.ObjectReference)
                {
                    // === 核心黑科技判定 ===
                    // objectReferenceValue 为 null，但 InstanceID 不为 0
                    // 这意味着 Unity 知道这里“应该”有个东西，但找不到它了 (GUID 断裂)
                    if (sp.objectReferenceValue == null && sp.objectReferenceInstanceIDValue != 0)
                    {
                        _entries.Add(
                            new MissingEntry
                            {
                                ContextObject = context,
                                Component = comp,
                                ErrorType = "Missing Reference",
                                PropertyPath = sp.propertyPath,
                            }
                        );
                    }
                }
            }
        }
    }
}
