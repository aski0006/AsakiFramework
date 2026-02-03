using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Utilities.Tools.AssetReplacer
{
    public class AsakiAssetReplacerWindow : EditorWindow
    {
        private Object _sourceAsset;
        private Object _targetAsset;
        private bool _searchProject; // 默认只搜场景，搜Project比较慢

        private List<AsakiAssetReplacerLogic.ReplaceEntry> _entries;
        private Vector2 _scrollPos;

        [MenuItem("Asaki/Tools/Asset Replacer", false, 17)]
        public static void Open()
        {
            GetWindow<AsakiAssetReplacerWindow>("Asset Replacer").Show();
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space();
            DrawSettings();
            EditorGUILayout.Space();
            DrawPreview();
            DrawFooter();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Asaki Asset Replacer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "通用资产替换工具：将 Source 资产的所有引用替换为 Target 资产。\n支持场景对象与 Prefab。",
                MessageType.Info
            );
        }

        private void DrawSettings()
        {
            GUILayout.Label("Configuration", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _sourceAsset = EditorGUILayout.ObjectField(
                    "Source (Old)",
                    _sourceAsset,
                    typeof(Object),
                    false
                );
                _targetAsset = EditorGUILayout.ObjectField(
                    "Target (New)",
                    _targetAsset,
                    typeof(Object),
                    false
                );

                // 类型检查警告
                if (_sourceAsset != null && _targetAsset != null)
                {
                    if (_sourceAsset.GetType() != _targetAsset.GetType())
                    {
                        EditorGUILayout.HelpBox(
                            $"类型不匹配! Source: {_sourceAsset.GetType().Name}, Target: {_targetAsset.GetType().Name}",
                            MessageType.Error
                        );
                    }
                }

                _searchProject = EditorGUILayout.Toggle("Include Project Assets", _searchProject);

                EditorGUILayout.Space();

                if (GUILayout.Button("1. Analyze References", GUILayout.Height(30)))
                {
                    PerformScan();
                }
            }
        }

        private void DrawPreview()
        {
            if (_entries == null)
                return;

            GUILayout.Label($"Found References: {_entries.Count}", EditorStyles.boldLabel);

            using (
                EditorGUILayout.ScrollViewScope scroll = new EditorGUILayout.ScrollViewScope(
                    _scrollPos
                )
            )
            {
                _scrollPos = scroll.scrollPosition;

                for (int i = 0; i < _entries.Count; i++)
                {
                    AsakiAssetReplacerLogic.ReplaceEntry entry = _entries[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        entry.IsSelected = EditorGUILayout.Toggle(
                            entry.IsSelected,
                            GUILayout.Width(20)
                        );

                        // 显示对象名称和组件类型
                        GUI.enabled = false;
                        EditorGUILayout.ObjectField(entry.TargetObject, typeof(Object), true);
                        GUI.enabled = true;

                        GUILayout.Label($"-> {entry.PropertyPath}", EditorStyles.miniLabel);
                    }
                }
            }
        }

        private void DrawFooter()
        {
            if (_entries == null || _entries.Count == 0)
                return;

            EditorGUILayout.Space();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("2. Execute Replace", GUILayout.Height(40)))
            {
                if (
                    EditorUtility.DisplayDialog(
                        "Confirm",
                        $"Replace {_entries.Count} references? This allows Undo.",
                        "Yes",
                        "Cancel"
                    )
                )
                {
                    AsakiAssetReplacerLogic.ExecuteReplace(_entries, _targetAsset);
                    _entries.Clear(); // 清空结果防止重复操作
                }
            }
            GUI.backgroundColor = Color.white;
        }

        private void PerformScan()
        {
            if (_sourceAsset == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Source Asset.", "OK");
                return;
            }

            _entries = AsakiAssetReplacerLogic.FindReferences(
                _sourceAsset,
                _targetAsset,
                _searchProject
            );

            if (_entries.Count == 0)
            {
                Debug.Log("[Asaki Replacer] No references found.");
            }
        }
    }
}
