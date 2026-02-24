using System;

namespace Asaki.Core.Attributes
{
    /// <summary>
    /// [Asaki 架构核心] 模块标记特性。
    /// 用于将一个类声明为 Asaki 系统模块，并定义其启动优先级和依赖关系。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AsakiModuleAttribute : Attribute
    {
        /// <summary>
        /// 初始化优先级。
        /// <para>值越小越早初始化 (0 > 100 > 1000)。</para>
        /// <para>注意：优先级仅在"同级"（无依赖关系）的模块间生效。依赖关系永远优于优先级。</para>
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// 强依赖列表。
        /// <para>声明此模块依赖的其他模块的具体类型 (Type)。</para>
        /// <para>加载器会确保这些依赖项在此模块之前完成初始化。</para>
        /// </summary>
        public Type[] Dependencies { get; }

        /// <summary>
        /// 是否为可选模块。
        /// <para>可选模块初始化失败时不会阻止系统启动。</para>
        /// <para>默认值为 false，即模块初始化失败会导致系统启动失败。</para>
        /// </summary>
        public bool Optional { get; set; } = false;

        /// <summary>
        /// 模块初始化超时时间（毫秒）。
        /// <para>超过此时间未完成初始化将视为初始化失败。</para>
        /// <para>默认值为 30000 毫秒（30 秒）。</para>
        /// </summary>
        public int TimeoutMs { get; set; } = 30000;

        /// <summary>
        /// 声明一个 Asaki 模块。
        /// </summary>
        /// <param name="priority">启动优先级 (默认 1000)</param>
        /// <param name="dependencies">依赖的模块类型列表 (params)</param>
        public AsakiModuleAttribute(int priority = 1000, params Type[] dependencies)
        {
            Priority = priority;
            Dependencies = dependencies ?? Array.Empty<Type>();
        }
    }
}
