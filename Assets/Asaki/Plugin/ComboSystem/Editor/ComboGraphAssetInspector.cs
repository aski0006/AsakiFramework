using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Asaki.Plugin.ComboSystem.Editor
{
    /// <summary>
    /// ComboGraphAsset 自定义 Inspector
    /// 提供便捷的导出按钮和统计信息
    /// </summary>
    [CustomEditor(typeof(ComboGraphAsset))]
    public class ComboGraphAssetInspector : UnityEditor.Editor
    {
        private bool _showExportSettings = true;
        private bool _showStatistics = true;

        public override void OnInspectorGUI()
        {
            var graph = target as ComboGraphAsset;
            if (graph == null)
                return;

            // 标题
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("连招图表设置", EditorStyles.largeLabel);
            EditorGUILayout.Space(5);

            // 基本信息
            EditorGUILayout.LabelField("基本信息", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ComboTreeId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Description"));
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // 设置
            EditorGUILayout.LabelField("连招设置", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("InputBufferWindow"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("MaxComboDuration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("MaxComboLength"));
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // 统计信息
            _showStatistics = EditorGUILayout.Foldout(_showStatistics, "图表统计", true);
            if (_showStatistics)
            {
                DrawStatistics(graph);
            }

            EditorGUILayout.Space(10);

            // 导出设置
            _showExportSettings = EditorGUILayout.Foldout(_showExportSettings, "导出设置", true);
            if (_showExportSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OutputPath"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            // 重置策略
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ResetStrategies"), true);

            EditorGUILayout.Space(10);

            // 操作按钮
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
            DrawActionButtons(graph);

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 绘制统计信息
        /// </summary>
        private void DrawStatistics(ComboGraphAsset graph)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField(
                $"总节点数: {graph.Nodes?.Count ?? 0}",
                EditorStyles.boldLabel
            );
            EditorGUILayout.LabelField(
                $"总边数: {graph.Edges?.Count ?? 0}",
                EditorStyles.boldLabel
            );

            if (graph.Nodes != null)
            {
                int moveCount = 0,
                    transitionCount = 0,
                    entryCount = 0,
                    endCount = 0,
                    conditionCount = 0;

                foreach (var node in graph.Nodes)
                {
                    switch (node)
                    {
                        case MoveNode _:
                            moveCount++;
                            break;
                        case TransitionNode _:
                            transitionCount++;
                            break;
                        case EntryNode _:
                            entryCount++;
                            break;
                        case EndNode _:
                            endCount++;
                            break;
                        case ConditionNode _:
                            conditionCount++;
                            break;
                    }
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("节点分布:", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"招式节点: {moveCount}");
                EditorGUILayout.LabelField($"转换节点: {transitionCount}");
                EditorGUILayout.LabelField($"入口节点: {entryCount}");
                EditorGUILayout.LabelField($"结束节点: {endCount}");
                EditorGUILayout.LabelField($"条件节点: {conditionCount}");
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制操作按钮
        /// </summary>
        private void DrawActionButtons(ComboGraphAsset graph)
        {
            EditorGUILayout.BeginHorizontal();

            // 打开编辑器按钮
            if (GUILayout.Button("打开图表编辑器", GUILayout.Height(30)))
            {
                OpenGraphEditor(graph);
            }

            // 导出按钮
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("导出 ComboTree", GUILayout.Height(30)))
            {
                ExportGraph(graph);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 验证按钮
            if (GUILayout.Button("验证图表"))
            {
                ValidateGraph(graph);
            }
        }

        /// <summary>
        /// 打开图表编辑器
        /// </summary>
        private void OpenGraphEditor(ComboGraphAsset graph)
        {
            // 使用 AsakiGraphWindow 打开
            var window = EditorWindow.GetWindow<Asaki.Editor.GraphEditors.AsakiGraphWindow>(
                "Asaki Graph Editor"
            );
            if (window != null)
            {
                // 通过反射调用 OpenInstance 方法
                var method = typeof(Asaki.Editor.GraphEditors.AsakiGraphWindow).GetMethod(
                    "OpenInstance",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public
                );
                method?.Invoke(null, new object[] { graph });
            }
        }

        /// <summary>
        /// 导出图表
        /// </summary>
        private void ExportGraph(ComboGraphAsset graph)
        {
            // 确保输出目录存在
            if (string.IsNullOrEmpty(graph.OutputPath))
            {
                graph.OutputPath = "Assets/Game/FrameworkSettings/Combos";
            }

            if (!System.IO.Directory.Exists(graph.OutputPath))
            {
                System.IO.Directory.CreateDirectory(graph.OutputPath);
            }

            try
            {
                var comboTree = graph.ExportToComboTree();

                string fileName = string.IsNullOrEmpty(graph.ComboTreeId)
                    ? graph.name
                    : graph.ComboTreeId;
                string path = System.IO.Path.Combine(graph.OutputPath, $"{fileName}.asset");

                // 检查是否已存在
                var existing = AssetDatabase.LoadAssetAtPath<ComboTree>(path);
                if (existing != null)
                {
                    // 复制数据到现有资产
                    EditorUtility.CopySerialized(comboTree, existing);
                    EditorUtility.SetDirty(existing);
                    EditorUtility.DisplayDialog("导出成功", $"已更新 ComboTree: {path}", "确定");
                }
                else
                {
                    AssetDatabase.CreateAsset(comboTree, path);
                    EditorUtility.DisplayDialog("导出成功", $"已创建 ComboTree: {path}", "确定");
                }

                AssetDatabase.SaveAssets();
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<ComboTree>(path));
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("导出失败", $"导出时发生错误:\n{e.Message}", "确定");
                Debug.LogError($"[ComboGraph] 导出失败: {e}");
            }
        }

        /// <summary>
        /// 验证图表
        /// </summary>
        private void ValidateGraph(ComboGraphAsset graph)
        {
            var errors = new System.Collections.Generic.List<string>();
            var warnings = new System.Collections.Generic.List<string>();

            // 检查ID
            if (string.IsNullOrEmpty(graph.ComboTreeId))
            {
                errors.Add("ComboTreeId Cannot be null");
            }

            // 检查是否有招式节点
            var moveNodes = graph.Nodes?.OfType<MoveNode>().ToList();
            if (moveNodes == null || moveNodes.Count == 0)
            {
                errors.Add("ComboGraphAsset must contain at least one MoveNode");
            }
            else
            {
                // 检查招式ID唯一性
                var moveIds = new System.Collections.Generic.HashSet<string>();
                foreach (var moveNode in moveNodes)
                {
                    if (string.IsNullOrEmpty(moveNode.MoveData?.MoveId))
                    {
                        errors.Add($"MoveNode '{moveNode.MoveData?.MoveName}' MoveId is not set");
                    }
                    else if (!moveIds.Add(moveNode.MoveData.MoveId))
                    {
                        errors.Add($"Duplicate MoveId: {moveNode.MoveData.MoveId}");
                    }
                }
            }

            // 检查无效的输入类型
            if (graph.Nodes != null)
            {
                foreach (var transitionNode in graph.Nodes.OfType<TransitionNode>())
                {
                    if (!ComboInputTypeRegistry.HasType(transitionNode.InputType))
                    {
                        warnings.Add(
                            $"TransitionNode '{transitionNode.Title}' uses unknown input type: {transitionNode.InputType}"
                        );
                    }
                }
            }

            // 显示结果
            if (errors.Count == 0 && warnings.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Validation Complete",
                    "ComboGraphAsset validation passed with no issues.",
                    "OK"
                );
            }
            else
            {
                string message = "";
                if (errors.Count > 0)
                {
                    message +=
                        $"Errors ({errors.Count}):\n" + string.Join("\n", errors.Take(5)) + "\n\n";
                }
                if (warnings.Count > 0)
                {
                    message +=
                        $"Warnings ({warnings.Count}):\n" + string.Join("\n", warnings.Take(5));
                }
                EditorUtility.DisplayDialog("Validation Result", message, "OK");
            }
        }
    }
}
