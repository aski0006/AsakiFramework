namespace Asaki.Core.Resources
{
    /// <summary>
    /// [Resources 运行模式]
    /// </summary>
    public enum AsakiResKitMode
    {
        /// <summary>
        /// 使用 UnityEngine.Resources (开发期/原型期)
        /// <para>优点：无需打包，即改即用。</para>
        /// <para>缺点：构建包体大，内存管理较差，不支持热更。</para>
        /// </summary>
        Resources,

        /// <summary>
        /// 使用 Unity Addressables (生产环境)
        /// <para>优点：内存管理优秀，自动依赖处理，支持热更。</para>
        /// <para>缺点：需要 Build Bundle 步骤。</para>
        /// </summary>
        Addressables,

        /// <summary>
        /// 自定义模式 (例如原生 AssetBundle)
        /// <para>需要通过 Factory.RegisterCustomStrategy 注册。</para>
        /// </summary>
        Custom,
    }
}
