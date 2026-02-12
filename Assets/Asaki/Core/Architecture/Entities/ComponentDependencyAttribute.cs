using System;

namespace Asaki.Core.Architecture.Entities
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ComponentDependencyAttribute : Attribute
    {
        public bool Required { get; }
        public ComponentDependencyAttribute() { }
        public ComponentDependencyAttribute(bool required) { Required = required; }
    }

    public interface IGeneratedDependencyInjector
    {
        void __Generated_InjectDependencies();
    }
}
