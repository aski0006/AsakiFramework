using System;
using System.Collections.Generic;

namespace Asaki.Core.Audio
{
    /// <summary>
    /// 音频播放状态机
    /// <para>使用有限状态机(FSM)管理音频播放器的所有状态转换。</para>
    /// <para>通过预定义的转换规则确保状态变更的合法性。</para>
    /// </summary>
    /// <author>Asaki Framework</author>
    /// <version>2.0</version>
    public sealed class AudioStateMachine
    {
        /// <summary>当前状态</summary>
        public AudioPlaybackState CurrentState { get; private set; } = AudioPlaybackState.Idle;

        /// <summary>前一个状态</summary>
        public AudioPlaybackState PreviousState { get; private set; } = AudioPlaybackState.Idle;

        /// <summary>状态改变事件</summary>
        public event Action<AudioPlaybackState, AudioPlaybackState> OnStateChanged;

        private readonly Dictionary<StateTransition, AudioPlaybackState> _transitions = new();
        private readonly HashSet<StateTrigger> _globalTriggers = new();

        public AudioStateMachine()
        {
            InitializeTransitions();
            InitializeGlobalTriggers();
        }

        private void InitializeTransitions()
        {
            // Idle -> Loading
            _transitions[new StateTransition(AudioPlaybackState.Idle, StateTrigger.Play)] =
                AudioPlaybackState.Loading;

            // Loading -> Ready/Error/Stopped
            _transitions[
                new StateTransition(AudioPlaybackState.Loading, StateTrigger.LoadComplete)
            ] = AudioPlaybackState.Ready;
            _transitions[new StateTransition(AudioPlaybackState.Loading, StateTrigger.LoadFailed)] =
                AudioPlaybackState.Error;
            _transitions[
                new StateTransition(AudioPlaybackState.Loading, StateTrigger.StopImmediate)
            ] = AudioPlaybackState.Stopped;

            // Ready -> Playing/Stopped
            _transitions[new StateTransition(AudioPlaybackState.Ready, StateTrigger.Play)] =
                AudioPlaybackState.Playing;
            _transitions[
                new StateTransition(AudioPlaybackState.Ready, StateTrigger.StopImmediate)
            ] = AudioPlaybackState.Stopped;

            // Playing -> Paused/FadingOut/Stopped/Error
            _transitions[new StateTransition(AudioPlaybackState.Playing, StateTrigger.Pause)] =
                AudioPlaybackState.Paused;
            _transitions[new StateTransition(AudioPlaybackState.Playing, StateTrigger.Stop)] =
                AudioPlaybackState.FadingOut;
            _transitions[
                new StateTransition(AudioPlaybackState.Playing, StateTrigger.StopImmediate)
            ] = AudioPlaybackState.Stopped;
            _transitions[
                new StateTransition(AudioPlaybackState.Playing, StateTrigger.PlaybackFinished)
            ] = AudioPlaybackState.Stopped;
            _transitions[new StateTransition(AudioPlaybackState.Playing, StateTrigger.Error)] =
                AudioPlaybackState.Error;

            // Paused -> Playing/FadingOut/Stopped
            _transitions[new StateTransition(AudioPlaybackState.Paused, StateTrigger.Resume)] =
                AudioPlaybackState.Playing;
            _transitions[new StateTransition(AudioPlaybackState.Paused, StateTrigger.Stop)] =
                AudioPlaybackState.FadingOut;
            _transitions[
                new StateTransition(AudioPlaybackState.Paused, StateTrigger.StopImmediate)
            ] = AudioPlaybackState.Stopped;

            // FadingOut -> Stopped
            _transitions[
                new StateTransition(AudioPlaybackState.FadingOut, StateTrigger.FadeComplete)
            ] = AudioPlaybackState.Stopped;
            _transitions[
                new StateTransition(AudioPlaybackState.FadingOut, StateTrigger.StopImmediate)
            ] = AudioPlaybackState.Stopped;

            // Stopped/Error -> Idle
            _transitions[new StateTransition(AudioPlaybackState.Stopped, StateTrigger.Reset)] =
                AudioPlaybackState.Idle;
            _transitions[new StateTransition(AudioPlaybackState.Error, StateTrigger.Reset)] =
                AudioPlaybackState.Idle;
        }

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
            if (!CanTransition(trigger, out var newState))
                return false;

            TransitionTo(newState);
            return true;
        }

        /// <summary>
        /// 检查是否可以进行状态转换
        /// </summary>
        /// <param name="trigger">触发器</param>
        /// <returns>是否可以转换</returns>
        public bool CanTransition(StateTrigger trigger)
        {
            return CanTransition(trigger, out _);
        }

        private bool CanTransition(StateTrigger trigger, out AudioPlaybackState targetState)
        {
            // 检查全局触发器
            if (_globalTriggers.Contains(trigger))
            {
                var globalTransition = new StateTransition(CurrentState, trigger);
                if (_transitions.TryGetValue(globalTransition, out targetState))
                    return true;
            }

            // 检查常规转换规则
            var transition = new StateTransition(CurrentState, trigger);
            if (_transitions.TryGetValue(transition, out targetState))
                return true;

            targetState = CurrentState;
            return false;
        }

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
    }
}
