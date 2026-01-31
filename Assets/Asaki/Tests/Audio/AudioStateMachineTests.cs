using Asaki.Core.Audio;
using NUnit.Framework;

namespace Asaki.Tests.Audio
{
    /// <summary>
    /// 音频状态机单元测试
    /// 验证 FSM 状态转换规则的正确性
    /// </summary>
    public class AudioStateMachineTests
    {
        private AudioStateMachine _fsm;

        [SetUp]
        public void Setup()
        {
            _fsm = new AudioStateMachine();
        }

        [TearDown]
        public void TearDown()
        {
            _fsm = null;
        }

        // ==========================================================
        // 初始状态测试
        // ==========================================================
        [Test]
        public void InitialState_ShouldBeIdle()
        {
            Assert.AreEqual(AudioPlaybackState.Idle, _fsm.CurrentState);
        }

        // ==========================================================
        // Idle 状态转换测试
        // ==========================================================
        [Test]
        public void Idle_Play_ShouldTransitionToLoading()
        {
            bool result = _fsm.TryTransition(StateTrigger.Play);

            Assert.IsTrue(result);
            Assert.AreEqual(AudioPlaybackState.Loading, _fsm.CurrentState);
        }

        [Test]
        public void Idle_Pause_ShouldNotTransition()
        {
            bool result = _fsm.TryTransition(StateTrigger.Pause);

            Assert.IsFalse(result);
            Assert.AreEqual(AudioPlaybackState.Idle, _fsm.CurrentState);
        }

        // ==========================================================
        // Loading 状态转换测试
        // ==========================================================
        [Test]
        public void Loading_LoadComplete_ShouldTransitionToReady()
        {
            _fsm.TryTransition(StateTrigger.Play); // -> Loading
            bool result = _fsm.TryTransition(StateTrigger.LoadComplete);

            Assert.IsTrue(result);
            Assert.AreEqual(AudioPlaybackState.Ready, _fsm.CurrentState);
        }

        [Test]
        public void Loading_LoadFailed_ShouldTransitionToError()
        {
            _fsm.TryTransition(StateTrigger.Play); // -> Loading
            bool result = _fsm.TryTransition(StateTrigger.LoadFailed);

            Assert.IsTrue(result);
            Assert.AreEqual(AudioPlaybackState.Error, _fsm.CurrentState);
        }

        [Test]
        public void Loading_StopImmediate_ShouldTransitionToStopped()
        {
            _fsm.TryTransition(StateTrigger.Play); // -> Loading
            bool result = _fsm.TryTransition(StateTrigger.StopImmediate);

            Assert.IsTrue(result);
            Assert.AreEqual(AudioPlaybackState.Stopped, _fsm.CurrentState);
        }

        [Test]
        public void Loading_Pause_ShouldNotTransition()
        {
            _fsm.TryTransition(StateTrigger.Play); // -> Loading
            bool result = _fsm.TryTransition(StateTrigger.Pause);

            Assert.IsFalse(result);
            Assert.AreEqual(AudioPlaybackState.Loading, _fsm.CurrentState);
        }

        // ==========================================================
        // Ready 状态转换测试
        // ==========================================================
        [Test]
        public void Ready_Play_ShouldTransitionToPlaying()
        {
            _fsm.TryTransition(StateTrigger.Play);        // -> Loading
            _fsm.TryTransition(StateTrigger.LoadComplete); // -> Ready
            bool result = _fsm.TryTransition(StateTrigger.Play);

            Assert.IsTrue(result);
            Assert.AreEqual(AudioPlaybackState.Playing, _fsm.CurrentState);
        }

        [Test]
        public void Ready_StopImmediate_ShouldTransitionToStopped()
        {
            _fsm.TryTransition(StateTrigger.Play);        // -> Loading
            _fsm.TryTransition(StateTrigger.LoadComplete); // -> Ready
            bool result = _fsm.TryTransition(StateTrigger.StopImmediate);

            Assert.IsTrue(result);
            Assert.AreEqual(AudioPlaybackState.Stopped, _fsm.CurrentState);
        }

