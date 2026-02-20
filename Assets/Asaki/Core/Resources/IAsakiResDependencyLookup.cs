using System.Collections.Generic;

namespace Asaki.Core.Resources
{
    /// <summary>
    /// 资源依赖查询接口
    /// <para>用于查询某个资源路径所依赖的其他资源路径。</para>
    /// <para>主要用于AssetBundle等需要手动管理依赖的加载方式。</para>
    /// </summary>
    /// <remarks>
    /// <para>不同加载方式的依赖处理：</para>
    /// <list type="bullet">
    /// <item><description>Resources：自动管理依赖，返回null即可</description></item>
    /// <item><description>Addressables：内部Catalog自动管理，返回null即可</description></item>
    /// <item><description>AssetBundle：需要读取Manifest返回依赖列表</description></item>
    /// </list>
    /// </remarks>
    public interface IAsakiResDependencyLookup
    {
        /// <summary>
        /// 获取指定资源的依赖项列表
        /// </summary>
        /// <param name="location">主资源地址</param>
        /// <returns>
        /// 依赖资源地址列表。
        /// <para>如果没有依赖，返回null或空集合。</para>
        /// <para>返回null表示底层系统自动处理依赖，无需应用层干预。</para>
        /// </returns>
        IEnumerable<string> GetDependencies(string location);
    }
}
