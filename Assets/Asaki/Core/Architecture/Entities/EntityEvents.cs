using Asaki.Core.Broker;

namespace Asaki.Core.Architecture.Entities
{
    /// <summary>
    /// 实体创建事件
    /// </summary>
    public struct EntityCreatedEvent : IAsakiEvent
    {
        /// <summary>
        /// 创建的实体ID
        /// </summary>
        public EntityId EntityId;

        /// <summary>
        /// 实体世界
        /// </summary>
        public IEntityWorld World;
    }

    /// <summary>
    /// 实体销毁事件
    /// </summary>
    public struct EntityDestroyedEvent : IAsakiEvent
    {
        /// <summary>
        /// 销毁的实体ID
        /// </summary>
        public EntityId EntityId;

        /// <summary>
        /// 实体世界
        /// </summary>
        public IEntityWorld World;
    }

    /// <summary>
    /// 组件添加事件
    /// </summary>
    public struct ComponentAddedEvent : IAsakiEvent
    {
        /// <summary>
        /// 目标实体ID
        /// </summary>
        public EntityId EntityId;

        /// <summary>
        /// 组件类型名称
        /// </summary>
        public string ComponentTypeName;
    }

    /// <summary>
    /// 组件移除事件
    /// </summary>
    public struct ComponentRemovedEvent : IAsakiEvent
    {
        /// <summary>
        /// 目标实体ID
        /// </summary>
        public EntityId EntityId;

        /// <summary>
        /// 组件类型名称
        /// </summary>
        public string ComponentTypeName;
    }
}