        // ==========================================================
        // Playing 状态转换测试
        // ==========================================================
        [Test]
        public void Playing_Pause_ShouldTransitionToPaused()
        {
            GoToPlayingState();
            bool result = _fsm.TryTransition(StateTrigger.Pause);

            Assert.IsTrue(result);
            Assert.AreEqual(AudioPlaybackState.Paused, _fsm.CurrentState);
        }

        [Test]
        public void Playing_Stop_ShouldTransitionToFadingOut()
        {
            GoToPlayingState();
            bool result = _fsm.TryTransition(StateTrigger.Stop);

            Assert.IsTrue(result);
            Assert.AreEqual(AudioPlaybackState.FadingOut, _fsm.CurrentState);
        }

        [Test]
        public void Playing_StopImmediate_ShouldTransitionToStopped()
        {
            GoToPlayingState();
            bool result = _fsm.TryTransition(StateTrigger.StopImmediate);

            Assert.IsTrue(result);
            Assert.AreEqual(AudioPlaybackState.Stopped, _fsm.CurrentState);
        }

        [Test]
        public void Playing_PlaybackFinished_ShouldTransitionToStopped()
        {
            GoToPlayingState();
            bool result = _fsm.TryTransition(StateTrigger.PlaybackFinished);

            Assert.IsTrue(result);
            Assert.AreEqual(AudioPlaybackState.Stopped, _fsm.CurrentState);
        }

        [Test]
        public void Playing_Error_ShouldTransitionToError()
        {
            GoToPlayingState();
            bool result = _fsm.TryTransition(StateTrigger.Error);

            Assert.IsTrue(result);
            Assert.AreEqual(AudioPlaybackState.Error, _fsm.CurrentState);
        }

        [Test]
        public void Playing_Resume_ShouldNotTransition()
        {
            GoToPlayingState();
            bool result = _fsm.TryTransition(StateTrigger.Resume);

            Assert.IsFalse(result);
            Assert.AreEqual(AudioPlaybackState.Playing, _fsm.CurrentState);
        }

        // ==========================================================
        // Paused 状态转换测试
        // ==========================================================
        [Test]
        public void Paused_Resume_ShouldTransitionToPlaying()
        {
            GoToPlayingState();
            _fsm.TryTransition(StateTrigger.Pause); // -> Paused
            bool result = _fsm.TryTransition(StateTrigger.Resume);

            Assert.IsTrue(result);
            Assert.AreEqual(AudioPlaybackState.Playing, _fsm.CurrentState);
        }

        [Test]
        public void Paused_Stop_ShouldTransitionToFadingOut()
        {
            GoToPlayingState();
            _fsm.TryTransition(StateTrigger.Pause); // -> Paused
            bool result = _fsm.TryTransition(StateTrigger.Stop);

            Assert.IsTrue(result);
            Assert.AreEqual(AudioPlaybackState.FadingOut, _fsm.CurrentState);
        }

        [Test]
        public void Paused_StopImmediate_ShouldTransitionToStopped()
        {
            GoToPlayingState();
            _fsm.TryTransition(StateTrigger.Pause); // -> Paused
            bool result = _fsm.TryTransition(StateTrigger.StopImmediate);

            Assert.IsTrue(result);
            Assert.AreEqual(AudioPlaybackState.Stopped, _fsm.CurrentState);
        }

        [Test]
        public void Paused_Pause_ShouldNotTransition()
        {
            GoToPlayingState();
            _fsm.TryTransition(StateTrigger.Pause); // -> Paused
            bool result = _fsm.TryTransition(StateTrigger.Pause);

            Assert.IsFalse(result);
            Assert.AreEqual(AudioPlaybackState.Paused, _fsm.CurrentState);
        }

        // ==========================================================
        // FadingOut 状态转换测试
        // ==========================================================
        [Test]
        public void FadingOut_FadeComplete_ShouldTransitionToStopped()
        {
            GoToPlayingState();
            _fsm.TryTransition(StateTrigger.Stop); // -> FadingOut
            bool result = _fsm.TryTransition(StateTrigger.FadeComplete);

            Assert.IsTrue(result);
            Assert.AreEqual(AudioPlaybackState.Stopped, _fsm.CurrentState);
        }

