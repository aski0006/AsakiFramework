using System;
using Asaki.Core.Configuration;

namespace Asaki.Core.Attributes
{
    /// <summary>
    /// 配置表元数据标记
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AsakiConfigAttribute : Attribute
    {
        /// <summary>
        /// 加载策略（默认 Auto）
        /// </summary>
        public AsakiConfigLoadStrategy LoadStrategy { get; set; } = AsakiConfigLoadStrategy.Auto;

        /// <summary>
        /// 优先级（越高越先加载，默认 0）
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// 是否允许卸载（释放内存，默认 true）
        /// </summary>
        public bool Unloadable { get; set; } = true;

        /// <summary>
        /// 依赖的其他配置表（自动级联加载）
        /// </summary>
        public Type[] Dependencies { get; set; }
    }
}
