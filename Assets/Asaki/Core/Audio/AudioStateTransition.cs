using System;

namespace Asaki.Core.Audio
{
    /// <summary>
    /// 状态转换触发器枚举
    /// <para>定义触发状态转换的所有可能事件。</para>
    /// </summary>
    /// <author>Asaki Framework</author>
    /// <version>2.0</version>
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

        /// <summary>停止播放(带淡出)</summary>
        Stop,

        /// <summary>立即停止(无淡出)</summary>
        StopImmediate,

        /// <summary>播放完成(非循环音频)</summary>
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
    /// <para>用于作为字典键，定义从当前状态通过触发器转换到目标状态的规则。</para>
    /// </summary>
    public readonly struct StateTransition : IEquatable<StateTransition>
    {
        /// <summary>当前状态</summary>
        public readonly AudioPlaybackState CurrentState;

        /// <summary>触发器</summary>
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