        [Test]
        public void FadingOut_StopImmediate_ShouldTransitionToStopped()
        {
            GoToPlayingState();
            _fsm.TryTransition(StateTrigger.Stop); // -> FadingOut
            bool result = _fsm.TryTransition(StateTrigger.StopImmediate);

            Assert.IsTrue(result);
            Assert.AreEqual(AudioPlaybackState.Stopped, _fsm.CurrentState);
        }

        // ==========================================================
        // Stopped 状态转换测试
        // ==========================================================
        [Test]
        public void Stopped_Reset_ShouldTransitionToIdle()
        {
            GoToPlayingState();
            _fsm.TryTransition(StateTrigger.StopImmediate); // -> Stopped
            bool result = _fsm.TryTransition(StateTrigger.Reset);

            Assert.IsTrue(result);
            Assert.AreEqual(AudioPlaybackState.Idle, _fsm.CurrentState);
        }

        [Test]
        public void Stopped_Play_ShouldNotTransition()
        {
            GoToPlayingState();
            _fsm.TryTransition(StateTrigger.StopImmediate); // -> Stopped
            bool result = _fsm.TryTransition(StateTrigger.Play);

            Assert.IsFalse(result);
            Assert.AreEqual(AudioPlaybackState.Stopped, _fsm.CurrentState);
        }

        // ==========================================================
        // Error 状态转换测试
        // ==========================================================
        [Test]
        public void Error_Reset_ShouldTransitionToIdle()
        {
            GoToPlayingState();
            _fsm.TryTransition(StateTrigger.Error); // -> Error
            bool result = _fsm.TryTransition(StateTrigger.Reset);

            Assert.IsTrue(result);
            Assert.AreEqual(AudioPlaybackState.Idle, _fsm.CurrentState);
        }

        // ==========================================================
        // 状态机重置测试
        // ==========================================================
        [Test]
        public void Reset_ShouldReturnToIdle()
        {
            GoToPlayingState();
            _fsm.Reset();

            Assert.AreEqual(AudioPlaybackState.Idle, _fsm.CurrentState);
        }

        [Test]
        public void Reset_ShouldTrackPreviousState()
        {
            GoToPlayingState();
            _fsm.Reset();

            Assert.AreEqual(AudioPlaybackState.Playing, _fsm.PreviousState);
        }

        // ==========================================================
        // 状态改变事件测试
        // ==========================================================
        [Test]
        public void StateChange_ShouldTriggerEvent()
        {
            AudioPlaybackState? fromState = null;
            AudioPlaybackState? toState = null;

            _fsm.OnStateChanged += (from, to) =>
            {
                fromState = from;
                toState = to;
            };

            _fsm.TryTransition(StateTrigger.Play);

            Assert.IsTrue(fromState.HasValue);
            Assert.IsTrue(toState.HasValue);
            Assert.AreEqual(AudioPlaybackState.Idle, fromState.Value);
            Assert.AreEqual(AudioPlaybackState.Loading, toState.Value);
        }

        // ==========================================================
        // CanTransition 测试
        // ==========================================================
        [Test]
        public void CanTransition_ValidTransition_ShouldReturnTrue()
        {
            bool canTransition = _fsm.CanTransition(StateTrigger.Play);

            Assert.IsTrue(canTransition);
        }

        [Test]
        public void CanTransition_InvalidTransition_ShouldReturnFalse()
        {
            bool canTransition = _fsm.CanTransition(StateTrigger.Pause);

            Assert.IsFalse(canTransition);
        }

        [Test]
        public void CanTransition_ShouldOutputTargetState()
        {
            bool canTransition = _fsm.CanTransition(StateTrigger.Play, out AudioPlaybackState targetState);

            Assert.IsTrue(canTransition);
            Assert.AreEqual(AudioPlaybackState.Loading, targetState);
        }

