using System;
using System.Collections.Generic;
using System.Linq;
using Asaki.Core.Attributes;
using Asaki.Core.Graphs;
using UnityEngine;

namespace Asaki.Plugin.ComboSystem.Editor
{
    /// <summary>
    /// 连招图资产 - 用于可视化编辑器
    /// </summary>
    [CreateAssetMenu(fileName = "ComboGraph", menuName = "Asaki/ComboSystem/ComboGraph")]
    public class ComboGraphAsset : AsakiGraphAsset
    {
        [Header("Combo Settings")]
        public string ComboTreeId = "new_combo";
        public string Description = "";
        public float InputBufferWindow = 0.3f;
        public float MaxComboDuration = 10f;
        public int MaxComboLength = 10;

        [Header("Reset Strategies")]
        public List<ResetStrategyData> ResetStrategies = new List<ResetStrategyData>();

        [Header("Export Settings")]
        public string OutputPath = "Assets/Game/FrameworkSettings/Combos";

        /// <summary>
        /// 导出为ComboTree运行时资产
        /// </summary>
        public ComboTree ExportToComboTree()
        {
            var comboTree = ScriptableObject.CreateInstance<ComboTree>();
            comboTree.name = ComboTreeId;
            comboTree.TreeId = ComboTreeId;
            comboTree.Description = Description;
            comboTree.InputBufferWindow = InputBufferWindow;
            comboTree.MaxComboDuration = MaxComboDuration;
            comboTree.MaxComboLength = MaxComboLength;

            // 收集所有招式节点
            var moveNodes = Nodes.OfType<MoveNode>().ToList();
            comboTree.Moves = moveNodes.Select(n => n.MoveData).ToArray();

            // 收集所有转换边
            var transitions = new List<ComboTransition>();
            foreach (var edge in Edges)
            {
                var fromNode = GetNodeByGUID(edge.BaseNodeGUID) as MoveNode;
                var toNode = GetNodeByGUID(edge.TargetNodeGUID) as MoveNode;

                if (fromNode != null && toNode != null)
                {
                    // 查找对应的TransitionNode（如果存在）
                    var transitionNode = FindTransitionNode(edge.BaseNodeGUID, edge.TargetNodeGUID);

                    var transition = new ComboTransition
                    {
                        FromMoveId = fromNode.MoveData.MoveId,
                        ToMoveId = toNode.MoveData.MoveId,
                        InputType = transitionNode?.InputType ?? "LightAttack",
                        Conditions =
                            transitionNode?.Conditions ?? Array.Empty<TransitionCondition>(),
                        ResetGroup = transitionNode?.ResetGroup ?? "default",
                    };
                    transitions.Add(transition);
                }
            }
            comboTree.Transitions = transitions.ToArray();

            // 转换重置策略
            comboTree.ResetStrategies = ResetStrategies
                .Select(s => new ResetStrategyDefinition
                {
                    GroupName = s.GroupName,
                    Mode = s.Mode,
                    DecayAmount = s.DecayAmount,
                    DecayPercent = s.DecayPercent,
                    MinCount = s.MinCount,
                    SpecificValue = s.SpecificValue,
                })
                .ToArray();

            return comboTree;
        }

