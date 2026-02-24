using System;
using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Audio;
using Asaki.Core.Broker;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.Logging;
using Asaki.Core.Pooling;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.Resources;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Unity.Services.Audio
{
    /// <summary>
    /// 音频服务门户
    /// <para>作为外观模式(Facade)的顶层服务，负责请求接收与分发。</para>
    /// <para>具体业务逻辑交由内部子服务执行，保持接口职责单一。</para>
    /// </summary>
    /// <author>Asaki Framework</author>
    /// <version>2.0</version>
    public sealed class AsakiAudioService : IAsakiAudioService
    {
        private readonly AsakiAudioConfig _config;
        private readonly IAsakiResourceService _resourceService;
        private readonly AudioAgentPoolService _agentPoolService;
        private readonly AudioGroupService _groupService;

        private CancellationTokenSource _serviceCts;
        private GameObject _rootObject;
        private Transform _rootTransform;
        private int _handleCounter;

        private readonly Dictionary<AsakiAudioHandle, IAudioAgent> _activeAgents = new(
            AudioConstants.DefaultActiveAgentCapacity
        );
        private readonly Dictionary<AsakiAudioHandle, int> _agentGroups = new();

        private float _globalVolume = AudioConstants.DefaultVolume;

        public AsakiAudioService(
            IAsakiPoolService poolService,
            IAsakiResourceService resourceService,
            AsakiAudioConfig config
        )
        {
            _resourceService =
                resourceService ?? throw new ArgumentNullException(nameof(resourceService));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (poolService == null)
                throw new ArgumentNullException(nameof(poolService));

            _groupService = new AudioGroupService();
            _agentPoolService = new AudioAgentPoolService(poolService, config, null);
        }

        public void OnInit()
        {
            _serviceCts = new CancellationTokenSource();

            _rootObject = new GameObject("[AsakiAudioSystem]");
            Object.DontDestroyOnLoad(_rootObject);
            _rootTransform = _rootObject.transform;

            _config?.InitializeLookup();

            // 初始化默认分组
            InitializeDefaultGroups();

            AudioLogger.LogServiceInitialized();
        }

        private void InitializeDefaultGroups()
        {
            _groupService.GetOrCreateGroup(AudioConstants.GroupSFX, "SFX");
            _groupService.GetOrCreateGroup(AudioConstants.GroupBGM, "BGM");
            _groupService.GetOrCreateGroup(AudioConstants.GroupUI, "UI");
            _groupService.GetOrCreateGroup(AudioConstants.GroupVoice, "Voice");
        }

        public async UniTask OnInitAsync()
        {
            await _agentPoolService.InitializeAsync(_serviceCts.Token);
            AudioLogger.LogPoolInitialized(_agentPoolService.GetStatistics());
        }

        public void OnDispose()
        {
            StopAll(AudioConstants.ImmediateStop);

            _serviceCts?.Cancel();
            _serviceCts?.Dispose();
            _serviceCts = null;

            _agentPoolService?.Dispose();
            _groupService?.ClearAllGroups();

            if (_rootObject != null)
            {
                Object.Destroy(_rootObject);
                _rootObject = null;
                _rootTransform = null;
            }

            _activeAgents.Clear();
            _agentGroups.Clear();

            AudioLogger.LogServiceDisposed();
        }

        #region IAsakiAudioPlayer

        public AsakiAudioHandle Play(
            int assetId,
            AsakiAudioParams parameters = default,
            CancellationToken token = default
        )
        {
            ValidateServiceState();

            if (!_config.TryGet(assetId, out var audioItem))
            {
                throw new ArgumentException($"AudioID {assetId} is not registered in config");
            }

            if (string.IsNullOrEmpty(audioItem.AssetPath))
            {
                throw new InvalidOperationException(
                    $"AssetPath is null or empty for audio ID: {assetId}"
                );
            }

            var mergedParams = MergeParameters(audioItem, parameters);
            var handle = CreateHandle();

            PlayInternalAsync(handle, audioItem.AssetPath, mergedParams, audioItem.Group, token)
                .Forget();

            return handle;
        }

        public void Stop(
            AsakiAudioHandle handle,
            float fadeDuration = AudioConstants.DefaultFadeDuration
        )
        {
            if (!TryGetAgent(handle, out var agent))
                return;

            agent.Stop(fadeDuration);
            ReturnAgent(handle, agent);
        }

        public void Pause(AsakiAudioHandle handle)
        {
            if (TryGetAgent(handle, out var agent))
            {
                agent.Pause();
            }
        }

        public void Resume(AsakiAudioHandle handle)
        {
            if (TryGetAgent(handle, out var agent))
            {
                agent.Resume();
            }
        }

        public bool IsPlaying(AsakiAudioHandle handle)
        {
            return TryGetAgent(handle, out var agent) && agent.IsPlaying;
        }

        public bool IsPaused(AsakiAudioHandle handle)
        {
            return TryGetAgent(handle, out var agent) && agent.IsPaused;
        }

        #endregion

        #region IAsakiAudioGlobalControl

        public void SetGlobalVolume(float volume)
        {
            _globalVolume = Mathf.Clamp(volume, AudioConstants.MinVolume, AudioConstants.MaxVolume);

            // 更新分组服务的全局音量系数
            _groupService.SetGlobalVolumeFactor(_globalVolume);

            // 发布全局音量变化事件
            AsakiBroker.Publish(new GlobalVolumeChangedEvent { Volume = _globalVolume });
        }

        public float GetGlobalVolume()
        {
            return _globalVolume;
        }

        public void StopAll(float fadeDuration = AudioConstants.DefaultStopAllFadeDuration)
        {
            var handles = new List<AsakiAudioHandle>(_activeAgents.Keys);

            foreach (var handle in handles)
            {
                if (_activeAgents.TryGetValue(handle, out var agent))
                {
                    agent.Stop(fadeDuration);
                    ReturnAgent(handle, agent);
                }
            }
        }

        public void PauseAll()
        {
            AudioListener.pause = true;
        }

        public void ResumeAll()
        {
            AudioListener.pause = false;
        }

        #endregion

        #region IAsakiAudioGroupControl

        public void SetGroupVolume(int groupId, float volume)
        {
            _groupService.SetGroupVolume(groupId, volume);
        }

        public void SetGroupVolumeWithFade(
            int groupId,
            float targetVolume,
            float duration,
            CancellationToken cancellationToken = default
        )
        {
            _groupService.SetGroupVolumeWithFade(
                groupId,
                targetVolume,
                duration,
                cancellationToken
            );
        }

        public float GetGroupVolume(int groupId)
        {
            return _groupService.GetGroupVolume(groupId);
        }

        public float GetGroupEffectiveVolume(int groupId)
        {
            return _groupService.GetEffectiveVolume(groupId);
        }

        public void SetGroupMuted(int groupId, bool isMuted)
        {
            _groupService.SetGroupMuted(groupId, isMuted);
        }

        public bool IsGroupMuted(int groupId)
        {
            return _groupService.IsGroupMuted(groupId);
        }

        public void StopGroup(int groupId, float fadeDuration = AudioConstants.DefaultFadeDuration)
        {
            _groupService.StopGroup(groupId, fadeDuration);

            var handlesToRemove = new List<AsakiAudioHandle>();
            foreach (var kvp in _agentGroups)
            {
                if (kvp.Value == groupId)
                {
                    handlesToRemove.Add(kvp.Key);
                }
            }

            foreach (var handle in handlesToRemove)
            {
                _activeAgents.Remove(handle);
                _agentGroups.Remove(handle);
            }
        }

        public void PauseGroup(int groupId)
        {
            _groupService.PauseGroup(groupId);
        }

        public void ResumeGroup(int groupId)
        {
            _groupService.ResumeGroup(groupId);
        }

        #endregion

        #region IAsakiAudioRuntimeControl

        public void SetVolume(AsakiAudioHandle handle, float volume)
        {
            if (TryGetAgent(handle, out var agent))
            {
                agent.SetVolume(volume);
            }
        }

        public void SetPitch(AsakiAudioHandle handle, float pitch)
        {
            if (TryGetAgent(handle, out var agent))
            {
                agent.SetPitch(pitch);
            }
        }

        public void SetSpatialBlend(AsakiAudioHandle handle, float spatialBlend)
        {
            if (TryGetAgent(handle, out var agent))
            {
                agent.SetSpatialBlend(spatialBlend);
            }
        }

        public void SetPosition(AsakiAudioHandle handle, Vector3 position)
        {
            if (TryGetAgent(handle, out var agent))
            {
                agent.SetPosition(position);
            }
        }

        public void SetLoop(AsakiAudioHandle handle, bool isLoop)
        {
            if (TryGetAgent(handle, out var agent))
            {
                agent.SetLoop(isLoop);
            }
        }

        public void SetMuted(AsakiAudioHandle handle, bool isMuted)
        {
            if (TryGetAgent(handle, out var agent))
            {
                agent.SetMuted(isMuted);
            }
        }

        public void SetPriority(AsakiAudioHandle handle, int priority)
        {
            if (TryGetAgent(handle, out var agent))
            {
                agent.SetPriority(priority);
            }
        }

        #endregion

        #region Query Methods

        public AudioPlaybackState GetState(AsakiAudioHandle handle)
        {
            return TryGetAgent(handle, out var agent) ? agent.State : AudioPlaybackState.Idle;
        }

        public bool IsActive(AsakiAudioHandle handle)
        {
            return TryGetAgent(handle, out var agent) && agent.IsActive;
        }

        public bool IsError(AsakiAudioHandle handle)
        {
            return TryGetAgent(handle, out var agent) && agent.IsError;
        }

        public float GetCurrentVolume(AsakiAudioHandle handle)
        {
            return TryGetAgent(handle, out var agent)
                ? agent.State != AudioPlaybackState.Idle
                    ? 1f
                    : 0f
                : 0f;
        }

        public float GetCurrentPitch(AsakiAudioHandle handle)
        {
            return AudioConstants.DefaultPitch;
        }

        public Vector3 GetPosition(AsakiAudioHandle handle)
        {
            return TryGetAgent(handle, out var agent) ? agent.Transform.position : Vector3.zero;
        }

        public string GetPoolStatistics()
        {
            return $"Pool: {_agentPoolService.GetStatistics()}, Active: {_activeAgents.Count}";
        }

        public AudioStateStatistics GetStateStatistics()
        {
            var statistics = new AudioStateStatistics();

            foreach (var agent in _activeAgents.Values)
            {
                if (agent == null)
                    continue;

                switch (agent.State)
                {
                    case AudioPlaybackState.Loading:
                        statistics.LoadingCount++;
                        break;
                    case AudioPlaybackState.Ready:
                        statistics.ReadyCount++;
                        break;
                    case AudioPlaybackState.Playing:
                        statistics.PlayingCount++;
                        break;
                    case AudioPlaybackState.Paused:
                        statistics.PausedCount++;
                        break;
                    case AudioPlaybackState.FadingOut:
                        statistics.FadingOutCount++;
                        break;
                    case AudioPlaybackState.Error:
                        statistics.ErrorCount++;
                        break;
                }
            }

            return statistics;
        }

        #endregion

        #region Private Methods

        private void ValidateServiceState()
        {
            if (_resourceService == null)
                throw new InvalidOperationException("ResourceService is not initialized");

            if (_rootTransform == null)
                throw new InvalidOperationException("Service is not initialized");
        }

        private AsakiAudioHandle CreateHandle()
        {
            return new AsakiAudioHandle(++_handleCounter, UnityEngine.Time.frameCount);
        }

        private AsakiAudioParams MergeParameters(AudioItem audioItem, AsakiAudioParams userParams)
        {
            var baseParams = audioItem.ToParams();

            // 检查是否使用默认参数
            if (userParams.Volume == 0 && userParams.Pitch == 0 && userParams.Priority == 0)
            {
                var result = baseParams;

                if (audioItem.RandomPitch)
                {
                    var randomPitch =
                        baseParams.Pitch
                        + UnityEngine.Random.Range(
                            -AudioConstants.DefaultRandomPitchRange,
                            AudioConstants.DefaultRandomPitchRange
                        );
                    result = result.SetPitch(randomPitch);
                }

                return result;
            }

            // 合并用户参数和配置参数
            var merged = new AsakiAudioParams()
                .SetVolume(baseParams.Volume * userParams.Volume)
                .SetPitch(baseParams.Pitch * userParams.Pitch)
                .SetLoop(baseParams.IsLoop || userParams.IsLoop)
                .SetPriority(userParams.Priority > 0 ? userParams.Priority : baseParams.Priority)
                .SetSpatialBlend(
                    userParams.SpatialBlend > 0 ? userParams.SpatialBlend : baseParams.SpatialBlend
                )
                .SetPosition(
                    userParams.Position != Vector3.zero ? userParams.Position : baseParams.Position
                );

            if (audioItem.RandomPitch)
            {
                var randomPitch =
                    merged.Pitch
                    + UnityEngine.Random.Range(
                        -AudioConstants.DefaultRandomPitchRange,
                        AudioConstants.DefaultRandomPitchRange
                    );
                merged = merged.SetPitch(randomPitch);
            }

            return merged;
        }

        private async UniTaskVoid PlayInternalAsync(
            AsakiAudioHandle handle,
            string clipPath,
            AsakiAudioParams parameters,
            AsakiAudioGroup group,
            CancellationToken userToken
        )
        {
            IAudioAgent agent = null;

            try
            {
                // 确定父节点：3D音效使用提供的位置，否则保持在池根节点
                Transform parent = null;
                if (
                    parameters.SpatialBlend > AudioConstants.Full2D
                    && parameters.Position != Vector3.zero
                )
                {
                    // 3D音效：创建临时父节点用于定位
                    var tempParent = new GameObject($"[Audio3D_{handle.Id}]");
                    tempParent.transform.position = parameters.Position;
                    parent = tempParent.transform;
                }

                agent = await _agentPoolService.BorrowAsync(parent, _serviceCts.Token);

                _activeAgents[handle] = agent;
                _agentGroups[handle] = (int)group;
                _groupService.RegisterToGroup((int)group, handle, agent);

                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    _serviceCts.Token,
                    userToken
                );

                try
                {
                    await agent.PlayAsync(clipPath, parameters, _resourceService, linkedCts.Token);
                }
                finally
                {
                    linkedCts.Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                // 取消是正常行为
            }
            catch (Exception ex)
            {
                AudioLogger.LogPlaybackError(handle.Id, ex.Message);
                throw;
            }
            finally
            {
                if (agent != null)
                {
                    ReturnAgent(handle, agent);
                }
            }
        }

        private bool TryGetAgent(AsakiAudioHandle handle, out IAudioAgent agent)
        {
            return _activeAgents.TryGetValue(handle, out agent) && agent != null;
        }

        private void ReturnAgent(AsakiAudioHandle handle, IAudioAgent agent)
        {
            _activeAgents.Remove(handle);

            if (_agentGroups.TryGetValue(handle, out var groupId))
            {
                _groupService.UnregisterFromGroup(groupId, handle);
                _agentGroups.Remove(handle);
            }

            _agentPoolService.Return(agent);
        }

        #endregion
    }
}
