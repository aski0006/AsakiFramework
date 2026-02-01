using System;

namespace Asaki.Core.Audio
{
    /// <summary>
    /// 音频状态转换触发器枚举
    /// 定义触发状态转换的所有可能事件
    /// </summary>
    public enum StateTrigger
    {
        /// <summary>开始播放</summary>
        Play,

        /// <summary>资源加载完成</summary>
        LoadComplete,

        /// <summary>资源加载失败</summary>
        LoadFailed,

        /// <summary>暂停播放</summary>
        Pause,

        /// <summary>恢复播放</summary>
        Resume,

        /// <summary>停止播放（带淡出）</summary>
        Stop,

        /// <summary>立即停止（无淡出）</summary>
        StopImmediate,

        /// <summary>播放完成（非循环音频）</summary>
        PlaybackFinished,

        /// <summary>淡出完成</summary>
        FadeComplete,

        /// <summary>发生错误</summary>
        Error,

        /// <summary>重置/清理</summary>
        Reset,
    }

    /// <summary>
    /// 状态转换规则定义
    /// </summary>
    public readonly struct StateTransition : IEquatable<StateTransition>
    {
        public readonly AudioPlaybackState CurrentState;
        public readonly StateTrigger Trigger;

        public StateTransition(AudioPlaybackState currentState, StateTrigger trigger)
        {
            CurrentState = currentState;
            Trigger = trigger;
        }

        public bool Equals(StateTransition other)
        {
            return CurrentState == other.CurrentState && Trigger == other.Trigger;
        }

        public override bool Equals(object obj)
        {
            return obj is StateTransition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)CurrentState, (int)Trigger);
        }

        public static bool operator ==(StateTransition left, StateTransition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StateTransition left, StateTransition right)
        {
            return !left.Equals(right);
        }
    }
}
