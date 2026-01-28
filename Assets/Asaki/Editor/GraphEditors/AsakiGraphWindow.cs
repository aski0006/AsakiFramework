using System;
using System.Collections.Generic;
using Asaki.Core.Graphs;
using Asaki.Editor.GraphEditors;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object; // 引用 UI Elements 扩展

namespace Asaki.Editor.GraphEditors
{
    public class AsakiGraphWindow : EditorWindow
    {
        private IAsakiGraphViewController _controller;
        private AsakiGraphDebugger _debugger;
        private static Dictionary<
            Type,
            Func<AsakiGraphAsset, IAsakiGraphViewController>
        > _registry = new Dictionary<Type, Func<AsakiGraphAsset, IAsakiGraphViewController>>();

        public static void Register<TGraph>(Func<TGraph, IAsakiGraphViewController> factory)
            where TGraph : AsakiGraphAsset
        {
            Type type = typeof(TGraph);
            if (_registry.ContainsKey(type))
                _registry[type] = (asset) => factory((TGraph)asset);
            else
                _registry.Add(type, (asset) => factory((TGraph)asset));
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            Object obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is not AsakiGraphAsset graphAsset)
                return false;

            Type type = graphAsset.GetType();
            if (!_registry.ContainsKey(type))
            {
                Debug.LogWarning(
                    $"[AsakiGraph] No controller registered for graph type: {type.Name}"
                );
                return false;
            }

            AsakiGraphWindow window = GetWindow<AsakiGraphWindow>("Asaki Graph Editor");
            window.Initialize(_registry[type](graphAsset));
            return true;
        }

        // 支持打开内存实例
        public static void OpenInstance(AsakiGraphAsset graph)
        {
            if (graph == null)
                return;
            Type type = graph.GetType();

            if (!_registry.ContainsKey(type))
            {
                Debug.LogWarning(
                    $"[AsakiGraph] No controller registered for graph type: {type.Name}"
                );
                return;
            }

            AsakiGraphWindow window = GetWindow<AsakiGraphWindow>("Asaki Graph Editor");
            window.Initialize(_registry[type](graph));
            window.Show();
            window.Focus();
        }

        private void Initialize(IAsakiGraphViewController controller)
        {
            if (_controller != null)
                _controller.Dispose();

            _controller = controller;

            // 清理旧 UI
            rootVisualElement.Clear();

            if (_controller != null)
            {
                AsakiGraphView graphView = _controller.CreateGraphView() as AsakiGraphView;
                if (graphView != null)
                {
                    // ★★★ [修复] 强制 GraphView 填满整个窗口 ★★★
                    // 如果不加这句，GraphView 高度可能为 0，导致看不见网格和节点
                    graphView.StretchToParentSize();

                    rootVisualElement.Add(graphView);

                    _debugger?.Dispose();
                    _debugger = new AsakiGraphDebugger(graphView);

                    // [可选优化] 自动聚焦所有节点 (延迟一帧执行以等待布局计算完毕)
                    rootVisualElement.schedule.Execute(() =>
                    {
                        graphView.FrameAll();
                    });
                }
            }
        }

        private void Update()
        {
            _controller?.Update();
        }

        private void OnDisable()
        {
            _controller?.Dispose();
            _debugger?.Dispose();
        }
    }
}
