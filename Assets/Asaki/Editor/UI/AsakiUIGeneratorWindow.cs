using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Editor.UI
{
    public class AsakiUIGeneratorWindow : EditorWindow
    {
        private const string CODE_GEN_PATH = "Assets/Asaki/Generated/UIAsset_2_Id/WindowAssetId.cs";
        private const string CONFIG_ASSET_PATH =
            "Assets/Resources/Asaki/DataTable/AsakiFrameworkSetting.asset";

        [Serializable]
        private class UIItem
        {
            public GameObject Prefab;
            public AsakiUILayer Layer = AsakiUILayer.Normal;
            public string EnumName;
            public string LoadPath;
            public bool HasConflict;

            public UIItem(GameObject prefab, string overridePath = null)
            {
                Prefab = prefab;
                RefreshName();

                if (!string.IsNullOrEmpty(overridePath))
                {
                    LoadPath = overridePath;
                }
                else
                {
                    string rawPath = AssetDatabase.GetAssetPath(prefab);
                    if (rawPath.Contains("/Resources/"))
                    {
                        string ext = Path.GetExtension(rawPath);
                        int resIndex =
                            rawPath.IndexOf("/Resources/", StringComparison.Ordinal) + 11;
                        LoadPath = rawPath.Substring(resIndex).Replace(ext, "");
                    }
                    else
                    {
                        LoadPath = rawPath;
                    }
                }
            }

            public void RefreshName()
            {
                if (Prefab != null)
                    EnumName = SanitizeName(Prefab.name);
            }
        }

        private List<UIItem> _items = new List<UIItem>();
        private Vector2 _scrollPos;
        private bool _hasGlobalConflict = false;

        [MenuItem("Asaki/Window/UI Asset Generator", false, 41)]
        public static void OpenWindow()
        {
            AsakiUIGeneratorWindow window = GetWindow<AsakiUIGeneratorWindow>("Asaki UI Gen");
            window.minSize = new Vector2(600, 400);
            window.Show();
            window.LoadCurrentConfig();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawDragDropArea();
            DrawList();
            DrawFooter();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            // [修改] 按钮文字更新
            if (GUILayout.Button("Load From AsakiFrameworkSetting", EditorStyles.toolbarButton))
            {
                LoadCurrentConfig();
            }
            if (GUILayout.Button("Clear All", EditorStyles.toolbarButton))
            {
                _items.Clear();
                _hasGlobalConflict = false;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDragDropArea()
        {
            Event evt = Event.current;
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag UI Prefabs or Folders Here", EditorStyles.helpBox);

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (!dropArea.Contains(evt.mousePosition))
                    return;
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (Object draggedTask in DragAndDrop.objectReferences)
                        AddObject(draggedTask);
                    ValidateConflicts();
                }
                Event.current.Use();
            }
        }

        private void AddObject(Object obj)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (Directory.Exists(path))
            {
                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (go != null)
                        AddSingleItem(go);
                }
            }
            else if (obj is GameObject go)
            {
                if (PrefabUtility.IsPartOfPrefabAsset(obj))
                    AddSingleItem(go);
            }
        }

        private void AddSingleItem(GameObject go)
        {
            if (_items.Any(x => x.Prefab == go))
                return;
            _items.Add(new UIItem(go));
        }

        private void DrawList()
        {
            EditorGUILayout.LabelField($"Total Items: {_items.Count}", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Prefab", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField(
                "Generated Enum",
                EditorStyles.boldLabel,
                GUILayout.Width(180)
            );
            EditorGUILayout.LabelField(
                "Load Path (Key)",
                EditorStyles.boldLabel,
                GUILayout.Width(200)
            );
            EditorGUILayout.LabelField("Layer", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            for (int i = 0; i < _items.Count; i++)
            {
                UIItem item = _items[i];
                if (item.Prefab == null)
                    continue;
                GUI.backgroundColor = item.HasConflict ? new Color(1f, 0.5f, 0.5f) : Color.white;
                EditorGUILayout.BeginHorizontal("box");

                EditorGUI.BeginChangeCheck();
                GameObject newPrefab = (GameObject)
                    EditorGUILayout.ObjectField(
                        item.Prefab,
                        typeof(GameObject),
                        false,
                        GUILayout.Width(150)
                    );
                if (EditorGUI.EndChangeCheck())
                {
                    item.Prefab = newPrefab;
                    item.RefreshName();
                    ValidateConflicts();
                }

                EditorGUILayout.LabelField(item.EnumName, GUILayout.Width(180));
                item.LoadPath = EditorGUILayout.TextField(item.LoadPath, GUILayout.Width(200));
                item.Layer = (AsakiUILayer)
                    EditorGUILayout.EnumPopup(item.Layer, GUILayout.Width(80));

                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    _items.RemoveAt(i);
                    ValidateConflicts();
                    i--;
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (_hasGlobalConflict)
            {
                EditorGUILayout.HelpBox("Duplicate Enum Names detected!", MessageType.Error);
            }
        }

        private void DrawFooter()
        {
            GUILayout.Space(10);
            GUI.enabled = !_hasGlobalConflict && _items.Count > 0;
            if (GUILayout.Button("Sync DataTable & Generate Code", GUILayout.Height(40)))
            {
                SyncAndGenerate();
            }
            GUI.enabled = true;
        }

        // ================= 逻辑区域 =================

        private void ValidateConflicts()
        {
            _hasGlobalConflict = false;
            var nameCount = new Dictionary<string, int>();
            foreach (UIItem item in _items)
            {
                if (item.Prefab == null)
                    continue;
                item.RefreshName();
                if (!nameCount.ContainsKey(item.EnumName))
                    nameCount[item.EnumName] = 0;
                nameCount[item.EnumName]++;
            }
            foreach (UIItem item in _items)
            {
                if (item.Prefab == null)
                    continue;
                bool isConflict = nameCount[item.EnumName] > 1;
                item.HasConflict = isConflict;
                if (isConflict)
                    _hasGlobalConflict = true;
            }
        }

        private void SyncAndGenerate()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Asaki UI Gen", "Processing...", 0.5f);

                // [修改] 获取主配置
                AsakiFrameworkSetting mainFrameworkSetting = LoadOrCreateConfig();

                GenerateCode(_items);

                // [修改] 同步到 mainFrameworkSetting.UIConfig
                UpdateConfigData(mainFrameworkSetting, _items);

                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog(
                    "Success",
                    $"Synced {_items.Count} items to AsakiFrameworkSetting & UIID.cs",
                    "OK"
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"[AsakiUI] Failed: {e.Message}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void GenerateCode(List<UIItem> items)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("// This file is generated by AsakiUIGeneratorWindow.");
            sb.AppendLine();
            sb.AppendLine("namespace Asaki.Generated");
            sb.AppendLine("{");
            sb.AppendLine("    public enum WindowAssetId");
            sb.AppendLine("    {");
            sb.AppendLine("        None = 0,");
            foreach (UIItem item in items.OrderBy(x => x.EnumName))
            {
                int id = Animator.StringToHash(item.EnumName);
                sb.AppendLine($"        {item.EnumName} = {id},");
            }
            sb.AppendLine("    }");
            sb.AppendLine("}");
            WriteFile(CODE_GEN_PATH, sb.ToString());
        }

        private static void UpdateConfigData(
            AsakiFrameworkSetting mainFrameworkSetting,
            List<UIItem> items
        )
        {
            // [修改] 操作 UIConfig 属性
            AsakiUIConfig uiConfig = mainFrameworkSetting.UIConfig;
            uiConfig.UIList.Clear();
            foreach (UIItem item in items.OrderBy(x => x.EnumName))
            {
                uiConfig.UIList.Add(
                    new UIInfo
                    {
                        Name = item.EnumName,
                        ID = Animator.StringToHash(item.EnumName),
                        Layer = item.Layer,
                        AssetPath = item.LoadPath,
                    }
                );
            }
            // 标记主 SO 为脏
            EditorUtility.SetDirty(mainFrameworkSetting);
            AssetDatabase.SaveAssets();
        }

        // [修改] 返回主配置类型
        private static AsakiFrameworkSetting LoadOrCreateConfig()
        {
            AsakiFrameworkSetting frameworkSetting =
                AssetDatabase.LoadAssetAtPath<AsakiFrameworkSetting>(CONFIG_ASSET_PATH);
            if (!frameworkSetting)
            {
                frameworkSetting = CreateInstance<AsakiFrameworkSetting>();
                string dir = Path.GetDirectoryName(CONFIG_ASSET_PATH);
                if (!Directory.Exists(dir))
                    if (dir != null)
                        Directory.CreateDirectory(dir);
                AssetDatabase.CreateAsset(frameworkSetting, CONFIG_ASSET_PATH);
            }
            return frameworkSetting;
        }

        private void LoadCurrentConfig()
        {
            // [修改] 加载主配置
            AsakiFrameworkSetting mainFrameworkSetting =
                AssetDatabase.LoadAssetAtPath<AsakiFrameworkSetting>(CONFIG_ASSET_PATH);
            if (mainFrameworkSetting == null)
                return;

            _items.Clear();
            // [修改] 访问 UIConfig.UIList
            foreach (UIInfo info in mainFrameworkSetting.UIConfig.UIList)
            {
                GameObject prefab = null;
                string searchPath = info.AssetPath;

                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(searchPath);
                if (prefab == null)
                {
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/Resources/" + searchPath + ".prefab"
                    );
                }
                if (prefab == null && !searchPath.Contains("/"))
                {
                    string[] guids = AssetDatabase.FindAssets(searchPath + " t:Prefab");
                    if (guids.Length > 0)
                    {
                        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                            AssetDatabase.GUIDToAssetPath(guids[0])
                        );
                    }
                }

                if (prefab != null)
                {
                    _items.Add(new UIItem(prefab, info.AssetPath) { Layer = info.Layer });
                }
                else
                {
                    Debug.LogWarning($"[AsakiUI] Could not find prefab for: {info.Name}");
                }
            }
            ValidateConflicts();
        }

        private static string SanitizeName(string rawName)
        {
            string name = rawName
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace(".", "_")
                .Replace("(", "")
                .Replace(")", "");
            if (char.IsDigit(name[0]))
                name = "Window_" + name;
            return name;
        }

        private static void WriteFile(string path, string content)
        {
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                if (dir != null)
                    Directory.CreateDirectory(dir);
            File.WriteAllText(path, content, Encoding.UTF8);
        }
    }
}
