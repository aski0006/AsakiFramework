using System;
using Asaki.Core.Attributes;

namespace Asaki.Core.Graphs
{
    /// <summary>
    /// 从黑板读取变量值的节点，作为数据流上游源。
    /// </summary>
    /// <remarks>
    /// <para>功能特性：</para>
    /// <list type="bullet">
    ///     <item>通过 <see cref="AsakiGraphContextAttribute"/> 注册到"Variable/Get"分类</item>
    ///     <item>支持全局变量和局部变量（通过 <see cref="IsGlobalVariable"/> 区分）</item>
    ///     <item>运行时由 <see cref="AsakiGraphRunner{TGraph}.ResolveNodeValue{T}"/> 解析值</item>
    ///     <item>在编辑器中动态显示变量名和类型</item>
    /// </list>
    /// <para>执行模型：</para>
    /// 此节点本身不主动执行，而是作为值源被连接它的节点触发求值。
    /// 当下游节点调用 <c>GetInputValue</c> 时，最终会通过 <c>ResolveNodeValue</c> 读取黑板。
    /// </remarks>
    /// <example>
    /// 在行为树中读取玩家位置：
    /// <code>
    /// // 1. 在黑板中定义变量 "PlayerPosition" (Vector3)
    /// // 2. 添加 GetVariableNode，设置 VariableName = "PlayerPosition"
    /// // 3. 连接到 MoveToNode 的 TargetPosition 输入端口
    /// // 4. 运行时 MoveToNode 会调用 GetInputValue 自动解析玩家位置
    /// </code>
    /// </example>
    [Serializable]
    [AsakiGraphContext(typeof(AsakiGraphAsset), "Variable/Get")]
    public class AsakiGetVariableNode : AsakiNodeBase
    {
        /// <summary>
        /// 覆盖基类标题，动态显示变量名。
        /// </summary>
        /// <value>格式为 "Get {VariableName}"，便于在编辑器中识别。</value>
        public override string Title => $"Get {VariableName}";

        /// <summary>
        /// 要读取的变量名称。
        /// </summary>
        /// <remarks>
        /// <para>变量解析顺序：</para>
        /// 先在Local黑板查找，找不到再追溯Global黑板（通过作用域链）。
        /// <para>命名建议：使用PascalCase，如"PlayerHealth"、"GameDifficulty"。</para>
        /// </remarks>
        public string VariableName;

        /// <summary>
        /// 变量的类型名称字符串（主要用于编辑器连线检查或颜色显示）。
        /// </summary>
        /// <remarks>
        /// 存储 <see cref="System.Type.FullName"/>，如"System.Int32"、"UnityEngine.Vector3"。
        /// 在编辑器中用于验证端口连接的类型兼容性。
        /// 运行时类型检查依赖实际黑板值，而非此字段。
        /// </remarks>
        public string VariableTypeName;

        /// <summary>
        /// 指示是否读取全局黑板（而非图局部黑板）。
        /// </summary>
        /// <value>
        /// <c>true</c> 表示从Global Blackboard读取；<c>false</c>（默认）从Local读取。
        /// </value>
        /// <remarks>
        /// <para>设计意图：</para>
        /// 允许直接访问全局状态，绕过Local作用域的遮蔽。
        /// 但写操作始终只能在Local进行，Global仍为只读源。
        /// </remarks>
        public bool IsGlobalVariable = false;

        /// <summary>
        /// 输出端口值（由特性标记），在运行时由执行器填充。
        /// </summary>
        /// <remarks>
        /// <para>实际用途：</para>
        /// 此字段在运行时不会被直接使用（值通过 <c>ResolveNodeValue</c> 动态获取），
        /// 主要服务于编辑器端口显示和连接验证。
        /// <para>特性：<see cref="AsakiNodeOutputAttribute"/> 标记为"Value"输出端口。</para>
        /// </remarks>
        [AsakiNodeOutput("Value")]
        public object Value;
    }

