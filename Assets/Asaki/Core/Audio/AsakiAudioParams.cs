using UnityEngine;

namespace Asaki.Core.Audio
{
    /// <summary>
    /// 音频播放参数包
    /// <para>不可变结构体，存储音频播放所需的所有参数。</para>
    /// <para>通过Fluent API创建配置实例。</para>
    /// </summary>
    /// <author>Asaki Framework</author>
    /// <version>2.0</version>
    public readonly struct AsakiAudioParams
    {
        /// <summary>3D空间坐标</summary>
        public readonly Vector3 Position;

        /// <summary>音量(0-1)</summary>
        public readonly float Volume;

        /// <summary>音调</summary>
        public readonly float Pitch;

        /// <summary>2D/3D混合值(0=2D, 1=3D)</summary>
        public readonly float SpatialBlend;

        /// <summary>是否循环</summary>
        public readonly bool IsLoop;

        /// <summary>优先级(0最高)</summary>
        public readonly int Priority;

        /// <summary>
        /// 默认参数实例
        /// </summary>
        public static readonly AsakiAudioParams Default = new AsakiAudioParams(
            Vector3.zero,
            AudioConstants.DefaultVolume,
            AudioConstants.DefaultPitch,
            AudioConstants.DefaultSpatialBlend,
            false,
            AudioConstants.DefaultPriority
        );

        private AsakiAudioParams(
            Vector3 position,
            float volume,
            float pitch,
            float spatialBlend,
            bool isLoop,
            int priority
        )
        {
            Position = position;
            Volume = volume;
            Pitch = pitch;
            SpatialBlend = spatialBlend;
            IsLoop = isLoop;
            Priority = priority;
        }

        /// <summary>
        /// 设置3D位置(自动设置SpatialBlend=1)
        /// </summary>
        /// <param name="position">世界坐标</param>
        /// <returns>新参数实例</returns>
        public AsakiAudioParams Set3D(Vector3 position)
        {
            return new AsakiAudioParams(
                position,
                Volume,
                Pitch,
                AudioConstants.Full3D,
                IsLoop,
                Priority
            );
        }

        /// <summary>
        /// 设置音量
        /// </summary>
        /// <param name="volume">音量值(0-1)</param>
        /// <returns>新参数实例</returns>
        public AsakiAudioParams SetVolume(float volume)
        {
            return new AsakiAudioParams(Position, volume, Pitch, SpatialBlend, IsLoop, Priority);
        }

        /// <summary>
        /// 设置音调
        /// </summary>
        /// <param name="pitch">音调值</param>
        /// <returns>新参数实例</returns>
        public AsakiAudioParams SetPitch(float pitch)
        {
            return new AsakiAudioParams(Position, Volume, pitch, SpatialBlend, IsLoop, Priority);
        }

        /// <summary>
        /// 设置循环状态
        /// </summary>
        /// <param name="isLoop">是否循环</param>
        /// <returns>新参数实例</returns>
        public AsakiAudioParams SetLoop(bool isLoop)
        {
            return new AsakiAudioParams(Position, Volume, Pitch, SpatialBlend, isLoop, Priority);
        }

        /// <summary>
        /// 设置优先级
        /// </summary>
        /// <param name="priority">优先级(0最高)</param>
        /// <returns>新参数实例</returns>
        public AsakiAudioParams SetPriority(int priority)
        {
            return new AsakiAudioParams(Position, Volume, Pitch, SpatialBlend, IsLoop, priority);
        }

        /// <summary>
        /// 设置空间混合值
        /// </summary>
        /// <param name="spatialBlend">混合值(0=2D, 1=3D)</param>
        /// <returns>新参数实例</returns>
        public AsakiAudioParams SetSpatialBlend(float spatialBlend)
        {
            return new AsakiAudioParams(Position, Volume, Pitch, spatialBlend, IsLoop, Priority);
        }

        /// <summary>
        /// 设置位置
        /// </summary>
        /// <param name="position">世界坐标</param>
        /// <returns>新参数实例</returns>
        public AsakiAudioParams SetPosition(Vector3 position)
        {
            return new AsakiAudioParams(position, Volume, Pitch, SpatialBlend, IsLoop, Priority);
        }

        public override string ToString()
        {
            return $"AsakiAudioParams(Volume={Volume:F2}, Pitch={Pitch:F2}, "
                + $"SpatialBlend={SpatialBlend:F2}, IsLoop={IsLoop}, Priority={Priority})";
        }
    }
}
