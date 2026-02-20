namespace Asaki.Core.Resources
{
    /// <summary>
    /// 资源加载运行模式
    /// <para>定义资源加载服务的底层实现方式。</para>
    /// </summary>
    /// <remarks>
    /// <para>模式选择建议：</para>
    /// <list type="bullet">
    /// <item><description>开发期/原型期：使用Resources模式，快速迭代</description></item>
    /// <item><description>生产环境：使用Addressables模式，优化内存和包体</description></item>
    /// <item><description>特殊需求：使用Custom模式，自定义加载逻辑</description></item>
    /// </list>
    /// </remarks>
    public enum AsakiResKitMode
    {
        /// <summary>
        /// Unity原生Resources模式
        /// <para>适用于开发期和原型期。</para>
        /// </summary>
        /// <remarks>
        /// <para>优点：</para>
        /// <list type="bullet">
        /// <item><description>无需打包，即改即用</description></item>
        /// <item><description>API简单，学习成本低</description></item>
        /// </list>
        /// <para>缺点：</para>
        /// <list type="bullet">
        /// <item><description>构建包体大</description></item>
        /// <item><description>内存管理较差</description></item>
        /// <item><description>不支持热更新</description></item>
        /// </list>
        /// </remarks>
        Resources,

        /// <summary>
        /// Unity Addressables模式
        /// <para>适用于生产环境。</para>
        /// </summary>
        /// <remarks>
        /// <para>优点：</para>
        /// <list type="bullet">
        /// <item><description>内存管理优秀</description></item>
        /// <item><description>自动依赖处理</description></item>
        /// <item><description>支持热更新</description></item>
        /// </list>
        /// <para>缺点：</para>
        /// <list type="bullet">
        /// <item><description>需要Build Bundle步骤</description></item>
        /// <item><description>需要配置Addressables Group</description></item>
        /// </list>
        /// <para>前置条件：</para>
        /// <list type="bullet">
        /// <item><description>安装Unity Addressables包</description></item>
        /// <item><description>定义编译宏 ASAKI_USE_ADDRESSABLES</description></item>
        /// </list>
        /// </remarks>
        Addressables,

        /// <summary>
        /// 自定义模式
        /// <para>用于扩展支持AssetBundle或其他自定义加载方式。</para>
        /// </summary>
        /// <remarks>
        /// <para>使用方式：</para>
        /// <list type="bullet">
        /// <item><description>实现IAsakiResStrategy接口</description></item>
        /// <item><description>（可选）实现IAsakiResDependencyLookup接口</description></item>
        /// <item><description>通过AsakiResKitFactory.RegisterCustom注册</description></item>
        /// </list>
        /// </remarks>
        Custom,
    }
}
