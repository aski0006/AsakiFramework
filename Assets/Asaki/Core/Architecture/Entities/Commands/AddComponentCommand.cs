using Asaki.Core.Architecture.Command;

namespace Asaki.Core.Architecture.Entities.Commands
{
    /// <summary>
    /// 添加组件命令（支持 Undo）
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    public class AddComponentCommand<T> : AsakiUndoCommand<T>
        where T : class, IEntityComponent, new()
    {
        private readonly EntityId _entityId;

        /// <summary>
        /// 创建添加组件命令
        /// </summary>
        /// <param name="entityId">目标实体ID</param>
        public AddComponentCommand(EntityId entityId)
        {
            _entityId = entityId;
        }

        /// <summary>
        /// 执行命令，添加组件
        /// </summary>
        /// <returns>添加的组件实例</returns>
        public override T Execute()
        {
            var entityModel = GetModel<EntityModel>();
            var entity = entityModel.World.GetEntity(_entityId);

            if (entity == null)
            {
                LogError($"Entity {_entityId} not found");
                return null;
            }

            var component = entity.AddComponent<T>();
            Log($"Added component {typeof(T).Name} to entity {_entityId}");
            return component;
        }

        /// <summary>
        /// 撤销命令，移除组件
        /// </summary>
        public override void Undo()
        {
            var entityModel = GetModel<EntityModel>();
            var entity = entityModel.World.GetEntity(_entityId);

            if (entity == null)
            {
                LogWarning($"Entity {_entityId} not found during undo");
                return;
            }

            entity.RemoveComponent<T>();
            Log($"Undo: Removed component {typeof(T).Name} from entity {_entityId}");
        }
    }
}
