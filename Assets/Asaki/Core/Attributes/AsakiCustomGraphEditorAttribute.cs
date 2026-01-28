using System;
using Asaki.Core.Graphs;

namespace Asaki.Core.Attributes
{
    /// <summary>
    /// 标记自定义图编辑器实现类的特性，用于将特定的图类型与其可视化编辑器进行映射。
    /// </summary>
    /// <remarks>
    /// <para>设计目的：实现图数据模型（继承自 <see cref="AsakiGraphAsset"/>）与自定义编辑器（继承自 Unity GraphView）的解耦关联。</para>
    /// <para>工作原理：</para>
    /// <list type="number">
    ///     <item>在编辑器初始化阶段，通过反射扫描所有标记了此特性的类</item>
    ///     <item>构建 <c>GraphType → EditorType</c> 的映射字典</item>
    ///     <item>当在Inspector中编辑特定图资源时，根据资源类型查找对应的编辑器类型并实例化</item>
    /// </list>
    /// <para>与 Unity 内置特性的区别：此特性专门服务于 Asaki 图框架，支持多态图类型的精确匹配。</para>
    /// </remarks>
    /// <example>
    /// 为自定义行为树图注册可视化编辑器：
    /// <code>
    /// // 数据模型定义
    /// public class BehaviorTreeGraph : AsakiGraphBase
    /// {
    ///     // 行为树特定逻辑
    /// }
    ///
    /// // 编辑器实现（在 Editor 命名空间）
    /// [AsakiCustomGraphEditor(typeof(BehaviorTreeGraph))]
    /// public class BehaviorTreeGraphEditor : AsakiGraphEditorWindow
    /// {
    ///     protected override void OnGraphLoaded()
    ///     {
    ///         // 自定义行为树节点视图创建逻辑
    ///     }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AsakiCustomGraphEditorAttribute : Attribute
    {
        /// <summary>
        /// 获取此编辑器关联的图数据模型的类型。
        /// </summary>
        /// <value>继承自 <see cref="AsakiGraphAsset"/> 的图类型。</value>
        /// <remarks>
        /// 此类型作为字典查找的键，必须精确匹配图资源实例的 <see cref="System.Type"/>。
        /// 若图类型继承层次复杂，建议使用抽象基类或接口作为标记类型。
        /// </remarks>
        public Type GraphType { get; }

        /// <summary>
        /// 初始化 <see cref="AsakiCustomGraphEditorAttribute"/> 的新实例。
        /// </summary>
        /// <param name="graphType">关联的图数据模型类型，必须为 <see cref="AsakiGraphAsset"/> 的子类。</param>
        /// <exception cref="ArgumentNullException"><paramref name="graphType"/> 为 <c>null</c>。</exception>
        /// <exception cref="ArgumentException"><paramref name="graphType"/> 不是 <see cref="AsakiGraphAsset"/> 的派生类型。</exception>
        /// <remarks>
        /// 构造函数会执行运行时类型检查，确保类型安全。
        /// 建议在编辑器启动时尽早创建特性实例，通常在类定义时静态初始化。
        /// </remarks>
        public AsakiCustomGraphEditorAttribute(Type graphType)
        {
            // 运行时类型验证（可选，但推荐）
            if (graphType == null)
                throw new ArgumentNullException(nameof(graphType), "GraphType cannot be null.");

            if (!typeof(AsakiGraphAsset).IsAssignableFrom(graphType))
                throw new ArgumentException(
                    $"Type {graphType.Name} must inherit from AsakiGraphBase.",
                    nameof(graphType)
                );

            GraphType = graphType;
        }
    }
}
