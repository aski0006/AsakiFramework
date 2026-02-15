namespace Asaki.Core.FSM
{
    /// <summary>
    /// Asaki状态机的标准状态基类。
    /// </summary>
    /// <typeparam name="TContext">状态持有者类型，通常是控制器类（如PlayerController、GameManager等）。</typeparam>
    /// <remarks>
    /// 此抽象类提供了状态机状态的基础实现，定义了状态的生命周期方法和必要的上下文访问。
    /// 所有具体状态类都应继承此类，并实现相应的生命周期方法。
    /// 支持Unity的Update和FixedUpdate生命周期集成，确保与Unity引擎的良好兼容。
    /// </remarks>
    public abstract class AsakiState<TContext> : IAsakiState
    {
        /// <summary>
        /// 获取状态持有者的上下文实例。
        /// </summary>
        /// <remarks>
        /// 上下文实例包含了状态操作所需的所有数据和方法引用。
        /// 例如，对于PlayerController上下文，可以访问玩家的移动、跳跃等方法。
        /// 此属性在Initialize方法中被设置，之后在状态的整个生命周期内保持不变。
        /// </remarks>
        protected TContext Context { get; private set; }

        /// <summary>
        /// 获取所属的状态机实例。
        /// </summary>
        /// <remarks>
        /// 状态可以通过此引用在内部切换到其他状态。
        /// 例如：Machine.ChangeState &lt; IdleState &gt; ();
        /// 此属性在Initialize方法中被设置，之后在状态的整个生命周期内保持不变。
        /// </remarks>
        protected AsakiStateMachine<TContext> Machine { get; private set; }

        /// <summary>
        /// 初始化状态实例。
        /// </summary>
        /// <param name="machine">所属的状态机实例。</param>
        /// <param name="context">状态持有者的上下文实例。</param>
        /// <remarks>
        /// 此方法仅在状态首次创建时被调用一次，由状态机负责调用。
        /// 子类可以重写此方法来执行额外的初始化操作，但必须调用base.Initialize(machine, context)。
        /// 初始化完成后，状态实例将被缓存以提高性能。
        /// </remarks>
        public virtual void Initialize(AsakiStateMachine<TContext> machine, TContext context)
        {
            Machine = machine;
            Context = context;
        }

        /// <summary>
        /// 当状态被激活时调用的方法。
        /// </summary>
        /// <remarks>
        /// 此方法在状态切换过程中被调用，用于执行状态进入时的初始化操作。
        /// 例如：重置动画、播放音效、初始化计时器等。
        /// 子类应重写此方法来实现特定状态的进入逻辑。
        /// </remarks>
        public virtual void OnEnter() { }

        /// <summary>
        /// 当状态处于激活状态时，每帧调用的更新方法。
        /// </summary>
        /// <param name="deltaTime">自上一帧以来经过的时间（秒）。</param>
        /// <remarks>
        /// 此方法通常在MonoBehaviour.Update中被调用，用于处理与时间相关的更新逻辑。
        /// 例如：输入处理、状态转换条件检查、动画更新等。
        /// 子类应重写此方法来实现特定状态的每帧更新逻辑。
        /// </remarks>
        public virtual void OnUpdate(float deltaTime)
        {
            CheckTransition(deltaTime);
        }

        /// <summary>
        /// 当状态处于激活状态时，每固定时间步调用的物理更新方法。
        /// </summary>
        /// <param name="fixedDeltaTime">固定时间步长（秒），通常为0.02秒。</param>
        /// <remarks>
        /// 此方法通常在MonoBehaviour.FixedUpdate中被调用，用于处理物理相关的更新逻辑。
        /// 例如：刚体移动、碰撞检测、物理力应用等。
        /// 子类应重写此方法来实现特定状态的物理更新逻辑。
        /// </remarks>
        public virtual void OnFixedUpdate(float fixedDeltaTime) { }

        /// <summary>
        /// 当状态被停用并切换到其他状态时调用的方法。
        /// </summary>
        /// <remarks>
        /// 此方法在状态切换过程中被调用，用于执行状态退出时的清理操作。
        /// 例如：停止动画、保存状态、释放临时资源等。
        /// 子类应重写此方法来实现特定状态的退出逻辑。
        /// </remarks>
        public virtual void OnExit() { }

        /// <summary>
        /// 检查是否应该转换到其他状态。
        /// </summary>
        /// <remarks>
        /// 此方法在每帧更新时被调用，用于检查是否满足转换条件。
        /// 如果满足条件，应调用Machine.ChangeState &lt; TNextState &gt; ()来切换到下一个状态。
        /// 子类应重写此方法来实现特定状态的转换逻辑。
        /// </remarks>
        protected virtual void CheckTransition(float deltaTime) { }
    }
}
