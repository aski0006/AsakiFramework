using System;

namespace Asaki.Core.Graphs
{

	/// <summary>
	/// 图的边数据结构，表示节点之间的连接关系。
	/// </summary>
	/// <remarks>
	/// <para>设计特点：</para>
	/// <list type="bullet">
	///     <item>纯数据类，不包含任何运行时逻辑</item>
	///     <item>使用GUID字符串引用节点，避免直接引用导致的序列化问题</item>
	///     <item>支持命名端口，允许单个节点拥有多个输入/输出连接</item>
	///     <item>可序列化，确保图结构持久化</item>
	/// </list>
	/// 
	/// <para>与节点引用的区别：</para>
	/// 直接存储节点引用会导致Unity序列化系统深度克隆整个对象图，引发循环引用和重复数据。
	/// 使用GUID间接引用保持数据扁平化，仅在运行时重建连接。
	/// </remarks>
	/// <example>
	/// 边的创建和序列化：
	/// <code>
	/// // 编辑器中创建连接时生成
	/// var edge = new AsakiEdgeData
	/// {
	///     BaseNodeGUID = sourceNode.GUID,      // "3f8a2b1c-..."
	///     BasePortName = "Out",                // 源节点的输出端口名
	///     TargetNodeGUID = targetNode.GUID,    // "e9c5d7a8-..."
	///     TargetPortName = "In"                // 目标节点的输入端口名
	/// };
	/// 
	/// graph.Edges.Add(edge); // 序列化时保存到图资源
	/// </code>
	/// </example>
	[Serializable]
	public class AsakiEdgeData
	{
		/// <summary>
		/// 源节点（输出端）的GUID。
		/// </summary>
		/// <remarks>
		/// 对应 <see cref="AsakiNodeBase.GUID"/> 的值，在运行时通过 <see cref="AsakiGraphAsset.GetNodeByGUID"/> 解析为实际节点实例。
		/// </remarks>
		public string BaseNodeGUID;

		/// <summary>
		/// 源节点的输出端口名称。
		/// </summary>
		/// <remarks>
		/// 允许节点拥有多个输出端口（如"Success"、"Failure"、"Out"）。
		/// 端口名称在节点类中定义，通常为常量字符串。
		/// </remarks>
		public string BasePortName;

		/// <summary>
		/// 目标节点（输入端）的GUID。
		/// </summary>
		/// <remarks>
		/// 对应 <see cref="AsakiNodeBase.GUID"/> 的值。
		/// </remarks>
		public string TargetNodeGUID;

		/// <summary>
		/// 目标节点的输入端口名称。
		/// </summary>
		/// <remarks>
		/// 允许节点拥有多个输入端口（如"In1"、"In2"、"Condition"）。
		/// 对于单输入节点，通常命名为"In"或"Input"。
		/// </remarks>
		public string TargetPortName;
	}
}
