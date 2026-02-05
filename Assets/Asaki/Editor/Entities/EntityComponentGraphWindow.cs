using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Asaki.Core.Architecture.Entities;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Entities
{
    /// <summary>
    /// 实体组件图 - 可视化实体-组件关系
    /// </summary>
    public class EntityComponentGraphWindow : EditorWindow
    {
        [MenuItem("Asaki/Entities/Component Graph", false, 30)]
        public static void ShowWindow()
        {
            GetWindow<EntityComponentGraphWindow>("Component Graph");
        }

        // 图形数据
        private List<Node> _nodes = new();
        private List<Edge> _edges = new();
        private IEntityWorld _world;

        // 视图状态
        private Vector2 _scrollPosition;
        private Vector2 _graphOffset = Vector2.zero;
        private float _zoom = 1f;
        private Node _selectedNode;
        private Node _hoveredNode;

        // 布局参数
        private const float NodeWidth = 140f;
        private const float NodeHeight = 60f;
        private const float EntitySpacing = 180f;
        private const float ComponentSpacing = 80f;
        private const float ComponentYOffset = 100f;

        // 颜色
        private readonly Color EntityNodeColor = new(0.2f, 0.6f, 0.9f);
        private readonly Color ComponentNodeColor = new(0.3f, 0.8f, 0.4f);
        private readonly Color TagNodeColor = new(0.9f, 0.7f, 0.2f);
        private readonly Color SelectedNodeColor = new(1f, 0.5f, 0.2f);
        private readonly Color EdgeColor = new(0.5f, 0.5f, 0.5f, 0.5f);

        // 运行时刷新
        private double _lastUpdateTime;
        private const double RefreshInterval = 1f;
        private bool _autoRefresh = true;

        private class Node
        {
            public int Id;
            public string Label;
            public Vector2 Position;
            public NodeType Type;
            public object Data;
            public Rect Rect => new(Position.x, Position.y, NodeWidth, NodeHeight);
            public List<Node> ConnectedNodes = new();
        }

        private class Edge
        {
            public Node From;
            public Node To;
        }

        private enum NodeType
        {
            Entity,
            Component,
            Tag,
        }

        private void OnEnable()
        {
            RefreshGraph();
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;
            if (!_autoRefresh)
                return;

            if (EditorApplication.timeSinceStartup - _lastUpdateTime > RefreshInterval)
            {
                RefreshGraph();
                _lastUpdateTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void RefreshGraph()
        {
            _nodes.Clear();
            _edges.Clear();

            // 查找世界
            var worlds = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .OfType<IEntityWorld>()
                .ToList();

            if (worlds.Count == 0)
                return;
            _world = worlds[0];

            var entities = _world.GetAllEntities().ToList();
            int nodeId = 0;

            // 收集所有唯一的组件类型
            var allComponentTypes = new Dictionary<Type, Node>();

            // 创建实体节点
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity == null)
                    continue;

                var entityNode = new Node
                {
                    Id = nodeId++,
                    Label = $"Entity\n{entity.Id}",
                    Position = new Vector2(50 + i * EntitySpacing, 50),
                    Type = NodeType.Entity,
                    Data = entity,
                };

                _nodes.Add(entityNode);

                // 创建组件节点和连接
                int compIndex = 0;
                foreach (var comp in entity.GetAllComponents())
                {
                    if (comp == null)
                        continue;

                    var compType = comp.GetType();
                    Node componentNode;

                    // 检查是否已存在此组件类型的节点
                    if (!allComponentTypes.TryGetValue(compType, out componentNode))
                    {
                        bool isTag = compType.IsSubclassOf(typeof(TagComponent));

                        componentNode = new Node
                        {
                            Id = nodeId++,
                            Label = compType.Name,
                            Position = new Vector2(
                                50 + i * EntitySpacing + compIndex * 20,
                                ComponentYOffset + compIndex * ComponentSpacing
                            ),
                            Type = isTag ? NodeType.Tag : NodeType.Component,
                            Data = compType,
                        };

                        allComponentTypes[compType] = componentNode;
                        _nodes.Add(componentNode);
                    }

                    // 创建边
                    _edges.Add(new Edge { From = entityNode, To = componentNode });
                    entityNode.ConnectedNodes.Add(componentNode);
                    compIndex++;
                }
            }

            // 重新布局
            LayoutGraph();
        }

        private void LayoutGraph()
        {
            var entityNodes = _nodes.Where(n => n.Type == NodeType.Entity).ToList();
            var componentNodes = _nodes.Where(n => n.Type != NodeType.Entity).ToList();

            // 布局实体节点（水平排列）
            for (int i = 0; i < entityNodes.Count; i++)
            {
                entityNodes[i].Position = new Vector2(50 + i * EntitySpacing, 50);
            }

            // 布局组件节点（按类型分组，垂直排列）
            var groupedComponents = componentNodes
                .GroupBy(n => n.Label)
                .SelectMany(
                    (g, i) =>
                        g.Select(
                            (n, j) =>
                                new
                                {
                                    Node = n,
                                    GroupIndex = i,
                                    IndexInGroup = j,
                                }
                        )
                )
                .ToList();

            foreach (var item in groupedComponents)
            {
                item.Node.Position = new Vector2(
                    50 + item.GroupIndex * 200,
                    ComponentYOffset + item.IndexInGroup * ComponentSpacing
                );
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawGraph();
            DrawInfoPanel();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshGraph();
            }

            _autoRefresh = GUILayout.Toggle(
                _autoRefresh,
                "Auto",
                EditorStyles.toolbarButton,
                GUILayout.Width(50)
            );

            GUILayout.Space(10);

            // 缩放控制
            GUILayout.Label("Zoom:", GUILayout.Width(40));
            _zoom = EditorGUILayout.Slider(_zoom, 0.5f, 2f, GUILayout.Width(100));

            GUILayout.FlexibleSpace();

            GUILayout.Label(
                $"Entities: {_nodes.Count(n => n.Type == NodeType.Entity)} | "
                    + $"Components: {_nodes.Count(n => n.Type == NodeType.Component)} | "
                    + $"Tags: {_nodes.Count(n => n.Type == NodeType.Tag)}",
                EditorStyles.miniLabel
            );

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGraph()
        {
            var graphRect = new Rect(0, 20, position.width - 200, position.height - 20);

            // 处理事件
            HandleGraphEvents(graphRect);

            // 绘制背景网格
            DrawGrid(graphRect);

            // 使用滚动视图
            GUI.BeginGroup(graphRect);

            // 绘制边
            foreach (var edge in _edges)
            {
                DrawEdge(edge);
            }

            // 绘制节点
            foreach (var node in _nodes)
            {
                DrawNode(node);
            }

            GUI.EndGroup();
        }

        private void DrawGrid(Rect rect)
        {
            var gridSpacing = 50f * _zoom;
            var offsetX = _graphOffset.x % gridSpacing;
            var offsetY = _graphOffset.y % gridSpacing;

            Handles.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);

            // 垂直线
            for (float x = offsetX; x < rect.width; x += gridSpacing)
            {
                Handles.DrawLine(new Vector3(x, 0), new Vector3(x, rect.height));
            }

            // 水平线
            for (float y = offsetY; y < rect.height; y += gridSpacing)
            {
                Handles.DrawLine(new Vector3(0, y), new Vector3(rect.width, y));
            }
        }

        private void DrawNode(Node node)
        {
            var rect = node.Rect;
            rect.position = (rect.position + _graphOffset) * _zoom;
            rect.size *= _zoom;

            // 确定颜色
            Color baseColor = node.Type switch
            {
                NodeType.Entity => EntityNodeColor,
                NodeType.Tag => TagNodeColor,
                _ => ComponentNodeColor,
            };

            if (node == _selectedNode)
                baseColor = SelectedNodeColor;
            else if (node == _hoveredNode)
                baseColor = Color.Lerp(baseColor, Color.white, 0.3f);

            // 绘制节点背景
            GUI.color = baseColor;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // 绘制边框
            var borderColor = node == _selectedNode ? Color.white : Color.black;
            DrawRectBorder(rect, borderColor, 2f);

            // 绘制文本
            var style = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(11 * _zoom),
                normal = { textColor = Color.white },
            };

            GUI.Label(rect, node.Label, style);
        }

        private void DrawEdge(Edge edge)
        {
            var fromPos = (edge.From.Rect.center + _graphOffset) * _zoom;
            var toPos = (edge.To.Rect.center + _graphOffset) * _zoom;

            Handles.color = EdgeColor;
            Handles.DrawLine(fromPos, toPos, 2f);

            // 绘制箭头
            var direction = (toPos - fromPos).normalized;
            var arrowPos = toPos - direction * NodeHeight * _zoom * 0.5f;
            var arrowSize = 8f * _zoom;

            var perpendicular = new Vector2(-direction.y, direction.x);
            var arrowPoint1 = arrowPos - direction * arrowSize + perpendicular * arrowSize * 0.5f;
            var arrowPoint2 = arrowPos - direction * arrowSize - perpendicular * arrowSize * 0.5f;

            Handles.DrawAAConvexPolygon(arrowPos, arrowPoint1, arrowPoint2);
        }

        /// <summary>
        /// 绘制矩形边框
        /// </summary>
        private void DrawRectBorder(Rect rect, Color color, float thickness)
        {
            Handles.color = color;
            Vector3[] points = new Vector3[5];
            points[0] = new Vector3(rect.x, rect.y, 0);
            points[1] = new Vector3(rect.x + rect.width, rect.y, 0);
            points[2] = new Vector3(rect.x + rect.width, rect.y + rect.height, 0);
            points[3] = new Vector3(rect.x, rect.y + rect.height, 0);
            points[4] = points[0];
            Handles.DrawAAPolyLine(thickness, points);
        }

        private void HandleGraphEvents(Rect rect)
        {
            var e = Event.current;
            if (!rect.Contains(e.mousePosition))
                return;

            var mousePos = (e.mousePosition - new Vector2(rect.x, rect.y)) / _zoom - _graphOffset;

            // 检测悬停
            _hoveredNode = null;
            foreach (var node in _nodes)
            {
                if (node.Rect.Contains(mousePos))
                {
                    _hoveredNode = node;
                    break;
                }
            }

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (_hoveredNode != null)
                    {
                        _selectedNode = _hoveredNode;
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (_hoveredNode == null)
                    {
                        _graphOffset += e.delta / _zoom;
                        e.Use();
                    }
                    break;

                case EventType.ScrollWheel:
                    _zoom = Mathf.Clamp(_zoom - e.delta.y * 0.01f, 0.5f, 2f);
                    e.Use();
                    break;
            }
        }

        private void DrawInfoPanel()
        {
            var panelRect = new Rect(position.width - 200, 20, 200, position.height - 20);

            GUILayout.BeginArea(panelRect, EditorStyles.helpBox);

            if (_selectedNode == null)
            {
                GUILayout.Label(
                    "Select a node to view details",
                    EditorStyles.centeredGreyMiniLabel
                );
            }
            else
            {
                DrawNodeDetails(_selectedNode);
            }

            GUILayout.EndArea();
        }

        private void DrawNodeDetails(Node node)
        {
            GUILayout.Label(
                $"<b>{node.Label}</b>",
                new GUIStyle(EditorStyles.label) { richText = true }
            );
            GUILayout.Space(10);

            GUILayout.Label($"Type: {node.Type}", EditorStyles.miniLabel);
            GUILayout.Label(
                $"Position: ({node.Position.x:F0}, {node.Position.y:F0})",
                EditorStyles.miniLabel
            );

            if (node.Type == NodeType.Entity && node.Data is IEntity entity)
            {
                GUILayout.Space(10);
                GUILayout.Label("Entity Info:", EditorStyles.miniBoldLabel);
                GUILayout.Label($"ID: {entity.Id}", EditorStyles.miniLabel);
                GUILayout.Label($"Active: {entity.IsActive}", EditorStyles.miniLabel);
                GUILayout.Label($"Components: {entity.ComponentCount}", EditorStyles.miniLabel);

                if (Application.isPlaying && GUILayout.Button("Select in Debugger"))
                {
                    EntityDebuggerWindow.ShowWindow();
                }
            }
            else if (node.Data is Type compType)
            {
                GUILayout.Space(10);
                GUILayout.Label("Component Info:", EditorStyles.miniBoldLabel);
                GUILayout.Label($"Full Name: {compType.FullName}", EditorStyles.miniLabel);
                GUILayout.Label(
                    $"Is Tag: {compType.IsSubclassOf(typeof(TagComponent))}",
                    EditorStyles.miniLabel
                );

                var connectedEntities = node.ConnectedNodes.Count;
                GUILayout.Label($"Used by: {connectedEntities} entities", EditorStyles.miniLabel);
            }

            GUILayout.FlexibleSpace();

            // 图例
            GUILayout.Label("Legend:", EditorStyles.miniBoldLabel);
            DrawLegendItem("Entity", EntityNodeColor);
            DrawLegendItem("Component", ComponentNodeColor);
            DrawLegendItem("Tag", TagNodeColor);
        }

        private void DrawLegendItem(string label, Color color)
        {
            EditorGUILayout.BeginHorizontal();
            GUI.color = color;
            GUILayout.Box(GUIContent.none, GUILayout.Width(20), GUILayout.Height(12));
            GUI.color = Color.white;
            GUILayout.Label(label, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
    }
}
