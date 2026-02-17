using System;
using System.Collections.Generic;

namespace Asaki.Core.Architecture.Entities.Extensions
{
    /// <summary>
    /// 实体查询结果 - 包含实体和组件引用
    /// </summary>
    public readonly struct EntityQueryResult<T1>
        where T1 : class, IEntityComponent
    {
        public readonly IEntity Entity;
        public readonly T1 Component1;

        public EntityQueryResult(IEntity entity, T1 component1)
        {
            Entity = entity;
            Component1 = component1;
        }

        /// <summary>
        /// 解构支持
        /// </summary>
        public void Deconstruct(out IEntity entity, out T1 component1)
        {
            entity = Entity;
            component1 = Component1;
        }
    }

    /// <summary>
    /// 双组件查询结果
    /// </summary>
    public readonly struct EntityQueryResult<T1, T2>
        where T1 : class, IEntityComponent
        where T2 : class, IEntityComponent
    {
        public readonly IEntity Entity;
        public readonly T1 Component1;
        public readonly T2 Component2;

        public EntityQueryResult(IEntity entity, T1 component1, T2 component2)
        {
            Entity = entity;
            Component1 = component1;
            Component2 = component2;
        }

        public void Deconstruct(out IEntity entity, out T1 component1, out T2 component2)
        {
            entity = Entity;
            component1 = Component1;
            component2 = Component2;
        }
    }

    /// <summary>
    /// 三组件查询结果
    /// </summary>
    public readonly struct EntityQueryResult<T1, T2, T3>
        where T1 : class, IEntityComponent
        where T2 : class, IEntityComponent
        where T3 : class, IEntityComponent
    {
        public readonly IEntity Entity;
        public readonly T1 Component1;
        public readonly T2 Component2;
        public readonly T3 Component3;

        public EntityQueryResult(IEntity entity, T1 component1, T2 component2, T3 component3)
        {
            Entity = entity;
            Component1 = component1;
            Component2 = component2;
            Component3 = component3;
        }

        public void Deconstruct(
            out IEntity entity,
            out T1 component1,
            out T2 component2,
            out T3 component3
        )
        {
            entity = Entity;
            component1 = Component1;
            component2 = Component2;
            component3 = Component3;
        }
    }

    /// <summary>
    /// 实体世界查询扩展
    /// </summary>
    public static class EntityWorldQueryExtensions
    {
        /// <summary>
        /// 查询并获取组件引用（高性能，避免二次查找）
        /// </summary>
        public static IEnumerable<EntityQueryResult<T1>> QueryWith<T1>(this IEntityWorld world)
            where T1 : class, IEntityComponent
        {
            foreach (var entity in world.Query<T1>())
            {
                var component = entity.GetComponent<T1>();
                if (component != null)
                {
                    yield return new EntityQueryResult<T1>(entity, component);
                }
            }
        }

        /// <summary>
        /// 双组件查询
        /// </summary>
        public static IEnumerable<EntityQueryResult<T1, T2>> QueryWith<T1, T2>(
            this IEntityWorld world
        )
            where T1 : class, IEntityComponent
            where T2 : class, IEntityComponent
        {
            foreach (var entity in world.Query<T1, T2>())
            {
                var c1 = entity.GetComponent<T1>();
                var c2 = entity.GetComponent<T2>();
                if (c1 != null && c2 != null)
                {
                    yield return new EntityQueryResult<T1, T2>(entity, c1, c2);
                }
            }
        }

        /// <summary>
        /// 三组件查询
        /// </summary>
        public static IEnumerable<EntityQueryResult<T1, T2, T3>> QueryWith<T1, T2, T3>(
            this IEntityWorld world
        )
            where T1 : class, IEntityComponent
            where T2 : class, IEntityComponent
            where T3 : class, IEntityComponent
        {
            foreach (var entity in world.Query<T1, T2, T3>())
            {
                var c1 = entity.GetComponent<T1>();
                var c2 = entity.GetComponent<T2>();
                var c3 = entity.GetComponent<T3>();
                if (c1 != null && c2 != null && c3 != null)
                {
                    yield return new EntityQueryResult<T1, T2, T3>(entity, c1, c2, c3);
                }
            }
        }

        public static void ForEach(this IEntityWorld world, Action<IEntity> action)
        {
            if (action == null)
                return;

            foreach (var entity in world.GetAllEntities())
            {
                action(entity);
            }
        }

