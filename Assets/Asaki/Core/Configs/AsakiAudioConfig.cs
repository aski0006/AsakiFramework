// 文件: Assets/Asaki/Core/Configs/AsakiAudioConfig.cs
using System;
using System.Collections.Generic;
using Asaki.Core.Audio;
using UnityEngine;

namespace Asaki.Core.Configs
{
    public enum AsakiAudioGroup
    {
        SFX = 0,
        BGM = 1,
        UI = 2,
        Voice = 3,
    }

    [Serializable]
    public class AudioItem
    {
        public string Key;
        public int ID;

        [Tooltip("Direct AudioClip reference")]
        public AudioClip Clip;

        [Tooltip("Asset path for resource loading")]
        public string AssetPath;

        // ========================================
        // ✅ 新增：完整的播放参数配置
        // ========================================
        [Header("Playback Parameters")]
        [Tooltip("Audio volume (0-1)")]
        [Range(0f, 1f)]
        public float Volume = 1f;

        [Tooltip("Audio pitch (0.1-3)")]
        [Range(0.1f, 3f)]
        public float Pitch = 1f;

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
        [Range(0f, 1f)]
        public float SpatialBlend = 0f;

        [Tooltip("Audio priority (0=highest)")]
        [Range(0, 256)]
        public int Priority = 128;

        // ========================================
        // ✅ 新增方法：转换为 AsakiAudioParams
        // ========================================
        /// <summary>
        /// Convert this AudioItem to AsakiAudioParams
        /// </summary>
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

    [Serializable]
    public class AsakiAudioConfig
    {
        [Header("Global Settings")]
        public GameObject AsakiSoundAgentPrefab;

        [Header("Pool Settings")]
        public int InitialPoolSize = 16;
        public int MaxPoolSize = 100;

        [Header("Audio Registry")]
        public List<AudioItem> Items = new List<AudioItem>();

        private Dictionary<int, AudioItem> _lookup;

        public void InitializeLookup()
        {
            if (_lookup != null)
                return;

            _lookup = new Dictionary<int, AudioItem>(Items.Count);
            foreach (AudioItem item in Items)
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
