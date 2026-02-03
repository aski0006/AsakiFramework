// File: Asaki/Core/Time/IAsakiTimerService.cs

using System;
using Asaki.Core.Context;
using Asaki.Core.Simulation;

namespace Asaki.Core.Time
{
    public interface IAsakiTimerService : IAsakiService, IAsakiTickable, IDisposable
    {
        /// <summary>
        /// 注册一个定时器
        /// </summary>
        /// <param name="duration">持续时间 (秒)</param>
        /// <param name="onComplete">完成回调</param>
        /// <param name="onUpdate">每帧回调 (可选，参数为剩余比例 0~1)</param>
        /// <param name="isLooped">是否循环</param>
        /// <param name="useUnscaledTime">是否忽略 TimeScale</param>
        /// <param name="tag">定时器标签 (用于分组管理)</param>
        AsakiTimerHandle Register(
            float duration,
            Action onComplete,
            Action<float> onUpdate = null,
            bool isLooped = false,
            bool useUnscaledTime = false,
            string tag = ""
        );

        /// <summary>
        /// 取消定时器
        /// </summary>
        void Cancel(AsakiTimerHandle handle);

        /// <summary>
        /// 暂停/恢复定时器
        /// </summary>
        void Pause(AsakiTimerHandle handle, bool isPaused);

        /// <summary>
        /// 根据标签取消所有定时器
        /// </summary>
        void CancelAllByTag(string tag);

        /// <summary>
        /// 暂停/恢复指定标签的所有定时器
        /// </summary>
        void PauseAllByTag(string tag, bool isPaused);

        /// <summary>
        /// 取消所有定时器
        /// </summary>
        void CancelAll();

        /// <summary>
        /// 暂停所有定时器
        /// </summary>
        void PauseAll();

        /// <summary>
        /// 恢复所有定时器
        /// </summary>
        void ResumeAll();

        /// <summary>
        /// 获取当前活跃的定时器数量
        /// </summary>
        int GetActiveTimerCount();

        /// <summary>
        /// 获取指定标签的定时器数量
        /// </summary>
        int GetTimerCountByTag(string tag);

#if UNITY_EDITOR
        /// <summary>
        /// [编辑器专用] 获取所有定时器信息用于调试
        /// </summary>
        System.Collections.Generic.List<AsakiTimerDebugInfo> GetAllTimerDebugInfos();

        /// <summary>
        /// [编辑器专用] 强制完成指定定时器
        /// </summary>
        void ForceComplete(AsakiTimerHandle handle);

        /// <summary>
        /// [编辑器专用] 设置全局时间缩放
        /// </summary>
        void SetGlobalTimeScale(float scale);

        /// <summary>
        /// [编辑器专用] 获取全局时间缩放
        /// </summary>
        float GetGlobalTimeScale();
#endif
    }
}
