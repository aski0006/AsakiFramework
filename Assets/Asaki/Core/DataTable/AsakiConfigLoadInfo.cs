using System;

namespace Asaki.Core.DataTable
{
    /// <summary>
    /// 表示配置表加载状态与元数据的结构体，用于监控和诊断配置系统的运行时行为。
    /// </summary>
    /// <remarks>
    /// <para>此结构体包含配置的加载策略、内存占用、访问频率等关键指标，常用于：</para>
    /// <list type="bullet">
    ///     <item>配置管理器UI显示加载状态</item>
    ///     <item>性能分析工具追踪内存占用</item>
    ///     <item>自动化测试验证加载行为</item>
    /// </list>
    /// <para>线程安全说明：所有成员均为值类型或不可变类型，天然支持多线程读取。</para>
    /// </remarks>
    public struct AsakiConfigLoadInfo
    {
        /// <summary>
        /// 获取或设置配置表的逻辑名称，通常为配置类名（如"ItemConfig"、"PlayerLevelConfig"）。
        /// </summary>
        /// <value>配置表名称字符串，在配置系统中唯一标识此配置。</value>
        public string ConfigName;

        /// <summary>
        /// 获取或设置一个值，指示该配置是否已成功加载到内存中。
        /// </summary>
        /// <value>
        /// <c>true</c> 表示配置数据已加载并可用；<c>false</c> 表示配置尚未加载或已卸载。
        /// </value>
        public bool IsLoaded;

        /// <summary>
        /// 获取或设置配置的加载策略，决定其在生命周期中的加载时机。
        /// </summary>
        /// <value>
        /// <see cref="AsakiConfigLoadStrategy"/> 枚举值，默认为 <see cref="AsakiConfigLoadStrategy.Auto"/>。
        /// </value>
        /// <seealso cref="AsakiConfigLoadStrategy"/>
        public AsakiConfigLoadStrategy Strategy;

        /// <summary>
        /// 获取或设置配置的加载优先级，数值越大表示优先级越高（0-100范围）。
        /// </summary>
        /// <value>优先级整数值，用于批量加载时的排序和优先级调度。</value>
        /// <remarks>
        /// 优先级影响以下行为：
        /// <list type="bullet">
        ///     <item><see cref="IAsakiConfigService.LoadAllAsync"/> 中的加载顺序</item>
        ///     <item>内存紧张时的卸载保护（高优先级配置不易被卸载）</item>
        ///     <item>并行加载的资源分配权重</item>
        /// </list>
        /// </remarks>
        public int Priority;

        /// <summary>
        /// 获取或设置一个值，指示该配置是否允许被显式卸载。
        /// </summary>
        /// <value>
        /// <c>true</c> 表示可通过 <see cref="IAsakiConfigService.Unload{T}"/> 卸载；<c>false</c> 表示常驻内存。
        /// </value>
        public bool Unloadable;

        /// <summary>
        /// 获取或设置配置数据的预估内存占用大小（字节数）。
        /// </summary>
        /// <value>预估的内存占用量，用于内存预算和加载决策，-1表示未知。</value>
        /// <remarks>
        /// 此值为预估值而非精确值，基于配置表行数和列数的启发式算法计算。
        /// 实际占用可能因CLR对象头、内存对齐等因素略有差异。
        /// </remarks>
        public long EstimatedSize;

        /// <summary>
        /// 获取或设置配置被访问的次数统计，用于热数据识别。
        /// </summary>
        /// <value>自加载以来的累计访问次数，通过 <see cref="IAsakiConfigService.Get{T}"/> 等接口触发。</value>
        public int AccessCount;

        /// <summary>
        /// 获取或设置最后一次访问的时间戳，用于LRU（最近最少使用）卸载策略。
        /// </summary>
        /// <value>UTC时间格式的最后访问时间。</value>
        public DateTime LastAccessTime;
    }
}
