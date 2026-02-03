using Asaki.Core.Architecture.Command;

namespace Asaki.Core.Architecture.Entities.Commands
{
    /// <summary>
    /// 销毁实体命令
    /// </summary>
    public class DestroyEntityCommand : AsakiCommand
    {
        private readonly EntityId _entityId;

        /// <summary>
        /// 创建销毁实体命令
        /// </summary>
        /// <param name="entityId">要销毁的实体ID</param>
        public DestroyEntityCommand(EntityId entityId)
        {
            _entityId = entityId;
        }

        /// <summary>
        /// 执行命令，销毁实体
        /// </summary>
        public override void Execute()
        {
            var entityModel = GetModel<EntityModel>();
            entityModel.World.DestroyEntity(_entityId);
            Log($"Destroyed entity: {_entityId}");
        }
    }
}
