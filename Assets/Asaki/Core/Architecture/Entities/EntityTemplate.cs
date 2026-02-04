using System;
using System.Collections.Generic;

namespace Asaki.Core.Architecture.Entities
{
    /// <summary>
    /// 实体模板 - 可复用的实体配置
    /// </summary>
    public class EntityTemplate
    {
        private readonly List<Action<IEntity>> _componentAdders = new();
        private readonly List<Action<IEntity>> _configurators = new();

        /// <summary>
        /// 模板名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 添加组件类型
        /// </summary>
        public EntityTemplate With<T>() where T : class, IEntityComponent, new()
        {
            _componentAdders.Add(e => e.AddComponent<T>());
            return this;
        }

        /// <summary>
        /// 添加并配置组件
        /// </summary>
        public EntityTemplate With<T>(Action<T> configure) where T : class, IEntityComponent, new()
        {
            _componentAdders.Add(e => {
                var component = e.AddComponent<T>();
                configure?.Invoke(component);
            });
            return this;
        }

        /// <summary>
        /// 添加标签组件
        /// </summary>
        public EntityTemplate WithTag<T>() where T : TagComponent, new()
        {
            _componentAdders.Add(e => e.AddComponent<T>());
            return this;
        }

        /// <summary>
        /// 添加配置步骤
        /// </summary>
        public EntityTemplate Configure(Action<IEntity> configure)
        {
            _configurators.Add(configure);
            return this;
        }

        /// <summary>
        /// 应用模板到实体
        /// </summary>
        public IEntity ApplyTo(IEntity entity)
        {
            foreach (var adder in _componentAdders)
            {
                adder(entity);
            }
            foreach (var configurator in _configurators)
            {
                configurator(entity);
            }
            return entity;
        }

        /// <summary>
        /// 创建新实体并应用模板
        /// </summary>
        public IEntity Instantiate(IEntityWorld world)
        {
            return ApplyTo(world.CreateEntity());
        }

        /// <summary>
        /// 创建新实体并应用模板，返回实体ID
        /// </summary>
        public EntityId InstantiateId(IEntityWorld world)
        {
            return Instantiate(world).Id;
        }
    }

    /// <summary>
    /// 实体模板注册表
    /// </summary>
    public static class EntityTemplateRegistry
    {
        private static readonly Dictionary<string, EntityTemplate> _templates = new();

        /// <summary>
        /// 注册模板
        /// </summary>
        public static void Register(string name, EntityTemplate template)
        {
            template.Name = name;
            _templates[name] = template;
        }

        /// <summary>
        /// 获取模板
        /// </summary>
        public static EntityTemplate Get(string name)
        {
            return _templates.TryGetValue(name, out var template) ? template : null;
        }

        /// <summary>
        /// 使用模板创建实体
        /// </summary>
        public static IEntity Instantiate(string templateName, IEntityWorld world)
        {
            var template = Get(templateName);
            return template?.Instantiate(world);
        }

        /// <summary>
        /// 使用模板创建实体，返回ID
        /// </summary>
        public static EntityId InstantiateId(string templateName, IEntityWorld world)
        {
            var template = Get(templateName);
            return template?.InstantiateId(world) ?? EntityId.Invalid;
        }

        /// <summary>
        /// 检查模板是否存在
        /// </summary>
        public static bool HasTemplate(string name)
        {
            return _templates.ContainsKey(name);
        }

        /// <summary>
        /// 移除模板
        /// </summary>
        public static bool Unregister(string name)
        {
            return _templates.Remove(name);
        }

        /// <summary>
        /// 清空所有模板
        /// </summary>
        public static void Clear()
        {
            _templates.Clear();
        }

        /// <summary>
        /// 获取所有模板名称
        /// </summary>
        public static IEnumerable<string> GetTemplateNames()
        {
            return _templates.Keys;
        }
    }

    /// <summary>
    /// 实体世界模板扩展
    /// </summary>
    public static class EntityWorldTemplateExtensions
    {
        /// <summary>
        /// 从模板创建实体
        /// </summary>
        public static IEntity CreateFromTemplate(this IEntityWorld world, string templateName)
        {
            return EntityTemplateRegistry.Instantiate(templateName, world);
        }

        /// <summary>
        /// 从模板创建实体，返回ID
        /// </summary>
        public static EntityId CreateFromTemplateId(this IEntityWorld world, string templateName)
        {
            return EntityTemplateRegistry.InstantiateId(templateName, world);
        }
    }
}
