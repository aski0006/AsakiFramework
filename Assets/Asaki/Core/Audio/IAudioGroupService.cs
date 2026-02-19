using System;
using System.Collections.Generic;
using System.Threading;

namespace Asaki.Core.Audio
{
    /// <summary>
    /// 音频分组数据
    /// <para>存储单个音频分组的完整状态信息。</para>
    /// </summary>
    public struct AudioGroupData
    {
        /// <summary>分组ID</summary>
        public int GroupId;

        /// <summary>当前音量(0-1)</summary>
        public float Volume;

        /// <summary>目标音量(用于渐变)</summary>
        public float TargetVolume;

        /// <summary>基础音量(不受全局音量影响)</summary>
        public float BaseVolume;

        /// <summary>是否静音</summary>
        public bool IsMuted;

        /// <summary>分组名称</summary>
        public string Name;
    }

    /// <summary>
    /// 音频分组服务接口
    /// <para>负责管理音频分组，提供按组控制音量、静音等功能。</para>
    /// <para>支持音量渐变、全局音量联动等高级功能。</para>
    /// </summary>
    /// <author>Asaki Framework</author>
    /// <version>2.0</version>
    public interface IAudioGroupService
    {
        /// <summary>
        /// 分组音量变化事件
        /// </summary>
        event Action<int, float, bool> OnGroupVolumeChanged;

        /// <summary>
        /// 分组静音状态变化事件
        /// </summary>
        event Action<int, bool> OnGroupMuteChanged;

        #region 注册管理

        /// <summary>
        /// 注册音频到分组
        /// </summary>
        void RegisterToGroup(int groupId, AsakiAudioHandle handle, IAudioAgent agent);

        /// <summary>
        /// 从分组注销音频
        /// </summary>
        void UnregisterFromGroup(int groupId, AsakiAudioHandle handle);

        /// <summary>
        /// 获取分组内的所有代理
        /// </summary>
        IReadOnlyList<IAudioAgent> GetGroupAgents(int groupId);

        /// <summary>
        /// 获取或创建分组数据
        /// </summary>
        AudioGroupData GetOrCreateGroup(int groupId, string groupName = null);

        /// <summary>
        /// 检查分组是否存在
        /// </summary>
        bool HasGroup(int groupId);

        #endregion

        #region 音量控制

        /// <summary>
        /// 获取分组音量
        /// </summary>
        float GetGroupVolume(int groupId);

        /// <summary>
        /// 设置分组音量(立即生效)
        /// </summary>
        void SetGroupVolume(int groupId, float volume);

        /// <summary>
        /// 设置分组音量(带渐变效果)
        /// </summary>
        /// <param name="groupId">分组ID</param>
        /// <param name="targetVolume">目标音量</param>
        /// <param name="duration">渐变时长(秒)</param>
        /// <param name="cancellationToken">取消令牌</param>
        void SetGroupVolumeWithFade(int groupId, float targetVolume, float duration, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取分组实际音量(基础音量×全局音量)
        /// </summary>
        float GetEffectiveVolume(int groupId);

        /// <summary>
        /// 设置全局音量系数
        /// </summary>
        void SetGlobalVolumeFactor(float globalVolume);

        #endregion

        #region 静音控制

        /// <summary>
        /// 获取分组静音状态
        /// </summary>
        bool IsGroupMuted(int groupId);

        /// <summary>
        /// 设置分组静音状态
        /// </summary>
        void SetGroupMuted(int groupId, bool isMuted);

        #endregion

        #region 播放控制

        /// <summary>
        /// 停止分组内所有音频
        /// </summary>
        void StopGroup(int groupId, float fadeDuration);

        /// <summary>
        /// 暂停分组内所有音频
        /// </summary>
        void PauseGroup(int groupId);

        /// <summary>
        /// 恢复分组内所有音频
        /// </summary>
        void ResumeGroup(int groupId);

        #endregion

        #region 批量操作

        /// <summary>
        /// 获取所有分组ID
        /// </summary>
        IEnumerable<int> GetAllGroupIds();

        /// <summary>
        /// 清空所有分组
        /// </summary>
        void ClearAllGroups();

        #endregion
    }
}
