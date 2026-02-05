using System;
using System.Collections.Generic;

namespace Asaki.Core.Architecture.Entities
{
    /// <summary>
    /// 实体构建器 - 支持链式创建实体
    /// </summary>
    public class EntityBuilder
    {
        private readonly IEntityWorld _world;
        private readonly IEntity _entity;
        private readonly List<Action<IEntity>> _configurations = new();

        public EntityBuilder(IEntityWorld world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _entity = world.CreateEntity();
        }

        /// <summary>
        /// 添加组件
        /// </summary>
        public EntityBuilder With<T>()
            where T : class, IEntityComponent, new()
        {
            _entity.AddComponent<T>();
            return this;
        }

        /// <summary>
        /// 添加并配置组件
        /// </summary>
        public EntityBuilder With<T>(Action<T> configure)
            where T : class, IEntityComponent, new()
        {
            var component = _entity.AddComponent<T>();
            configure?.Invoke(component);
            return this;
        }

        /// <summary>
        /// 添加已有组件实例
        /// </summary>
        public EntityBuilder With<T>(T component)
            where T : class, IEntityComponent
        {
            _entity.AddComponent(component);
            return this;
        }

        /// <summary>
        /// 设置实体激活状态
        /// </summary>
        public EntityBuilder SetActive(bool active)
        {
            _entity.IsActive = active;
            return this;
        }

        /// <summary>
        /// 添加标签组件（便捷方法）
        /// </summary>
        public EntityBuilder WithTag<T>()
            where T : TagComponent, new()
        {
            _entity.AddComponent<T>();
            return this;
        }

        /// <summary>
        /// 构建并返回实体
        /// </summary>
        public IEntity Build()
        {
            foreach (var config in _configurations)
            {
                config(_entity);
            }
            return _entity;
        }

        /// <summary>
        /// 构建并返回实体ID
        /// </summary>
        public EntityId BuildId()
        {
            return Build().Id;
        }
    }

    /// <summary>
    /// 实体世界扩展 - 添加构建器支持
    /// </summary>
    public static class EntityWorldBuilderExtensions
    {
        /// <summary>
        /// 创建实体构建器
        /// </summary>
        public static EntityBuilder Create(this IEntityWorld world)
        {
            return new EntityBuilder(world);
        }
    }
}
