using Asaki.Core.Blackboard;
using Asaki.Core.Graphs;

namespace Asaki.Editor.GraphEditors
{
    public class DragVariableData
    {
        public AsakiVariableDef Variable { get; }
        public bool IsGlobal { get; }

        public DragVariableData(AsakiVariableDef variable, bool isGlobal)
        {
            Variable = variable;
            IsGlobal = isGlobal;
        }
    }
}
