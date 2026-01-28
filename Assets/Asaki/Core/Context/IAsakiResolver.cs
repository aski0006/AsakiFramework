namespace Asaki.Core.Context
{
    /// <summary>
    /// Asaki依赖解析器接口，用于解析注册的服务实例。
    /// </summary>
    /// <remarks>
    /// 此接口定义了获取服务实例的标准方法，是Asaki依赖注入系统的核心组成部分。
    /// 不同的实现可以提供不同的解析策略，如全局解析、场景级解析或临时解析。
    /// </remarks>
    public interface IAsakiResolver
    {
        /// <summary>
        /// 获取指定类型的服务实例。
        /// </summary>
        /// <typeparam name="T">服务类型，必须是实现了<see cref="IAsakiService"/>接口的类类型。</typeparam>
        /// <returns>请求的服务实例。</returns>
        /// <exception cref="KeyNotFoundException">当指定类型的服务未找到时抛出。</exception>
        T Get<T>()
            where T : class, IAsakiService;

        /// <summary>
        /// 尝试获取指定类型的服务实例，如果找到则返回true，否则返回false。
        /// </summary>
        /// <typeparam name="T">服务类型，必须是实现了<see cref="IAsakiService"/>接口的类类型。</typeparam>
        /// <param name="service">如果找到服务，将返回的服务实例赋值给此参数；否则为null。</param>
        /// <returns>如果找到服务则返回true，否则返回false。</returns>
        bool TryGet<T>(out T service)
            where T : class, IAsakiService;
    }
}