        // ==========================================================
        // 强制转换测试
        // ==========================================================
        [Test]
        public void ForceTransition_ShouldIgnoreRules()
        {
            _fsm.ForceTransition(AudioPlaybackState.Playing);

            Assert.AreEqual(AudioPlaybackState.Playing, _fsm.CurrentState);
        }

        // ==========================================================
        // 完整播放流程测试
        // ==========================================================
        [Test]
        public void FullPlaybackFlow_NormalPlayback_ShouldCompleteSuccessfully()
        {
            // 正常播放流程: Idle -> Loading -> Ready -> Playing -> Stopped -> Idle
            Assert.AreEqual(AudioPlaybackState.Idle, _fsm.CurrentState);

            Assert.IsTrue(_fsm.TryTransition(StateTrigger.Play));
            Assert.AreEqual(AudioPlaybackState.Loading, _fsm.CurrentState);

            Assert.IsTrue(_fsm.TryTransition(StateTrigger.LoadComplete));
            Assert.AreEqual(AudioPlaybackState.Ready, _fsm.CurrentState);

            Assert.IsTrue(_fsm.TryTransition(StateTrigger.Play));
            Assert.AreEqual(AudioPlaybackState.Playing, _fsm.CurrentState);

            Assert.IsTrue(_fsm.TryTransition(StateTrigger.PlaybackFinished));
            Assert.AreEqual(AudioPlaybackState.Stopped, _fsm.CurrentState);

            Assert.IsTrue(_fsm.TryTransition(StateTrigger.Reset));
            Assert.AreEqual(AudioPlaybackState.Idle, _fsm.CurrentState);
        }

        [Test]
        public void FullPlaybackFlow_PauseAndResume_ShouldCompleteSuccessfully()
        {
            // 暂停恢复流程: Idle -> Loading -> Ready -> Playing -> Paused -> Playing -> Stopped
            GoToPlayingState();

            Assert.IsTrue(_fsm.TryTransition(StateTrigger.Pause));
            Assert.AreEqual(AudioPlaybackState.Paused, _fsm.CurrentState);

            Assert.IsTrue(_fsm.TryTransition(StateTrigger.Resume));
            Assert.AreEqual(AudioPlaybackState.Playing, _fsm.CurrentState);

            Assert.IsTrue(_fsm.TryTransition(StateTrigger.Stop));
            Assert.AreEqual(AudioPlaybackState.FadingOut, _fsm.CurrentState);

            Assert.IsTrue(_fsm.TryTransition(StateTrigger.FadeComplete));
            Assert.AreEqual(AudioPlaybackState.Stopped, _fsm.CurrentState);
        }

        [Test]
        public void FullPlaybackFlow_ImmediateStop_ShouldCompleteSuccessfully()
        {
            // 立即停止流程
            GoToPlayingState();

            Assert.IsTrue(_fsm.TryTransition(StateTrigger.StopImmediate));
            Assert.AreEqual(AudioPlaybackState.Stopped, _fsm.CurrentState);
        }

        [Test]
        public void FullPlaybackFlow_ErrorHandling_ShouldCompleteSuccessfully()
        {
            // 错误处理流程: Idle -> Loading -> Error -> Idle
            _fsm.TryTransition(StateTrigger.Play);

            Assert.IsTrue(_fsm.TryTransition(StateTrigger.LoadFailed));
            Assert.AreEqual(AudioPlaybackState.Error, _fsm.CurrentState);

            Assert.IsTrue(_fsm.TryTransition(StateTrigger.Reset));
            Assert.AreEqual(AudioPlaybackState.Idle, _fsm.CurrentState);
        }

        // ==========================================================
        // 辅助方法
        // ==========================================================
        private void GoToPlayingState()
        {
            _fsm.TryTransition(StateTrigger.Play);         // -> Loading
            _fsm.TryTransition(StateTrigger.LoadComplete);  // -> Ready
            _fsm.TryTransition(StateTrigger.Play);         // -> Playing
        }
    }
}
