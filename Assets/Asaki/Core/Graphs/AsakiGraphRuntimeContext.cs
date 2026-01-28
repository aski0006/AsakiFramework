using System;
using System.Collections.Generic;
using Asaki.Core.Blackboard;
using UnityEngine;

namespace Asaki.Core.Graphs
{
    /// <summary>
    /// 图执行期间的运行时上下文，封装黑板和Owner引用以及生命周期管理。
    /// </summary>
    /// <remarks>
    /// <para>核心职责：</para>
    /// <list type="bullet">
    ///     <item>持有 <see cref="IAsakiBlackboard"/> 实例，提供变量访问入口</item>
    ///     <item>持有 <see cref="GameObject"/> Owner引用，用于节点逻辑中的场景查询</item>
    ///     <item>实现 <see cref="IDisposable"/> 接口，确保资源正确释放</item>
    ///     <item>提供扩展点，可添加执行状态、调试信息等</item>
    /// </list>
    ///
    /// <para>生命周期：</para>
    /// <list type="number">
    ///     <item>在 <see cref="AsakiGraphRunner{TGraph}.Start"/> 中创建并初始化</item>
    ///     <item>在整个图执行期间存活</item>
    ///     <item>在 <see cref="AsakiGraphRunner{TGraph}.OnDestroy"/> 中调用 <see cref="Dispose"/></item>
    /// </list>
    ///
    /// <para>资源管理：</para>
    /// 黑板内部可能包含大量 <see cref="AsakiProperty{T}"/>，每个Property维护订阅者列表。
    /// 若不调用 <see cref="Dispose"/>，已销毁的UI组件可能仍订阅Property变更，导致内存泄漏。
    /// </remarks>
    /// <example>
    /// 在自定义节点中访问上下文：
    /// <code>
    /// public class SpawnEnemyNode : AsakiNodeBase
    /// {
    ///     public string PrefabKey = "EnemyPrefab";
    ///
    ///     protected override NodeStatus OnExecute(AsakiGraphRuntimeContext context)
    ///     {
    ///         // 1. 获取预制体名称
    ///         string prefabName = context.Blackboard.GetValue&lt;string&gt;(PrefabKey);
    ///
    ///         // 2. 使用Owner作为父对象
    ///         var spawnPoint = context.Owner.transform.Find("SpawnPoint");
    ///         var enemy = Instantiate(Resources.Load&lt;GameObject&gt;(prefabName), spawnPoint);
    ///
    ///         // 3. 将生成的敌人存入黑板
    ///         context.Blackboard.SetValue("LastSpawnedEnemy", enemy);
    ///
    ///         return NodeStatus.Success;
    ///     }
    /// }
    /// </code>
    /// </example>
    public class AsakiGraphRuntimeContext : IDisposable
    {
        /// <summary>
        /// 运行时黑板实例，提供变量读写能力。
        /// </summary>
        /// <remarks>
        /// 可能为Local黑板（带父作用域），也可能直接使用Global黑板，取决于初始化逻辑。
        /// 永不直接设置为 <c>null</c>（初始化时必须提供有效实例）。
        /// </remarks>
        public IAsakiBlackboard Blackboard;

        /// <summary>
        /// 拥有此Runner的GameObject，用于场景查询和对象操作。
        /// </summary>
        /// <remarks>
        /// <para>典型使用：</para>
        /// <list type="bullet">
        ///     <item><c>Owner.transform</c> - 获取位置、旋转、缩放</item>
        ///     <item><c>Owner.GetComponent&lt;T&gt;()</c> - 查询组件</item>
        ///     <item><c>Owner.CompareTag()</c> - 标签检查</item>
        /// </list>
        /// </remarks>
        public GameObject Owner;

        /// <summary>
        /// 释放上下文资源，特别是黑板及其订阅者。
        /// </summary>
        /// <remarks>
        /// <para>实现逻辑：</para>
        /// <list type="number">
        ///     <item>若 <see cref="Blackboard"/> 实现 <see cref="IDisposable"/>，调用其 <c>Dispose</c> 方法</item>
        ///     <item>将所有引用置 <c>null</c> 辅助垃圾回收</item>
        /// </list>
        /// <para>调用时机：必须由 <see cref="AsakiGraphRunner{TGraph}.OnDestroy"/> 显式调用，不依赖GC自动回收。</para>
        /// </remarks>
        public void Dispose()
        {
            // M-01: 级联释放，断开所有属性订阅
            Blackboard?.Dispose();
            Blackboard = null;
            Owner = null;
        }
    }
}
