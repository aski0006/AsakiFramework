using Asaki.Core.Attributes;
using Asaki.Core.Graphs;
using Asaki.Editor.GraphEditors;
using UnityEngine.UIElements;

namespace Asaki.Editor.ModuleSystem.Graph
{
    [AsakiCustomGraphEditor(typeof(AsakiModuleGraph))]
    public class AsakiModuleGraphController : IAsakiGraphViewController
    {
        private AsakiModuleGraph _graph;

        public AsakiModuleGraphController(AsakiModuleGraph graph)
        {
            _graph = graph;
        }

        public VisualElement CreateGraphView()
        {
            AsakiGraphView view = new AsakiGraphView(_graph);
            // 禁用创建新节点，因为这是生成的图
            view.nodeCreationRequest = null;
            return view;
        }

        public void Update() { }

        public void Save() { }

        public void Dispose() { }
    }
}
