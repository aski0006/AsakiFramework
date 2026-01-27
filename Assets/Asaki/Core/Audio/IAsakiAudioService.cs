using Asaki.Core.Context;
using System.Threading;
using UnityEngine;

namespace Asaki.Core.Audio
{
	/// <summary>
	/// 纯粹的音频播放行为接口。
	/// 资源生命周期由实现层通过 AsakiResKit 自动管理。
	/// 此接口定义了音频播放相关的一系列操作，包括核心播放与停止行为、运行时控制以及全局和分组控制等功能。
	/// </summary>
	public interface IAsakiAudioService : IAsakiModule
	{
		// ==========================================================
		// 1. 核心行为 (Core Behavior)
		// ==========================================================

		/// <summary>
		/// 播放音频。
		/// <para>实现层逻辑：解析 <paramref name="assetId"/> -> 调用 <see cref="Resources.LoadAsync"/> -> 播放 -> 结束时 Release。</para>
		/// </summary>
		/// <param name="assetId">音频资源的唯一标识符，用于定位和加载音频资源。</param>
		/// <param name="p">音频播放参数包，默认为 <see cref="AsakiAudioParams.Default"/>。可用于配置音频的位置、音量、音调等参数。</param>
		/// <param name="token">用于取消异步操作的 <see cref="CancellationToken"/>，默认为 <see cref="CancellationToken.Default"/>。</param>
		/// <returns>返回一个 <see cref="AsakiAudioHandle"/> 句柄，用于标识和管理正在播放的音频。</returns>
		AsakiAudioHandle Play(int assetId, AsakiAudioParams p = default(AsakiAudioParams), CancellationToken token = default(CancellationToken));

		/// <summary>
		/// 停止音频。
		/// <para>实现层逻辑：淡出 -> 停止 -> Release Resources Handle。</para>
		/// </summary>
		/// <param name="handle">要停止的音频的句柄，通过 <see cref="Play"/> 方法获取。</param>
		/// <param name="fadeDuration">音频淡出的持续时间，单位为秒，默认为 0.2 秒。</param>
		void Stop(AsakiAudioHandle handle, float fadeDuration = 0.2f);

		// ==========================================================
		// 2. 运行时控制 (Runtime Control)
		// ==========================================================

		/// <summary>
		/// 暂停指定句柄的音频播放。
		/// </summary>
		/// <param name="handle">要暂停的音频的句柄，通过 <see cref="Play"/> 方法获取。</param>
		void Pause(AsakiAudioHandle handle);

		/// <summary>
		/// 恢复指定句柄的音频播放。
		/// </summary>
		/// <param name="handle">要恢复的音频的句柄，通过 <see cref="Play"/> 方法获取。</param>
		void Resume(AsakiAudioHandle handle);

		/// <summary>
		/// 动态设置指定句柄音频的音量。
		/// </summary>
		/// <param name="handle">要设置音量的音频的句柄，通过 <see cref="Play"/> 方法获取。</param>
		/// <param name="volume">新的音量值，范围为 0 - 1。</param>
		void SetVolume(AsakiAudioHandle handle, float volume);

		/// <summary>
		/// 动态设置指定句柄音频的音调。
		/// </summary>
		/// <param name="handle">要设置音调的音频的句柄，通过 <see cref="Play"/> 方法获取。</param>
		/// <param name="pitch">新的音调值，例如 1 为正常音调，大于 1 提高音调，小于 1 降低音调。</param>
		void SetPitch(AsakiAudioHandle handle, float pitch);

		/// <summary>
		/// 动态设置指定句柄音频的 2D/3D 混合值。
		/// </summary>
		/// <param name="handle">要设置 2D/3D 混合值的音频的句柄，通过 <see cref="Play"/> 方法获取。</param>
		/// <param name="spatialBlend">新的 2D/3D 混合值，0 表示完全 2D 音效，1 表示完全 3D 音效，中间值表示混合效果。</param>
		void SetSpatialBlend(AsakiAudioHandle handle, float spatialBlend);

		/// <summary>
		/// 更新指定句柄音频的 3D 位置，用于 Core 层驱动移动物体。
		/// </summary>
		/// <param name="handle">要更新位置的音频的句柄，通过 <see cref="Play"/> 方法获取。</param>
		/// <param name="position">新的 3D 空间坐标。</param>
		void SetPosition(AsakiAudioHandle handle, Vector3 position);

