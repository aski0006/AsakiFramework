using System;
using System.Collections.Generic;
using UnityEngine;

namespace Asaki.Core.Configs
{
	/// <summary>
	/// 定义了不同类型音频的分组枚举。
	/// </summary>
	public enum AsakiAudioGroup
	{
		/// <summary>
		/// 普通音效分组。
		/// </summary>
		SFX = 0,
		/// <summary>
		/// 背景音乐分组。
		/// </summary>
		BGM = 1,
		/// <summary>
		/// UI交互音分组。
		/// </summary>
		UI = 2,
		/// <summary>
		/// 语音分组。
		/// </summary>
		Voice = 3,
	}

	/// <summary>
	/// 表示音频项的可序列化类，包含音频的相关属性。
	/// </summary>
	[Serializable]
	public class AudioItem
	{
		/// <summary>
		/// 用于代码引用的Enum Key。
		/// </summary>
		public string Key;
		/// <summary>
		/// 音频项的Hash ID。
		/// </summary>
		public int ID;

		/// <summary>
		/// 编辑器引用，用于预览和生成路径。
		/// </summary>
		[Tooltip("编辑器引用，用于预览和生成路径")]
		public AudioClip Clip;

		/// <summary>
		/// 运行时加载路径 (自动生成)。
		/// </summary>
		[Tooltip("运行时加载路径 (自动生成)")]
		public string AssetPath;

		/// <summary>
		/// 音频的音量，范围在0f到1f之间，默认值为1f。
		/// </summary>
		[Range(0f, 1f)] public float Volume = 1f;
		/// <summary>
		/// 音频的音高，范围在0.1f到3f之间，默认值为1f。
		/// </summary>
		[Range(0.1f, 3f)] public float Pitch = 1f;
		/// <summary>
		/// 音频是否循环，默认值为false。
		/// </summary>
		public bool Loop = false;
		/// <summary>
		/// 音频所属的分组，默认值为AsakiAudioGroup.SFX。
		/// </summary>
		public AsakiAudioGroup Group = AsakiAudioGroup.SFX;
		/// <summary>
		/// 音频是否随机音高，默认值为false。
		/// </summary>
		public bool RandomPitch = false;

		#if UNITY_EDITOR
		/// <summary>
		/// 编辑器扩展状态，仅在编辑器模式下使用。
		/// </summary>
		public bool _editorExpanded = false;
		#endif
	}

	/// <summary>
	/// Asaki音频配置的可序列化类，管理音频的全局设置和音频项注册表。
	/// </summary>
	[Serializable]
	public class AsakiAudioConfig
	{
		/// <summary>
		/// 全局设置部分，声音代理预制体的资产键。
		/// </summary>
		[Header("Global Settings")]
		public string SoundAgentPrefabAssetKey;
		/// <summary>
		/// 全局设置部分，初始池大小，默认值为16。
		/// </summary>
		public int InitialPoolSize = 16;

		/// <summary>
		/// 注册表部分，包含所有音频项的列表。
		/// </summary>
		[Header("Registry")]
		public List<AudioItem> Items = new List<AudioItem>();

		/// <summary>
		/// 运行时缓存，用于快速查找音频项。
		/// </summary>
		private Dictionary<int, AudioItem> _lookup;

		/// <summary>
		/// 初始化查找表。如果查找表已存在则不执行操作。
		/// </summary>
		public void InitializeLookup()
		{
			if (_lookup != null) return;
			_lookup = new Dictionary<int, AudioItem>(Items.Count);
			foreach (AudioItem item in Items)
			{
				if (!_lookup.ContainsKey(item.ID))
				{
					_lookup.Add(item.ID, item);
				}
			}
		}

		/// <summary>
		/// 根据给定的ID尝试获取音频项。
		/// 如果查找表未初始化，则先进行初始化。
		/// </summary>
		/// <param name="id">要查找的音频项的ID。</param>
		/// <param name="item">找到的音频项，如果未找到则为null。</param>
		/// <returns>如果找到音频项则返回true，否则返回false。</returns>
		public bool TryGet(int id, out AudioItem item)
		{
			item = null;
			if (_lookup == null) InitializeLookup();
			return _lookup != null && _lookup.TryGetValue(id, out item);
		}
	}
}
