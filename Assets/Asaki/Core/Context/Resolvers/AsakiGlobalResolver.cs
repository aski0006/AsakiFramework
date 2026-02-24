namespace Asaki.Core.Context.Resolvers
{
    /// <summary>
    /// Asaki全局服务解析器。
    /// </summary>
    /// <remarks>
    /// 此解析器将解析请求委托给全局的<see cref="AsakiContext"/>服务容器。
    /// 实现了<see cref="IAsakiResolver"/>接口，提供了一种统一的方式来访问全局服务。
    /// 作为结构体实现，确保了高效的内存使用和实例化。
    /// </remarks>
    public readonly struct AsakiGlobalResolver : IAsakiResolver
    {
        /// <summary>
        /// 获取<see cref="AsakiGlobalResolver"/>的单例实例。
        /// </summary>
        /// <remarks>
        /// 由于结构体的特性，此单例实例是线程安全且高效的。
        /// </remarks>
        public static readonly AsakiGlobalResolver Instance = new AsakiGlobalResolver();

        /// <summary>
        /// 从全局服务容器获取指定类型的服务实例。
        /// </summary>
        /// <typeparam name="T">服务类型，必须是实现了<see cref="IAsakiService"/>接口的类类型。</typeparam>
        /// <returns>请求的服务实例。</returns>
        /// <exception cref="KeyNotFoundException">当指定类型的服务未找到时抛出。</exception>
        /// <exception cref="CircularDependencyException">当检测到循环依赖时抛出。</exception>
        /// <remarks>
        /// 此方法直接调用<see cref="AsakiContext.Get{T}()"/>，提供了与全局容器一致的服务解析行为。
        /// 使用<see cref="AsakiResolveContext"/>进行循环依赖检测，确保解析过程的安全性。
        /// </remarks>
        public T Get<T>()
            where T : class, IAsakiService
        {
            AsakiResolveContext.BeginResolve(typeof(T));
            try
            {
                return AsakiContext.Get<T>();
            }
            finally
            {
                AsakiResolveContext.EndResolve(typeof(T));
            }
        }

        /// <summary>
        /// 尝试从全局服务容器获取指定类型的服务实例。
        /// </summary>
        /// <typeparam name="T">服务类型，必须是实现了<see cref="IAsakiService"/>接口的类类型。</typeparam>
        /// <param name="service">如果找到服务，将返回的服务实例赋值给此参数；否则为null。</param>
        /// <returns>如果找到服务则返回true，否则返回false。</returns>
        /// <exception cref="CircularDependencyException">当检测到循环依赖时抛出。</exception>
        /// <remarks>
        /// 此方法直接调用<see cref="AsakiContext.TryGet{T}(out T)"/>，提供了与全局容器一致的服务解析行为。
        /// 使用<see cref="AsakiResolveContext"/>进行循环依赖检测，确保解析过程的安全性。
        /// </remarks>
        public bool TryGet<T>(out T service)
            where T : class, IAsakiService
        {
            AsakiResolveContext.BeginResolve(typeof(T));
            try
            {
                return AsakiContext.TryGet(out service);
            }
            finally
            {
                AsakiResolveContext.EndResolve(typeof(T));
            }
        }
    }
}
