using Asaki.Core.Blackboard;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Asaki.Core.Graphs
{
	/// <summary>
	/// 图资源的抽象基类，管理节点、边和黑板变量的运行时生命周期。
	/// </summary>
	/// <remarks>
	/// <para>核心职责：</para>
	/// <list type="bullet">
	///     <item>存储节点和边的序列化数据（<see cref="Nodes"/>、<see cref="Edges"/>）</item>
	///     <item>管理图级黑板变量（<see cref="Variables"/>）</item>
	///     <item>构建运行时拓扑缓存以加速查询（O(1)时间复杂度）</item>
	///     <item>提供节点导航API（GetNextNode、GetInputConnection等）</item>
	///     <item>处理Unity序列化生命周期（<see cref="OnAfterDeserialize"/>）</item>
	/// </list>
	/// 
	/// <para>缓存设计（Lazy Initialization）：</para>
	/// <list type="table">
	///     <item>
	///         <term><see cref="_nodeLookup"/></term>
	///         <description>GUID → 节点映射，支持快速节点查找</description>
	///     </item>
	///     <item>
	///         <term><see cref="_outgoingCache"/></term>
	///         <description>源节点 → 端口 → 目标节点列表，正向连接查询</description>
	///     </item>
	///     <item>
	///         <term><see cref="_incomingCache"/></term>
	///         <description>目标节点 → 端口 → 边数据，反向连接查询（用于数据回溯）</description>
	///     </item>
	/// </list>
	/// 
	/// <para>线程安全：此类不是线程安全的，所有操作必须在Unity主线程执行。</para>
	/// </remarks>
	/// <example>
	/// 实现自定义图类型：
	/// <code>
	/// [CreateAssetMenu(menuName = "Asaki/Behavior Tree")]
	/// public class BehaviorTreeGraph : AsakiGraphBase
	/// {
	///     // 行为树特定逻辑可在此扩展
	///     public BlackboardVariable &lt; int &gt; GlobalCooldown;
	/// }
	/// 
	/// // 使用示例（在Runner中）
	/// public class BehaviorTreeRunner : MonoBehaviour
	/// {
	///     [SerializeField] private BehaviorTreeGraph tree;
	///     
	///     private void Start()
	///     {
	///         tree.InitializeRuntime(); // 必须在执行前初始化
	///         var root = tree.GetEntryNode&lt;RootNode&gt;();
	///         ExecuteNode(root);
	///     }
	/// }
	/// </code>
	/// </example>
	public abstract class AsakiGraphAsset : ScriptableObject
	{
		/// <summary>
		/// 图中所有节点的集合，使用 <see cref="SerializeReference"/> 支持多态节点类型。
		/// </summary>
		/// <remarks>
		/// <para>序列化策略：</para>
		/// <list type="bullet">
		///     <item><see cref="SerializeReference"/> 允许存储继承自 <see cref="AsakiNodeBase"/> 的任意子类</item>
		///     <item>Unity负责序列化完整类型信息和数据状态</item>
		///     <item>支持编辑器中的节点复制、粘贴和撤销/重做</item>
		/// </list>
		/// <para>运行时访问：通过 <see cref="InitializeRuntime"/> 构建的 <see cref="_nodeLookup"/> 缓存进行高效查询。</para>
		/// </remarks>
		[SerializeReference]
		public List<AsakiNodeBase> Nodes = new List<AsakiNodeBase>();

		/// <summary>
		/// 图中所有边的集合，定义节点间的连接关系。
		/// </summary>
		/// <remarks>
		/// 边数据在编辑器中创建连接时生成，在运行时通过 <see cref="InitializeRuntime"/> 解析为高效的缓存结构。
		/// 此列表顺序不影响运行时行为，因为所有查询都基于GUID映射。
		/// </remarks>
		public List<AsakiEdgeData> Edges = new List<AsakiEdgeData>();

		/// <summary>
		/// 图级黑板变量列表，用于在节点间共享数据。
		/// </summary>
		/// <remarks>
		/// 与 <see cref="AsakiGlobalBlackboardAsset"/> 不同，此处的变量作用域仅限于本图实例。
		/// 适用于图特定的临时数据，如循环计数器、节点状态标记等。
		/// </remarks>
		[Header("Blackboard")]
		[SerializeReference] // 支持多态，虽然目前是 List<T>
		public List<AsakiVariableDef> Variables = new List<AsakiVariableDef>();

		/// <summary>
		/// 运行时节点查找缓存：GUID → 节点实例映射。
		/// </summary>
		/// <remarks>
		/// 在 <see cref="InitializeRuntime"/> 中从 <see cref="Nodes"/> 构建，提供O(1)时间复杂度的节点查找。
		/// 此字段不序列化，每次反序列化后通过延迟初始化重建。
		/// </remarks>
		private Dictionary<string, AsakiNodeBase> _nodeLookup;

		/// <summary>
		/// 运行时输入连接缓存：目标节点 → 端口 → 边数据映射。
		/// </summary>
		/// <remarks>
		/// 用于反向查询，支持数据回溯和依赖分析。
		/// 例如：查找某个节点的输入数据来源节点。
		/// </remarks>
		private Dictionary<AsakiNodeBase, Dictionary<string, AsakiEdgeData>> _incomingCache;

		/// <summary>
		/// 运行时输出连接缓存：源节点 → 端口 → 目标节点列表映射。
		/// </summary>
		/// <remarks>
		/// 用于正向查询，支持执行流导航。
		/// 例如：获取当前节点的下一个可执行节点。
		/// 结构允许一个端口连接多个目标节点（广播语义）。
		/// </remarks>
		private Dictionary<AsakiNodeBase, Dictionary<string, List<AsakiNodeBase>>> _outgoingCache;

		/// <summary>
		/// 指示运行时缓存是否已初始化，用于延迟加载和状态验证。
		/// </summary>
		/// <remarks>
		/// 当此值为 <c>false</c> 时，任何访问缓存的方法都会触发 <see cref="InitializeRuntime"/>。
		/// 在 <see cref="OnAfterDeserialize"/> 中重置为 <c>false</c>，确保反序列化后重新构建缓存。
		/// </remarks>
		[System.NonSerialized]
		private bool _isInitialized = false;



		/// <summary>
		/// 确保运行时缓存已初始化，若未初始化则调用 <see cref="InitializeRuntime"/>。
		/// </summary>
		/// <remarks>
		/// 此方法提供线程安全（Unity主线程内）的延迟初始化机制。
		/// 所有依赖缓存的公共方法都应先调用此方法。
		/// </remarks>
		private void EnsureRuntimeInitialized()
		{
			if (!_isInitialized || _outgoingCache == null || _nodeLookup == null)
			{
				InitializeRuntime();
			}
		}



		/// <summary>
		/// 构建运行时拓扑缓存，将序列化数据转换为高效查询结构。
		/// </summary>
		/// <remarks>
		/// <para>初始化流程：</para>
		/// <list type="number">
		///     <item>从 <see cref="Nodes"/> 构建 <see cref="_nodeLookup"/> 映射</item>
		///     <item>遍历 <see cref="Edges"/>，填充 <see cref="_outgoingCache"/> 和 <see cref="_incomingCache"/></item>
		///     <item>验证GUID有效性，跳过无效连接（防御性编程）</item>
		///     <item>标记 <see cref="_isInitialized"/> 为 <c>true</c></item>
		/// </list>
		/// <para>调用时机：</para>
		/// <list type="bullet">
		///     <item>图执行前（通常在 <c>AsakiGraphRunner.Start</c> 中）</item>
		///     <item>反序列化后首次访问（延迟加载）</item>
		///     <item>编辑器中图结构变更后（节点/边增删）</item>
		/// </list>
		/// <para>性能优化：仅在需要时构建缓存，避免编辑器中频繁序列化带来的性能损耗。</para>
		/// <para>线程警告：此方法非线程安全，必须在Unity主线程调用。</para>
		/// </remarks>
		public void InitializeRuntime()
		{
			if (_isInitialized && _outgoingCache != null && _nodeLookup != null) return;

			// 预分配容量避免 Resize
			int nodeCount = Nodes.Count;
			int edgeCount = Edges.Count;

			_nodeLookup = new Dictionary<string, AsakiNodeBase>(nodeCount);

			foreach (AsakiNodeBase node in Nodes)
			{
				if (node != null && !string.IsNullOrEmpty(node.GUID))
				{
					_nodeLookup[node.GUID] = node;
				}
			}

			// 预估容量：假设 80% 的节点有输出连接
			int estimatedCapacity = (int)(nodeCount * 0.8f);
			_outgoingCache = new Dictionary<AsakiNodeBase, Dictionary<string, List<AsakiNodeBase>>>(estimatedCapacity);
			_incomingCache = new Dictionary<AsakiNodeBase, Dictionary<string, AsakiEdgeData>>(estimatedCapacity);

			foreach (AsakiEdgeData edge in Edges)
			{
				if (!_nodeLookup.TryGetValue(edge.BaseNodeGUID, out AsakiNodeBase source)) continue;
				if (!_nodeLookup.TryGetValue(edge.TargetNodeGUID, out AsakiNodeBase target)) continue;

				if (!_outgoingCache.TryGetValue(source, out var outPortMap))
				{
					outPortMap = new Dictionary<string, List<AsakiNodeBase>>(2);
					_outgoingCache[source] = outPortMap;
				}
				if (!outPortMap.TryGetValue(edge.BasePortName, out var targets))
				{
					targets = new List<AsakiNodeBase>(1);
					outPortMap[edge.BasePortName] = targets;
				}
				targets.Add(target);

				if (!_incomingCache.TryGetValue(target, out var inPortMap))
				{
					inPortMap = new Dictionary<string, AsakiEdgeData>(2);
					_incomingCache[target] = inPortMap;
				}
				inPortMap[edge.TargetPortName] = edge;
			}

			_isInitialized = true;
		}



		// --------------------------------------------------------
		// ★ 常用 API (Helper Methods)
		// --------------------------------------------------------
		/// <summary>
		/// 获取图的入口节点，默认返回节点列表中的第一个节点。
		/// </summary>
		/// <typeparam name="T">期望的节点类型，用于类型安全转换。</typeparam>
		/// <returns>第一个节点转换为 <typeparamref name="T"/> 的结果，若为空或类型不匹配返回 <c>null</c>。</returns>
		/// <remarks>
		/// <para>默认策略：返回 <see cref="Nodes"/> 集合中的第一个元素。</para>
		/// <para>扩展建议：可重写此方法实现更复杂的入口逻辑（如按名称查找 "EntryPoint" 节点）。</para>
		/// </remarks>
		public T GetEntryNode<T>() where T : AsakiNodeBase
		{
			if (Nodes.Count == 0) return null;
			return Nodes[0] as T; // 简单粗暴，返回第一个
		}



		/// <summary>
		/// 获取连接到目标节点指定输入端口的边数据。
		/// </summary>
		/// <param name="targetNode">目标节点实例。</param>
		/// <param name="inputPortName">输入端口名称。</param>
		/// <returns>匹配的 <see cref="AsakiEdgeData"/>，若未找到返回 <c>null</c>。</returns>
		/// <remarks>
		/// 用于反向查询，支持数据回溯场景（如获取输入数据来源）。
		/// 例如：在变量节点中查找连接到其"In"端口的常量节点。
		/// </remarks>
		public AsakiEdgeData GetInputConnection(AsakiNodeBase targetNode, string inputPortName)
		{
			EnsureRuntimeInitialized();
			if (_incomingCache.TryGetValue(targetNode, out var portMap))
			{
				if (portMap.TryGetValue(inputPortName, out AsakiEdgeData edge))
				{
					return edge;
				}
			}
			return null;
		}



		/// <summary>
		/// 通过GUID查找节点实例。
		/// </summary>
		/// <param name="guid">节点的全局唯一标识符。</param>
		/// <returns>匹配的节点实例，若未找到返回 <c>null</c>。</returns>
		/// <remarks>
		/// 此方法依赖运行时缓存 <see cref="_nodeLookup"/>，首次调用会触发 <see cref="InitializeRuntime"/>。
		/// 时间复杂度 O(1)。
		/// </remarks>
		public AsakiNodeBase GetNodeByGUID(string guid)
		{
			EnsureRuntimeInitialized();
			return _nodeLookup.GetValueOrDefault(guid);
		}



		/// <summary>
		/// 获取从指定端口连接的第一个目标节点。
		/// </summary>
		/// <param name="current">源节点实例。</param>
		/// <param name="portName">输出端口名称，默认为"Out"。</param>
		/// <returns>第一个目标节点，若未找到返回 <c>null</c>。</returns>
		/// <remarks>
		/// <para>适用场景：</para>
		/// 适用于单输出端口的节点（如大多数行为树节点）。
		/// 如果端口有多个连接，此方法返回第一个添加的连接（顺序由序列化决定）。
		/// <para>性能：O(1) 时间复杂度。</para>
		/// </remarks>
		public AsakiNodeBase GetNextNode(AsakiNodeBase current, string portName = "Out")
		{
			EnsureRuntimeInitialized();
			if (_outgoingCache.TryGetValue(current, out var portMap))
			{
				if (portMap.TryGetValue(portName, out var targets) && targets.Count > 0)
				{
					return targets[0];
				}
			}
			return null;
		}



		/// <summary>
		/// 获取从指定端口连接的所有目标节点。
		/// </summary>
		/// <param name="current">源节点实例。</param>
		/// <param name="portName">输出端口名称，默认为"Out"。</param>
		/// <returns>目标节点列表，若未找到返回空列表（非null）。</returns>
		/// <remarks>
		/// <para>适用场景：</para>
		/// 适用于广播或多输出端口的节点（如事件分发器、并行节点）。
		/// 返回列表的顺序与编辑器中创建连接的顺序一致。
		/// <para>返回策略：总是返回有效列表，调用方可安全遍历无需空检查。</para>
		/// </remarks>
		public List<AsakiNodeBase> GetNextNodes(AsakiNodeBase current, string portName = "Out")
		{
			EnsureRuntimeInitialized();
			if (_outgoingCache.TryGetValue(current, out var portMap))
			{
				if (portMap.TryGetValue(portName, out var targets))
				{
					return targets;
				}
			}
			return new List<AsakiNodeBase>(); // 返回空列表防止 NullReference
		}



		/// <summary>
		/// 泛型版本，获取从指定端口连接的单个目标节点并转换类型。
		/// </summary>
		/// <typeparam name="T">期望的节点类型。</typeparam>
		/// <param name="current">源节点实例。</param>
		/// <param name="portName">输出端口名称。</param>
		/// <returns>类型转换后的目标节点，若未找到或类型不匹配返回 <c>null</c>。</returns>
		/// <seealso cref="GetNextNode(AsakiNodeBase, string)"/>
		public T GetNextNode<T>(AsakiNodeBase current, string portName = "Out") where T : AsakiNodeBase
		{
			return GetNextNode(current, portName) as T;
		}



		/// <summary>
		/// Unity 序列化回调，在对象序列化前调用（来自 <see cref="ISerializationCallbackReceiver"/>）。
		/// </summary>
		/// <remarks>
		/// <para>当前实现为空，因为所有序列化数据已在 <see cref="Nodes"/>、<see cref="Edges"/> 和 <see cref="Variables"/> 中。</para>
		/// <para>扩展点：若需要序列化运行时计算的数据，可在此处理。</para>
		/// </remarks>
		public void OnBeforeSerialize()
		{
			// 序列化前无需操作，数据都在 List 中
		}


		/// <summary>
		/// Unity 反序列化回调，在对象反序列化后调用（来自 <see cref="ISerializationCallbackReceiver"/>）。
		/// </summary>
		/// <remarks>
		/// <para>处理逻辑：</para>
		/// <list type="bullet">
		///     <item>将 <see cref="_isInitialized"/> 标记为 <c>false</c>，强制下次访问时重建缓存</item>
		///     <item>清空所有运行时缓存引用，避免持有过期数据</item>
		///     <item>不直接调用 <see cref="InitializeRuntime"/>，原因如下：</item>
		/// </list>
		/// 
		/// <para>不立即初始化的原因：</para>
		/// <list type="number">
		///     <item>Unity 反序列化可能在非主线程或受限环境下执行，字典操作可能不安全</item>
		///     <item>避免编辑器中加载资源时的即时性能开销（Lazy Load 策略）</item>
		///     <item>某些操作（如 Ctrl+Z 撤销）触发反序列化后，图可能不会立即使用</item>
		/// </list>
		/// 
		/// <para>线程安全：此方法由Unity在合适时机调用，无需手动调用。</para>
		/// </remarks>
		public void OnAfterDeserialize()
		{
			// 反序列化后（如 Ctrl+Z，或资源加载），缓存表是空的
			// 我们标记为未初始化，下次访问时会自动重建 (Lazy Load)
			// 或者更激进地：如果已经在运行中，强制刷新

			_isInitialized = false;
			_nodeLookup = null;
			_outgoingCache = null;
			_incomingCache = null;

			// 注意：不要在这里直接调用 InitializeRuntime()
			// 因为 Unity 反序列化是在非主线程或受限环境下进行的，
			// 复杂的字典操作可能不安全。
			// 我们通过置空标志位，让 GetInputConnection 等方法在下次调用时自动重建。
		}


		public virtual TGraph Clone<TGraph>() where TGraph : AsakiGraphAsset
		{
			TGraph clone = Instantiate(this) as TGraph;
			if (clone == null) return null;

			clone.Nodes = Nodes.Select(n => CloneNode(n)).ToList();

			clone.Edges = Edges.Select(e => new AsakiEdgeData
			{
				BaseNodeGUID = e.BaseNodeGUID,
				BasePortName = e.BasePortName,
				TargetNodeGUID = e.TargetNodeGUID,
				TargetPortName = e.TargetPortName,
			}).ToList();

			clone.Variables = Variables.Select(v => new AsakiVariableDef
			{
				Name = v.Name,
				ValueData = v.ValueData?.Clone(),
				IsExposed = v.IsExposed,
			}).ToList();

			clone._isInitialized = false;
			clone._nodeLookup = null;
			clone._outgoingCache = null;
			clone._incomingCache = null;

			return clone;
		}

		protected virtual AsakiNodeBase CloneNode(AsakiNodeBase original)
		{
			if (original == null) return null;

			string json = JsonUtility.ToJson(original);
			AsakiNodeBase clone = JsonUtility.FromJson(json, original.GetType()) as AsakiNodeBase;

			return clone;
		}

		public virtual void CopyTo(AsakiGraphAsset target)
		{
			if (target == null) return;

			target.Nodes = Nodes.Select(n => CloneNode(n)).ToList();
			target.Edges = new List<AsakiEdgeData>(Edges);
			target.Variables = Variables.Select(v => new AsakiVariableDef
			{
				Name = v.Name,
				ValueData = v.ValueData?.Clone(),
				IsExposed = v.IsExposed,
			}).ToList();

			target._isInitialized = false;
		}
	}
}
