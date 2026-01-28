using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Asaki.Editor.GraphEditors;
using Asaki.Editor.ModuleSystem.Graph;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.ModuleSystem
{
    public class AsakiModuleDashboard : EditorWindow
    {
        [MenuItem("Asaki/Module Dashboard")]
        public static void ShowWindow()
        {
            GetWindow<AsakiModuleDashboard>("Asaki Modules");
        }

        private class ModuleNode
        {
            public Type Type;
            public int Priority;
            public List<Type> Dependencies = new List<Type>();
            public List<Type> Dependents = new List<Type>();
            public bool HasCircularError = false;

            // [New] 拓扑深度 (0 = Root)
            public int Depth = 0;

            // [New] 优先级配置警告
            public string PriorityWarning = null;
        }

        private List<ModuleNode> _sortedNodes = new List<ModuleNode>();
        private Dictionary<Type, ModuleNode> _nodeMap = new Dictionary<Type, ModuleNode>();
        private ModuleNode _selectedNode;
        private Vector2 _scrollPos;
        private string _circularErrorMsg;
        private int _editPriorityValue;

        private void OnEnable()
        {
            RefreshData();
        }

        private void OnGUI()
        {
            DrawToolbar();
            if (!string.IsNullOrEmpty(_circularErrorMsg))
                EditorGUILayout.HelpBox($"Graphs Error: {_circularErrorMsg}", MessageType.Error);

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Refresh Data", EditorStyles.toolbarButton, GUILayout.Width(100)))
                RefreshData();
            GUILayout.Space(10);

            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (
                GUILayout.Button(
                    "Open Dependency Graph",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(160)
                )
            )
                OpenVisualGraph();
            GUI.backgroundColor = originalColor;

            GUILayout.FlexibleSpace();
            GUILayout.Label($"Modules: {_sortedNodes.Count}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void OpenVisualGraph()
        {
            try
            {
                AsakiGraphWindow.OpenInstance(AsakiModuleGraphBuilder.Build());
            }
            catch (Exception e)
            {
                Debug.LogError($"[Asaki] Failed to open graph: {e}");
            }
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(350)); // 加宽一点以适应层级缩进
            GUILayout.Label("Initialization Order (Topological Sort)", EditorStyles.boldLabel);
            GUILayout.Space(5);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, "box");

            for (int i = 0; i < _sortedNodes.Count; i++)
            {
                ModuleNode node = _sortedNodes[i];
                DrawNodeItem(i, node);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawNodeItem(int index, ModuleNode node)
        {
            GUIStyle style = new GUIStyle(EditorStyles.helpBox);
            style.richText = true;

            // 高亮逻辑
            if (_selectedNode == node)
                GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f);
            else if (_selectedNode != null)
            {
                if (_selectedNode.Dependencies.Contains(node.Type))
                    GUI.backgroundColor = new Color(1f, 0.8f, 0.4f);
                else if (_selectedNode.Dependents.Contains(node.Type))
                    GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
                else
                    GUI.backgroundColor = Color.white;
            }
            else
                GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal(style);

            // 序号
            GUILayout.Label(
                $"<b>{index + 1}.</b>",
                new GUIStyle(EditorStyles.label) { richText = true, fixedWidth = 25 }
            );

            // 缩进 (基于拓扑深度)
            GUILayout.Space(node.Depth * 15);

            // 绘制模块信息
            string label = $"<b>{node.Type.Name}</b>";
            if (!string.IsNullOrEmpty(node.PriorityWarning))
            {
                label += $" <color=red>⚠</color>"; // 警告图标
            }
            label += $"\n<size=10>Priority: {node.Priority}</size>";

            if (GUILayout.Button(label, new GUIStyle(EditorStyles.label) { richText = true }))
            {
                SelectNode(node);
            }

            // 如果有警告，显示红色提示文字
            if (!string.IsNullOrEmpty(node.PriorityWarning))
            {
                GUILayout.Label(
                    $"<color=red>Priority < Dep</color>",
                    new GUIStyle(EditorStyles.miniLabel)
                    {
                        richText = true,
                        alignment = TextAnchor.MiddleRight,
                    },
                    GUILayout.Width(80)
                );
            }

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
        }

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();
            if (_selectedNode == null)
            {
                GUILayout.Label(
                    "Select a module to view details.",
                    EditorStyles.centeredGreyMiniLabel
                );
            }
            else
            {
                GUILayout.Space(10);
                GUILayout.Label($"Inspector: {_selectedNode.Type.Name}", EditorStyles.boldLabel);
                EditorGUILayout.Separator();

                // 警告信息展示
                if (!string.IsNullOrEmpty(_selectedNode.PriorityWarning))
                {
                    EditorGUILayout.HelpBox(_selectedNode.PriorityWarning, MessageType.Warning);
                }

                DrawPriorityEditor();
                EditorGUILayout.Space(10);

                GUILayout.Label("Dependencies (Upstream):", EditorStyles.boldLabel);
                if (_selectedNode.Dependencies.Count == 0)
                    GUILayout.Label("  - None", EditorStyles.miniLabel);
                else
                    foreach (Type dep in _selectedNode.Dependencies)
                    {
                        ModuleNode depNode = _nodeMap[dep];
                        // 显示依赖项的优先级，方便对比
                        if (
                            GUILayout.Button(
                                $"  ➡ {dep.Name} (P: {depNode.Priority})",
                                EditorStyles.linkLabel
                            )
                        )
                            SelectNode(depNode);
                    }

                EditorGUILayout.Space(10);

                GUILayout.Label("Dependents (Downstream):", EditorStyles.boldLabel);
                if (_selectedNode.Dependents.Count == 0)
                    GUILayout.Label("  - None", EditorStyles.miniLabel);
                else
                    foreach (Type dept in _selectedNode.Dependents)
                    {
                        ModuleNode deptNode = _nodeMap[dept];
                        if (
                            GUILayout.Button(
                                $"  ⬅ {dept.Name} (P: {deptNode.Priority})",
                                EditorStyles.linkLabel
                            )
                        )
                            SelectNode(deptNode);
                    }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Open Script File", GUILayout.Height(30)))
                    OpenScriptFile(_selectedNode.Type);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawPriorityEditor()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Settings Modification", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            _editPriorityValue = EditorGUILayout.IntField("Priority", _editPriorityValue);

            if (_editPriorityValue != _selectedNode.Priority)
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Apply", GUILayout.Width(60)))
                {
                    if (
                        EditorUtility.DisplayDialog(
                            "Confirm",
                            $"Set Priority to {_editPriorityValue}?",
                            "Yes",
                            "No"
                        )
                    )
                        UpdateScriptPriority(_selectedNode.Type, _editPriorityValue);
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void SelectNode(ModuleNode node)
        {
            _selectedNode = node;
            _editPriorityValue = node.Priority;
            GUI.FocusControl(null);
        }

        private void RefreshData()
        {
            _nodeMap.Clear();
            _sortedNodes.Clear();
            _circularErrorMsg = null;
            _selectedNode = null;
            var missingDependencies = new List<string>();

            // 1. 扫描与构建节点
            var types = TypeCache
                .GetTypesDerivedFrom<IAsakiModule>()
                .Where(t => !t.IsAbstract && t.IsDefined(typeof(AsakiModuleAttribute), false));

            foreach (Type type in types)
            {
                AsakiModuleAttribute attr = type.GetCustomAttribute<AsakiModuleAttribute>();
                _nodeMap[type] = new ModuleNode { Type = type, Priority = attr.Priority };
            }

            // 2. 构建连接
            foreach (ModuleNode node in _nodeMap.Values)
            {
                AsakiModuleAttribute attr = node.Type.GetCustomAttribute<AsakiModuleAttribute>();
                foreach (Type depType in attr.Dependencies)
                {
                    if (_nodeMap.TryGetValue(depType, out ModuleNode parentNode))
                    {
                        node.Dependencies.Add(depType);
                        parentNode.Dependents.Add(node.Type);
                    }
                    else
                        missingDependencies.Add(
                            $"[{node.Type.Name}] missing dependency '{depType.Name}'"
                        );
                }
            }

            if (missingDependencies.Count > 0)
                _circularErrorMsg =
                    "Missing Dependencies:\n" + string.Join("\n", missingDependencies);

            // 3. 拓扑排序 (Kahn's Algorithm) + 深度计算 + 优先级检查
            var inDegree = _nodeMap.Values.ToDictionary(n => n.Type, n => n.Dependencies.Count);

            // 初始队列：入度为0的节点（Root），按 Priority 排序
            var queue = new Queue<ModuleNode>(
                _nodeMap.Values.Where(n => inDegree[n.Type] == 0).OrderBy(n => n.Priority)
            );

            while (queue.Count > 0)
            {
                ModuleNode current = queue.Dequeue();
                _sortedNodes.Add(current);

                // --- 健康检查逻辑 ---
                int maxDepPriority = int.MinValue;
                int maxDepDepth = -1;

                foreach (Type depType in current.Dependencies)
                {
                    ModuleNode depNode = _nodeMap[depType];
                    if (depNode.Priority > maxDepPriority)
                        maxDepPriority = depNode.Priority;
                    if (depNode.Depth > maxDepDepth)
                        maxDepDepth = depNode.Depth;
                }

                // 计算深度：基于依赖链最深处 + 1
                current.Depth = maxDepDepth + 1;

                // 检查优先级倒置：如果我的优先级比依赖项还小（或者相等），这虽然在拓扑排序中能跑通，
                // 但在语义上通常是错的（通常依赖项 Priority 数值更小，启动更早）。
                // Asaki 规则：Priority 越小越早。
                // 如果 DepPriority = 100, MyPriority = 50。
                // MyPriority < DepPriority，意味着我想在 50 启动，但必须等 100。
                // 这是一个倒置警告。
                // 正确情况：DepPriority (100) < MyPriority (200)。

                if (current.Dependencies.Count > 0 && current.Priority <= maxDepPriority)
                {
                    current.PriorityWarning =
                        $"Priority ({current.Priority}) <= Dependency Max Priority ({maxDepPriority}).\nIdeally, dependent modules should have higher priority values (run later).";
                }
                // ------------------

                // 处理后续节点
                var sortedDependents = current
                    .Dependents.Select(t => _nodeMap[t])
                    .OrderBy(n => n.Priority); // 同级按 Priority 排

                foreach (ModuleNode neighbor in sortedDependents)
                {
                    inDegree[neighbor.Type]--;
                    if (inDegree[neighbor.Type] == 0)
                        queue.Enqueue(neighbor);
                }
            }

            if (_sortedNodes.Count != _nodeMap.Count && string.IsNullOrEmpty(_circularErrorMsg))
                _circularErrorMsg = "Circular dependency detected!";
        }

        // ... OpenScriptFile & UpdateScriptPriority 保持不变 ...
        private void OpenScriptFile(Type type)
        {
            string[] guids = AssetDatabase.FindAssets("t:script " + type.Name);
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                MonoScript obj = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                AssetDatabase.OpenAsset(obj);
            }
        }

        private void UpdateScriptPriority(Type type, int newPriority)
        {
            string[] guids = AssetDatabase.FindAssets("t:script " + type.Name);
            if (guids.Length == 0)
                return;
            string fullPath = Path.GetFullPath(AssetDatabase.GUIDToAssetPath(guids[0]));
            try
            {
                string content = File.ReadAllText(fullPath);
                Regex regex = new Regex(@"\[AsakiModule\s*\(\s*(\d+)");
                if (regex.IsMatch(content))
                {
                    File.WriteAllText(
                        fullPath,
                        regex.Replace(
                            content,
                            m => m.Value.Replace(m.Groups[1].Value, newPriority.ToString())
                        )
                    );
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }
    }
}
