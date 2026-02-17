using System;
using Asaki.Core.Architecture;
using Asaki.Core.Architecture.Entities;
using Asaki.Core.Logging;

namespace Asaki.Game.Scripts.Examples.ECS
{
    /// <summary>
    /// ECS示例架构 - 展示如何使用Asaki的ECS系统
    /// </summary>
    [Serializable]
    public class ECSArchitecture : AsakiArchitecture
    {
        protected override void OnSetup()
        {
            var entityModel = new EntityModel();
            RegisterModel(entityModel);

            RegisterSystem(new MovementSystem());
            RegisterSystem(new HealthSystem());
            RegisterSystem(new PlayerInputSystem());
            RegisterSystem(new RenderSystem());
            RegisterSystem(new EntityStatsSystem());

            ALog.Info(
                "[ECSArchitecture] ECS Architecture initialized with EntityModel and 5 systems"
            );
        }

        /// <summary>
        /// 创建一个基础移动实体
        /// </summary>
        public IEntity CreateMovingEntity()
        {
            var world = GetEntityWorld();
            var entity = world.CreateEntity();

            entity.AddComponent<PositionComponent>();
            entity.AddComponent<VelocityComponent>();

            ALog.Info($"[ECSArchitecture] Created moving entity: {entity.Id}");
            return entity;
        }

        /// <summary>
        /// 创建一个玩家实体
        /// </summary>
        public IEntity CreatePlayerEntity()
        {
            var world = GetEntityWorld();
            var entity = world.CreateEntity();

            entity.AddComponent<PositionComponent>();
            entity.AddComponent<VelocityComponent>();
            entity.AddComponent<HealthComponent>();
            entity.AddComponent<PlayerInputComponent>();
            entity.AddComponent<RenderComponent>();

            entity.AddComponent<PlayerTag>();

            ALog.Info($"[ECSArchitecture] Created player entity: {entity.Id}");
            return entity;
        }

        /// <summary>
        /// 创建一个敌人实体
        /// </summary>
        public IEntity CreateEnemyEntity(int health = 50)
        {
            var world = GetEntityWorld();
            var entity = world.CreateEntity();

            entity.AddComponent<PositionComponent>();
            entity.AddComponent<VelocityComponent>();

            var healthComp = entity.AddComponent<HealthComponent>();
            healthComp.MaxHealth = health;
            healthComp.CurrentHealth = health;

            entity.AddComponent<RenderComponent>();

            entity.AddComponent<EnemyTag>();

            ALog.Info($"[ECSArchitecture] Created enemy entity: {entity.Id} with health: {health}");
            return entity;
        }

        /// <summary>
        /// 创建一个静态装饰实体
        /// </summary>
        public IEntity CreateStaticEntity()
        {
            var world = GetEntityWorld();
            var entity = world.CreateEntity();

            entity.AddComponent<PositionComponent>();
            entity.AddComponent<RenderComponent>();

            entity.AddComponent<StaticTag>();

            ALog.Info($"[ECSArchitecture] Created static entity: {entity.Id}");
            return entity;
        }

        /// <summary>
        /// 获取HealthSystem以便外部调用
        /// </summary>
        public HealthSystem GetHealthSystem()
        {
            return GetSystem<HealthSystem>();
        }

        /// <summary>
        /// 获取EntityWorld以便外部访问
        /// </summary>
        public IEntityWorld GetWorld()
        {
            return GetEntityWorld();
        }
    }
}
