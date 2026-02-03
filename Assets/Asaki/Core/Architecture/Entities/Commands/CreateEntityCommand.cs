using Asaki.Core.Architecture.Command;

namespace Asaki.Core.Architecture.Entities.Commands
{
    /// <summary>
    /// 创建实体命令
    /// </summary>
    public class CreateEntityCommand : AsakiCommand<EntityId>
    {
        /// <summary>
        /// 执行命令，创建新实体
        /// </summary>
        /// <returns>新实体的ID</returns>
        public override EntityId Execute()
        {
            var entityModel = GetModel<EntityModel>();
            var entity = entityModel.World.CreateEntity();
            Log($"Created entity: {entity.Id}");
            return entity.Id;
        }
    }
}
