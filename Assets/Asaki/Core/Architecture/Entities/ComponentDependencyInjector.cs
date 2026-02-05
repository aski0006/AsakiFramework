using System;
using System.Reflection;

namespace Asaki.Core.Architecture.Entities
{
    /// <summary>
    /// 组件依赖特性 - 标记需要自动注入的组件字段
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ComponentDependencyAttribute : Attribute
    {
        /// <summary>
        /// 是否必需（如果为true且组件不存在则抛出异常）
        /// </summary>
        public bool Required { get; set; } = true;

        public ComponentDependencyAttribute() { }

        public ComponentDependencyAttribute(bool required)
        {
            Required = required;
        }
    }

    /// <summary>
    /// 组件依赖注入器
    /// </summary>
    public static class ComponentDependencyInjector
    {
        /// <summary>
        /// 注入组件依赖
        /// </summary>
        public static void Inject(IEntityComponent component)
        {
            if (component?.Entity == null)
                return;

            var type = component.GetType();
            var fields = type.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<ComponentDependencyAttribute>();
                if (attr == null)
                    continue;

                var fieldType = field.FieldType;
                if (!typeof(IEntityComponent).IsAssignableFrom(fieldType))
                    continue;

                var method = typeof(IEntity).GetMethod("GetComponent").MakeGenericMethod(fieldType);
                var value = method.Invoke(component.Entity, null);

                if (value == null && attr.Required)
                {
                    throw new InvalidOperationException(
                        $"Required component {fieldType.Name} not found on entity {component.Entity.Id}"
                    );
                }

                field.SetValue(component, value);
            }
        }
    }

    /// <summary>
    /// 支持依赖注入的组件基类
    /// </summary>
    public abstract class InjectableComponent : EntityComponent
    {
        public override void OnAttach()
        {
            base.OnAttach();
            ComponentDependencyInjector.Inject(this);
        }
    }
}
