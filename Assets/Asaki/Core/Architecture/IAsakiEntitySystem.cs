using Asaki.Core.Architecture.Entities;
using Asaki.Core.Simulation;

namespace Asaki.Core.Architecture.Entities
{
    /// <summary>
    /// ECS 系统接口 - 用于操作实体的系统
    /// </summary>
    public interface IAsakiEntitySystem : IAsakiSystem
    {
        /// <summary>
        /// 设置实体世界
        /// </summary>
        void SetEntityWorld(IEntityWorld world);
    }

    /// <summary>
    /// ECS 系统基类 - 提供实体世界的自动获取
    /// </summary>
    public abstract class AsakiEntitySystemBase : AsakiSystemBase, IAsakiEntitySystem
    {
        /// <summary>
        /// 实体世界引用
        /// </summary>
        protected IEntityWorld World { get; private set; }

        public void SetEntityWorld(IEntityWorld world)
        {
            World = world;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            // 自动从 ServiceProvider 获取 EntityWorld
            if (ServiceProvider is IAsakiArchitecture arch)
            {
                World = arch.GetEntityWorld();
            }
        }
    }

    /// <summary>
    /// 带 Tick 能力的 ECS 系统基类
    /// </summary>
    public abstract class AsakiEntityTickableSystemBase : AsakiEntitySystemBase, IAsakiTickable
    {
        private bool _isStarted;

        public override void Start()
        {
            base.Start();
            _isStarted = true;
        }

        public virtual void Tick(float deltaTime)
        {
            if (!_isStarted)
                return;
            OnEntityTick(deltaTime);
        }

        /// <summary>
        /// 每帧更新实体系统
        /// </summary>
        protected abstract void OnEntityTick(float deltaTime);
    }

    /// <summary>
    /// 带 FixedTick 能力的 ECS 系统基类
    /// </summary>
    public abstract class AsakiEntityFixedTickableSystemBase
        : AsakiEntitySystemBase,
            IAsakiFixedTickable
    {
        private bool _isStarted;

        public override void Start()
        {
            base.Start();
            _isStarted = true;
        }

        public virtual void FixedTick(float fixedDeltaTime)
        {
            if (!_isStarted)
                return;
            OnEntityFixedTick(fixedDeltaTime);
        }

        /// <summary>
        /// 物理帧更新实体系统
        /// </summary>
        protected abstract void OnEntityFixedTick(float fixedDeltaTime);
    }

    /// <summary>
    /// 带 LateTick 能力的 ECS 系统基类
    /// </summary>
    public abstract class AsakiEntityLateTickableSystemBase
        : AsakiEntitySystemBase,
            IAsakiLateTickable
    {
        private bool _isStarted;

        public override void Start()
        {
            base.Start();
            _isStarted = true;
        }

        public virtual void LateTick(float lateDeltaTime)
        {
            if (!_isStarted)
                return;
            OnEntityLateTick(lateDeltaTime);
        }

        /// <summary>
        /// 延迟帧更新实体系统
        /// </summary>
        protected abstract void OnEntityLateTick(float lateDeltaTime);
    }
}
