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

}
