using System;
using System.Collections.Generic;

namespace Asaki.Core.Architecture.Entities.Extensions
{
    /// <summary>
    /// 实体世界批量操作扩展
    /// </summary>
    public static class EntityWorldBatchExtensions
    {
        /// <summary>
        /// 批量修改组件数据
        /// </summary>
        /// <typeparam name="T1">组件类型</typeparam>
        /// <param name="world">实体世界</param>
        /// <param name="modifier">修改函数，返回true表示已修改</param>
        /// <returns>修改的实体数量</returns>
        public static int BatchModify<T1>(this IEntityWorld world, Func<T1, bool> modifier)
            where T1 : class, IEntityComponent
        {
            int modifiedCount = 0;
            foreach (var (entity, component) in world.QueryWith<T1>())
            {
                if (modifier(component))
                {
                    modifiedCount++;
                }
            }
            return modifiedCount;
        }

        /// <summary>
        /// 批量修改组件数据（带实体访问）
        /// </summary>
        public static int BatchModify<T1>(this IEntityWorld world, Func<IEntity, T1, bool> modifier)
            where T1 : class, IEntityComponent
        {
            int modifiedCount = 0;
            foreach (var (entity, component) in world.QueryWith<T1>())
            {
                if (modifier(entity, component))
                {
                    modifiedCount++;
                }
            }
            return modifiedCount;
        }

        /// <summary>
        /// 批量添加组件
        /// </summary>
        public static int BatchAddComponent<T1>(
            this IEntityWorld world,
            Func<IEntity, bool> predicate
        )
            where T1 : class, IEntityComponent, new()
        {
            int addedCount = 0;
            foreach (var entity in world.GetAllEntities())
            {
                if (predicate(entity) && !entity.HasComponent<T1>())
                {
                    entity.AddComponent<T1>();
                    addedCount++;
                }
            }
            return addedCount;
        }

        /// <summary>
        /// 批量添加组件到所有实体
        /// </summary>
        public static int BatchAddComponent<T1>(this IEntityWorld world)
            where T1 : class, IEntityComponent, new()
        {
            int addedCount = 0;
            foreach (var entity in world.GetAllEntities())
            {
                if (!entity.HasComponent<T1>())
                {
                    entity.AddComponent<T1>();
                    addedCount++;
                }
            }
            return addedCount;
        }

        /// <summary>
        /// 批量移除组件
        /// </summary>
        public static int BatchRemoveComponent<T1>(
            this IEntityWorld world,
            Func<IEntity, bool> predicate = null
        )
            where T1 : class, IEntityComponent
        {
            int removedCount = 0;
            foreach (var entity in world.GetAllEntities())
            {
                if (entity.HasComponent<T1>() && (predicate?.Invoke(entity) ?? true))
                {
                    entity.RemoveComponent<T1>();
                    removedCount++;
                }
            }
            return removedCount;
        }

        /// <summary>
        /// 批量销毁实体
        /// </summary>
        public static int BatchDestroy(this IEntityWorld world, Func<IEntity, bool> predicate)
        {
            var toDestroy = new List<EntityId>();
            foreach (var entity in world.GetAllEntities())
            {
                if (predicate(entity))
                {
                    toDestroy.Add(entity.Id);
                }
            }

            foreach (var id in toDestroy)
            {
                world.DestroyEntity(id);
            }

            return toDestroy.Count;
        }

        /// <summary>
        /// 批量设置激活状态
        /// </summary>
        public static int BatchSetActive(
            this IEntityWorld world,
            bool active,
            Func<IEntity, bool> predicate
        )
        {
            int modifiedCount = 0;
            foreach (var entity in world.GetAllEntities())
            {
                if (predicate(entity) && entity.IsActive != active)
                {
                    entity.IsActive = active;
                    modifiedCount++;
                }
            }
            return modifiedCount;
        }

        /// <summary>
        /// 批量设置所有实体激活状态
        /// </summary>
        public static int BatchSetActive(this IEntityWorld world, bool active)
        {
            int modifiedCount = 0;
            foreach (var entity in world.GetAllEntities())
            {
                if (entity.IsActive != active)
                {
                    entity.IsActive = active;
                    modifiedCount++;
                }
            }
            return modifiedCount;
        }
    }
}
