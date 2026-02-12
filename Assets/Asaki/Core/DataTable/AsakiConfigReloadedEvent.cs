using System;
using Asaki.Core.Broker;

namespace Asaki.Core.DataTable
{
    /// <summary>
    /// 当配置表通过<see cref="IAsakiConfigService.ReloadAsync{T}"/>成功重载后发布的事件。
    /// </summary>
    /// <remarks>
    /// <para>事件发布时机：</para>
    /// <list type="number">
    ///     <item>新的配置数据成功反序列化并验证</item>
    ///     <item>内部缓存已原子性替换为新数据</item>
    ///     <item>旧配置数据已被释放或标记为可回收</item>
    /// </list>
    /// <para>订阅者使用场景：</para>
    /// <list type="bullet">
    ///     <item>UI系统刷新显示的配置数值</item>
    ///     <item>运行中的系统重新应用新配置（如难度调整）</item>
    ///     <item>日志系统记录配置变更历史</item>
    ///     <item>热更新系统触发相关模块重启</item>
    /// </list>
    /// <para>事件保证：此事件在配置加载成功后始终在主线程发布。</para>
    /// </remarks>
    /// <example>
    /// 配置热重载响应示例：
    /// <code>
    /// public class GameDifficultySystem
    /// {
    ///     public GameDifficultySystem(IAsakiEventBroker broker)
    ///     {
    ///         broker.Subscribe&lt;AsakiConfigReloadedEvent&gt;(OnConfigReloaded);
    ///     }
    ///
    ///     private void OnConfigReloaded(AsakiConfigReloadedEvent e)
    ///     {
    ///         if (e.ConfigType == typeof(DifficultyConfig))
    ///         {
    ///             // 重新应用难度系数到运行中的游戏
    ///             ApplyNewDifficulty();
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public struct AsakiConfigReloadedEvent : IAsakiEvent
    {
        /// <summary>
        /// 获取或设置已重载的配置类型。
        /// </summary>
        /// <value>继承自<see cref="IAsakiDataTable"/>的配置类类型。</value>
        public Type ConfigType { get; set; }
    }
}