        private TransitionNode FindTransitionNode(string fromGuid, string toGuid)
        {
            // 查找连接两个MoveNode的TransitionNode
            foreach (var node in Nodes.OfType<TransitionNode>())
            {
                // 检查TransitionNode是否连接这两个节点
                var incoming = Edges.FirstOrDefault(e => e.TargetNodeGUID == node.GUID);
                var outgoing = Edges.FirstOrDefault(e => e.BaseNodeGUID == node.GUID);

                if (incoming?.BaseNodeGUID == fromGuid && outgoing?.TargetNodeGUID == toGuid)
                {
                    return node;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// 重置策略数据（用于编辑器）
    /// </summary>
    [Serializable]
    public class ResetStrategyData
    {
        public string GroupName = "default";
        public ResetComboMode Mode = ResetComboMode.ResetToZero;
        public int DecayAmount = 1;
        public float DecayPercent = 0.5f;
        public int MinCount = 0;
        public int SpecificValue = 0;
    }

    // ============================================================
    // 节点定义
    // ============================================================

    /// <summary>
    /// 招式节点 - 连招图的核心节点
    /// </summary>
    [Serializable]
    [AsakiGraphContext(typeof(ComboGraphAsset), "Combo System/Move")]
    public class MoveNode : AsakiNodeBase
    {
        public ComboMove MoveData = new ComboMove();

        [AsakiNodeInput("Input", multiple: true)]
        public AsakiFlowPort InputFlow;

        [AsakiNodeOutput("Output", multiple: true)]
        public AsakiFlowPort OutputFlow;

        public override string Title =>
            string.IsNullOrEmpty(MoveData.MoveName) ? "moves" : $"{MoveData.MoveName}";

        public override void OnCreated()
        {
            base.OnCreated();
            if (string.IsNullOrEmpty(GUID))
            {
                GUID = System.Guid.NewGuid().ToString();
            }
            if (string.IsNullOrEmpty(MoveData.MoveId))
            {
                MoveData.MoveId = $"move_{GUID.Substring(0, 8)}";
            }
        }
    }

    /// <summary>
    /// 转换节点 - 定义招式之间的转换条件和输入类型
    /// </summary>
    [Serializable]
    [AsakiGraphContext(typeof(ComboGraphAsset), "Combo System/Transition")]
    public class TransitionNode : AsakiNodeBase
    {
        /// <summary>
        /// 输入类型ID（使用可扩展的类型系统）
        /// </summary>
        public string InputType = "LightAttack";

        /// <summary>
        /// 转换条件（使用数组避免SerializedProperty失效问题）
        /// </summary>
        public TransitionCondition[] Conditions = Array.Empty<TransitionCondition>();

        /// <summary>
        /// 重置策略组
        /// </summary>
        public string ResetGroup = "default";

        [AsakiNodeInput("from")]
        public AsakiFlowPort InputFrom;

        [AsakiNodeOutput("to")]
        public AsakiFlowPort OutputTo;

        public override string Title
        {
            get
            {
                var def = ComboInputTypeRegistry.GetDefinition(InputType);
                string displayName = def?.DisplayName ?? InputType;
                return $"➡️ {displayName}";
            }
        }

        public override void OnCreated()
        {
            base.OnCreated();
            if (string.IsNullOrEmpty(GUID))
            {
                GUID = System.Guid.NewGuid().ToString();
            }
        }

        /// <summary>
        /// 添加条件
        /// </summary>
        public void AddCondition()
        {
            Array.Resize(ref Conditions, Conditions.Length + 1);
            Conditions[^1] = new TransitionCondition();
        }

        /// <summary>
        /// 移除条件
        /// </summary>
        public void RemoveCondition(int index)
        {
            if (index < 0 || index >= Conditions.Length)
                return;

            var newConditions = new TransitionCondition[Conditions.Length - 1];
            for (int i = 0, j = 0; i < Conditions.Length; i++)
            {
                if (i != index)
                {
                    newConditions[j++] = Conditions[i];
                }
            }
            Conditions = newConditions;
        }
    }

    /// <summary>
    /// 入口节点 - 连招的起始点
    /// </summary>
    [Serializable]
    [AsakiGraphContext(typeof(ComboGraphAsset), "Combo System/Flow")]
    public class EntryNode : AsakiNodeBase
    {
        [AsakiNodeOutput("entry")]
        public AsakiFlowPort OutputFlow;

        public override string Title => "entry";

        public override void OnCreated()
        {
            base.OnCreated();
            if (string.IsNullOrEmpty(GUID))
            {
                GUID = System.Guid.NewGuid().ToString();
            }
        }
    }

    /// <summary>
    /// 条件节点 - 用于复杂的分支逻辑
    /// </summary>
    [Serializable]
    [AsakiGraphContext(typeof(ComboGraphAsset), "Combo System/Logic")]
    public class ConditionNode : AsakiNodeBase
    {
        public ConditionType ConditionType = ConditionType.ComboCount;
        public string Parameter = "";
        public float Value = 0f;
        public ComparisonOperator Operator = ComparisonOperator.GreaterOrEqual;

        [AsakiNodeInput("input")]
        public AsakiFlowPort InputFlow;

        [AsakiNodeOutput("true")]
        public AsakiFlowPort TrueOutput;

        [AsakiNodeOutput("false")]
        public AsakiFlowPort FalseOutput;

        public override string Title => $"{GetOperatorSymbol(Operator)}{Value}";

        private string GetOperatorSymbol(ComparisonOperator op)
        {
            return op switch
            {
                ComparisonOperator.Equal => "=",
                ComparisonOperator.NotEqual => "≠",
                ComparisonOperator.Greater => ">",
                ComparisonOperator.GreaterOrEqual => "≥",
                ComparisonOperator.Less => "<",
                ComparisonOperator.LessOrEqual => "≤",
                _ => "?",
            };
        }

        public override void OnCreated()
        {
            base.OnCreated();
            if (string.IsNullOrEmpty(GUID))
            {
                GUID = System.Guid.NewGuid().ToString();
            }
        }
    }

    public enum ComparisonOperator
    {
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
    }

    /// <summary>
    /// 结束节点 - 标记连招的结束点
    /// </summary>
    [Serializable]
    [AsakiGraphContext(typeof(ComboGraphAsset), "Combo System/Flow")]
    public class EndNode : AsakiNodeBase
    {
        [AsakiNodeInput("end", multiple: true)]
        public AsakiFlowPort InputFlow;

        public override string Title => "end";

        public override void OnCreated()
        {
            base.OnCreated();
            if (string.IsNullOrEmpty(GUID))
            {
                GUID = System.Guid.NewGuid().ToString();
            }
        }
    }
}
