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
    /// Sound playback agent - Async resource loading
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AsakiSoundAgent : MonoBehaviour, IAsakiPoolable
    {
        private AudioSource _source;
        private Transform _transform;
        private ResHandle<AudioClip> _clipHandle;
        private CancellationTokenSource _playCts;

        public bool IsPlaying { get; private set; }
        private bool _isPaused;
        public bool IsPaused => _isPaused;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _transform = transform;
            _source.playOnAwake = false;
        }

        // ==========================================================
        // IAsakiPoolable Lifecycle
        // ==========================================================
        public void OnSpawn()
        {
            IsPlaying = true;
            _isPaused = false;
            _playCts = new CancellationTokenSource();

            ALog.Info($"[AsakiSoundAgent] Agent spawned: {GetInstanceID()}");
        }

        public void OnDespawn()
        {
            // Stop playback
            if (_source != null)
            {
                if (_source.isPlaying)
                    _source.Stop();
                _source.clip = null;
            }

            // Cancel token
            if (_playCts != null)
            {
                _playCts.Cancel();
                _playCts.Dispose();
                _playCts = null;
            }

            // ✅ Release resource handle
            if (_clipHandle != null && _clipHandle.IsValid)
            {
                _clipHandle.Dispose();
                _clipHandle = null;
            }

            IsPlaying = false;
            _isPaused = false;

            ALog.Info($"[AsakiSoundAgent] Agent despawned: {GetInstanceID()}");
        }

        // ==========================================================
        // Core Playback (Resource Loading)
        // ==========================================================
        public async UniTask PlayAsync(
            string resourcePath,
            AsakiAudioParams p,
            IAsakiResourceService resourceService,
            CancellationToken serviceToken
        )
        {
            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                serviceToken,
                _playCts.Token,
                this.GetCancellationTokenOnDestroy()
            );

            try
            {
                ALog.Info($"[AsakiSoundAgent] Loading audio from: {resourcePath}");

                // ✅ Async load audio clip
                _clipHandle = await resourceService.LoadAsync<AudioClip>(
                    resourcePath,
                    linkedCts.Token
                );

                if (linkedCts.IsCancellationRequested)
                    return;

                if (_clipHandle == null || !_clipHandle.IsValid)
                {
                    ALog.Warn($"[AsakiSoundAgent] Failed to load clip: {resourcePath}");
                    return;
                }

                ALog.Info($"[AsakiSoundAgent] Starting playback: {_clipHandle.Asset.name}");

                // Configure AudioSource
                _transform.position = p.Position;
                _source.clip = _clipHandle.Asset;
                _source.volume = p.Volume;
                _source.pitch = p.Pitch;
                _source.spatialBlend = p.SpatialBlend;
                _source.loop = p.IsLoop;
                _source.priority = p.Priority;
                _source.mute = false;

                // Start playback
                _source.Play();

                // Wait for completion
                if (p.IsLoop)
                {
                    await UniTask.WaitUntilCanceled(linkedCts.Token);
                }
                else
                {
                    while (_source.isPlaying || _isPaused)
                    {
                        if (linkedCts.IsCancellationRequested)
                            break;

                        await UniTask.Yield(cancellationToken: linkedCts.Token);

                        if (_source == null)
                            break;
                    }
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
            }
            finally
            {
                linkedCts.Dispose();
            }
        }

        // ==========================================================
        // Control Methods
        // ==========================================================
        public void Pause()
        {
            if (!IsPlaying || _isPaused || _source == null)
                return;

            _source.Pause();
            _isPaused = true;
            ALog.Info($"[AsakiSoundAgent] Paused: {GetInstanceID()}");
        }

        public void Resume()
        {
            if (!IsPlaying || !_isPaused || _source == null)
                return;

            _source.UnPause();
            _isPaused = false;
            ALog.Info($"[AsakiSoundAgent] Resumed: {GetInstanceID()}");
        }

        public void Stop(float fadeDuration)
        {
            if (!IsPlaying)
                return;

            FadeOutAndStop(fadeDuration)
                .Forget(ex =>
                {
                    if (ex is not OperationCanceledException)
                    {
                        ALog.Error($"[AsakiSoundAgent] FadeOut error: {ex.Message}", ex);
                    }
                });
        }

        private async UniTask FadeOutAndStop(float duration)
        {
            if (_source == null)
                return;

            if (_isPaused)
            {
                _source.UnPause();
                _isPaused = false;
            }

            float startVol = _source.volume;
            float timer = 0f;

            while (timer < duration)
            {
                timer += UnityEngine.Time.unscaledDeltaTime;
                _source.volume = Mathf.Lerp(startVol, 0f, timer / duration);
                await UniTask.Yield();

                if (!IsPlaying || _source == null)
                    return;
            }

            _playCts?.Cancel();
            ALog.Info($"[AsakiSoundAgent] Stopped with fade: {GetInstanceID()}");
        }

        // ==========================================================
        // Setters
        // ==========================================================
        public void SetVolume(float vol)
        {
            if (_source)
                _source.volume = vol;
        }

        public void SetPitch(float pitch)
        {
            if (_source)
                _source.pitch = pitch;
        }

        public void SetPosition(Vector3 pos)
        {
            if (_transform)
                _transform.position = pos;
        }

        public void SetLoop(bool loop)
        {
            if (_source)
                _source.loop = loop;
        }

        public void SetMuted(bool muted)
        {
            if (_source)
                _source.mute = muted;
        }
    }
}