		/// <summary>
		/// 设置指定句柄音频是否循环播放。
		/// </summary>
		/// <param name="handle">要设置循环标志的音频的句柄，通过 <see cref="Play"/> 方法获取。</param>
		/// <param name="isLoop">新的循环标志，true 表示循环，false 表示不循环。</param>
		void SetLoop(AsakiAudioHandle handle, bool isLoop);

		/// <summary>
		/// 设置指定句柄音频是否静音。
		/// </summary>
		/// <param name="handle">要设置静音状态的音频的句柄，通过 <see cref="Play"/> 方法获取。</param>
		/// <param name="isMuted">新的静音标志，true 表示静音，false 表示不静音。</param>
		void SetMuted(AsakiAudioHandle handle, bool isMuted);

		/// <summary>
		/// 设置指定句柄音频的优先级。
		/// </summary>
		/// <param name="handle">要设置优先级的音频的句柄，通过 <see cref="Play"/> 方法获取。</param>
		/// <param name="priority">新的优先级值，0 为最高优先级。</param>
		void SetPriority(AsakiAudioHandle handle, int priority);

		// ==========================================================
		// 3. 全局与分组控制 (Global & Groups)
		// ==========================================================

		/// <summary>
		/// 设置全局音频音量。
		/// 此操作将影响所有音频的音量。
		/// </summary>
		/// <param name="volume">新的全局音量值，范围为 0 - 1。</param>
		void SetGlobalVolume(float volume);

		/// <summary>
		/// 停止所有正在播放的音频，并带有淡出效果。
		/// </summary>
		/// <param name="fadeDuration">音频淡出的持续时间，单位为秒，默认为 0.5 秒。</param>
		void StopAll(float fadeDuration = 0.5f);

		/// <summary>
		/// 暂停所有正在播放的音频。
		/// </summary>
		void PauseAll();

		/// <summary>
		/// 恢复所有暂停的音频播放。
		/// </summary>
		void ResumeAll();

		/// <summary>
		/// 判断指定句柄的音频是否正在播放。
		/// </summary>
		/// <param name="handle">要检查的音频的句柄，通过 <see cref="Play"/> 方法获取。</param>
		/// <returns>如果音频正在播放则返回 true，否则返回 false。</returns>
		bool IsPlaying(AsakiAudioHandle handle);

		/// <summary>
		/// 判断指定句柄的音频是否已暂停。
		/// </summary>
		/// <param name="handle">要检查的音频的句柄，通过 <see cref="Play"/> 方法获取。</param>
		/// <returns>如果音频已暂停则返回 true，否则返回 false。</returns>
		bool IsPaused(AsakiAudioHandle handle);

		// 分组控制 (Music, SFX, Voice)
		// 建议：groupId 对应 Unity 的 AudioMixerGroup

		/// <summary>
		/// 设置指定音频组的音量。
		/// </summary>
		/// <param name="groupId">音频组的唯一标识符，建议对应 Unity 的 <see cref="AudioMixerGroup"/>。</param>
		/// <param name="volume">新的音量值，范围为 0 - 1。</param>
		void SetGroupVolume(int groupId, float volume);

		/// <summary>
		/// 设置指定音频组是否静音。
		/// </summary>
		/// <param name="groupId">音频组的唯一标识符，建议对应 Unity 的 <see cref="AudioMixerGroup"/>。</param>
		/// <param name="isMuted">新的静音标志，true 表示静音，false 表示不静音。</param>
		void SetGroupMuted(int groupId, bool isMuted);

		/// <summary>
		/// 停止指定音频组内的所有音频，并带有淡出效果。
		/// </summary>
		/// <param name="groupId">音频组的唯一标识符，建议对应 Unity 的 <see cref="AudioMixerGroup"/>。</param>
		/// <param name="fadeDuration">音频淡出的持续时间，单位为秒，默认为 0.2 秒。</param>
		void StopGroup(int groupId, float fadeDuration = 0.2f);

		/// <summary>
		/// 暂停指定音频组内的所有音频。
		/// </summary>
		/// <param name="groupId">音频组的唯一标识符，建议对应 Unity 的 <see cref="AudioMixerGroup"/>。</param>
		void PauseGroup(int groupId);

		/// <summary>
		/// 恢复指定音频组内所有暂停的音频播放。
		/// </summary>
		/// <param name="groupId">音频组的唯一标识符，建议对应 Unity 的 <see cref="AudioMixerGroup"/>。</param>
		void ResumeGroup(int groupId);
	}
}
