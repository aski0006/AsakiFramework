using Asaki.Core.Graphs;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.GraphEditors
{
    public static class AsakiGraphIOUtils
    {
        // ★ T 必须有无参构造函数 (new())，不再是 ScriptableObject
        public static T AddNode<T>(AsakiGraphAsset graph, Vector2 pos)
            where T : AsakiNodeBase, new()
        {
            // 1. 记录 Graphs 的状态 (因为节点是 Graphs 的一部分)
            Undo.RecordObject(graph, "Add Node");

            // 2. 创建纯 C# 对象
            T node = new T();
            node.GUID = System.Guid.NewGuid().ToString();
            node.Position = pos;

            // 如果有基类初始化逻辑
            node.OnCreated();

            // 3. 直接加入列表
            graph.Nodes.Add(node);

            // 4. 标记脏数据
            EditorUtility.SetDirty(graph);

            return node;
        }

        public static void DeleteNode(AsakiGraphAsset graph, AsakiNodeBase node)
        {
            Undo.RecordObject(graph, "Delete Node");

            // 1. 移除节点
            graph.Nodes.Remove(node);

            // 2. 清理相关连线
            graph.Edges.RemoveAll(e =>
                e.BaseNodeGUID == node.GUID || e.TargetNodeGUID == node.GUID
            );

            EditorUtility.SetDirty(graph);
        }

        public static void AddEdge(AsakiGraphAsset graph, AsakiEdgeData edgeData)
        {
            Undo.RecordObject(graph, "Add Edge");
            // 简单的去重检查
            bool exists = graph.Edges.Exists(e =>
                e.BaseNodeGUID == edgeData.BaseNodeGUID
                && e.BasePortName == edgeData.BasePortName
                && e.TargetNodeGUID == edgeData.TargetNodeGUID
                && e.TargetPortName == edgeData.TargetPortName
            );

            if (!exists)
            {
                graph.Edges.Add(edgeData);
            }
            EditorUtility.SetDirty(graph);
        }

        public static void RemoveEdge(AsakiGraphAsset graph, AsakiEdgeData edgeData)
        {
            Undo.RecordObject(graph, "Remove Edge");
            graph.Edges.RemoveAll(e =>
                e.BaseNodeGUID == edgeData.BaseNodeGUID
                && e.BasePortName == edgeData.BasePortName
                && e.TargetNodeGUID == edgeData.TargetNodeGUID
                && e.TargetPortName == edgeData.TargetPortName
            );
            EditorUtility.SetDirty(graph);
        }
    }
}
