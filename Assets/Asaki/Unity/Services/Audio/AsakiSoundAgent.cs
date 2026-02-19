using System;
using System.Threading;
using Asaki.Core.Audio;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.Resources;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Unity.Services.Audio
{
    /// <summary>
    /// 音频播放代理
    /// <para>使用有限状态机管理音频播放状态，实现IAudioAgent接口。</para>
    /// <para>支持3D音效定位，可动态挂载到指定父节点。</para>
    /// </summary>
    /// <author>Asaki Framework</author>
    /// <version>2.0</version>
    [RequireComponent(typeof(AudioSource))]
    public sealed class AsakiSoundAgent : MonoBehaviour, IAudioAgent, IAsakiPoolable
    {
        private AudioSource _audioSource;
        private Transform _cachedTransform;
        private ResHandle<AudioClip> _clipHandle;
        private CancellationTokenSource _playCts;
        private Transform _originalParent;

        public AudioPlaybackState State => StateMachine?.CurrentState ?? AudioPlaybackState.Idle;
        public bool IsPlaying => State == AudioPlaybackState.Playing;
        public bool IsPaused => State == AudioPlaybackState.Paused;
        public bool IsError => State == AudioPlaybackState.Error;
        public bool IsActive =>
            State
                is AudioPlaybackState.Loading
                    or AudioPlaybackState.Ready
                    or AudioPlaybackState.Playing
                    or AudioPlaybackState.Paused
                    or AudioPlaybackState.FadingOut;
        public string CurrentAudioPath { get; private set; }
        public Transform Transform => _cachedTransform;

        public AudioStateMachine StateMachine { get; private set; }
        public event Action<AudioPlaybackState, AudioPlaybackState> OnStateChanged;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _cachedTransform = transform;
            _audioSource.playOnAwake = false;

            StateMachine = new AudioStateMachine();
            StateMachine.OnStateChanged += HandleStateChanged;
        }

        private void HandleStateChanged(
            AudioPlaybackState previousState,
            AudioPlaybackState currentState
        )
        {
            OnStateChanged?.Invoke(previousState, currentState);
        }

        public void OnSpawn()
        {
            _playCts = new CancellationTokenSource();
            _originalParent = _cachedTransform.parent;
            CurrentAudioPath = null;
        }

        public void OnDespawn()
        {
            StopImmediate();
            Cleanup();
            StateMachine.Reset();
            RestoreParent();
        }

        public async UniTask PlayAsync(
            string resourcePath,
            AsakiAudioParams parameters,
            IAsakiResourceService resourceService,
            CancellationToken cancellationToken
        )
        {
            if (!StateMachine.TryTransition(StateTrigger.Play))
            {
                throw new InvalidOperationException($"Cannot start playback from state: {State}");
            }

            CurrentAudioPath = resourcePath;

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _playCts.Token,
                this.GetCancellationTokenOnDestroy()
            );

            try
            {
                _clipHandle = await resourceService.LoadAsync<AudioClip>(
                    resourcePath,
                    linkedCts.Token
                );

                if (linkedCts.IsCancellationRequested)
                    return;

                if (_clipHandle == null || !_clipHandle.IsValid)
                {
                    StateMachine.TryTransition(StateTrigger.LoadFailed);
                    throw new InvalidOperationException(
                        $"Failed to load audio clip: {resourcePath}"
                    );
                }

                if (!StateMachine.TryTransition(StateTrigger.LoadComplete))
                {
                    throw new InvalidOperationException(
                        $"Cannot transition to Ready from state: {State}"
                    );
                }

                ConfigureAudioSource(parameters);

                if (!StateMachine.TryTransition(StateTrigger.Play))
                {
                    throw new InvalidOperationException(
                        $"Cannot start playing from state: {State}"
                    );
                }

                _audioSource.Play();
                await WaitForPlaybackCompletion(linkedCts.Token, parameters.IsLoop);

                if (State == AudioPlaybackState.Playing)
                {
                    StateMachine.TryTransition(StateTrigger.PlaybackFinished);
                }
            }
            catch (OperationCanceledException)
            {
                // 取消是正常行为，不抛出异常
            }
            finally
            {
                linkedCts.Dispose();
            }
        }

        private void ConfigureAudioSource(AsakiAudioParams parameters)
        {
            _cachedTransform.position = parameters.Position;
            _audioSource.clip = _clipHandle.Asset;
            _audioSource.volume = parameters.Volume;
            _audioSource.pitch = parameters.Pitch;
            _audioSource.spatialBlend = parameters.SpatialBlend;
            _audioSource.loop = parameters.IsLoop;
            _audioSource.priority = parameters.Priority;
            _audioSource.mute = false;
        }

        private async UniTask WaitForPlaybackCompletion(CancellationToken token, bool isLoop)
        {
            if (isLoop)
            {
                await UniTask.WaitUntilCanceled(token);
            }
            else
            {
                while (_audioSource.isPlaying || IsPaused)
                {
                    if (token.IsCancellationRequested)
                        break;

                    await UniTask.Yield(cancellationToken: token);

                    if (_audioSource == null)
                        break;
                }
            }
        }

        public bool Pause()
        {
            if (!StateMachine.CanTransition(StateTrigger.Pause) || _audioSource == null)
                return false;

            _audioSource.Pause();
            StateMachine.TryTransition(StateTrigger.Pause);
            return true;
        }

        public bool Resume()
        {
            if (!StateMachine.CanTransition(StateTrigger.Resume) || _audioSource == null)
                return false;

            _audioSource.UnPause();
            StateMachine.TryTransition(StateTrigger.Resume);
            return true;
        }

        public bool Stop(float fadeDuration)
        {
            if (!StateMachine.CanTransition(StateTrigger.Stop))
            {
                if (StateMachine.CanTransition(StateTrigger.StopImmediate))
                {
                    StopImmediate();
                    return true;
                }
                return false;
            }

            StateMachine.TryTransition(StateTrigger.Stop);
            FadeOutAndStop(fadeDuration).Forget();
            return true;
        }

        public void StopImmediate()
        {
            if (!StateMachine.CanTransition(StateTrigger.StopImmediate))
                return;

            _playCts?.Cancel();

            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }

            StateMachine.TryTransition(StateTrigger.StopImmediate);
        }

        private async UniTaskVoid FadeOutAndStop(float duration)
        {
            if (_audioSource == null)
            {
                StateMachine.TryTransition(StateTrigger.FadeComplete);
                return;
            }

            if (IsPaused && _audioSource != null)
            {
                _audioSource.UnPause();
            }

            float startVolume = _audioSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += UnityEngine.Time.unscaledDeltaTime;

                if (_audioSource != null)
                {
                    _audioSource.volume = Mathf.Lerp(
                        startVolume,
                        AudioConstants.MinVolume,
                        elapsed / duration
                    );
                }

                await UniTask.Yield();

                if (State == AudioPlaybackState.Stopped || _audioSource == null)
                    return;
            }

            _playCts?.Cancel();
            StateMachine.TryTransition(StateTrigger.FadeComplete);
        }

        public void SetVolume(float volume)
        {
            if (_audioSource != null && IsActive)
            {
                _audioSource.volume = Mathf.Clamp(
                    volume,
                    AudioConstants.MinVolume,
                    AudioConstants.MaxVolume
                );
            }
        }

        public void SetPitch(float pitch)
        {
            if (_audioSource != null && IsActive)
            {
                _audioSource.pitch = Mathf.Clamp(
                    pitch,
                    AudioConstants.MinPitch,
                    AudioConstants.MaxPitch
                );
            }
        }

        public void SetPosition(Vector3 position)
        {
            if (_cachedTransform != null && IsActive)
            {
                _cachedTransform.position = position;
            }
        }

        public void SetLoop(bool isLoop)
        {
            if (_audioSource != null && IsActive)
            {
                _audioSource.loop = isLoop;
            }
        }

        public void SetMuted(bool isMuted)
        {
            if (_audioSource != null && IsActive)
            {
                _audioSource.mute = isMuted;
            }
        }

        public void SetPriority(int priority)
        {
            if (_audioSource != null && IsActive)
            {
                _audioSource.priority = Mathf.Clamp(
                    priority,
                    AudioConstants.HighestPriority,
                    AudioConstants.LowestPriority
                );
            }
        }

        public void SetSpatialBlend(float spatialBlend)
        {
            if (_audioSource != null && IsActive)
            {
                _audioSource.spatialBlend = Mathf.Clamp(
                    spatialBlend,
                    AudioConstants.Full2D,
                    AudioConstants.Full3D
                );
            }
        }

        /// <summary>
        /// 设置父节点(用于3D音效定位)
        /// </summary>
        /// <param name="parent">父节点，为null则保持原位置</param>
        public void SetParent(Transform parent)
        {
            if (_cachedTransform != null)
            {
                _cachedTransform.SetParent(parent);
            }
        }

        /// <summary>
        /// 恢复原始父节点
        /// </summary>
        public void RestoreParent()
        {
            if (_cachedTransform != null && _originalParent != null)
            {
                _cachedTransform.SetParent(_originalParent);
            }
        }

        private void Cleanup()
        {
            if (_audioSource != null)
            {
                if (_audioSource.isPlaying)
                    _audioSource.Stop();
                _audioSource.clip = null;
            }

            if (_playCts != null)
            {
                _playCts.Cancel();
                _playCts.Dispose();
                _playCts = null;
            }

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
            StateMachine.OnStateChanged -= HandleStateChanged;
        }
    }
}
