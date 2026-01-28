using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Utilities.Tools.DuplicateFinder
{
    public class AsakiDuplicateFinderWindow : EditorWindow
    {
        private float _tolerance = 0.05f;
        private List<DuplicateGroup> _groups;
        private Vector2 _scrollPos;

        // 样式配置
        private static readonly Color HighlightColor = new Color(1f, 0.2f, 0.2f, 1f); // 红色高亮
        private static readonly Color FillColor = new Color(1f, 0.2f, 0.2f, 0.1f);

        [MenuItem("Asaki/Tools/Duplicate Finder")]
        public static void Open()
        {
            AsakiDuplicateFinderWindow win = GetWindow<AsakiDuplicateFinderWindow>(
                "Duplicate Finder"
            );
            win.Show();
        }

        private void OnEnable()
        {
            // 订阅场景重绘事件，实现 SceneView 高亮
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawSettings();
            DrawResults();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Asaki Duplicate Finder", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "检测场景中位置重叠且外观相同的物体。\n红色线框将在 Scene 视图中标记它们。",
                MessageType.Info
            );
        }

        private void DrawSettings()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _tolerance = EditorGUILayout.Slider("Pos Tolerance", _tolerance, 0.001f, 1.0f);

                if (GUILayout.Button("Scan Scene", GUILayout.Height(30)))
                {
                    PerformScan();
                }
            }
        }

        private void DrawResults()
        {
            if (_groups == null)
                return;

            EditorGUILayout.Space();
            GUILayout.Label($"Found Groups: {_groups.Count}", EditorStyles.boldLabel);

            if (_groups.Count > 0)
            {
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Select All Duplicates (For Deletion)", GUILayout.Height(30)))
                {
                    SelectAllDuplicates();
                }
                GUI.backgroundColor = Color.white;
            }

            using (
                EditorGUILayout.ScrollViewScope scroll = new EditorGUILayout.ScrollViewScope(
                    _scrollPos
                )
            )
            {
                _scrollPos = scroll.scrollPosition;
                foreach (DuplicateGroup group in _groups)
                {
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        if (group.Original == null)
                            continue;

                        GUILayout.Label(
                            $"{group.Original.name}",
                            EditorStyles.boldLabel,
                            GUILayout.Width(150)
                        );
                        GUILayout.Label(
                            $"Contains {group.Duplicates.Count} Copies",
                            EditorStyles.miniLabel
                        );

                        if (GUILayout.Button("Select Group", GUILayout.Width(100)))
                        {
                            var all = new List<Object> { group.Original };
                            all.AddRange(group.Duplicates);
                            Selection.objects = all.ToArray();
                            SceneView.FrameLastActiveSceneView();
                        }
                    }
                }
            }
        }

        // 核心可视化逻辑：在 Scene 窗口绘制
        private void OnSceneGUI(SceneView sceneView)
        {
            if (_groups == null || _groups.Count == 0)
                return;

            Handles.color = HighlightColor;

            foreach (DuplicateGroup group in _groups)
            {
                if (group.Original == null)
                    continue;

                // 绘制线框
                Handles.DrawWireCube(group.WorldBounds.center, group.WorldBounds.size);

                // 绘制半透明填充（增强可见性）
                Handles.color = FillColor;
                Handles.DrawWireCube(group.WorldBounds.center, group.WorldBounds.size);
                Handles.color = HighlightColor;

                // 绘制文字标签
                Handles.Label(
                    group.WorldBounds.center + Vector3.up * group.WorldBounds.extents.y,
                    $"⚠ {group.Duplicates.Count} Duplicates"
                );

                // 绘制连接线，指向所有重叠物体（如果稍微有点偏移的话能看出来）
                foreach (GameObject dup in group.Duplicates)
                {
                    if (dup != null)
                        Handles.DrawLine(group.WorldBounds.center, dup.transform.position);
                }
            }

            // 强制刷新 SceneView，防止绘制卡顿或消失
            sceneView.Repaint();
        }

        private void PerformScan()
        {
            _groups = AsakiDuplicateFinderLogic.FindDuplicates(_tolerance);
            if (_groups.Count == 0)
            {
                EditorUtility.DisplayDialog("Result", "No duplicates found!", "OK");
            }
            SceneView.RepaintAll();
        }

        private void SelectAllDuplicates()
        {
            var selection = new List<Object>();
            foreach (DuplicateGroup group in _groups)
            {
                // 只选中副本，不选中原件
                foreach (GameObject dup in group.Duplicates)
                {
                    if (dup != null)
                        selection.Add(dup);
                }
            }
            Selection.objects = selection.ToArray();
            Debug.Log($"[Asaki Finder] Selected {selection.Count} duplicates.");
        }
    }
}
