using System;
using System.Collections.Generic;

namespace Asaki.Core.Architecture.Entities
{
    /// <summary>
    /// 实体接口 - 代表游戏中的对象实例
    /// </summary>
    public interface IEntity : IDisposable
    {
        /// <summary>
        /// 实体唯一标识符（包含魔法容器句柄）
        /// </summary>
        EntityId Id { get; }

        /// <summary>
        /// 实体是否激活
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// 实体所属的世界
        /// </summary>
        IEntityWorld World { get; }

        /// <summary>
        /// 添加组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件实例</returns>
        T AddComponent<T>()
            where T : class, IEntityComponent, new();

        /// <summary>
        /// 添加已有组件实例
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="component">组件实例</param>
        /// <returns>组件实例</returns>
        T AddComponent<T>(T component)
            where T : class, IEntityComponent;

        /// <summary>
        /// 获取组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件实例，不存在则返回null</returns>
        T GetComponent<T>()
            where T : class, IEntityComponent;

        /// <summary>
        /// 尝试获取组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="component">输出组件</param>
        /// <returns>是否成功获取</returns>
        bool TryGetComponent<T>(out T component)
            where T : class, IEntityComponent;

        /// <summary>
        /// 移除组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>是否成功移除</returns>
        bool RemoveComponent<T>()
            where T : class, IEntityComponent;

        /// <summary>
        /// 检查是否具有指定组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>是否具有该组件</returns>
        bool HasComponent<T>()
            where T : class, IEntityComponent;

        /// <summary>
        /// 检查是否具有指定组件（基于类型）
        /// </summary>
        /// <param name="componentType">组件类型</param>
        /// <returns>是否具有该组件</returns>
        bool HasComponent(Type componentType);

        /// <summary>
        /// 移除组件（基于类型）
        /// </summary>
        /// <param name="componentType">组件类型</param>
        /// <returns>是否成功移除</returns>
        bool RemoveComponent(Type componentType);

        /// <summary>
        /// 获取所有组件
        /// </summary>
        /// <returns>组件枚举</returns>
        IEnumerable<IEntityComponent> GetAllComponents();

        /// <summary>
        /// 组件数量
        /// </summary>
        int ComponentCount { get; }
    }
}
