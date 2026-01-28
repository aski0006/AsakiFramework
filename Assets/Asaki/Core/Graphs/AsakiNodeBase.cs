using System;
using UnityEngine;

namespace Asaki.Core.Graphs
{
    /// <summary>
    /// 图节点基类，定义所有节点共有的基础数据和可视化属性。
    /// </summary>
    /// <remarks>
    /// <para>设计目的：提供统一的节点数据模型，支持：</para>
    /// <list type="bullet">
    ///     <item>节点位置持久化（<see cref="Position"/>）</item>
    ///     <item>唯一标识符（<see cref="GUID"/>）用于序列化和引用</item>
    ///     <item>执行顺序控制（<see cref="ExecutionOrder"/>）</item>
    ///     <item>可定制的节点标题显示（<see cref="Title"/>）</item>
    ///     <item>节点创建时的初始化钩子（<see cref="OnCreated"/>）</item>
    /// </list>
    /// <para>序列化策略：所有字段标记为 <c>[HideInInspector]</c>，避免在Inspector中显示，但保留序列化能力。</para>
    /// <para>继承要求：所有具体节点类型必须继承此类并实现执行逻辑（通常在 <c>Execute</c> 方法中）。</para>
    /// </remarks>
    /// <example>
    /// 创建自定义节点类型：
    /// <code>
    /// [Serializable]
    /// public class LogNode : AsakiNodeBase
    /// {
    ///     public string Message = "Hello World";
    ///
    ///     public override string Title => $"Log: {Message}"; // 动态标题
    ///
    ///     public override void OnCreated()
    ///     {
    ///         // 节点创建时自动生成GUID（若为空）
    ///         if (string.IsNullOrEmpty(GUID))
    ///         {
    ///             GUID = System.Guid.NewGuid().ToString();
    ///         }
    ///     }
    ///
    ///     protected override NodeStatus OnExecute(AsakiGraphRunner runner)
    ///     {
    ///         Debug.Log(Message);
    ///         return NodeStatus.Success;
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public abstract class AsakiNodeBase
    {
        /// <summary>
        /// 节点在图编辑器中的二维坐标位置，用于持久化布局信息。
        /// </summary>
        /// <remarks>
        /// 坐标系原点为编辑器视图的左上角，单位像素。
        /// Unity GraphView 会自动更新此值以响应用户的拖拽操作。
        /// </remarks>
        [HideInInspector]
        public Vector2 Position;

        /// <summary>
        /// 节点全局唯一标识符（GUID），用于跨序列化周期稳定引用节点。
        /// </summary>
        /// <remarks>
        /// <para>生成策略：</para>
        /// 应在节点首次创建时（通常在 <see cref="OnCreated"/> 或编辑器创建逻辑中）通过 <c>Guid.NewGuid().ToString()</c> 生成。
        /// <para>稳定性要求：</para>
        /// 一旦生成，不应再修改，即使节点被复制或序列化/反序列化。
        /// 在复制节点时，必须生成新的GUID以避免冲突。
        /// <para>用途：</para>
        /// <list type="bullet">
        ///     <item>边数据（<see cref="AsakiEdgeData"/>）通过GUID引用源和目标节点</item>
        ///     <item>节点引用稳定性（跨编辑器会话）</item>
        ///     <item>节点链接的撤销/重做系统</item>
        /// </list>
        /// </remarks>
        [HideInInspector]
        public string GUID;

        /// <summary>
        /// 节点执行顺序，数值越小越早执行（仅对需要排序的节点有效，如事件触发器）。
        /// </summary>
        /// <remarks>
        /// 默认值为0，表示未指定顺序。
        /// 对于并行节点或事件节点，执行器可能按此值排序以确定优先级。
        /// 对于顺序执行的节点（如行为树），此值通常被忽略。
        /// </remarks>
        [HideInInspector]
        public int ExecutionOrder;

        /// <summary>
        /// 获取节点在编辑器中显示的标题文本，可重写以提供动态标题。
        /// </summary>
        /// <value>默认返回类型的简短名称（不含命名空间）。</value>
        /// <remarks>
        /// <para>重写建议：</para>
        /// <list type="bullet">
        ///     <item>包含关键参数值以提升可读性（如 <c>"Log: PlayerSpawned"</c>）</item>
        ///     <item>保持标题简洁（建议长度 < 30字符）</item>
        ///     <item>避免频繁变化的值（可能影响编辑器性能）</item>
        /// </list>
        /// <para>调用时机：每次编辑器重绘时调用，实现应考虑性能。</para>
        /// </remarks>
        public virtual string Title => GetType().Name;

        /// <summary>
        /// 节点创建时的生命周期回调，用于初始化默认值和生成GUID。
        /// </summary>
        /// <remarks>
        /// <para>调用时机：</para>
        /// <list type="bullet">
        ///     <item>在编辑器中通过节点创建菜单添加节点时</item>
        ///     <item>通过代码调用 <c>new T() { ... }</c> 初始化后，需手动调用</item>
        ///     <item>反序列化后<strong>不会</strong>自动调用（与构造函数行为一致）</item>
        /// </list>
        /// <para>默认实现为空，子类应重写以完成初始化。</para>
        /// <para>典型实现包括：GUID生成、默认值设置、初始端口创建。</para>
        /// </remarks>
        public virtual void OnCreated() { }
    }
}
