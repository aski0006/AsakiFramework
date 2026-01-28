namespace Asaki.Core.Context.Resolvers
{
    /// <summary>
    /// Asaki临时服务解析器。
    /// </summary>
    /// <remarks>
    /// 此解析器提供了一种临时的服务解析方式，优先检查提供的临时参数，然后再委托给父级解析器。
    /// 通常用于在特定上下文中传递临时参数或覆盖默认服务实现。
    /// 作为结构体实现，确保了高效的内存使用和实例化。
    /// </remarks>
    public readonly struct AsakiTransientResolver : IAsakiResolver
    {
        private readonly IAsakiResolver _parent; // 通常是 SceneContext 或 Global
        private readonly object _param; // 临时参数

        /// <summary>
        /// 初始化<see cref="AsakiTransientResolver"/>结构体的新实例。
        /// </summary>
        /// <param name="parent">父级解析器，通常是SceneContext或GlobalResolver。</param>
        /// <param name="param">临时参数，用于优先解析服务。</param>
        /// <remarks>
        /// 父级解析器将在临时参数无法匹配时被调用。
        /// 临时参数通常用于初始化参数或上下文特定的服务。
        /// </remarks>
        public AsakiTransientResolver(IAsakiResolver parent, object param)
        {
            _parent = parent;
            _param = param;
        }

        /// <summary>
        /// 解析指定类型的服务实例。
        /// </summary>
        /// <typeparam name="T">服务类型，必须是实现了IAsakiService接口的类类型。</typeparam>
        /// <returns>请求的服务实例。</returns>
        /// <exception cref="KeyNotFoundException">当指定类型的服务未找到时抛出。</exception>
        /// <remarks>
        /// 首先检查临时参数是否匹配请求的服务类型，如果匹配则返回。
        /// 如果临时参数不匹配，则委托给父级解析器查找服务。
        /// </remarks>
        public T Get<T>()
            where T : class, IAsakiService
        {
            // 1. 优先匹配参数 (InitArgs)
            if (_param is T t)
                return t;

            // 2. 没匹配上，交给父级去查
            return _parent.Get<T>();
        }

        /// <summary>
        /// 尝试解析指定类型的服务实例。
        /// </summary>
        /// <typeparam name="T">服务类型，必须是实现了IAsakiService接口的类类型。</typeparam>
        /// <param name="service">如果找到服务，将返回的服务实例赋值给此参数；否则为null。</param>
        /// <returns>如果找到服务则返回true，否则返回false。</returns>
        /// <remarks>
        /// 首先检查临时参数是否匹配请求的服务类型，如果匹配则返回true。
        /// 如果临时参数不匹配，则委托给父级解析器查找服务。
        /// </remarks>
        public bool TryGet<T>(out T service)
            where T : class, IAsakiService
        {
            // 1. 优先匹配参数 (InitArgs)
            if (_param is not T t)
                return _parent.TryGet(out service);
            service = t;
            return true;

            // 2. 没匹配上，交给父级去查
        }
    }
}
