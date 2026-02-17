using Asaki.Core.Context;
using Asaki.Core.Context.Resolvers;

namespace Asaki.Core.Architecture.Entities
{
    /// <summary>
    /// 组件依赖注入器 - 统一使用 AsakiGlobalInjector
    /// </summary>
    public static class ComponentDependencyInjector
    {
        /// <summary>
        /// 注入组件依赖
        /// </summary>
        /// <param name="component">要注入的组件</param>
        /// <param name="resolver">可选的解析器，默认使用全局解析器</param>
        public static void Inject(IEntityComponent component, IAsakiResolver resolver = null)
        {
            resolver ??= AsakiGlobalResolver.Instance;

            // 使用 AsakiGlobalInjector 的统一逻辑
            if (component is IGeneratedDependencyInjector injector)
            {
                injector.__Generated_InjectDependencies();
            }
        }
    }

    /// <summary>
    /// 可注入组件基类
    /// </summary>
    public abstract class InjectableComponent : EntityComponent
    {
        public override void OnAttach()
        {
            base.OnAttach();

            // 尝试从 Entity 获取 World，再获取 Resolver
            var world = Entity?.World;
            if (world is IAsakiResolverProvider resolverProvider)
            {
                ComponentDependencyInjector.Inject(this, resolverProvider.Resolver);
            }
            else
            {
                ComponentDependencyInjector.Inject(this);
            }
        }
    }

    /// <summary>
    /// 解析器提供者接口
    /// </summary>
    public interface IAsakiResolverProvider
    {
        IAsakiResolver Resolver { get; }
    }
}
