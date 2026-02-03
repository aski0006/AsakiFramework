using System;

namespace Asaki.Core.Architecture.Entities
{
    /// <summary>
    /// 实体组件接口
    /// </summary>
    public interface IEntityComponent : IDisposable
    {
        /// <summary>
        /// 所属实体
        /// </summary>
        IEntity Entity { get; set; }

        /// <summary>
        /// 组件被添加到实体时调用
        /// </summary>
        void OnAttach();

        /// <summary>
        /// 组件从实体移除时调用
        /// </summary>
        void OnDetach();

        /// <summary>
        /// 实体激活时调用
        /// </summary>
        void OnEnable();

        /// <summary>
        /// 实体禁用时调用
        /// </summary>
        void OnDisable();
    }
}
