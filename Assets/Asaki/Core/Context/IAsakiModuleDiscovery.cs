using System;
using System.Collections.Generic;

namespace Asaki.Core.Context
{
    /// <summary>
    /// [物理隔离适配器] 模块发现策略接口。
    /// <para>Core 层定义需求，Unity 层负责实现 (通过反射扫描或代码生成)。</para>
    /// </summary>
    public interface IAsakiModuleDiscovery
    {
        /// <summary>
        /// 获取所有符合条件的模块类型。
        /// </summary>
        /// <returns>带有 [AsakiModule] 标记且实现了 IAsakiModule 的类型集合。</returns>
        IEnumerable<Type> GetModuleTypes();
    }
}
