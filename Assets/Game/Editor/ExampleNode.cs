using Asaki.Core.Attributes;
using Asaki.Core.Graphs;
using UnityEngine;

namespace Game.Editor
{
    [AsakiGraphContext(typeof(ExampleGraph), "ExampleGraph")]
    public class ExampleNode : AsakiNodeBase
    {
        [AsakiNodeInput] public float a;
    }

    [CreateAssetMenu(menuName = "Asaki/ExampleGraph")]
    public class ExampleGraph : AsakiGraphAsset{}

    [AsakiCustomGraphEditor(typeof(ExampleGraph))]
    public class ExampleGraphController : GenericAsakiGraphController
    {
        public ExampleGraphController(AsakiGraphAsset graph) : base(graph) { }
    }
}
