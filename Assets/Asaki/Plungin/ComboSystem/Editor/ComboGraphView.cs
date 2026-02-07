using System;
using System.Collections.Generic;
using System.Linq;
using Asaki.Core.Graphs;
using Asaki.Editor.GraphEditors;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Asaki.Plungin.ComboSystem.Editor
{
    /// <summary>
    /// 连招图视图 - 继承自AsakiGraphView
    /// </summary>
    public class ComboGraphView : AsakiGraphView
    {
        public new ComboGraphAsset GraphAsset => base.GraphAsset as ComboGraphAsset;

        public ComboGraphView(ComboGraphAsset graph) : base(graph)
        {
            // 连招图特定的设置
            SetupComboToolbar();
        }

        /// <summary>
        /// 设置连招图工具栏
        /// </summary>
        void SetupComboToolbar()
        {
            // 创建工具栏
            var toolbar = new Toolbar();

            // 导出按钮
            var exportButton = new ToolbarButton(() => ExportComboTree())
            {
                text = "导出 ComboTree"
            };
            toolbar.Add(exportButton);

            // 验证按钮
            var validateButton = new ToolbarButton(() => ValidateGraph())
            {
                text = "验证"
            };
            toolbar.Add(validateButton);

            // 自动布局按钮
            var layoutButton = new ToolbarButton(() => AutoLayout())
            {
                text = "自动布局"
            };
            toolbar.Add(layoutButton);

            // 输入类型管理按钮
            var inputTypesButton = new ToolbarButton(() => ShowInputTypeManager())
            {
                text = "输入类型"
            };
            toolbar.Add(inputTypesButton);

            // 将工具栏插入到最前面
            Insert(0, toolbar);
        }

        /// <summary>
        /// 导出为ComboTree
        /// </summary>
        void ExportComboTree()
        {
            var comboTree = GraphAsset.ExportToComboTree();

            // 确保输出目录存在
            if (!System.IO.Directory.Exists(GraphAsset.OutputPath))
            {
                System.IO.Directory.CreateDirectory(GraphAsset.OutputPath);
            }

            string path = $"{GraphAsset.OutputPath}/{GraphAsset.ComboTreeId}.asset";

            // 检查是否已存在
            var existing = AssetDatabase.LoadAssetAtPath<ComboTree>(path);
            if (existing != null)
            {
                // 复制数据到现有资产
                EditorUtility.CopySerialized(comboTree, existing);
                EditorUtility.SetDirty(existing);
                Debug.Log($"[ComboGraph] 已更新 ComboTree: {path}");
            }
            else
            {
                AssetDatabase.CreateAsset(comboTree, path);
                Debug.Log($"[ComboGraph] 已导出 ComboTree: {path}");
            }

            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(comboTree);
        }

        /// <summary>
        /// 验证图表
        /// </summary>
        void ValidateGraph()
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // 检查招式ID唯一性
            var moveIds = new HashSet<string>();
            foreach (var node in GraphAsset.Nodes.OfType<MoveNode>())
            {
                if (string.IsNullOrEmpty(node.MoveData.MoveId))
                {
                    errors.Add($"招式节点 '{node.Title}' 没有设置MoveId");
                }
                else if (!moveIds.Add(node.MoveData.MoveId))
                {
                    errors.Add($"重复的MoveId: {node.MoveData.MoveId}");
                }
            }

            // 检查孤立节点
            var connectedNodes = new HashSet<string>();
            foreach (var edge in GraphAsset.Edges)
            {
                connectedNodes.Add(edge.BaseNodeGUID);
                connectedNodes.Add(edge.TargetNodeGUID);
            }

            foreach (var node in GraphAsset.Nodes)
            {
                // EntryNode 和 EndNode 可以是孤立节点（作为起点或终点）
                if (!connectedNodes.Contains(node.GUID) && !(node is EntryNode) && !(node is EndNode))
                {
                    warnings.Add($"孤立节点: {node.Title}");
                }
            }

            // 检查无效的输入类型
            foreach (var node in GraphAsset.Nodes.OfType<TransitionNode>())
            {
                if (!ComboInputTypeRegistry.HasType(node.InputType))
                {
                    warnings.Add($"转换节点使用未知的输入类型: {node.InputType}");
                }
            }

            // 显示结果
            if (errors.Count == 0 && warnings.Count == 0)
            {
                EditorUtility.DisplayDialog("验证完成", "图表验证通过，未发现问题。", "确定");
            }
            else
            {
                string message = "";
                if (errors.Count > 0)
                {
                    message += $"错误 ({errors.Count}):\n" + string.Join("\n", errors.Take(10)) + "\n\n";
                }
                if (warnings.Count > 0)
                {
                    message += $"警告 ({warnings.Count}):\n" + string.Join("\n", warnings.Take(10));
                }
                EditorUtility.DisplayDialog("验证结果", message, "确定");
            }
        }

        /// <summary>
        /// 自动布局
        /// </summary>
        void AutoLayout()
        {
            // 简单的自动布局实现
            var entryNodes = GraphAsset.Nodes.OfType<EntryNode>().ToList();
            if (entryNodes.Count == 0)
            {
                EditorUtility.DisplayDialog("自动布局", "没有找到入口节点，无法自动布局。", "确定");
                return;
            }

            float x = 100;
            float y = 100;
            float levelHeight = 150;

            // 从入口节点开始，按层级布局
            foreach (var entry in entryNodes)
            {
                LayoutNodeRecursive(entry, ref x, y, levelHeight, new HashSet<string>());
                x += 300;
            }

            // 刷新视图
            PopulateView();
        }

        void LayoutNodeRecursive(AsakiNodeBase node, ref float x, float y, float levelHeight, HashSet<string> visited)
        {
            if (visited.Contains(node.GUID)) return;
            visited.Add(node.GUID);

            node.Position = new Vector2(x, y);

            // 获取连接的下一级节点
            var nextNodes = GraphAsset.GetNextNodes(node);
            float childX = x + 250;
            float childY = y;

            foreach (var child in nextNodes)
            {
                LayoutNodeRecursive(child, ref childX, childY, levelHeight, visited);
                childY += levelHeight;
            }
        }

        /// <summary>
        /// 显示输入类型管理器
        /// </summary>
        void ShowInputTypeManager()
        {
            ComboInputTypeManagerWindow.ShowWindow();
        }

        /// <summary>
        /// 创建节点视图
        /// </summary>
        public override void CreateNodeView(AsakiNodeBase node)
        {
            var nodeView = new ComboNodeView(node, new SerializedObject(GraphAsset));
            AddElement(nodeView);
            nodeView.SetPosition(new Rect(node.Position, Vector2.zero));
        }
    }
}
