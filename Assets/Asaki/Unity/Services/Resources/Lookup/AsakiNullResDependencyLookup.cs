using System.Collections.Generic;
using Asaki.Core.Resources;

namespace Asaki.Unity.Services.Resources.Lookup
{
    /// <summary>
    /// 空依赖查询实现
    /// <para>适用于Resources和Addressables等自动管理依赖的加载方式。</para>
    /// <para>返回null表示底层系统自动处理依赖，无需应用层干预。</para>
    /// </summary>
    /// <remarks>
    /// <para>使用场景：</para>
    /// <list type="bullet">
    /// <item><description>Resources模式：Unity自动管理资源依赖</description></item>
    /// <item><description>Addressables模式：Addressables内部Catalog自动管理依赖</description></item>
    /// </list>
    /// <para>设计模式：单例模式，避免重复创建实例</para>
    /// </remarks>
    public class AsakiNullResDependencyLookup : IAsakiResDependencyLookup
    {
        /// <summary>
        /// 单例实例
        /// <para>线程安全的懒加载单例。</para>
        /// </summary>
        public static readonly AsakiNullResDependencyLookup Instance = new();

        /// <summary>
        /// 私有构造函数，防止外部实例化
        /// </summary>
        private AsakiNullResDependencyLookup() { }

        /// <summary>
        /// 获取依赖项列表
        /// </summary>
        /// <param name="location">资源地址（未使用）</param>
        /// <returns>始终返回null，表示无需手动管理依赖</returns>
        public IEnumerable<string> GetDependencies(string location)
        {
            return null;
        }
    }
}
