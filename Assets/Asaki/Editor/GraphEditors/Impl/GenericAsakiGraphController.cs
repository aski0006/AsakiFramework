using Asaki.Core.Graphs;
using UnityEngine.UIElements;

namespace Asaki.Editor.GraphEditors.Impl
{
    public class GenericAsakiGraphController : IAsakiGraphViewController
    {
        private readonly AsakiGraphAsset _graph;
        private AsakiGraphView _graphView; // 持有引用

        public GenericAsakiGraphController(AsakiGraphAsset graph)
        {
            _graph = graph;
        }

        public VisualElement CreateGraphView()
        {
            _graphView = new AsakiGraphView(_graph);
            _graphView.style.flexGrow = 1; // 填满窗口
            return _graphView;
        }

        public void Update() { }

        public void Save() { }

        public void Dispose() { }
    }
}
