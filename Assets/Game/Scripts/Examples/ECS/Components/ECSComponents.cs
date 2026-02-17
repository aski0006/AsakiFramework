using Asaki.Core.Architecture.Entities;
using UnityEngine;

namespace Asaki.Game.Scripts.Examples.ECS
{
    /// <summary>
    /// 位置组件 - 存储实体的位置信息
    /// </summary>
    public class PositionComponent : EntityComponent
    {
        public Vector3 Position;

        public override void OnAttach()
        {
            Position = Vector3.zero;
        }
    }

    /// <summary>
    /// 速度组件 - 存储实体的移动速度
    /// </summary>
    public class VelocityComponent : EntityComponent
    {
        public Vector3 Velocity;
        public float Speed = 5f;

        public override void OnAttach()
        {
            Velocity = Vector3.forward;
            Speed = 5f;
        }
    }

    /// <summary>
    /// 生命值组件 - 存储实体的生命值信息
    /// </summary>
    public class HealthComponent : EntityComponent
    {
        public int CurrentHealth;
        public int MaxHealth;
        public bool IsDead => CurrentHealth <= 0;

        public override void OnAttach()
        {
            MaxHealth = 100;
            CurrentHealth = MaxHealth;
        }

        public void TakeDamage(int damage)
        {
            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        }

        public void Heal(int amount)
        {
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        }
    }

    public class PlayerTag : TagComponent { }
    public class EnemyTag : TagComponent { }
    public class StaticTag : TagComponent { }
    /// <summary>
    /// 渲染组件 - 存储实体的渲染信息
    /// </summary>
    public class RenderComponent : EntityComponent
    {
        public Color Color = Color.white;
        public Vector3 Scale = Vector3.one;
        public bool IsVisible = true;

        public override void OnAttach()
        {
            Color = Color.white;
            Scale = Vector3.one;
            IsVisible = true;
        }
    }

    /// <summary>
    /// 玩家输入组件 - 存储玩家输入状态
    /// </summary>
    public class PlayerInputComponent : EntityComponent
    {
        public Vector2 MoveDirection;
        public bool JumpPressed;
        public bool AttackPressed;

        public override void OnAttach()
        {
            MoveDirection = Vector2.zero;
            JumpPressed = false;
            AttackPressed = false;
        }
    }
}
