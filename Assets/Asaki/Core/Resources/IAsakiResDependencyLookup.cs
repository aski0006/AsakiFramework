using System.Collections.Generic;

namespace Asaki.Core.Resources
{
    /// <summary>
    /// [依赖查询接口]
    /// 用于查询某个资源路径所依赖的其他资源路径。
    /// 对于 AssetBundle，这里通常读取 Manifest；
    /// 对于 Resources/Addressables，这里通常返回空。
    /// </summary>
    public interface IAsakiResDependencyLookup
    {
        /// <summary>
        /// 获取依赖项列表
        /// </summary>
        /// <param name="location">主资源地址</param>
        /// <returns>依赖资源地址列表 (如果没有依赖返回 null 或 空数组)</returns>
        IEnumerable<string> GetDependencies(string location);
    }
}
