using System;
using System.Collections.Generic;

namespace Asaki.Core.Audio
{
    /// <summary>
    /// 音频播放状态机
    /// 使用有限状态机(FSM)管理音频播放器的所有状态转换
    /// </summary>
    public class AudioStateMachine
    {
        /// <summary>当前状态</summary>
        public AudioPlaybackState CurrentState { get; private set; } = AudioPlaybackState.Idle;

        /// <summary>前一个状态</summary>
        public AudioPlaybackState PreviousState { get; private set; } = AudioPlaybackState.Idle;

        /// <summary>状态改变事件</summary>
        public event Action<AudioPlaybackState, AudioPlaybackState> OnStateChanged;

        /// <summary>状态转换规则表</summary>
        private readonly Dictionary<StateTransition, AudioPlaybackState> _transitions = new();

        /// <summary>任意状态可转换到的目标状态</summary>
        private readonly HashSet<StateTrigger> _globalTriggers = new();

        public AudioStateMachine()
        {
            InitializeTransitions();
            InitializeGlobalTriggers();
        }

        /// <summary>
        /// 初始化所有状态转换规则
        /// </summary>
        private void InitializeTransitions()
        {
            // Idle 状态转换
            _transitions[new StateTransition(AudioPlaybackState.Idle, StateTrigger.Play)] = AudioPlaybackState.Loading;

            // Loading 状态转换
            _transitions[new StateTransition(AudioPlaybackState.Loading, StateTrigger.LoadComplete)] = AudioPlaybackState.Ready;
            _transitions[new StateTransition(AudioPlaybackState.Loading, StateTrigger.LoadFailed)] = AudioPlaybackState.Error;
            _transitions[new StateTransition(AudioPlaybackState.Loading, StateTrigger.StopImmediate)] = AudioPlaybackState.Stopped;

            // Ready 状态转换
            _transitions[new StateTransition(AudioPlaybackState.Ready, StateTrigger.Play)] = AudioPlaybackState.Playing;
            _transitions[new StateTransition(AudioPlaybackState.Ready, StateTrigger.StopImmediate)] = AudioPlaybackState.Stopped;

            // Playing 状态转换
            _transitions[new StateTransition(AudioPlaybackState.Playing, StateTrigger.Pause)] = AudioPlaybackState.Paused;
            _transitions[new StateTransition(AudioPlaybackState.Playing, StateTrigger.Stop)] = AudioPlaybackState.FadingOut;
            _transitions[new StateTransition(AudioPlaybackState.Playing, StateTrigger.StopImmediate)] = AudioPlaybackState.Stopped;
            _transitions[new StateTransition(AudioPlaybackState.Playing, StateTrigger.PlaybackFinished)] = AudioPlaybackState.Stopped;
            _transitions[new StateTransition(AudioPlaybackState.Playing, StateTrigger.Error)] = AudioPlaybackState.Error;

            // Paused 状态转换
            _transitions[new StateTransition(AudioPlaybackState.Paused, StateTrigger.Resume)] = AudioPlaybackState.Playing;
            _transitions[new StateTransition(AudioPlaybackState.Paused, StateTrigger.Stop)] = AudioPlaybackState.FadingOut;
            _transitions[new StateTransition(AudioPlaybackState.Paused, StateTrigger.StopImmediate)] = AudioPlaybackState.Stopped;

            // FadingOut 状态转换
            _transitions[new StateTransition(AudioPlaybackState.FadingOut, StateTrigger.FadeComplete)] = AudioPlaybackState.Stopped;
            _transitions[new StateTransition(AudioPlaybackState.FadingOut, StateTrigger.StopImmediate)] = AudioPlaybackState.Stopped;

            // Stopped 状态转换
            _transitions[new StateTransition(AudioPlaybackState.Stopped, StateTrigger.Reset)] = AudioPlaybackState.Idle;

            // Error 状态转换
            _transitions[new StateTransition(AudioPlaybackState.Error, StateTrigger.Reset)] = AudioPlaybackState.Idle;
        }

        /// <summary>
        /// 初始化全局触发器（可从任意状态触发）
        /// </summary>
        private void InitializeGlobalTriggers()
        {
            _globalTriggers.Add(StateTrigger.Error);
            _globalTriggers.Add(StateTrigger.Reset);
        }

        /// <summary>
        /// 尝试触发状态转换
        /// </summary>
        /// <param name="trigger">触发器</param>
        /// <returns>是否成功转换</returns>
        public bool TryTransition(StateTrigger trigger)
        {
            if (CanTransition(trigger, out AudioPlaybackState newState))
            {
                TransitionTo(newState);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 强制触发状态转换（无视规则，谨慎使用）
        /// </summary>
        /// <param name="newState">目标状态</param>
        public void ForceTransition(AudioPlaybackState newState)
        {
            TransitionTo(newState);
        }

        /// <summary>
        /// 检查是否可以进行状态转换
        /// </summary>
        /// <param name="trigger">触发器</param>
        /// <param name="targetState">输出目标状态</param>
        /// <returns>是否可以转换</returns>
        public bool CanTransition(StateTrigger trigger, out AudioPlaybackState targetState)
        {
            // 检查全局触发器
            if (_globalTriggers.Contains(trigger))
            {
                var globalTransition = new StateTransition(CurrentState, trigger);
                if (_transitions.TryGetValue(globalTransition, out targetState))
                {
                    return true;
                }
            }

            // 检查常规转换规则
            var transition = new StateTransition(CurrentState, trigger);
            if (_transitions.TryGetValue(transition, out targetState))
            {
                return true;
            }

            targetState = CurrentState;
            return false;
        }

        /// <summary>
        /// 检查是否可以进行状态转换（简化版）
        /// </summary>
        /// <param name="trigger">触发器</param>
        /// <returns>是否可以转换</returns>
        public bool CanTransition(StateTrigger trigger)
        {
            return CanTransition(trigger, out _);
        }

        /// <summary>
        /// 执行状态转换
        /// </summary>
        /// <param name="newState">新状态</param>
        private void TransitionTo(AudioPlaybackState newState)
        {
            if (CurrentState == newState)
                return;

            PreviousState = CurrentState;
            CurrentState = newState;
            OnStateChanged?.Invoke(PreviousState, CurrentState);
        }

        /// <summary>
        /// 重置状态机到初始状态
        /// </summary>
        public void Reset()
        {
            PreviousState = CurrentState;
            CurrentState = AudioPlaybackState.Idle;
            OnStateChanged?.Invoke(PreviousState, CurrentState);
        }

        /// <summary>
        /// 获取当前状态的字符串表示
        /// </summary>
        public override string ToString()
        {
            return $"{CurrentState} (from {PreviousState})";
        }
    }
}
