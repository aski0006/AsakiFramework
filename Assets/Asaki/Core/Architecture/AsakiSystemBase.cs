using System;
using Asaki.Core.Simulation;

namespace Asaki.Core.Architecture
{
    /// <summary>
    /// AsakiSystem 可选基类 - 提供完整的生命周期管理
    /// </summary>
    public abstract class AsakiSystemBase : IAsakiSystem
    {
        protected IAsakiServiceProvider ServiceProvider { get; private set; }

        public virtual void Create(IAsakiServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            OnCreate();
        }

        /// <summary>
        /// 系统创建时调用
        /// </summary>
        protected virtual void OnCreate() { }

        public void Create() { }
        public virtual void Start()
        {
            OnStart();
        }

        /// <summary>
        /// 所有系统创建完成后调用
        /// </summary>
        protected virtual void OnStart() { }

        public abstract void Dispose();

        /// <summary>
        /// 获取 System
        /// </summary>
        protected T GetSystem<T>() where T : class, IAsakiSystem
        {
            if (ServiceProvider is IAsakiArchitecture arch)
                return arch.GetSystem<T>();
            throw new InvalidOperationException("ServiceProvider is not IAsakiArchitecture");
        }

        /// <summary>
        /// 获取 Model
        /// </summary>
        protected T GetModel<T>() where T : class, IAsakiModel
        {
            if (ServiceProvider is IAsakiArchitecture arch)
                return arch.GetModel<T>();
            throw new InvalidOperationException("ServiceProvider is not IAsakiArchitecture");
        }
    }

    /// <summary>
    /// 带 Tick 能力的系统基类
    /// </summary>
    public abstract class AsakiTickableSystemBase : AsakiSystemBase, IAsakiTickable
    {
        private bool _isStarted;

        public override void Start()
        {
            base.Start();
            _isStarted = true;
        }

        public virtual void Tick(float deltaTime)
        {
            if (!_isStarted) return;
            OnTick(deltaTime);
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        protected abstract void OnTick(float deltaTime);
    }

    /// <summary>
    /// 带 FixedTick 能力的系统基类
    /// </summary>
    public abstract class AsakiFixedTickableSystemBase : AsakiSystemBase, IAsakiFixedTickable
    {
        private bool _isStarted;

        public override void Start()
        {
            base.Start();
            _isStarted = true;
        }

        public virtual void FixedTick(float fixedDeltaTime)
        {
            if (!_isStarted) return;
            OnFixedTick(fixedDeltaTime);
        }

        /// <summary>
        /// 物理帧更新
        /// </summary>
        protected abstract void OnFixedTick(float fixedDeltaTime);
    }

    /// <summary>
    /// 带 LateTick 能力的系统基类
    /// </summary>
    public abstract class AsakiLateTickableSystemBase : AsakiSystemBase, IAsakiLateTickable
    {
        private bool _isStarted;

        public override void Start()
        {
            base.Start();
            _isStarted = true;
        }

        public virtual void LateTick(float lateDeltaTime)
        {
            if (!_isStarted) return;
            OnLateTick(lateDeltaTime);
        }

        /// <summary>
        /// 延迟帧更新
        /// </summary>
        protected abstract void OnLateTick(float lateDeltaTime);
    }
}
