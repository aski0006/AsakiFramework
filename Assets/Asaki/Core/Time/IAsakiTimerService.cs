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
        AsakiTimerHandle Register(
            float duration,
            Action onComplete,
            Action<float> onUpdate = null,
            bool isLooped = false,
            bool useUnscaledTime = false
        );

        /// <summary>
        /// 取消定时器
        /// </summary>
        void Cancel(AsakiTimerHandle handle);

        /// <summary>
        /// 暂停/恢复定时器
        /// </summary>
        void Pause(AsakiTimerHandle handle, bool isPaused);
    }
}
