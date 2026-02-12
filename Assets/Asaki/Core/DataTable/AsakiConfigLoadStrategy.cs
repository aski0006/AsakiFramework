namespace Asaki.Core.DataTable
{
    /// <summary>
    /// 定义配置表在应用程序生命周期中的加载时机和策略。
    /// </summary>
    /// <remarks>
    /// <para>策略选择直接影响：</para>
    /// <list type="bullet">
    ///     <item>启动时间 - 预加载过多配置会延长启动时间</item>
    ///     <item>内存占用 - 按需加载可优化运行时内存</item>
    ///     <item>首次访问延迟 - 按需加载可能导致首次访问时的卡顿</item>
    /// </list>
    /// <para>推荐实践：</para>
    /// <list type="table">
    ///     <item><term>核心配置（如GameBalanceConfig）</term><description>使用 <see cref="Preload"/></description></item>
    ///     <item><term>场景配置（如Level1Config）</term><description>使用 <see cref="OnDemand"/></description></item>
    ///     <item><term>DLC配置（如DLC2ItemsConfig）</term><description>使用 <see cref="Manual"/></description></item>
    ///     <item><term>不确定时使用</term><description>使用 <see cref="Auto"/></description></item>
    /// </list>
    /// </remarks>
    public enum AsakiConfigLoadStrategy
    {
        /// <summary>
        /// 自动决策策略：框架根据配置表大小、引用频率和可用内存动态选择加载时机。
        /// </summary>
        /// <remarks>
        /// 决策算法考虑以下因素：
        /// <list type="bullet">
        ///     <item>配置表文件大小（小于1MB倾向于预加载）</item>
        ///     <item>上次运行时的访问频率统计</item>
        ///     <item>当前可用内存与总内存比例</item>
        ///     <item>设备性能等级（移动端更保守）</item>
        /// </list>
        /// 此策略适合大多数通用配置，在性能和内存之间取得平衡。
        /// </remarks>
        Auto = 0,

        /// <summary>
        /// 预加载策略：在应用程序启动阶段（<see cref="IAsakiModule.OnInitAsync"/>）立即加载配置。
        /// </summary>
        /// <remarks>
        /// 适用于以下场景：
        /// <list type="bullet">
        ///     <item>游戏核心玩法配置（伤害公式、资源产出）</item>
        ///     <item>启动画面和主菜单所需配置</item>
        ///     <item>体积较小（&lt; 5MB）的全局配置</item>
        /// </list>
        /// 预加载配置会增加启动时间，但确保任何时刻访问都不会有延迟。
        /// </remarks>
        Preload = 1,

        /// <summary>
        /// 按需加载策略：在首次通过<see cref="IAsakiConfigService.Get{T}"/>访问时自动加载。
        /// </summary>
        /// <remarks>
        /// 实现机制：
        /// <list type="number">
        ///     <item>首次访问时检测到未加载状态</item>
        ///     <item>触发异步加载并返回默认值或缓存值</item>
        ///     <item>加载完成后更新内部缓存</item>
        ///     <item>后续访问直接返回缓存数据</item>
        /// </list>
        /// 适合场景：场景特定配置、大型配置表、使用频率低的边缘功能配置。
        /// </remarks>
        OnDemand = 2,

        /// <summary>
        /// 手动加载策略：必须通过显式调用<see cref="IAsakiConfigService.PreloadAsync{T}"/>或<see cref="IAsakiConfigService.LoadAllAsync"/>加载。
        /// </summary>
        /// <remarks>
        /// <para>访问未加载的手动加载配置会抛出<see cref="InvalidOperationException"/>异常。</para>
        /// <para>典型应用场景：</para>
        /// <list type="bullet">
        ///     <item>DLC/DLC内容配置（需玩家购买后加载）</item>
        ///     <item>活动/赛季限时配置（在特定时间窗口加载）</item>
        ///     <item>开发者/调试配置（仅在开发模式可用）</item>
        /// </list>
        /// 此策略提供最大控制权，确保配置仅在明确需要时加载。
        /// </remarks>
        Manual = 3,
    }
}
