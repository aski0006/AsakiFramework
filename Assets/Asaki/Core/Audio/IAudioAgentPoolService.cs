using System.Threading;
using UnityEngine;

namespace Asaki.Core.Audio
{
    /// <summary>
    /// 音频代理池服务接口
    /// <para>负责音频代理对象的生命周期管理，包括池化、借用和归还。</para>
    /// </summary>
    /// <author>Asaki Framework</author>
    /// <version>2.0</version>
    public interface IAudioAgentPoolService
    {
        /// <summary>
        /// 初始化代理池
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>初始化任务</returns>
        System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 从池中借用代理
        /// </summary>
        /// <param name="parent">父节点(用于3D音效定位)，为null则保持在池根节点下</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>音频代理实例</returns>
        Cysharp.Threading.Tasks.UniTask<IAudioAgent> BorrowAsync(
            Transform parent,
            CancellationToken cancellationToken
        );

        /// <summary>
        /// 归还代理到池
        /// </summary>
        /// <param name="agent">音频代理实例</param>
        void Return(IAudioAgent agent);

        /// <summary>
        /// 释放池资源
        /// </summary>
        void Dispose();

        /// <summary>
        /// 获取池统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        string GetStatistics();
    }

    /// <summary>
    /// 音频代理接口
    /// <para>封装单个音频播放实例的行为。</para>
    /// </summary>
    public interface IAudioAgent
    {
        /// <summary>当前播放状态</summary>
        AudioPlaybackState State { get; }

        /// <summary>是否正在播放</summary>
        bool IsPlaying { get; }

        /// <summary>是否已暂停</summary>
        bool IsPaused { get; }

        /// <summary>是否处于活跃状态</summary>
        bool IsActive { get; }

        /// <summary>是否处于错误状态</summary>
        bool IsError { get; }

        /// <summary>当前音频路径</summary>
        string CurrentAudioPath { get; }

        /// <summary>Transform组件</summary>
        Transform Transform { get; }

        /// <summary>
        /// 播放音频
        /// </summary>
        /// <param name="resourcePath">资源路径</param>
        /// <param name="parameters">播放参数</param>
        /// <param name="resourceService">资源服务</param>
        /// <param name="cancellationToken">取消令牌</param>
        Cysharp.Threading.Tasks.UniTask PlayAsync(
            string resourcePath,
            AsakiAudioParams parameters,
            Resources.IAsakiResourceService resourceService,
            CancellationToken cancellationToken
        );

        /// <summary>
        /// 暂停播放
        /// </summary>
        /// <returns>是否成功</returns>
        bool Pause();

        /// <summary>
        /// 恢复播放
        /// </summary>
        /// <returns>是否成功</returns>
        bool Resume();

        /// <summary>
        /// 停止播放
        /// </summary>
        /// <param name="fadeDuration">淡出时长</param>
        /// <returns>是否成功</returns>
        bool Stop(float fadeDuration);

        /// <summary>
        /// 立即停止
        /// </summary>
        void StopImmediate();

        /// <summary>
        /// 设置音量
        /// </summary>
        void SetVolume(float volume);

        /// <summary>
        /// 设置音调
        /// </summary>
        void SetPitch(float pitch);

        /// <summary>
        /// 设置位置
        /// </summary>
        void SetPosition(Vector3 position);

        /// <summary>
        /// 设置循环
        /// </summary>
        void SetLoop(bool isLoop);

        /// <summary>
        /// 设置静音
        /// </summary>
        void SetMuted(bool isMuted);

        /// <summary>
        /// 设置优先级
        /// </summary>
        void SetPriority(int priority);

        /// <summary>
        /// 设置空间混合值
        /// </summary>
        void SetSpatialBlend(float spatialBlend);
    }
}
