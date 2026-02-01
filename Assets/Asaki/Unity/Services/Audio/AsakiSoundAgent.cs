using System;
using System.Threading;
using Asaki.Core.Audio;
using Asaki.Core.Logging;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.Resources;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Unity.Services.Audio
{
    /// <summary>
    /// Sound playback agent with FSM state management
    /// 使用有限状态机管理音频播放状态的音频代理
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AsakiSoundAgent : MonoBehaviour, IAsakiPoolable
    {
        private AudioSource _source;
        private Transform _transform;
        private ResHandle<AudioClip> _clipHandle;
        private CancellationTokenSource _playCts;

        /// <summary>状态机实例</summary>
        public AudioStateMachine StateMachine { get; private set; }

        /// <summary>当前播放状态</summary>
        public AudioPlaybackState State => StateMachine?.CurrentState ?? AudioPlaybackState.Idle;

        /// <summary>前一个状态</summary>
        public AudioPlaybackState PreviousState =>
            StateMachine?.PreviousState ?? AudioPlaybackState.Idle;

        /// <summary>是否正在播放（包含Loading和Ready状态）</summary>
        public bool IsActive =>
            State
                is AudioPlaybackState.Loading
                    or AudioPlaybackState.Ready
                    or AudioPlaybackState.Playing
                    or AudioPlaybackState.Paused
                    or AudioPlaybackState.FadingOut;

        /// <summary>是否真正在播放音频</summary>
        public bool IsPlaying => State == AudioPlaybackState.Playing;

        /// <summary>是否已暂停</summary>
        public bool IsPaused => State == AudioPlaybackState.Paused;

        /// <summary>是否处于错误状态</summary>
        public bool IsError => State == AudioPlaybackState.Error;

        /// <summary>当前播放的音频路径</summary>
        public string CurrentAudioPath { get; private set; }

        /// <summary>状态改变事件</summary>
        public event Action<AudioPlaybackState, AudioPlaybackState> OnStateChanged;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _transform = transform;
            _source.playOnAwake = false;
            StateMachine = new AudioStateMachine();
            StateMachine.OnStateChanged += (prev, curr) =>
            {
                ALog.Info($"[AsakiSoundAgent] State changed: {prev} -> {curr}");
                OnStateChanged?.Invoke(prev, curr);
            };
        }

        // ==========================================================
        // IAsakiPoolable Lifecycle
        // ==========================================================
        public void OnSpawn()
        {
            _playCts = new CancellationTokenSource();
            CurrentAudioPath = null;
            ALog.Info($"[AsakiSoundAgent] Agent spawned: {GetInstanceID()}");
        }

        public void OnDespawn()
        {
            // 立即停止播放
            StopImmediate();

            // 清理资源
            Cleanup();

            // 重置状态机
            StateMachine.Reset();

            ALog.Info($"[AsakiSoundAgent] Agent despawned: {GetInstanceID()}");
        }

        // ==========================================================
        // Core Playback with FSM
        // ==========================================================
        public async UniTask PlayAsync(
            string resourcePath,
            AsakiAudioParams p,
            IAsakiResourceService resourceService,
            CancellationToken serviceToken
        )
        {
            // 验证状态转换
            if (!StateMachine.TryTransition(StateTrigger.Play))
            {
                ALog.Warn($"[AsakiSoundAgent] Cannot start playback from state: {State}");
                return;
            }

            CurrentAudioPath = resourcePath;

            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                serviceToken,
                _playCts.Token,
                this.GetCancellationTokenOnDestroy()
            );

            try
            {
                ALog.Info($"[AsakiSoundAgent] Loading audio from: {resourcePath}");

                // 异步加载音频资源
                _clipHandle = await resourceService.LoadAsync<AudioClip>(
                    resourcePath,
                    linkedCts.Token
                );

                if (linkedCts.IsCancellationRequested)
                    return;

                if (_clipHandle == null || !_clipHandle.IsValid)
                {
                    ALog.Warn($"[AsakiSoundAgent] Failed to load clip: {resourcePath}");
                    StateMachine.TryTransition(StateTrigger.LoadFailed);
                    return;
                }

                // 资源加载完成
                if (!StateMachine.TryTransition(StateTrigger.LoadComplete))
                {
                    ALog.Warn($"[AsakiSoundAgent] Cannot transition to Ready from state: {State}");
                    return;
                }

                ALog.Info($"[AsakiSoundAgent] Starting playback: {_clipHandle.Asset.name}");

                // 配置 AudioSource
                ConfigureAudioSource(p);

                // 开始播放
                if (!StateMachine.TryTransition(StateTrigger.Play))
                {
                    ALog.Warn($"[AsakiSoundAgent] Cannot start playing from state: {State}");
                    return;
                }

                _source.Play();

                // 等待播放完成
                await WaitForPlaybackCompletion(linkedCts.Token, p.IsLoop);

                // 播放完成
                if (State == AudioPlaybackState.Playing)
                {
                    StateMachine.TryTransition(StateTrigger.PlaybackFinished);
                }

                ALog.Info($"[AsakiSoundAgent] Playback finished: {resourcePath}");
            }
            catch (OperationCanceledException)
            {
                ALog.Info($"[AsakiSoundAgent] Playback canceled: {resourcePath}");
            }
            catch (Exception e)
            {
                ALog.Error($"[AsakiSoundAgent] Playback error: {e.Message}", e);
                StateMachine.TryTransition(StateTrigger.Error);
            }
            finally
            {
                linkedCts.Dispose();
            }
        }

        /// <summary>
        /// 配置音频源参数
        /// </summary>
        private void ConfigureAudioSource(AsakiAudioParams p)
        {
            _transform.position = p.Position;
            _source.clip = _clipHandle.Asset;
            _source.volume = p.Volume;
            _source.pitch = p.Pitch;
            _source.spatialBlend = p.SpatialBlend;
            _source.loop = p.IsLoop;
            _source.priority = p.Priority;
            _source.mute = false;
        }

        /// <summary>
        /// 等待播放完成
        /// </summary>
        private async UniTask WaitForPlaybackCompletion(CancellationToken token, bool isLoop)
        {
            if (isLoop)
            {
                // 循环音频等待取消信号
                await UniTask.WaitUntilCanceled(token);
            }
            else
            {
                // 非循环音频等待播放完成
                while (_source.isPlaying || IsPaused)
                {
                    if (token.IsCancellationRequested)
                        break;

                    await UniTask.Yield(cancellationToken: token);

                    if (_source == null)
                        break;
                }
            }
        }

        // ==========================================================
        // Control Methods with FSM
        // ==========================================================
        public bool Pause()
        {
            if (!StateMachine.CanTransition(StateTrigger.Pause))
            {
                ALog.Warn($"[AsakiSoundAgent] Cannot pause from state: {State}");
                return false;
            }

            if (_source == null)
                return false;

            _source.Pause();
            StateMachine.TryTransition(StateTrigger.Pause);
            ALog.Info($"[AsakiSoundAgent] Paused: {GetInstanceID()}");
            return true;
        }

        public bool Resume()
        {
            if (!StateMachine.CanTransition(StateTrigger.Resume))
            {
                ALog.Warn($"[AsakiSoundAgent] Cannot resume from state: {State}");
                return false;
            }

            if (_source == null)
                return false;

            _source.UnPause();
            StateMachine.TryTransition(StateTrigger.Resume);
            ALog.Info($"[AsakiSoundAgent] Resumed: {GetInstanceID()}");
            return true;
        }

        public bool Stop(float fadeDuration)
        {
            if (!StateMachine.CanTransition(StateTrigger.Stop))
            {
                // 如果不能淡出，尝试立即停止
                if (StateMachine.CanTransition(StateTrigger.StopImmediate))
                {
                    StopImmediate();
                    return true;
                }

                ALog.Warn($"[AsakiSoundAgent] Cannot stop from state: {State}");
                return false;
            }

            StateMachine.TryTransition(StateTrigger.Stop);
            FadeOutAndStop(fadeDuration).Forget(HandleFadeError);
            return true;
        }

        public void StopImmediate()
        {
            if (!StateMachine.CanTransition(StateTrigger.StopImmediate))
            {
                ALog.Warn($"[AsakiSoundAgent] Cannot stop immediately from state: {State}");
                return;
            }

            _playCts?.Cancel();

            if (_source != null && _source.isPlaying)
            {
                _source.Stop();
            }

            StateMachine.TryTransition(StateTrigger.StopImmediate);
            ALog.Info($"[AsakiSoundAgent] Stopped immediately: {GetInstanceID()}");
        }

        private async UniTask FadeOutAndStop(float duration)
        {
            if (_source == null)
            {
                StateMachine.TryTransition(StateTrigger.FadeComplete);
                return;
            }

            // 如果已暂停，先恢复
            if (IsPaused && _source != null)
            {
                _source.UnPause();
            }

            float startVol = _source.volume;
            float timer = 0f;

            while (timer < duration)
            {
                timer += UnityEngine.Time.unscaledDeltaTime;

                if (_source != null)
                {
                    _source.volume = Mathf.Lerp(startVol, 0f, timer / duration);
                }

                await UniTask.Yield();

                // 检查是否被强制停止
                if (State == AudioPlaybackState.Stopped)
                    return;

                if (_source == null)
                    break;
            }

            _playCts?.Cancel();
            StateMachine.TryTransition(StateTrigger.FadeComplete);
            ALog.Info($"[AsakiSoundAgent] Fade out complete: {GetInstanceID()}");
        }

        private void HandleFadeError(Exception ex)
        {
            if (ex is not OperationCanceledException)
            {
                ALog.Error($"[AsakiSoundAgent] FadeOut error: {ex.Message}", ex);
                StateMachine.TryTransition(StateTrigger.Error);
            }
        }

        // ==========================================================
        // Setters with State Validation
        // ==========================================================
        public void SetVolume(float vol)
        {
            // 允许在 Playing、Paused、FadingOut 状态修改音量
            if (
                _source
                && State
                    is AudioPlaybackState.Playing
                        or AudioPlaybackState.Paused
                        or AudioPlaybackState.FadingOut
            )
            {
                _source.volume = vol;
            }
        }

        public void SetPitch(float pitch)
        {
            if (
                _source
                && State
                    is AudioPlaybackState.Playing
                        or AudioPlaybackState.Paused
                        or AudioPlaybackState.FadingOut
            )
            {
                _source.pitch = pitch;
            }
        }

        public void SetPosition(Vector3 pos)
        {
            if (_transform && IsActive)
            {
                _transform.position = pos;
            }
        }

        public void SetLoop(bool loop)
        {
            if (_source && IsActive)
            {
                _source.loop = loop;
            }
        }

        public void SetMuted(bool muted)
        {
            if (_source && IsActive)
            {
                _source.mute = muted;
            }
        }

        public void SetPriority(int priority)
        {
            if (_source && IsActive)
            {
                _source.priority = priority;
            }
        }

        // ==========================================================
        // Cleanup
        // ==========================================================
        private void Cleanup()
        {
            // 停止播放
            if (_source != null)
            {
                if (_source.isPlaying)
                    _source.Stop();
                _source.clip = null;
            }

            // 取消 token
            if (_playCts != null)
            {
                _playCts.Cancel();
                _playCts.Dispose();
                _playCts = null;
            }

            // 释放资源句柄
            if (_clipHandle != null && _clipHandle.IsValid)
            {
                _clipHandle.Dispose();
                _clipHandle = null;
            }

            CurrentAudioPath = null;
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
