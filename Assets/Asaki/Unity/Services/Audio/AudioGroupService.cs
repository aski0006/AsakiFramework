using System;
using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Audio;
using Asaki.Core.Broker;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Unity.Services.Audio
{
    /// <summary>
    /// 音频分组服务实现
    /// <para>管理音频分组，提供按组控制功能，支持音量渐变和全局音量联动。</para>
    /// </summary>
    /// <author>Asaki Framework</author>
    /// <version>2.0</version>
    internal sealed class AudioGroupService : IAudioGroupService
    {
        private readonly Dictionary<int, List<IAudioAgent>> _groupAgents = new();
        private readonly Dictionary<int, AudioGroupData> _groupData = new();
        private readonly Dictionary<int, CancellationTokenSource> _fadeTokens = new();

        private float _globalVolumeFactor = AudioConstants.DefaultVolume;

        /// <summary>
        /// 分组音量变化事件
        /// </summary>
        public event Action<int, float, bool> OnGroupVolumeChanged;

        /// <summary>
        /// 分组静音状态变化事件
        /// </summary>
        public event Action<int, bool> OnGroupMuteChanged;

        #region 注册管理

        public void RegisterToGroup(int groupId, AsakiAudioHandle handle, IAudioAgent agent)
        {
            if (!_groupAgents.TryGetValue(groupId, out var agents))
            {
                agents = new List<IAudioAgent>();
                _groupAgents[groupId] = agents;
            }

            if (!agents.Contains(agent))
            {
                agents.Add(agent);

                // 应用当前分组音量
                var effectiveVolume = GetEffectiveVolume(groupId);
                agent.SetVolume(effectiveVolume);

                // 应用静音状态
                if (_groupData.TryGetValue(groupId, out var data) && data.IsMuted)
                {
                    agent.SetMuted(true);
                }
            }
        }

        public void UnregisterFromGroup(int groupId, AsakiAudioHandle handle)
        {
            if (_groupAgents.TryGetValue(groupId, out var agents))
            {
                agents.RemoveAll(a => a == null || !a.IsActive);
            }
        }

        public IReadOnlyList<IAudioAgent> GetGroupAgents(int groupId)
        {
            return _groupAgents.TryGetValue(groupId, out var agents) ? agents : null;
        }

        public AudioGroupData GetOrCreateGroup(int groupId, string groupName = null)
        {
            if (!_groupData.TryGetValue(groupId, out var data))
            {
                data = new AudioGroupData
                {
                    GroupId = groupId,
                    Volume = AudioConstants.DefaultVolume,
                    TargetVolume = AudioConstants.DefaultVolume,
                    BaseVolume = AudioConstants.DefaultVolume,
                    IsMuted = false,
                    Name = groupName ?? GetDefaultGroupName(groupId),
                };
                _groupData[groupId] = data;
            }
            return data;
        }

        public bool HasGroup(int groupId)
        {
            return _groupData.ContainsKey(groupId);
        }

        private static string GetDefaultGroupName(int groupId)
        {
            return groupId switch
            {
                AudioConstants.GroupSFX => "SFX",
                AudioConstants.GroupBGM => "BGM",
                AudioConstants.GroupUI => "UI",
                AudioConstants.GroupVoice => "Voice",
                _ => $"Group_{groupId}",
            };
        }

        #endregion

        #region 音量控制

        public float GetGroupVolume(int groupId)
        {
            return _groupData.TryGetValue(groupId, out var data)
                ? data.Volume
                : AudioConstants.DefaultVolume;
        }

        public void SetGroupVolume(int groupId, float volume)
        {
            // 取消正在进行的渐变
            CancelFade(groupId);

            var clampedVolume = Mathf.Clamp(
                volume,
                AudioConstants.MinVolume,
                AudioConstants.MaxVolume
            );

            var data = GetOrCreateGroup(groupId);
            data.Volume = clampedVolume;
            data.TargetVolume = clampedVolume;
            data.BaseVolume = clampedVolume;
            _groupData[groupId] = data;

            // 应用到所有代理
            ApplyVolumeToAgents(groupId);

            // 发布事件
            PublishVolumeChangedEvent(groupId, clampedVolume, false);
            OnGroupVolumeChanged?.Invoke(groupId, clampedVolume, false);
        }

        public void SetGroupVolumeWithFade(
            int groupId,
            float targetVolume,
            float duration,
            CancellationToken cancellationToken = default
        )
        {
            // 取消之前的渐变
            CancelFade(groupId);

            if (duration <= 0f)
            {
                SetGroupVolume(groupId, targetVolume);
                return;
            }

            var data = GetOrCreateGroup(groupId);
            var startVolume = data.Volume;
            var target = Mathf.Clamp(
                targetVolume,
                AudioConstants.MinVolume,
                AudioConstants.MaxVolume
            );

            // 创建新的取消令牌
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _fadeTokens[groupId] = cts;

            FadeVolumeAsync(groupId, startVolume, target, duration, cts.Token).Forget();
        }

        private async UniTaskVoid FadeVolumeAsync(
            int groupId,
            float startVolume,
            float targetVolume,
            float duration,
            CancellationToken cancellationToken
        )
        {
            var elapsed = 0f;

            while (elapsed < duration && !cancellationToken.IsCancellationRequested)
            {
                elapsed += UnityEngine.Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var currentVolume = Mathf.Lerp(startVolume, targetVolume, t);

                // 更新分组数据
                if (_groupData.TryGetValue(groupId, out var data))
                {
                    data.Volume = currentVolume;
                    data.TargetVolume = targetVolume;
                    _groupData[groupId] = data;
                }

                // 应用到代理
                ApplyVolumeToAgents(groupId);

                // 发布渐变事件
                PublishVolumeChangedEvent(groupId, currentVolume, true);
                OnGroupVolumeChanged?.Invoke(groupId, currentVolume, true);

                await UniTask.Yield(cancellationToken);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                // 渐变完成，设置最终值
                if (_groupData.TryGetValue(groupId, out var data))
                {
                    data.Volume = targetVolume;
                    data.TargetVolume = targetVolume;
                    data.BaseVolume = targetVolume;
                    _groupData[groupId] = data;
                }

                ApplyVolumeToAgents(groupId);

                // 发布完成事件
                PublishVolumeChangedEvent(groupId, targetVolume, false);
                OnGroupVolumeChanged?.Invoke(groupId, targetVolume, false);
            }

            _fadeTokens.Remove(groupId);
        }

        private void CancelFade(int groupId)
        {
            if (_fadeTokens.TryGetValue(groupId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _fadeTokens.Remove(groupId);
            }
        }

        public float GetEffectiveVolume(int groupId)
        {
            var groupVolume = GetGroupVolume(groupId);
            return groupVolume * _globalVolumeFactor;
        }

        public void SetGlobalVolumeFactor(float globalVolume)
        {
            var oldFactor = _globalVolumeFactor;
            _globalVolumeFactor = Mathf.Clamp(
                globalVolume,
                AudioConstants.MinVolume,
                AudioConstants.MaxVolume
            );

            // 如果全局音量变化，更新所有分组的实际音量
            if (Math.Abs(oldFactor - _globalVolumeFactor) > 0.001f)
            {
                foreach (var groupId in _groupData.Keys)
                {
                    ApplyVolumeToAgents(groupId);
                }
            }
        }

        private void ApplyVolumeToAgents(int groupId)
        {
            if (!_groupAgents.TryGetValue(groupId, out var agents))
                return;

            var effectiveVolume = GetEffectiveVolume(groupId);

            foreach (var agent in agents)
            {
                if (agent != null && agent.IsActive)
                {
                    agent.SetVolume(effectiveVolume);
                }
            }
        }

        #endregion

        #region 静音控制

        public bool IsGroupMuted(int groupId)
        {
            return _groupData.TryGetValue(groupId, out var data) && data.IsMuted;
        }

        public void SetGroupMuted(int groupId, bool isMuted)
        {
            var data = GetOrCreateGroup(groupId);
            data.IsMuted = isMuted;
            _groupData[groupId] = data;

            if (_groupAgents.TryGetValue(groupId, out var agents))
            {
                foreach (var agent in agents)
                {
                    if (agent != null && agent.IsActive)
                    {
                        agent.SetMuted(isMuted);
                    }
                }
            }

            // 发布事件
            PublishMuteChangedEvent(groupId, isMuted);
            OnGroupMuteChanged?.Invoke(groupId, isMuted);
        }

        #endregion

        #region 播放控制

        public void StopGroup(int groupId, float fadeDuration)
        {
            CancelFade(groupId);

            if (_groupAgents.TryGetValue(groupId, out var agents))
            {
                var agentsCopy = new List<IAudioAgent>(agents);
                agents.Clear();

                foreach (var agent in agentsCopy)
                {
                    if (agent != null && agent.IsActive)
                    {
                        agent.Stop(fadeDuration);
                    }
                }
            }
        }

        public void PauseGroup(int groupId)
        {
            if (_groupAgents.TryGetValue(groupId, out var agents))
            {
                foreach (var agent in agents)
                {
                    if (agent != null && agent.IsPlaying)
                    {
                        agent.Pause();
                    }
                }
            }
        }

        public void ResumeGroup(int groupId)
        {
            if (_groupAgents.TryGetValue(groupId, out var agents))
            {
                foreach (var agent in agents)
                {
                    if (agent != null && agent.IsPaused)
                    {
                        agent.Resume();
                    }
                }
            }
        }

        #endregion

        #region 批量操作

        public IEnumerable<int> GetAllGroupIds()
        {
            return _groupData.Keys;
        }

        public void ClearAllGroups()
        {
            // 取消所有渐变
            foreach (var cts in _fadeTokens.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _fadeTokens.Clear();

            // 清空代理
            foreach (var agents in _groupAgents.Values)
            {
                agents.Clear();
            }
            _groupAgents.Clear();
            _groupData.Clear();
        }

        #endregion

        #region 事件发布

        private static void PublishVolumeChangedEvent(
            int groupId,
            float volume,
            bool isTransitioning
        )
        {
            AsakiBroker.Publish(
                new AudioGroupVolumeChangedEvent
                {
                    GroupId = groupId,
                    Volume = volume,
                    IsTransitioning = isTransitioning,
                }
            );
        }

        private static void PublishMuteChangedEvent(int groupId, bool isMuted)
        {
            AsakiBroker.Publish(
                new AudioGroupMuteChangedEvent { GroupId = groupId, IsMuted = isMuted }
            );
        }

        #endregion
    }
}
