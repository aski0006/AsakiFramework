using UnityEngine;

namespace Asaki.Core.Audio
{
    /// <summary>
    /// 标准化音频播放参数包 (纯配置数据)
    /// 该结构体用于存储音频播放所需的各种参数，方便在音频播放逻辑中进行统一配置和管理。
    /// </summary>
    public readonly struct AsakiAudioParams
    {
        /// <summary>
        /// 获取音频播放的 3D 空间坐标。
        /// 用于指定音频在 3D 空间中的位置，影响声音的空间定位效果。
        /// </summary>
        public readonly Vector3 Position;

        /// <summary>
        /// 获取音频的音量。
        /// 音量范围为 0 - 1，0 表示静音，1 表示最大音量。
        /// </summary>
        public readonly float Volume;

        /// <summary>
        /// 获取音频的音调。
        /// 用于调整音频的音高，例如 1 为正常音调，大于 1 提高音调，小于 1 降低音调。
        /// </summary>
        public readonly float Pitch;

        /// <summary>
        /// 获取音频的 2D/3D 混合值。
        /// 0 表示完全 2D 音效，1 表示完全 3D 音效，中间值表示混合效果。
        /// </summary>
        public readonly float SpatialBlend;

        /// <summary>
        /// 获取音频是否循环播放的标志。
        /// true 表示音频将循环播放，false 表示播放一次后停止。
        /// </summary>
        public readonly bool IsLoop;

        /// <summary>
        /// 获取音频播放的优先级。
        /// 值为 0 时表示最高优先级，数值越大优先级越低。
        /// </summary>
        public readonly int Priority;

        /// <summary>
        /// 获取默认的音频参数实例。
        /// 提供了一组常用的默认音频参数，方便在不需要特殊配置时直接使用。
        /// 位置为 Vector3.zero，音量为 1.0f，音调为 1.0f，SpatialBlend 为 0f，不循环，优先级为 128。
        /// </summary>
        public static readonly AsakiAudioParams Default = new AsakiAudioParams(
            Vector3.zero,
            1.0f,
            1.0f,
            0f,
            false,
            128
        );

        // 私有全参构造
        /// <summary>
        /// 使用指定的参数初始化 <see cref="AsakiAudioParams"/> 结构体。
        /// 此构造函数为私有，外部通过提供的 Fluent API 方法来创建具有特定配置的实例。
        /// </summary>
        /// <param name="pos">音频播放的 3D 空间坐标。</param>
        /// <param name="volume">音频的音量，范围为 0 - 1。</param>
        /// <param name="pitch">音频的音调。</param>
        /// <param name="spatialBlend">音频的 2D/3D 混合值，范围为 0 - 1。</param>
        /// <param name="isLoop">音频是否循环播放的标志。</param>
        /// <param name="priority">音频播放的优先级，0 为最高优先级。</param>
        private AsakiAudioParams(
            Vector3 pos,
            float volume,
            float pitch,
            float spatialBlend,
            bool isLoop,
            int priority
        )
        {
            Position = pos;
            Volume = volume;
            Pitch = pitch;
            SpatialBlend = spatialBlend;
            IsLoop = isLoop;
            Priority = priority;
        }

        /// <summary>
        /// 创建一个新的 <see cref="AsakiAudioParams"/> 实例，设置其 3D 位置并将 SpatialBlend 设置为 1。
        /// </summary>
        /// <param name="pos">新的 3D 空间坐标。</param>
        /// <returns>具有新 3D 位置和 SpatialBlend 为 1 的 <see cref="AsakiAudioParams"/> 实例。</returns>
        public AsakiAudioParams Set3D(Vector3 pos)
        {
            return new AsakiAudioParams(pos, Volume, Pitch, 1.0f, IsLoop, Priority);
            // Set3D 隐含 SpatialBlend=1
        }

        /// <summary>
        /// 创建一个新的 <see cref="AsakiAudioParams"/> 实例，设置其音量。
        /// </summary>
        /// <param name="vol">新的音量值，范围为 0 - 1。</param>
        /// <returns>具有新音量的 <see cref="AsakiAudioParams"/> 实例。</returns>
        public AsakiAudioParams SetVolume(float vol)
        {
            return new AsakiAudioParams(Position, vol, Pitch, SpatialBlend, IsLoop, Priority);
        }

        /// <summary>
        /// 创建一个新的 <see cref="AsakiAudioParams"/> 实例，设置其音调。
        /// </summary>
        /// <param name="pitch">新的音调值。</param>
        /// <returns>具有新音调的 <see cref="AsakiAudioParams"/> 实例。</returns>
        public AsakiAudioParams SetPitch(float pitch)
        {
            return new AsakiAudioParams(Position, Volume, pitch, SpatialBlend, IsLoop, Priority);
        }

        /// <summary>
        /// 创建一个新的 <see cref="AsakiAudioParams"/> 实例，设置其循环标志。
        /// </summary>
        /// <param name="loop">新的循环标志，true 表示循环，false 表示不循环。</param>
        /// <returns>具有新循环标志的 <see cref="AsakiAudioParams"/> 实例。</returns>
        public AsakiAudioParams SetLoop(bool loop)
        {
            return new AsakiAudioParams(Position, Volume, Pitch, SpatialBlend, loop, Priority);
        }

        /// <summary>
        /// 创建一个新的 <see cref="AsakiAudioParams"/> 实例，设置其优先级。
        /// </summary>
        /// <param name="priority">新的优先级值，0 为最高优先级。</param>
        /// <returns>具有新优先级的 <see cref="AsakiAudioParams"/> 实例。</returns>
        public AsakiAudioParams SetPriority(int priority)
        {
            return new AsakiAudioParams(Position, Volume, Pitch, SpatialBlend, IsLoop, priority);
        }
        public AsakiAudioParams SetSpatialBlend(float spatialBlend)
        {
            return new AsakiAudioParams(Position, Volume, Pitch, spatialBlend, IsLoop, Priority);
        }

        public AsakiAudioParams SetPosition(Vector3 pos)
        {
            return new AsakiAudioParams(pos, Volume, Pitch, SpatialBlend, IsLoop, Priority);
        }
        public override string ToString()
        {
            return "AsakiAudioParams(" +
                "Position=" + Position +
                ", Volume=" + Volume +
                ", Pitch=" + Pitch +
                ", SpatialBlend=" + SpatialBlend +
                ", IsLoop=" + IsLoop +
                ", Priority=" + Priority +
                ")";
        }
    }
}
