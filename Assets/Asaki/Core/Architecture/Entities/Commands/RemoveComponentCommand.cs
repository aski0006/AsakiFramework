using Asaki.Core.Architecture.Command;

namespace Asaki.Core.Architecture.Entities.Commands
{
    /// <summary>
    /// 移除组件命令（支持 Undo）
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    public class RemoveComponentCommand<T> : AsakiUndoCommand
        where T : class, IEntityComponent, new()
    {
        private readonly EntityId _entityId;
        private T _removedComponent;

        /// <summary>
        /// 创建移除组件命令
        /// </summary>
        /// <param name="entityId">目标实体ID</param>
        public RemoveComponentCommand(EntityId entityId)
        {
            _entityId = entityId;
        }

        /// <summary>
        /// 执行命令，移除组件
        /// </summary>
        public override void Execute()
        {
            var entityModel = GetModel<EntityModel>();
            var entity = entityModel.World.GetEntity(_entityId);

            if (entity == null)
            {
                LogError($"Entity {_entityId} not found");
                return;
            }

            // 保存组件数据用于 Undo
            _removedComponent = entity.GetComponent<T>();
            entity.RemoveComponent<T>();

            Log($"Removed component {typeof(T).Name} from entity {_entityId}");
        }

        /// <summary>
        /// 撤销命令，重新添加组件
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

            if (_removedComponent != null)
            {
                entity.AddComponent(_removedComponent);
                Log($"Undo: Re-added component {typeof(T).Name} to entity {_entityId}");
            }
        }
    }
}
