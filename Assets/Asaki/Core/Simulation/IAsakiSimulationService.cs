using Asaki.Core.Context;

namespace Asaki.Core.Simulation
{
    /// <summary>
    /// 仿真时间管理服务接口
    /// 统一管理Update/FixedUpdate/LateUpdate的生命周期与优先级
    /// </summary>
    public interface IAsakiSimulationService : IAsakiService
    {
        // =========================================================
        // 状态控制
        // =========================================================

        /// <summary>
        /// 是否暂停所有更新
        /// </summary>
        bool IsPaused { get; set; }

        /// <summary>
        /// 时间缩放因子（1.0为正常速度）
        /// </summary>
        float TimeScale { get; set; }

        /// <summary>
        /// 暂停所有更新
        /// </summary>
        void Pause();

        /// <summary>
        /// 恢复所有更新
        /// </summary>
        void Resume();

        // =========================================================
        // 统计信息
        // =========================================================

        /// <summary>
        /// 已注册的Tick对象数量
        /// </summary>
        int TickableCount { get; }

        /// <summary>
        /// 已注册的FixedTick对象数量
        /// </summary>
        int FixedTickableCount { get; }

        /// <summary>
        /// 已注册的LateTick对象数量
        /// </summary>
        int LateTickableCount { get; }

        // =========================================================
        // 注册与注销
        // =========================================================

        /// <summary>
        /// 注册标准帧更新对象（对应Unity Update）
        /// </summary>
        /// <param name="tickable">可更新对象</param>
        /// <param name="priority">优先级，数值越小越先执行</param>
        void Register(IAsakiTickable tickable, int priority = (int)TickPriority.Normal);

        /// <summary>
        /// 注册物理帧更新对象（对应Unity FixedUpdate）
        /// </summary>
        /// <param name="tickable">可更新对象</param>
        void Register(IAsakiFixedTickable tickable);

        /// <summary>
        /// 注册延迟帧更新对象（对应Unity LateUpdate）
        /// </summary>
        /// <param name="tickable">可更新对象</param>
        /// <param name="priority">优先级，数值越小越先执行</param>
        void Register(IAsakiLateTickable tickable, int priority = (int)TickPriority.Normal);

        /// <summary>
        /// 注销标准帧更新对象
        /// </summary>
        void Unregister(IAsakiTickable tickable);

        /// <summary>
        /// 注销物理帧更新对象
        /// </summary>
        void Unregister(IAsakiFixedTickable tickable);

        /// <summary>
        /// 注销延迟帧更新对象
        /// </summary>
        void Unregister(IAsakiLateTickable tickable);

        // =========================================================
        // 驱动调用（由Unity层触发）
        // =========================================================

        /// <summary>
        /// 驱动标准帧更新（Unity Update回调）
        /// </summary>
        void Tick(float deltaTime);

        /// <summary>
        /// 驱动物理帧更新（Unity FixedUpdate回调）
        /// </summary>
        void FixedTick(float fixedDeltaTime);

        /// <summary>
        /// 驱动延迟帧更新（Unity LateUpdate回调）
        /// </summary>
        void LateTick(float lateDeltaTime);
    }
}
