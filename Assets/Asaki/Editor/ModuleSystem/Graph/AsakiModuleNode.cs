using System;
using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.Graphs;

namespace Asaki.Editor.ModuleSystem.Graph
{
    // 这是一个纯展示用的节点
    [Serializable]
    // 标记它属于 ModuleGraph 上下文
    [AsakiGraphContext(typeof(AsakiModuleGraph), "Module")]
    public class AsakiModuleNode : AsakiNodeBase
    {
        public string ModuleName;
        public int Priority;

        // 我们利用 Port 机制来显示连线
        // "In" 表示谁依赖我 (Dependents)
        [AsakiNodeInput("In", Multiple = true)]
        public AsakiFlowPort Input;

        // "Out" 表示我依赖谁 (Dependencies)
        [AsakiNodeOutput("Out", Multiple = true)]
        public AsakiFlowPort Output;

        public override string Title => $"{ModuleName} ({Priority})";
    }

    // 这是一个虚拟的图类型，用于区分上下文
    public class AsakiModuleGraph : AsakiGraphAsset { }

    [Serializable]
    public struct AsakiFlowPort { }
}
