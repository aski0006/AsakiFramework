using System;
using System.Collections.Generic;
using System.Reflection;
using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.Graphs;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Asaki.Editor.GraphEditors
{
    public class AsakiNodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private AsakiGraphView _graphView;
        private AsakiGraphAsset _graphAsset;
        private EditorWindow _window;
        private Texture2D _indentationIcon;

        public void Initialize(
            AsakiGraphView graphView,
            AsakiGraphAsset graphAsset,
            EditorWindow window
        )
        {
            _graphView = graphView;
            _graphAsset = graphAsset;
            _window = window;

            // 创建一个透明图片用于缩进排版（纯视觉优化）
            _indentationIcon = new Texture2D(1, 1);
            _indentationIcon.SetPixel(0, 0, new Color(0, 0, 0, 0));
            _indentationIcon.Apply();
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),
            };

            TypeCache.TypeCollection nodeTypes = TypeCache.GetTypesDerivedFrom<AsakiNodeBase>();

            foreach (Type type in nodeTypes)
            {
                if (type.IsAbstract)
                    continue;

                // ★ 核心变革：上下文过滤
                // 1. 获取节点的 [GraphContext] 特性
                AsakiGraphContextAttribute contextAttr =
                    Attribute.GetCustomAttribute(type, typeof(AsakiGraphContextAttribute))
                    as AsakiGraphContextAttribute;

                // 2. 如果没有标记，或者标记的图类型与当前打开的图不匹配，则跳过
                // _graphAsset.GetType() 获取当前打开的图类型
                if (contextAttr == null || contextAttr.GraphType != _graphAsset.GetType())
                    continue;

                // 3. 使用 Path 属性构建菜单 (支持 Log/DebugLog 这种子菜单)
                // 这里为了简单，暂且直接用 Name，后续可以解析 Path 分组
                tree.Add(
                    new SearchTreeEntry(new GUIContent(type.Name, _indentationIcon))
                    {
                        userData = type,
                        level = 1,
                    }
                );
            }

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            Type type = SearchTreeEntry.userData as Type;
            if (type == null)
                return false;

            VisualElement windowRoot = _window.rootVisualElement;
            Vector2 windowMousePosition = windowRoot.ChangeCoordinatesTo(
                windowRoot.parent,
                context.screenMousePosition - _window.position.position
            );
            Vector2 graphMousePosition = _graphView.contentViewContainer.WorldToLocal(
                windowMousePosition
            );

            // 调用 IO 工具创建数据
            MethodInfo method = typeof(AsakiGraphIOUtils)
                .GetMethod("AddNode")
                ?.MakeGenericMethod(type);
            if (method != null)
            {
                // ★ 修复：获取返回值 (新创建的 Node 数据)
                AsakiNodeBase newNode =
                    method.Invoke(null, new object[] { _graphAsset, graphMousePosition })
                    as AsakiNodeBase;

                // ★ 修复：立刻在 View 中创建对应的视觉节点
                if (newNode != null)
                {
                    _graphView.CreateNodeView(newNode);
                }

                return true;
            }

            return false;
        }
    }
}