        /// <summary>
        /// 批量处理组件（最高性能）
        /// </summary>
        public static void ForEach<T1>(this IEntityWorld world, Action<IEntity, T1> action)
            where T1 : class, IEntityComponent
        {
            if (action == null)
                return;

            foreach (var (entity, component) in world.QueryWith<T1>())
            {
                action(entity, component);
            }
        }

        /// <summary>
        /// 批量处理双组件
        /// </summary>
        public static void ForEach<T1, T2>(this IEntityWorld world, Action<IEntity, T1, T2> action)
            where T1 : class, IEntityComponent
            where T2 : class, IEntityComponent
        {
            if (action == null)
                return;

            foreach (var (entity, c1, c2) in world.QueryWith<T1, T2>())
            {
                action(entity, c1, c2);
            }
        }

        /// <summary>
        /// 批量处理三组件
        /// </summary>
        public static void ForEach<T1, T2, T3>(
            this IEntityWorld world,
            Action<IEntity, T1, T2, T3> action
        )
            where T1 : class, IEntityComponent
            where T2 : class, IEntityComponent
            where T3 : class, IEntityComponent
        {
            if (action == null)
                return;

            foreach (var (entity, c1, c2, c3) in world.QueryWith<T1, T2, T3>())
            {
                action(entity, c1, c2, c3);
            }
        }

        /// <summary>
        /// 获取第一个匹配的实体
        /// </summary>
        public static IEntity FirstOrDefault<T1>(this IEntityWorld world)
            where T1 : class, IEntityComponent
        {
            foreach (var entity in world.Query<T1>())
            {
                return entity;
            }
            return null;
        }

        /// <summary>
        /// 获取第一个匹配的实体及其组件
        /// </summary>
        public static (IEntity Entity, T1 Component) FirstOrDefaultWith<T1>(this IEntityWorld world)
            where T1 : class, IEntityComponent
        {
            foreach (var (entity, component) in world.QueryWith<T1>())
            {
                return (entity, component);
            }
            return (null, null);
        }

        /// <summary>
        /// 获取所有匹配的实体数量
        /// </summary>
        public static int Count<T1>(this IEntityWorld world)
            where T1 : class, IEntityComponent
        {
            int count = 0;
            foreach (var _ in world.Query<T1>())
            {
                count++;
            }
            return count;
        }

        #region Safe Enumeration (Snapshot Support)

        /// <summary>
        /// 创建查询结果的快照列表，避免在遍历过程中修改集合导致的枚举器失效问题
        /// 使用场景：需要在遍历内添加/移除组件时
        /// </summary>
        /// <example>
        /// // 安全地在遍历内移除组件
        /// foreach (var entity in world.Query<HealthComponent>().ToList())
        /// {
        ///     if (entity.GetComponent<HealthComponent>().IsDead)
        ///     {
        ///         entity.RemoveComponent<HealthComponent>(); // 不会抛出异常
        ///     }
        /// }
        /// </example>
        public static List<IEntity> ToList<T1>(this IEnumerable<IEntity> queryResult)
            where T1 : class, IEntityComponent
        {
            var list = new List<IEntity>();
            foreach (var entity in queryResult)
            {
                list.Add(entity);
            }
            return list;
        }

        /// <summary>
        /// 创建查询结果的快照列表
        /// </summary>
        public static List<EntityQueryResult<T1>> ToList<T1>(
            this IEnumerable<EntityQueryResult<T1>> queryResult
        )
            where T1 : class, IEntityComponent
        {
            var list = new List<EntityQueryResult<T1>>();
            foreach (var result in queryResult)
            {
                list.Add(result);
            }
            return list;
        }

        /// <summary>
        /// 创建查询结果的快照列表
        /// </summary>
        public static List<EntityQueryResult<T1, T2>> ToList<T1, T2>(
            this IEnumerable<EntityQueryResult<T1, T2>> queryResult
        )
            where T1 : class, IEntityComponent
            where T2 : class, IEntityComponent
        {
            var list = new List<EntityQueryResult<T1, T2>>();
            foreach (var result in queryResult)
            {
                list.Add(result);
            }
            return list;
        }

        /// <summary>
        /// 创建查询结果的快照列表
        /// </summary>
        public static List<EntityQueryResult<T1, T2, T3>> ToList<T1, T2, T3>(
            this IEnumerable<EntityQueryResult<T1, T2, T3>> queryResult
        )
            where T1 : class, IEntityComponent
            where T2 : class, IEntityComponent
            where T3 : class, IEntityComponent
        {
            var list = new List<EntityQueryResult<T1, T2, T3>>();
            foreach (var result in queryResult)
            {
                list.Add(result);
            }
            return list;
        }

        #endregion
    }
}
