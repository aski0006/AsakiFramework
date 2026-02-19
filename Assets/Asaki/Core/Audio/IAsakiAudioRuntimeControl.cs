using UnityEngine;

namespace Asaki.Core.Audio
{
    /// <summary>
    /// 音频运行时控制接口
    /// <para>提供对单个音频实例的运行时参数调整功能。</para>
    /// <para>适用于动态音效、3D音效位置跟踪等场景。</para>
    /// </summary>
    /// <author>Asaki Framework</author>
    /// <version>2.0</version>
    public interface IAsakiAudioRuntimeControl
    {
        /// <summary>
        /// 设置音量
        /// </summary>
        /// <param name="handle">音频句柄</param>
        /// <param name="volume">音量值(0-1)</param>
        void SetVolume(AsakiAudioHandle handle, float volume);

        /// <summary>
        /// 设置音调
        /// </summary>
        /// <param name="handle">音频句柄</param>
        /// <param name="pitch">音调值</param>
        void SetPitch(AsakiAudioHandle handle, float pitch);

        /// <summary>
        /// 设置2D/3D混合值
        /// </summary>
        /// <param name="handle">音频句柄</param>
        /// <param name="spatialBlend">混合值(0=2D, 1=3D)</param>
        void SetSpatialBlend(AsakiAudioHandle handle, float spatialBlend);

        /// <summary>
        /// 设置3D位置
        /// </summary>
        /// <param name="handle">音频句柄</param>
        /// <param name="position">世界坐标</param>
        void SetPosition(AsakiAudioHandle handle, Vector3 position);

        /// <summary>
        /// 设置循环状态
        /// </summary>
        /// <param name="handle">音频句柄</param>
        /// <param name="isLoop">是否循环</param>
        void SetLoop(AsakiAudioHandle handle, bool isLoop);

        /// <summary>
        /// 设置静音状态
        /// </summary>
        /// <param name="handle">音频句柄</param>
        /// <param name="isMuted">是否静音</param>
        void SetMuted(AsakiAudioHandle handle, bool isMuted);

        /// <summary>
        /// 设置优先级
        /// </summary>
        /// <param name="handle">音频句柄</param>
        /// <param name="priority">优先级(0最高)</param>
        void SetPriority(AsakiAudioHandle handle, int priority);
    }
}
