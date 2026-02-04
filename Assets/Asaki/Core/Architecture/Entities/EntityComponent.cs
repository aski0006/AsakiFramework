using System;

namespace Asaki.Core.Architecture.Entities
{
    /// <summary>
    /// 实体组件抽象基类 - 提供生命周期默认实现
    /// </summary>
    public abstract class EntityComponent : IEntityComponent
    {
        /// <summary>
        /// 所属实体
        /// </summary>
        public IEntity Entity { get; set; }

        /// <summary>
        /// 组件被添加到实体时调用（可重写）
        /// </summary>
        public virtual void OnAttach() { }

        /// <summary>
        /// 组件从实体移除时调用（可重写）
        /// </summary>
        public virtual void OnDetach() { }

        /// <summary>
        /// 实体激活时调用（可重写）
        /// </summary>
        public virtual void OnEnable() { }

        /// <summary>
        /// 实体禁用时调用（可重写）
        /// </summary>
        public virtual void OnDisable() { }

        /// <summary>
        /// 释放组件资源（可重写）
        /// </summary>
        public virtual void Dispose() { }

        /// <summary>
        /// 获取同一实体的其他组件（便捷方法）
        /// </summary>
        protected T GetSibling<T>() where T : class, IEntityComponent
        {
            return Entity?.GetComponent<T>();
        }

        /// <summary>
        /// 检查同一实体是否有其他组件（便捷方法）
        /// </summary>
        protected bool HasSibling<T>() where T : class, IEntityComponent
        {
            return Entity?.HasComponent<T>() ?? false;
        }
    }

    /// <summary>
    /// 标签组件基类 - 无数据，仅作标记
    /// </summary>
    public abstract class TagComponent : EntityComponent
    {
        // 标签组件不需要任何实现
    }
}
