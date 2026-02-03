using Asaki.Core.Architecture.Command;

namespace Asaki.Core.Architecture.Entities.Extensions
{
    /// <summary>
    /// Architecture 实体系统扩展方法
    /// </summary>
    public static class EntityArchitectureExtensions
    {
        /// <summary>
        /// 获取实体世界
        /// </summary>
        /// <param name="architecture">架构实例</param>
        /// <returns>实体世界</returns>
        public static IEntityWorld GetEntityWorld(this IAsakiArchitecture architecture)
        {
            return architecture.GetModel<EntityModel>()?.World;
        }

        /// <summary>
        /// 创建实体
        /// </summary>
        /// <param name="architecture">架构实例</param>
        /// <returns>新创建的实体</returns>
        public static IEntity CreateEntity(this IAsakiArchitecture architecture)
        {
            return architecture.GetModel<EntityModel>()?.World.CreateEntity();
        }

        /// <summary>
        /// 获取实体
        /// </summary>
        /// <param name="architecture">架构实例</param>
        /// <param name="id">实体ID</param>
        /// <returns>实体实例</returns>
        public static IEntity GetEntity(this IAsakiArchitecture architecture, EntityId id)
        {
            return architecture.GetModel<EntityModel>()?.World.GetEntity(id);
        }

        /// <summary>
        /// 销毁实体
        /// </summary>
        /// <param name="architecture">架构实例</param>
        /// <param name="id">实体ID</param>
        public static void DestroyEntity(this IAsakiArchitecture architecture, EntityId id)
        {
            architecture.GetModel<EntityModel>()?.World.DestroyEntity(id);
        }

        /// <summary>
        /// 执行创建实体命令
        /// </summary>
        /// <param name="architecture">架构实例</param>
        /// <returns>新实体的ID</returns>
        public static EntityId ExecuteCreateEntity(this AsakiArchitecture architecture)
        {
            var command = new Commands.CreateEntityCommand();
            command.Create(architecture);
            return command.Execute();
        }
    }
}
