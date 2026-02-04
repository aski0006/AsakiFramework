using System;
using System.Threading;
using Asaki.Core.Context;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Serialization
{
    /// <summary>
    /// 自动保存服务事件参数
    /// </summary>
    public struct AsakiAutoSaveEventArgs
    {
        /// <summary>
        /// 触发的槽位信息
        /// </summary>
        public IAsakiSaveSlot Slot { get; set; }

        /// <summary>
        /// 触发原因
        /// </summary>
        public AsakiAutoSaveTrigger Trigger { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误信息（如果失败）
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 保存耗时（毫秒）
        /// </summary>
        public long ElapsedMilliseconds { get; set; }
    }

    /// <summary>
    /// 自动保存服务接口
    /// </summary>
    /// <remarks>
    /// 自动保存服务监控游戏状态，根据配置的触发条件自动执行保存操作。
    /// 提供倒计时、通知、事件等功能，简化自动保存的实现。
    /// </remarks>
    public interface IAsakiAutoSaveService : IAsakiModule
    {
        /// <summary>
        /// 当前配置
        /// </summary>
        IAsakiAutoSaveConfig Config { get; }

        /// <summary>
        /// 是否正在执行自动保存
        /// </summary>
        bool IsAutoSaving { get; }

        /// <summary>
        /// 距离下次自动保存的时间（秒，如果按时间触发）
        /// </summary>
        float TimeUntilNextAutoSave { get; }

        /// <summary>
        /// 上次自动保存的时间戳
        /// </summary>
        long LastAutoSaveTime { get; }

        /// <summary>
        /// 自动保存计数（当前会话）
        /// </summary>
        int AutoSaveCount { get; }

        /// <summary>
        /// 配置变更事件
        /// </summary>
        event Action<IAsakiAutoSaveConfig> OnConfigChanged;

        /// <summary>
        /// 自动保存开始事件
        /// </summary>
        event Action<AsakiAutoSaveEventArgs> OnAutoSaveBegin;

        /// <summary>
        /// 自动保存完成事件
        /// </summary>
        event Action<AsakiAutoSaveEventArgs> OnAutoSaveComplete;

        /// <summary>
        /// 自动保存倒计时开始事件
        /// </summary>
        event Action<float> OnCountdownBegin;

        /// <summary>
        /// 自动保存倒计时更新事件
        /// </summary>
        event Action<float> OnCountdownUpdate;

        /// <summary>
        /// 自动保存倒计时取消事件
        /// </summary>
        event Action OnCountdownCancelled;

        /// <summary>
        /// 更新配置
        /// </summary>
        /// <param name="config">新配置</param>
        void SetConfig(IAsakiAutoSaveConfig config);

        /// <summary>
        /// 注册存档数据提供者
        /// </summary>
        /// <typeparam name="TData">存档数据类型</typeparam>
        /// <param name="provider">数据提供者函数</param>
        void RegisterDataProvider<TData>(Func<TData> provider) where TData : IAsakiSavable;

        /// <summary>
        /// 启动自动保存服务
        /// </summary>
        void StartService();

        /// <summary>
        /// 停止自动保存服务
        /// </summary>
        void StopService();

        /// <summary>
        /// 暂停自动保存（临时）
        /// </summary>
        void Pause();

        /// <summary>
        /// 恢复自动保存
        /// </summary>
        void Resume();

        /// <summary>
        /// 立即执行自动保存（绕过倒计时）
        /// </summary>
        /// <param name="trigger">触发原因</param>
        /// <param name="token">取消令牌</param>
        /// <returns>是否成功</returns>
        UniTask<bool> ForceAutoSaveAsync(
            AsakiAutoSaveTrigger trigger = AsakiAutoSaveTrigger.Manual,
            CancellationToken token = default
        );

        /// <summary>
        /// 触发检查点保存
        /// </summary>
        /// <param name="checkpointName">检查点名称</param>
        /// <param name="token">取消令牌</param>
        /// <returns>是否成功</returns>
        UniTask<bool> TriggerCheckpointSaveAsync(
            string checkpointName = null,
            CancellationToken token = default
        );

        /// <summary>
        /// 触发场景切换保存
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="isEnter">是否进入场景</param>
        /// <param name="token">取消令牌</param>
        /// <returns>是否成功</returns>
        UniTask<bool> TriggerSceneSaveAsync(
            string sceneName,
            bool isEnter,
            CancellationToken token = default
        );

        /// <summary>
        /// 取消当前倒计时
        /// </summary>
        void CancelCountdown();

        /// <summary>
        /// 重置计时器
        /// </summary>
        void ResetTimer();

        /// <summary>
        /// 检查是否可以执行自动保存
        /// </summary>
        /// <returns>是否可以保存</returns>
        bool CanAutoSave();

        /// <summary>
        /// 获取下次自动保存的预估时间
        /// </summary>
        /// <returns>预估时间</returns>
        DateTime? GetNextAutoSaveTime();
    }
}
