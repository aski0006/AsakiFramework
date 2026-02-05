using System;

namespace Asaki.Core.Architecture.Entities.Components
{
    /// <summary>
    /// 生命周期组件 - 管理实体的创建、激活、禁用和销毁事件
    /// </summary>
    public class LifecycleComponent : EntityComponent
    {
        /// <summary>
        /// 实体创建时间
        /// </summary>
        public DateTime CreationTime { get; private set; }

        /// <summary>
        /// 实体激活时间
        /// </summary>
        public DateTime? ActivationTime { get; private set; }

        /// <summary>
        /// 实体存活时间
        /// </summary>
        public TimeSpan Lifetime => DateTime.Now - CreationTime;

        /// <summary>
        /// 实体是否已被销毁
        /// </summary>
        public bool IsDestroyed { get; private set; }

        /// <summary>
        /// 组件被附加到实体时调用
        /// </summary>
        public override void OnAttach()
        {
            CreationTime = DateTime.Now;
        }

        /// <summary>
        /// 组件从实体移除时调用
        /// </summary>
        public override void OnDetach()
        {
            // 清理事件订阅
            OnActivated = null;
            OnDeactivated = null;
            OnDestroyed = null;
        }

        /// <summary>
        /// 实体激活时调用
        /// </summary>
        public override void OnEnable()
        {
            ActivationTime = DateTime.Now;
            OnActivated?.Invoke();
        }

        /// <summary>
        /// 实体禁用时调用
        /// </summary>
        public override void OnDisable()
        {
            OnDeactivated?.Invoke();
        }

        /// <summary>
        /// 组件被销毁时调用
        /// </summary>
        public override void Dispose()
        {
            if (IsDestroyed)
                return;
            IsDestroyed = true;
            OnDestroyed?.Invoke();
        }

        /// <summary>
        /// 实体激活事件
        /// </summary>
        public event Action OnActivated;

        /// <summary>
        /// 实体停用事件
        /// </summary>
        public event Action OnDeactivated;

        /// <summary>
        /// 实体销毁事件
        /// </summary>
        public event Action OnDestroyed;
    }
}