    /// <summary>
    /// 向黑板写入变量值的节点，支持数据流输入和值设置。
    /// </summary>
    /// <remarks>
    /// <para>功能特性：</para>
    /// <list type="bullet">
    ///     <item>提供Flow端口（InputFlow/OutputFlow）支持顺序执行</item>
    ///     <item>接受动态值输入（<see cref="NewValue"/>），可连接上游值节点</item>
    ///     <item>运行时由 <see cref="AsakiGraphRunner{TGraph}.ExecuteNode"/> 特殊处理</item>
    ///     <item>写入遵循Shadowing语义：仅修改Local作用域</item>
    /// </list>
    /// <para>执行语义：</para>
    /// 当执行到Set节点时：
    /// <list type="number">
    ///     <item>计算 <see cref="NewValue"/> 输入端口的值（递归解析上游节点）</item>
    ///     <item>通过 <see cref="AsakiGraphRunner{TGraph}.WriteToBlackboard"/> 写入Local黑板</item>
    ///     <item>从 <see cref="OutputFlow"/> 端口继续执行下游节点</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// 在对话系统中设置任务状态：
    /// <code>
    /// // 1. 连接 InputFlow 到上一个节点（如 ShowTextNode.Out）
    /// // 2. 设置 VariableName = "QuestState"
    /// // 3. 连接 Value 输入到常量节点或表达式节点，值为 "Completed"
    /// // 4. 连接 OutputFlow 到下一个节点
    /// // 5. 执行时 quest.QuestState = "Completed" 被写入黑板
    /// </code>
    /// </example>
    [Serializable]
    [AsakiGraphContext(typeof(AsakiGraphAsset), "Variable/Set")]
    public class AsakiSetVariableNode : AsakiNodeBase
    {
        /// <summary>
        /// 覆盖基类标题，动态显示变量名。
        /// </summary>
        public override string Title => $"Set {VariableName}";

        /// <summary>
        /// 要写入的变量名称。
        /// </summary>
        /// <remarks>
        /// 若变量不存在，会自动在Local黑板创建（类型由 <see cref="NewValue"/> 推断）。
        /// 若变量已存在，类型不匹配时可能抛出异常。
        /// </remarks>
        public string VariableName;

        /// <summary>
        /// 变量的类型名称字符串（主要用于编辑器可视化）。
        /// </summary>
        /// <seealso cref="AsakiGetVariableNode.VariableTypeName"/>
        public string VariableTypeName;

        /// <summary>
        /// 输入Flow端口，标记为"In"，用于顺序执行连接。
        /// </summary>
        /// <remarks>
        /// <see cref="AsakiNodeInputAttribute"/> 标记为"In"端口。
        /// Flow端口在编辑器中显示为白色菱形，用于控制执行顺序。
        /// </remarks>
        [AsakiNodeInput("In")]
        public AsakiFlowPort InputFlow;

        /// <summary>
        /// 要写入的新值，可连接上游值节点（如常量、GetVariable、表达式）。
        /// </summary>
        /// <remarks>
        /// 在编辑器中显示为动态类型端口，颜色由 <see cref="VariableTypeName"/> 决定。
        /// 运行时通过 <see cref="AsakiGraphRunner{TGraph}.GetInputValue{T}"/> 解析实际值。
        /// </remarks>
        [AsakiNodeInput("Value")]
        public object NewValue;

        /// <summary>
        /// 输出Flow端口，标记为"Out"，执行完成后继续下游节点。
        /// </summary>
        /// <remarks>
        /// 节点写入变量成功后，从OutputFlow继续执行，支持链式操作。
        /// </remarks>
        [AsakiNodeOutput("Out")]
        public AsakiFlowPort OutputFlow;
    }

    /// <summary>
    /// Flow端口标记结构体，用于表示执行流连接点。
    /// </summary>
    /// <remarks>
    /// <para>设计目的：</para>
    /// Flow端口不携带数据，仅表示控制流方向。
    /// 使用空结构体而非 <c>object</c> 或 <c>string</c> 提供类型安全和语义清晰。
    ///
    /// <para>序列化：</para>
    /// 作为结构体字段，Unity会自动序列化（值为空，但字段存在）。
    /// 在编辑器中可被 <see cref="AsakiNodeInputAttribute"/>/<see cref="AsakiNodeOutputAttribute"/> 标记。
    ///
    /// <para>执行语义：</para>
    /// 当节点有Flow端口时，执行器按以下顺序：
    /// <list type="number">
    ///     <item>处理所有输入值端口（计算上游值）</item>
    ///     <item>执行节点逻辑</item>
    ///     <item>从输出Flow端口继续执行</item>
    /// </list>
    /// </remarks>
    [Serializable]
    public struct AsakiFlowPort { }
}
