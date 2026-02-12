using Asaki.Core.Architecture.Entities;

public static class ComponentDependencyInjector
{
    public static void Inject(IEntityComponent component)
    {
        // 极速路径：直接调用生成的代码
        if (component is IGeneratedDependencyInjector injector)
        {
            injector.__Generated_InjectDependencies();
        }
    }
}

public abstract class InjectableComponent : EntityComponent
{
    public override void OnAttach()
    {
        base.OnAttach();
        ComponentDependencyInjector.Inject(this);
    }
}
