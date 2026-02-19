using System.Threading;
using Asaki.Core.Context;
using UnityEngine;

namespace Asaki.Core.Audio
{
    /// <summary>
    /// 音频服务门户接口
    /// <para>组合所有音频子接口，提供统一的音频服务入口。</para>
    /// <para>作为外观模式(Facade)的顶层接口，将请求分发到内部子服务执行。</para>
    /// </summary>
    /// <author>Asaki Framework</author>
    /// <version>2.0</version>
    public interface IAsakiAudioService
        : IAsakiAudioPlayer,
            IAsakiAudioGlobalControl,
            IAsakiAudioGroupControl,
            IAsakiAudioRuntimeControl
    {
        /// <summary>
        /// 获取音频播放状态
        /// </summary>
        /// <param name="handle">音频句柄</param>
        /// <returns>播放状态</returns>
        AudioPlaybackState GetState(AsakiAudioHandle handle);

        /// <summary>
        /// 判断音频是否处于活跃状态
        /// </summary>
        /// <param name="handle">音频句柄</param>
        /// <returns>是否活跃</returns>
        bool IsActive(AsakiAudioHandle handle);

        /// <summary>
        /// 判断音频是否处于错误状态
        /// </summary>
        /// <param name="handle">音频句柄</param>
        /// <returns>是否错误</returns>
        bool IsError(AsakiAudioHandle handle);

        /// <summary>
        /// 获取当前音量
        /// </summary>
        /// <param name="handle">音频句柄</param>
        /// <returns>音量值</returns>
        float GetCurrentVolume(AsakiAudioHandle handle);

        /// <summary>
        /// 获取当前音调
        /// </summary>
        /// <param name="handle">音频句柄</param>
        /// <returns>音调值</returns>
        float GetCurrentPitch(AsakiAudioHandle handle);

        /// <summary>
        /// 获取当前位置
        /// </summary>
        /// <param name="handle">音频句柄</param>
        /// <returns>位置坐标</returns>
        Vector3 GetPosition(AsakiAudioHandle handle);

        /// <summary>
        /// 获取池统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        string GetPoolStatistics();

        /// <summary>
        /// 获取状态统计信息
        /// </summary>
        /// <returns>状态统计</returns>
        AudioStateStatistics GetStateStatistics();
    }
}
