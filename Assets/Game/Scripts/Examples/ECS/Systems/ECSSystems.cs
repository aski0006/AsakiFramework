using Asaki.Core.Architecture.Entities;
using Asaki.Core.Logging;
using UnityEngine;

namespace Asaki.Game.Scripts.Examples.ECS
{
    /// <summary>
    /// 移动系统 - 处理实体的移动逻辑
    /// 每帧更新所有拥有 PositionComponent 和 VelocityComponent 的实体
    /// </summary>
    public class MovementSystem : AsakiEntityTickableSystemBase
    {
        protected override void OnEntityTick(float deltaTime)
        {
            if (World == null)
                return;

            foreach (var entity in World.Query<PositionComponent, VelocityComponent>())
            {
                var position = entity.GetComponent<PositionComponent>();
                var velocity = entity.GetComponent<VelocityComponent>();

                position.Position += velocity.Velocity * velocity.Speed * deltaTime;

                if (position.Position.magnitude > 100f)
                {
                    position.Position = Vector3.zero;
                    ALog.Info($"[MovementSystem] Entity {entity.Id} position reset");
                }
            }
        }
        public override void Dispose() { }
    }

    /// <summary>
    /// 生命值系统 - 处理实体的生命值逻辑
    /// 监控死亡状态并移除死亡实体
    /// </summary>
    public class HealthSystem : AsakiEntityTickableSystemBase
    {
        protected override void OnEntityTick(float deltaTime)
        {
            if (World == null)
                return;

            var entitiesToRemove = new System.Collections.Generic.List<EntityId>();

            foreach (var entity in World.Query<HealthComponent>())
            {
                var health = entity.GetComponent<HealthComponent>();

                if (health.IsDead)
                {
                    ALog.Info($"[HealthSystem] Entity {entity.Id} died, marking for removal");
                    entitiesToRemove.Add(entity.Id);
                }
            }

            foreach (var entityId in entitiesToRemove)
            {
                World.DestroyEntity(entityId);
            }
        }

        public void ApplyDamage(IEntity entity, int damage)
        {
            if (entity == null)
                return;

            var health = entity.GetComponent<HealthComponent>();
            if (health != null && !health.IsDead)
            {
                health.TakeDamage(damage);
                ALog.Info($"[HealthSystem] Entity {entity.Id} took {damage} damage, health: {health.CurrentHealth}/{health.MaxHealth}");
            }
        }

        public void HealEntity(IEntity entity, int amount)
        {
            if (entity == null)
                return;

            var health = entity.GetComponent<HealthComponent>();
            if (health != null)
            {
                health.Heal(amount);
                ALog.Info($"[HealthSystem] Entity {entity.Id} healed {amount}, health: {health.CurrentHealth}/{health.MaxHealth}");
            }
        }
        public override void Dispose() { }
    }

    /// <summary>
    /// 玩家输入系统 - 处理玩家输入并更新输入组件
    /// </summary>
    public class PlayerInputSystem : AsakiEntityTickableSystemBase
    {
        protected override void OnEntityTick(float deltaTime)
        {
            if (World == null)
                return;

            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            foreach (var entity in World.Query<PlayerInputComponent, VelocityComponent>())
            {
                var input = entity.GetComponent<PlayerInputComponent>();
                var velocity = entity.GetComponent<VelocityComponent>();

                input.MoveDirection = new Vector2(horizontal, vertical);
                input.JumpPressed = Input.GetKeyDown(KeyCode.Space);
                input.AttackPressed = Input.GetMouseButtonDown(0);

                velocity.Velocity = new Vector3(horizontal, 0f, vertical).normalized;
            }
        }
        public override void Dispose() { }
    }

    /// <summary>
    /// 渲染系统 - 处理实体的渲染逻辑（示例中仅记录日志）
    /// </summary>
    public class RenderSystem : AsakiEntityLateTickableSystemBase
    {
        protected override void OnEntityLateTick(float lateDeltaTime)
        {
            if (World == null)
                return;

            int visibleCount = 0;
            foreach (var entity in World.Query<RenderComponent>())
            {
                var render = entity.GetComponent<RenderComponent>();
                if (render.IsVisible)
                {
                    visibleCount++;
                }
            }
        }
        public override void Dispose() { }
    }

    /// <summary>
    /// 实体统计系统 - 收集并报告实体统计信息
    /// </summary>
    public class EntityStatsSystem : AsakiEntityTickableSystemBase
    {
        private float _reportInterval = 5f;
        private float _timeSinceLastReport;

        protected override void OnEntityTick(float deltaTime)
        {
            if (World == null)
                return;

            _timeSinceLastReport += deltaTime;

            if (_timeSinceLastReport >= _reportInterval)
            {
                _timeSinceLastReport = 0f;
                ReportStats();
            }
        }

        private void ReportStats()
        {
            int totalEntities = 0;
            int movingEntities = 0;
            int aliveEntities = 0;
            int visibleEntities = 0;

            foreach (var entity in World.GetAllEntities())
            {
                totalEntities++;

                if (entity.HasComponent<VelocityComponent>())
                    movingEntities++;

                if (entity.HasComponent<HealthComponent>())
                {
                    var health = entity.GetComponent<HealthComponent>();
                    if (!health.IsDead)
                        aliveEntities++;
                }

                if (entity.HasComponent<RenderComponent>())
                {
                    var render = entity.GetComponent<RenderComponent>();
                    if (render.IsVisible)
                        visibleEntities++;
                }
            }

            ALog.Info(
                $"[EntityStatsSystem] Stats: Total={totalEntities}, Moving={movingEntities}, Alive={aliveEntities}, Visible={visibleEntities}"
            );
        }
        public override void Dispose() { }
    }
}
