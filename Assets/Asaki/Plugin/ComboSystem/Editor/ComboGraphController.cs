using Asaki.Core.Graphs;
using Asaki.Editor.GraphEditors;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Asaki.Plugin.ComboSystem.Editor
{
    /// <summary>
    /// 连招图视图控制器
    /// 实现IAsakiGraphViewController接口，集成到AsakiGraphWindow
    /// </summary>
    public class ComboGraphController : IAsakiGraphViewController
    {
        private readonly ComboGraphAsset _graph;
        private ComboGraphView _graphView;

        public ComboGraphController(ComboGraphAsset graph)
        {
            _graph = graph;
        }

        /// <summary>
        /// 创建GraphView视觉元素
        /// </summary>
        public VisualElement CreateGraphView()
        {
            _graphView = new ComboGraphView(_graph);
            return _graphView;
        }

        /// <summary>
        /// 每帧更新逻辑
        /// </summary>
        public void Update()
        {
            // 处理复制/粘贴等快捷键
            // 当前由GraphView内部处理
        }

        /// <summary>
        /// 保存逻辑
        /// </summary>
        public void Save()
        {
            if (_graph != null)
            {
                EditorUtility.SetDirty(_graph);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            _graphView = null;
        }
    }

    /// <summary>
    /// 连招图注册
    /// 在编辑器加载时自动注册到AsakiGraphWindow
    /// </summary>
    [InitializeOnLoad]
    public static class ComboGraphRegistration
    {
        static ComboGraphRegistration()
        {
            // 注册ComboGraphAsset到AsakiGraphWindow
            AsakiGraphWindow.Register<ComboGraphAsset>(graph => new ComboGraphController(graph));
        }
    }
}
