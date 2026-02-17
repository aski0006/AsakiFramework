using System;
using System.Collections.Generic;

namespace Asaki.Core.Architecture.Entities
{
    /// <summary>
    /// 实体世界 - 管理所有实体的容器
    /// </summary>
    public interface IEntityWorld : IDisposable
    {
        /// <summary>
        /// 创建实体
        /// </summary>
        /// <returns>新创建的实体</returns>
        IEntity CreateEntity();

        /// <summary>
        /// 销毁实体
        /// </summary>
        /// <param name="id">实体ID</param>
        void DestroyEntity(EntityId id);

        /// <summary>
        /// 获取实体
        /// </summary>
        /// <param name="id">实体ID</param>
        /// <returns>实体实例，不存在则返回null</returns>
        IEntity GetEntity(EntityId id);

        /// <summary>
        /// 尝试获取实体
        /// </summary>
        /// <param name="id">实体ID</param>
        /// <param name="entity">输出实体</param>
        /// <returns>是否成功获取</returns>
        bool TryGetEntity(EntityId id, out IEntity entity);

        /// <summary>
        /// 获取所有实体（连续内存遍历，高性能）
        /// </summary>
        /// <returns>实体枚举</returns>
        IEnumerable<IEntity> GetAllEntities();

        /// <summary>
        /// 查询具有指定组件的实体
        /// </summary>
        /// <typeparam name="T1">组件类型</typeparam>
        /// <returns>符合条件的实体</returns>
        IEnumerable<IEntity> Query<T1>()
            where T1 : class, IEntityComponent;

        /// <summary>
        /// 查询具有指定组件组合的实体
        /// </summary>
        /// <typeparam name="T1">组件类型1</typeparam>
        /// <typeparam name="T2">组件类型2</typeparam>
        /// <returns>符合条件的实体</returns>
        IEnumerable<IEntity> Query<T1, T2>()
            where T1 : class, IEntityComponent
            where T2 : class, IEntityComponent;

        /// <summary>
        /// 查询具有指定组件组合的实体
        /// </summary>
        /// <typeparam name="T1">组件类型1</typeparam>
        /// <typeparam name="T2">组件类型2</typeparam>
        /// <typeparam name="T3">组件类型3</typeparam>
        /// <returns>符合条件的实体</returns>
        IEnumerable<IEntity> Query<T1, T2, T3>()
            where T1 : class, IEntityComponent
            where T2 : class, IEntityComponent
            where T3 : class, IEntityComponent;

        /// <summary>
        /// 查询具有指定组件组合的实体
        /// </summary>
        /// <typeparam name="T1">组件类型1</typeparam>
        /// <typeparam name="T2">组件类型2</typeparam>
        /// <typeparam name="T3">组件类型3</typeparam>
        /// <typeparam name="T4">组件类型4</typeparam>
        /// <returns>符合条件的实体</returns>
        IEnumerable<IEntity> Query<T1, T2, T3, T4>()
            where T1 : class, IEntityComponent
            where T2 : class, IEntityComponent
            where T3 : class, IEntityComponent
            where T4 : class, IEntityComponent;

        /// <summary>
        /// 查询具有指定组件组合的实体
        /// </summary>
        /// <typeparam name="T1">组件类型1</typeparam>
        /// <typeparam name="T2">组件类型2</typeparam>
        /// <typeparam name="T3">组件类型3</typeparam>
        /// <typeparam name="T4">组件类型4</typeparam>
        /// <typeparam name="T5">组件类型5</typeparam>
        /// <returns>符合条件的实体</returns>
        IEnumerable<IEntity> Query<T1, T2, T3, T4, T5>()
            where T1 : class, IEntityComponent
            where T2 : class, IEntityComponent
            where T3 : class, IEntityComponent
            where T4 : class, IEntityComponent
            where T5 : class, IEntityComponent;

        /// <summary>
        /// 查询具有指定组件组合的实体
        /// </summary>
        /// <typeparam name="T1">组件类型1</typeparam>
        /// <typeparam name="T2">组件类型2</typeparam>
        /// <typeparam name="T3">组件类型3</typeparam>
        /// <typeparam name="T4">组件类型4</typeparam>
        /// <typeparam name="T5">组件类型5</typeparam>
        /// <typeparam name="T6">组件类型6</typeparam>
        /// <returns>符合条件的实体</returns>
        IEnumerable<IEntity> Query<T1, T2, T3, T4, T5, T6>()
            where T1 : class, IEntityComponent
            where T2 : class, IEntityComponent
            where T3 : class, IEntityComponent
            where T4 : class, IEntityComponent
            where T5 : class, IEntityComponent
            where T6 : class, IEntityComponent;

        /// <summary>
        /// 实体数量
        /// </summary>
        int EntityCount { get; }

        /// <summary>
        /// 通过索引获取实体（用于高性能遍历）
        /// </summary>
        /// <param name="index">数组索引</param>
        /// <returns>实体实例</returns>
        IEntity GetEntityAt(int index);
    }
}
