using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.Graphs;
using Asaki.Editor.GraphEditors;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Asaki.Plungin.ComboSystem.Editor
{
    /// <summary>
    /// 连招节点搜索窗口
    /// </summary>
    public class ComboNodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private ComboGraphView _graphView;
        private ComboGraphAsset _graphAsset;
        private EditorWindow _window;
        private Texture2D _indentationIcon;

        public void Initialize(
            ComboGraphView graphView,
            ComboGraphAsset graphAsset,
            EditorWindow window
        )
        {
            _graphView = graphView;
            _graphAsset = graphAsset;
            _window = window;

            // 创建透明图片用于缩进排版
            _indentationIcon = new Texture2D(1, 1);
            _indentationIcon.SetPixel(0, 0, new Color(0, 0, 0, 0));
            _indentationIcon.Apply();
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("创建连招节点"), 0),
            };

            // 按类别分组 - 使用分层路径（如"招式/Move"）
            var categorizedNodes = new Dictionary<string, List<Type>>();

            TypeCache.TypeCollection nodeTypes = TypeCache.GetTypesDerivedFrom<AsakiNodeBase>();

            foreach (Type type in nodeTypes)
            {
                if (type.IsAbstract)
                    continue;

                // 获取节点的 [GraphContext] 特性
                var contextAttr =
                    Attribute.GetCustomAttribute(type, typeof(AsakiGraphContextAttribute))
                    as AsakiGraphContextAttribute;

                // 检查是否匹配 ComboGraphAsset
                if (
                    contextAttr == null
                    || !typeof(ComboGraphAsset).IsAssignableFrom(contextAttr.GraphType)
                )
                    continue;

                // 使用完整路径作为分类（如"招式/Move"）
                string category = contextAttr.Path;
                if (!categorizedNodes.ContainsKey(category))
                {
                    categorizedNodes[category] = new List<Type>();
                }
                categorizedNodes[category].Add(type);
            }

            // 构建分层树形结构
            var categoryGroups = new Dictionary<string, SearchTreeGroupEntry>();

            foreach (var category in categorizedNodes.OrderBy(c => c.Key))
            {
                // 解析分层路径（如"招式/Move"）
                string[] pathParts = category.Key.Split('/');
                string currentPath = "";
                SearchTreeGroupEntry parentGroup = null;

                for (int i = 0; i < pathParts.Length; i++)
                {
                    string part = pathParts[i];
                    currentPath = string.IsNullOrEmpty(currentPath)
                        ? part
                        : $"{currentPath}/{part}";

                    if (!categoryGroups.ContainsKey(currentPath))
                    {
                        // 创建分组
                        var group = new SearchTreeGroupEntry(
                            new GUIContent(part),
                            i + 1 // 层级
                        );

                        categoryGroups[currentPath] = group;

                        if (parentGroup == null)
                        {
                            // 顶级分组
                            tree.Add(group);
                        }
                        // 子分组会在父分组之后添加
                    }

                    parentGroup = categoryGroups[currentPath];
                }

                // 添加该分类下的节点
                string parentPath = string.Join("/", pathParts);
                foreach (Type type in category.Value)
                {
                    string nodeName = GetNodeDisplayName(type);
                    var entry = new SearchTreeEntry(new GUIContent(nodeName, _indentationIcon))
                    {
                        userData = type,
                        level = pathParts.Length + 1,
                    };
                    tree.Add(entry);
                }
            }

            // 添加快速创建区域
            tree.Add(new SearchTreeGroupEntry(new GUIContent("快速创建"), 1));
            tree.Add(
                new SearchTreeEntry(new GUIContent("轻攻击招式", _indentationIcon))
                {
                    userData = new QuickCreateData
                    {
                        NodeType = typeof(MoveNode),
                        InputType = "LightAttack",
                    },
                    level = 2,
                }
            );
            tree.Add(
                new SearchTreeEntry(new GUIContent("重攻击招式", _indentationIcon))
                {
                    userData = new QuickCreateData
                    {
                        NodeType = typeof(MoveNode),
                        InputType = "HeavyAttack",
                    },
                    level = 2,
                }
            );
            tree.Add(
                new SearchTreeEntry(new GUIContent("轻攻击转换", _indentationIcon))
                {
                    userData = new QuickCreateData
                    {
                        NodeType = typeof(TransitionNode),
                        InputType = "LightAttack",
                    },
                    level = 2,
                }
            );

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            var userData = searchTreeEntry.userData;
            if (userData == null)
                return false;

            // 计算鼠标位置
            Vector2 windowMousePosition = CalculateMousePosition(context);
            Vector2 graphMousePosition = _graphView.contentViewContainer.WorldToLocal(
                windowMousePosition
            );

            // 处理快速创建
            if (userData is QuickCreateData quickData)
            {
                return CreateQuickNode(quickData, graphMousePosition);
            }

            // 处理普通节点创建
            if (userData is Type nodeType)
            {
                return CreateNode(nodeType, graphMousePosition);
            }

            return false;
        }

        /// <summary>
        /// 计算鼠标位置
        /// </summary>
        Vector2 CalculateMousePosition(SearchWindowContext context)
        {
            VisualElement windowRoot = _window.rootVisualElement;
            return windowRoot.ChangeCoordinatesTo(
                windowRoot.parent,
                context.screenMousePosition - _window.position.position
            );
        }

        /// <summary>
        /// 创建普通节点
        /// </summary>
        bool CreateNode(Type nodeType, Vector2 position)
        {
            MethodInfo method = typeof(AsakiGraphIOUtils)
                .GetMethod("AddNode")
                ?.MakeGenericMethod(nodeType);

            if (method != null)
            {
                AsakiNodeBase newNode =
                    method.Invoke(null, new object[] { _graphAsset, position }) as AsakiNodeBase;

                if (newNode != null)
                {
                    _graphView.CreateNodeView(newNode);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 快速创建节点
        /// </summary>
        bool CreateQuickNode(QuickCreateData data, Vector2 position)
        {
            MethodInfo method = typeof(AsakiGraphIOUtils)
                .GetMethod("AddNode")
                ?.MakeGenericMethod(data.NodeType);

            if (method != null)
            {
                AsakiNodeBase newNode =
                    method.Invoke(null, new object[] { _graphAsset, position }) as AsakiNodeBase;

                if (newNode != null)
                {
                    // 设置快速创建参数
                    if (newNode is MoveNode moveNode)
                    {
                        moveNode.MoveData.MoveName = GetDefaultMoveName(data.InputType);
                        moveNode.MoveData.MoveId =
                            $"move_{data.InputType.ToLower()}_{System.Guid.NewGuid().ToString().Substring(0, 4)}";
                    }
                    else if (newNode is TransitionNode transitionNode)
                    {
                        transitionNode.InputType = data.InputType;
                    }

                    _graphView.CreateNodeView(newNode);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取节点显示名称
        /// </summary>
        string GetNodeDisplayName(Type type)
        {
            // 可以根据类型返回更友好的名称
            switch (type.Name)
            {
                case "MoveNode":
                    return "⚔️ 招式节点";
                case "TransitionNode":
                    return "➡️ 转换节点";
                case "EntryNode":
                    return "🚦 入口节点";
                case "EndNode":
                    return "🏁 结束节点";
                case "ConditionNode":
                    return "❓ 条件节点";
                default:
                    return type.Name;
            }
        }

        /// <summary>
        /// 获取默认招式名称
        /// </summary>
        string GetDefaultMoveName(string inputType)
        {
            var def = ComboInputTypeRegistry.GetDefinition(inputType);
            return def?.DisplayName ?? inputType;
        }

        /// <summary>
        /// 快速创建数据
        /// </summary>
        class QuickCreateData
        {
            public Type NodeType;
            public string InputType;
        }
    }
}
