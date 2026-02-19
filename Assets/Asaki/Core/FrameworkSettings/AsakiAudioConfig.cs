using System;
using System.Collections.Generic;
using Asaki.Core.Audio;
using UnityEngine;

namespace Asaki.Core.FrameworkSettings
{
    /// <summary>
    /// 音频分组枚举
    /// </summary>
    public enum AsakiAudioGroup
    {
        SFX = AudioConstants.GroupSFX,
        BGM = AudioConstants.GroupBGM,
        UI = AudioConstants.GroupUI,
        Voice = AudioConstants.GroupVoice,
    }

    /// <summary>
    /// 音频配置项
    /// <para>定义单个音频资源的所有配置参数。</para>
    /// </summary>
    [Serializable]
    public class AudioItem
    {
        public string Key;
        public int ID;

        [Tooltip("Direct AudioClip reference")]
        public AudioClip Clip;

        [Tooltip("Asset path for resource loading")]
        public string AssetPath;

        [Header("Playback Parameters")]
        [Tooltip("Audio volume (0-1)")]
        [Range(AudioConstants.MinVolume, AudioConstants.MaxVolume)]
        public float Volume = AudioConstants.DefaultVolume;

        [Tooltip("Audio pitch (0.1-3)")]
        [Range(AudioConstants.MinPitch, AudioConstants.MaxPitch)]
        public float Pitch = AudioConstants.DefaultPitch;

        [Tooltip("Loop playback")]
        public bool Loop = false;

        [Tooltip("Random pitch variation")]
        public bool RandomPitch = false;

        [Tooltip("Audio group")]
        public AsakiAudioGroup Group = AsakiAudioGroup.SFX;

        [Header("3D Audio Settings")]
        [Tooltip("3D position (only if SpatialBlend > 0)")]
        public Vector3 Position = Vector3.zero;

        [Tooltip("2D/3D blend (0=2D, 1=3D)")]
        [Range(AudioConstants.Full2D, AudioConstants.Full3D)]
        public float SpatialBlend = AudioConstants.DefaultSpatialBlend;

        [Tooltip("Audio priority (0=highest)")]
        [Range(AudioConstants.HighestPriority, AudioConstants.LowestPriority)]
        public int Priority = AudioConstants.DefaultPriority;

        /// <summary>
        /// 转换为播放参数
        /// </summary>
        /// <returns>音频参数结构体</returns>
        public AsakiAudioParams ToParams()
        {
            return new AsakiAudioParams()
                .SetVolume(Volume)
                .SetPitch(Pitch)
                .SetLoop(Loop)
                .SetPriority(Priority)
                .SetSpatialBlend(SpatialBlend)
                .SetPosition(Position);
        }

#if UNITY_EDITOR
        public bool _editorExpanded = false;
#endif
    }

    /// <summary>
    /// 音频配置
    /// <para>管理所有音频资源的注册表和池配置。</para>
    /// </summary>
    [Serializable]
    public class AsakiAudioConfig
    {
        [Header("Global Settings")]
        public GameObject AsakiSoundAgentPrefab;

        [Header("Pool Settings")]
        public int InitialPoolSize = AudioConstants.DefaultInitialPoolSize;
        public int MaxPoolSize = AudioConstants.DefaultMaxPoolSize;

        [Header("Audio Registry")]
        public List<AudioItem> Items = new List<AudioItem>();

        private Dictionary<int, AudioItem> _lookup;

        public void InitializeLookup()
        {
            if (_lookup != null)
                return;

            _lookup = new Dictionary<int, AudioItem>(Items.Count);
            foreach (var item in Items)
            {
                _lookup.TryAdd(item.ID, item);
            }
        }

        public bool TryGet(int id, out AudioItem item)
        {
            item = null;
            if (_lookup == null)
                InitializeLookup();

            return _lookup != null && _lookup.TryGetValue(id, out item);
        }
    }
}
