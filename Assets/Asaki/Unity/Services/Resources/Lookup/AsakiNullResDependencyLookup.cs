using System.Collections.Generic;
using Asaki.Core.Resources;

namespace Asaki.Unity.Services.Resources.Lookup
{
    /// <summary>
    /// [直通式依赖查询]
    /// 适用于 Resources 和 Addressables。
    /// 因为这两个系统内部会自动处理依赖加载，不需要 Resources 介入递归逻辑。
    /// </summary>
    public class AsakiNullResDependencyLookup : IAsakiResDependencyLookup
    {
        // 静态单例，避免重复分配
        public static readonly AsakiNullResDependencyLookup Instance =
            new AsakiNullResDependencyLookup();

        public IEnumerable<string> GetDependencies(string location)
        {
            // 返回 null，告诉 ResManager：“这个资源没有需要我在应用层手动管理的依赖，
            // 请直接调用 Strategy.LoadAssetInternalAsync，让底层去处理。”
            return null;
        }
    }
}
