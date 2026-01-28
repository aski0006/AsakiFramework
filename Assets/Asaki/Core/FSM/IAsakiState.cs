namespace Asaki.Core.FSM
{
    /// <summary>
    /// 定义状态机状态的核心接口，提供状态生命周期方法。
    /// </summary>
    /// <remarks>
    /// 实现此接口的类可以作为状态机的状态使用，状态机将在适当的时机调用这些方法。
    /// 通常使用 <see cref="AsakiState{TContext}"/> 作为基类来简化实现。
    /// </remarks>
    public interface IAsakiState
    {
        /// <summary>
        /// 当状态机进入此状态时调用。
        /// </summary>
        /// <remarks>
        /// 在此方法中初始化状态所需的资源和状态数据。
        /// 此方法在状态转换开始时立即调用，在当前状态的 OnExit 方法之后。
        /// </remarks>
        void OnEnter();

        /// <summary>
        /// 每帧更新时调用，与 Unity 的 Update 方法同步。
        /// </summary>
        /// <param name="deltaTime">自上一帧以来经过的时间（秒）。</param>
        /// <remarks>
        /// 用于处理基于时间的状态逻辑，如动画、移动等。
        /// 避免在此方法中执行过于复杂的计算，以免影响性能。
        /// </remarks>
        void OnUpdate(float deltaTime);

        /// <summary>
        /// 以固定时间间隔调用，与 Unity 的 FixedUpdate 方法同步。
        /// </summary>
        /// <param name="fixedDeltaTime">固定更新时间间隔（秒）。</param>
        /// <remarks>
        /// 用于处理物理相关的状态逻辑，如碰撞检测、力的应用等。
        /// 此方法的调用频率由 Unity 的物理时间步长设置决定。
        /// </remarks>
        void OnFixedUpdate(float fixedDeltaTime);

        /// <summary>
        /// 当状态机退出此状态时调用。
        /// </summary>
        /// <remarks>
        /// 在此方法中清理状态使用的资源和状态数据。
        /// 此方法在状态转换开始时立即调用，在新状态的 OnEnter 方法之前。
        /// </remarks>
        void OnExit();
    }
}
