using Asaki.Core.Context.Resolvers;

namespace Asaki.Core.Context
{
    /// <summary>
    /// 标记接口，用于指示类需要自动依赖注入。
    /// </summary>
    /// <remarks>
    /// 实现此接口的类将被Asaki的注入系统自动检测并处理依赖注入。
    /// 通常与[Inject]属性或类似机制结合使用。
    /// </remarks>
    public interface IAsakiAutoInject { }

    /// <summary>
    /// [Asaki V5] 分布式注入器接口
    /// <para>每个程序集都会生成一个实现此接口的类，用于处理该程序集内的依赖注入。</para>
    /// </summary>
    /// <remarks>
    /// 此接口是Asaki依赖注入系统的核心组件，负责将注册的服务实例注入到目标对象中。
    /// 支持通过指定的解析器进行依赖解析，提供灵活的注入机制。
    /// </remarks>
    public interface IAsakiInjector
    {
        /// <summary>
        /// 尝试为目标对象注入其声明的所有依赖项。
        /// </summary>
        /// <param name="target">要注入依赖的目标对象，通常实现了<see cref="IAsakiAutoInject"/>接口。</param>
        /// <param name="resolver">可选的依赖解析器，如果不指定则使用默认解析器。</param>
        /// <remarks>
        /// 该方法会扫描目标对象的字段和属性，识别需要注入的依赖项（通常通过属性标记），
        /// 然后使用提供的解析器或默认解析器来获取依赖实例并注入到目标对象中。
        /// </remarks>
        void Inject(object target, IAsakiResolver resolver = null);
    }
}
