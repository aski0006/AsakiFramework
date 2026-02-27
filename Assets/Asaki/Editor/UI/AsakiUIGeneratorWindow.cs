using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        /// <summary>
        /// 验证 UI 配置与 WindowAssetId 枚举的同步状态
        /// </summary>
        [MenuItem("Asaki/Window/Validate UI Config Sync", false, 42)]
        public static void ValidateConfigSync()
        {
            ValidateConfigSyncInternal(autoFix: true);
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

        /// <summary>
        /// 同步配置并生成代码
        /// </summary>
        private void SyncAndGenerate()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Asaki UI Gen", "Processing...", 0.5f);

                // 获取主配置
                AsakiFrameworkSetting mainFrameworkSetting = LoadOrCreateConfig();

                GenerateCode(_items);

                // 同步到 mainFrameworkSetting.UIConfig
                UpdateConfigData(mainFrameworkSetting, _items);

                AssetDatabase.Refresh();

                // 构建成功消息
                string successMessage = BuildSuccessMessage();

                Debug.Log($"[AsakiUI] 同步完成: {_items.Count} 个条目");
                EditorUtility.DisplayDialog("Success", successMessage, "OK");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Error", $"同步失败: {e.Message}", "OK");
            }
            finally
            {
                Debug.Log("[AsakiUI] 同步操作结束");
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// 构建成功消息，显示同步的条目数量和枚举值列表
        /// </summary>
        /// <returns>格式化的成功消息</returns>
        private string BuildSuccessMessage()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"同步完成，共 {_items.Count} 个条目");
            sb.AppendLine();

            // 获取排序后的枚举名称列表
            var sortedItems = _items.OrderBy(x => x.EnumName).ToList();

            if (sortedItems.Count <= 5)
            {
                sb.AppendLine("生成的枚举值:");
                foreach (var item in sortedItems)
                {
                    sb.AppendLine($"  - {item.EnumName}");
                }
            }
            else
            {
                sb.AppendLine("生成的枚举值 (前5个):");
                for (int i = 0; i < 5; i++)
                {
                    sb.AppendLine($"  - {sortedItems[i].EnumName}");
                }
                sb.AppendLine($"  等 {sortedItems.Count - 5} 个条目...");
            }

            return sb.ToString();
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

        #region 配置同步验证

        /// <summary>
        /// 枚举条目信息，包含名称和ID
        /// </summary>
        private struct EnumEntry
        {
            public string Name;
            public int ID;

            public EnumEntry(string name, int id)
            {
                Name = name;
                ID = id;
            }
        }

        /// <summary>
        /// 验证配置同步的内部实现
        /// </summary>
        /// <param name="autoFix">是否在检测到差异时自动提示修复</param>
        private static void ValidateConfigSyncInternal(bool autoFix)
        {
            // 加载配置文件
            AsakiFrameworkSetting frameworkSetting =
                AssetDatabase.LoadAssetAtPath<AsakiFrameworkSetting>(CONFIG_ASSET_PATH);
            if (frameworkSetting == null)
            {
                Debug.LogError("[AsakiUI] 未找到 AsakiFrameworkSetting 配置文件");
                return;
            }

            // 读取枚举文件
            if (!File.Exists(CODE_GEN_PATH))
            {
                Debug.LogError($"[AsakiUI] 未找到枚举文件: {CODE_GEN_PATH}");
                return;
            }

            string enumContent = File.ReadAllText(CODE_GEN_PATH);
            List<EnumEntry> enumEntries = ParseEnumEntries(enumContent);

            // 获取配置中的UI列表
            Dictionary<string, UIInfo> configDict = new Dictionary<string, UIInfo>();
            Dictionary<int, UIInfo> configByIdDict = new Dictionary<int, UIInfo>();
            foreach (UIInfo info in frameworkSetting.UIConfig.UIList)
            {
                configDict[info.Name] = info;
                configByIdDict[info.ID] = info;
            }

            // 分析差异
            List<EnumEntry> missingInConfig = new List<EnumEntry>(); // 枚举中有但配置中没有
            List<string> missingInEnum = new List<string>(); // 配置中有但枚举中没有
            List<string> idMismatches = new List<string>(); // ID不匹配的条目

            // 检查枚举中的条目是否都在配置中
            foreach (EnumEntry entry in enumEntries)
            {
                if (!configDict.ContainsKey(entry.Name))
                {
                    missingInConfig.Add(entry);
                }
                else
                {
                    UIInfo configInfo = configDict[entry.Name];
                    if (configInfo.ID != entry.ID)
                    {
                        idMismatches.Add(
                            $"{entry.Name}: 枚举ID={entry.ID}, 配置ID={configInfo.ID}"
                        );
                    }
                }
            }

            // 检查配置中的条目是否都在枚举中
            HashSet<string> enumNames = new HashSet<string>(enumEntries.Select(e => e.Name));
            foreach (UIInfo info in frameworkSetting.UIConfig.UIList)
            {
                if (!enumNames.Contains(info.Name))
                {
                    missingInEnum.Add(info.Name);
                }
            }

            // 输出报告
            bool hasDiff =
                missingInConfig.Count > 0 || missingInEnum.Count > 0 || idMismatches.Count > 0;

            Debug.Log("========== UI 配置同步验证报告 ==========");

            if (!hasDiff)
            {
                Debug.Log("[AsakiUI] 配置与枚举完全同步，无差异");
                EditorUtility.DisplayDialog("验证结果", "配置与枚举完全同步，无差异", "确定");
                return;
            }

            // 输出缺失条目
            if (missingInConfig.Count > 0)
            {
                Debug.Log($"[缺失条目] 枚举中有但配置中没有 ({missingInConfig.Count} 个):");
                foreach (EnumEntry entry in missingInConfig)
                {
                    Debug.Log($"  - {entry.Name} (ID: {entry.ID})");
                }
            }

            // 输出多余条目
            if (missingInEnum.Count > 0)
            {
                Debug.Log($"[多余条目] 配置中有但枚举中没有 ({missingInEnum.Count} 个):");
                foreach (string name in missingInEnum)
                {
                    Debug.Log($"  - {name}");
                }
            }

            // 输出ID不匹配
            if (idMismatches.Count > 0)
            {
                Debug.Log($"[ID不匹配] ({idMismatches.Count} 个):");
                foreach (string mismatch in idMismatches)
                {
                    Debug.Log($"  - {mismatch}");
                }
            }

            Debug.Log("==========================================");

            // 自动修复
            if (autoFix && missingInConfig.Count > 0)
            {
                bool shouldFix = EditorUtility.DisplayDialog(
                    "检测到差异",
                    $"发现 {missingInConfig.Count} 个枚举条目在配置中缺失\n"
                        + $"是否自动修复？\n\n"
                        + $"(将根据枚举更新配置，保留现有条目的Layer和UsePool设置)",
                    "修复",
                    "取消"
                );

                if (shouldFix)
                {
                    FixConfigSync(frameworkSetting, enumEntries);
                }
            }
            else if (autoFix)
            {
                EditorUtility.DisplayDialog(
                    "验证结果",
                    $"检测到差异:\n"
                        + $"- 缺失条目: {missingInConfig.Count}\n"
                        + $"- 多余条目: {missingInEnum.Count}\n"
                        + $"- ID不匹配: {idMismatches.Count}\n\n"
                        + $"请查看控制台获取详细信息",
                    "确定"
                );
            }
        }

        /// <summary>
        /// 解析枚举文件中的条目
        /// </summary>
        /// <param name="enumContent">枚举文件内容</param>
        /// <returns>枚举条目列表</returns>
        private static List<EnumEntry> ParseEnumEntries(string enumContent)
        {
            List<EnumEntry> entries = new List<EnumEntry>();
            // 匹配格式: EnumName = ID,
            Regex regex = new Regex(@"(\w+)\s*=\s*(\d+)\s*,");
            MatchCollection matches = regex.Matches(enumContent);

            foreach (Match match in matches)
            {
                string name = match.Groups[1].Value;
                string idStr = match.Groups[2].Value;

                // 跳过 None 枚举值
                if (name == "None")
                    continue;

                if (int.TryParse(idStr, out int id))
                {
                    entries.Add(new EnumEntry(name, id));
                }
            }

            return entries;
        }

        /// <summary>
        /// 修复配置同步问题，根据枚举更新配置
        /// </summary>
        /// <param name="frameworkSetting">框架设置</param>
        /// <param name="enumEntries">枚举条目列表</param>
        private static void FixConfigSync(
            AsakiFrameworkSetting frameworkSetting,
            List<EnumEntry> enumEntries
        )
        {
            try
            {
                EditorUtility.DisplayProgressBar("修复配置同步", "正在处理...", 0.5f);

                // 保留现有配置的映射
                Dictionary<string, UIInfo> existingConfig = new Dictionary<string, UIInfo>();
                foreach (UIInfo info in frameworkSetting.UIConfig.UIList)
                {
                    existingConfig[info.Name] = info;
                }

                // 根据枚举重建配置列表
                frameworkSetting.UIConfig.UIList.Clear();
                foreach (EnumEntry entry in enumEntries.OrderBy(e => e.Name))
                {
                    UIInfo newInfo;
                    if (existingConfig.TryGetValue(entry.Name, out UIInfo existingInfo))
                    {
                        // 保留现有设置，但更新ID
                        newInfo = new UIInfo
                        {
                            Name = entry.Name,
                            ID = entry.ID,
                            Layer = existingInfo.Layer,
                            AssetPath = existingInfo.AssetPath,
                            UsePool = existingInfo.UsePool,
                        };
                    }
                    else
                    {
                        // 新条目使用默认值
                        newInfo = new UIInfo
                        {
                            Name = entry.Name,
                            ID = entry.ID,
                            Layer = AsakiUILayer.Normal,
                            AssetPath = "",
                            UsePool = false,
                        };
                    }
                    frameworkSetting.UIConfig.UIList.Add(newInfo);
                }

                EditorUtility.SetDirty(frameworkSetting);
                AssetDatabase.SaveAssets();

                Debug.Log($"[AsakiUI] 配置同步修复完成，共 {enumEntries.Count} 个条目");
                EditorUtility.DisplayDialog(
                    "修复完成",
                    $"配置已同步，共 {enumEntries.Count} 个条目",
                    "确定"
                );
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("修复失败", $"发生错误: {e.Message}", "确定");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        #endregion
    }
}
