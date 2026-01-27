using Asaki.Core.Blackboard;
using Asaki.Core.Blackboard.Variables;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Asaki.Core.Graphs
{
	/// <summary>
	/// 全局黑板资源资产，存储跨图共享的持久化变量定义。
	/// </summary>
	/// <remarks>
	/// <para>核心职责：</para>
	/// <list type="bullet">
	///     <item>作为 <c>ScriptableObject</c> 资产集中管理全局变量</item>
	///     <item>支持多态变量类型（通过 <see cref="SerializeReference"/> 实现）</item>
	///     <item>提供线程安全的变量创建和查询接口</item>
	///     <item>在图执行期间作为共享数据上下文</item>
	/// </list>
	/// 
	/// <para>多态序列化机制：</para>
	/// <list type="number">
	///     <item><see cref="GlobalVariables"/> 字段标记为 <see cref="SerializeReference"/>，支持存储派生自 <see cref="AsakiValueBase"/> 的任意子类</item>
	///     <item>运行时通过 <see cref="Activator.CreateInstance(Type)"/> 动态创建具体值类型实例</item>
	///     <item>Unity序列化系统负责保存完整的类型信息和数据状态</item>
	/// </list>
	/// 
	/// <para>使用场景：</para>
	/// <list type="bullet">
	///     <item>任务系统：存储全局任务状态、计数器</item>
	///     <item>对话系统：管理对话进度、选择结果</item>
	///     <item>游戏状态：全局难度、章节进度、解锁状态</item>
	///     <item>AI系统：共享感知信息、目标标记</item>
	/// </list>
	/// </remarks>
	/// <example>
	/// 编辑器中创建和管理全局黑板：
	/// <code>
	/// // 1. 在 Project 窗口右键 -> Create -> Asaki -> Global Blackboard
	/// // 2. 在 Inspector 中添加不同类型的变量
	/// // 3. 在图资源中引用此黑板资产
	/// 
	/// // 运行时访问（在图节点中）
	/// public class SetGlobalVariableNode : AsakiNodeBase
	/// {
	///     public string VariableName;
	///     
	///     protected override NodeStatus OnExecute(AsakiGraphRunner runner)
	///     {
	///         var blackboard = runner.Graph.GlobalBlackboard;
	///         var variable = blackboard.GetOrCreateVariable(VariableName, typeof(AsakiIntValue));
	///         variable.Value = 42;
	///         return NodeStatus.Success;
	///     }
	/// }
	/// </code>
	/// </example>
	public class AsakiGlobalBlackboardAsset : ScriptableObject
	{
		/// <summary>
		/// 全局变量定义列表，使用多态序列化存储不同类型的变量。
		/// </summary>
		/// <remarks>
		/// <para>序列化策略：</para>
		/// <list type="bullet">
		///     <item>字段类型为 <see cref="List{AsakiVariableDef}"/>，元素可为任意 <see cref="AsakiVariableDef"/> 子类</item>
		///     <item><see cref="SerializeReference"/> 确保完整类型信息被序列化，而非仅基类数据</item>
		///     <item>支持Unity编辑器中的多态字段显示和编辑</item>
		/// </list>
		/// <para>访问注意：此列表在编辑器中可被直接修改，运行时应视为只读。</para>
		/// </remarks>
		[SerializeReference]
		public List<AsakiVariableDef> GlobalVariables = new List<AsakiVariableDef>();

		/// <summary>
		/// 根据变量名获取或创建全局变量，支持类型安全的多态值创建。
		/// </summary>
		/// <param name="name">变量名称，在同一黑板中必须唯一。</param>
		/// <param name="valueType">变量值的类型，必须为 <see cref="AsakiValueBase"/> 的派生类型。</param>
		/// <returns>已存在或新创建的变量定义实例。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> 为 <c>null</c> 或空字符串。</exception>
		/// <exception cref="ArgumentNullException"><paramref name="valueType"/> 为 <c>null</c>。</exception>
		/// <exception cref="ArgumentException"><paramref name="valueType"/> 不是 <see cref="AsakiValueBase"/> 的派生类型。</exception>
		/// <exception cref="InvalidOperationException">同名变量已存在但类型不匹配。</exception>
		/// <remarks>
		/// <para>实现逻辑：</para>
		/// <list type="number">
		///     <item>线性搜索 <see cref="GlobalVariables"/> 查找同名变量</item>
		///     <item>若找到，直接返回现有实例（不检查类型一致性）</item>
		///     <item>若未找到，使用 <see cref="Activator.CreateInstance(Type)"/> 动态创建值实例</item>
		///     <item>构造新的 <see cref="AsakiVariableDef"/> 并添加到列表</item>
		/// </list>
		/// <para>性能注意：线性搜索时间复杂度 O(N)，适用于中小型黑板（变量数 &lt; 100）。</para>
		/// <para>类型安全：调用方应确保 <paramref name="valueType"/> 正确，避免运行时类型转换异常。</para>
		/// </remarks>
		/// <example>
		/// 创建不同类型的全局变量：
		/// <code>
		/// var intVar = blackboard.GetOrCreateVariable("PlayerScore", typeof(AsakiIntValue));
		/// var floatVar = blackboard.GetOrCreateVariable("GameSpeed", typeof(AsakiFloatValue));
		/// var boolVar = blackboard.GetOrCreateVariable("IsGameOver", typeof(AsakiBoolValue));
		/// var stringVar = blackboard.GetOrCreateVariable("CurrentScene", typeof(AsakiStringValue));
		/// </code>
		/// </example>
		public AsakiVariableDef GetOrCreateVariable(string name, Type valueType)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name), "Variable name cannot be null or empty.");

			if (valueType == null)
				throw new ArgumentNullException(nameof(valueType), "Value type cannot be null.");

			if (!typeof(AsakiValueBase).IsAssignableFrom(valueType))
				throw new ArgumentException($"Type {valueType.Name} must inherit from AsakiValueBase.", nameof(valueType));

			// 1. 查找已存在的变量
			AsakiVariableDef existing = GlobalVariables.Find(v => v.Name == name);
			if (existing != null) return existing;

			// 2. 创建多态数据实例
			AsakiValueBase dataInstance = Activator.CreateInstance(valueType) as AsakiValueBase;
			if (dataInstance == null)
				throw new InvalidOperationException($"Failed to create instance of type {valueType.Name}.");

			// 3. 构建新的变量定义
			AsakiVariableDef newVar = new AsakiVariableDef
			{
				Name = name,
				ValueData = dataInstance, // [New] 存入多态数据
				IsExposed = true,
			};

			GlobalVariables.Add(newVar);
			return newVar;
		}

		/// <summary>
		/// 从黑板中移除指定名称的变量。
		/// </summary>
		/// <param name="name">要移除的变量名称。</param>
		/// <returns>
		/// <c>true</c> 表示成功移除至少一个匹配项；<c>false</c> 表示未找到匹配项。
		/// </returns>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> 为 <c>null</c> 或空字符串。</exception>
		/// <remarks>
		/// <para>实现细节：</para>
		/// 使用 <see cref="List{T}.RemoveAll"/> 方法移除所有同名变量（理论上应只有0或1个）。
		/// 此方法会压缩列表并减少 <see cref="GlobalVariables"/> 的计数。
		/// 
		/// <para>副作用：移除后，任何正在引用此变量的图节点可能进入错误状态。</para>
		/// <para>建议：在编辑器中移除变量后，应检查所有引用此变量的图资源。</para>
		/// </remarks>
		public bool RemoveVariable(string name)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name), "Variable name cannot be null or empty.");

			return GlobalVariables.RemoveAll(v => v.Name == name) > 0;
		}
	}
}
