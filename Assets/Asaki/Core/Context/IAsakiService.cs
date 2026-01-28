namespace Asaki.Core.Context
{
    /// <summary>
    /// Asaki服务标记接口。
    /// </summary>
    /// <remarks>
    /// 所有要注册到Asaki服务容器的服务都必须实现此接口。
    /// 这是一个标记接口，用于类型识别和约束。
    /// </remarks>
    public interface IAsakiService { }

    /// <summary>
    /// Asaki场景上下文服务标记接口。
    /// </summary>
    /// <remarks>
    /// 实现此接口的服务是特定于场景的，通常由<see cref="Asaki.Core.Context.Resolvers.AsakiSceneContext"/>管理。
    /// 场景上下文服务的生命周期通常与场景相同，场景加载时创建，场景卸载时销毁。
    /// </remarks>
    public interface IAsakiSceneService : IAsakiService { }

    /// <summary>
    /// Asaki全局MonoBehaviour服务接口。
    /// </summary>
    /// <remarks>
    /// 实现此接口的服务是全局的MonoBehaviour组件，通常由Asaki的引导程序初始化。
    /// 这些服务可以访问Unity的生命周期方法，同时享受Asaki依赖注入系统的便利。
    /// </remarks>
    public interface IAsakiGlobalService : IAsakiService
    {
        /// <summary>
        /// 在引导程序初始化阶段调用的方法。
        /// </summary>
        /// <remarks>
        /// 此方法在服务注册到容器后，由引导程序统一调用，用于执行服务的初始化操作。
        /// 适合执行需要在Unity生命周期早期完成的设置。
        /// </remarks>
        void OnBootstrapInit();
    }
}
