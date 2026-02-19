using System;

namespace Asaki.Core.Audio
{
    /// <summary>
    /// 音频句柄
    /// <para>轻量级标识符，用于引用和管理音频播放实例。</para>
    /// <para>不可变结构体，通过唯一ID和时间戳确保有效性。</para>
    /// </summary>
    /// <author>Asaki Framework</author>
    /// <version>2.0</version>
    public readonly struct AsakiAudioHandle : IEquatable<AsakiAudioHandle>
    {
        /// <summary>唯一标识ID</summary>
        public readonly int Id;

        /// <summary>创建时间戳(帧号)</summary>
        public readonly int Timestamp;

        /// <summary>无效句柄</summary>
        public static readonly AsakiAudioHandle Invalid = new AsakiAudioHandle(0, 0);

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">唯一ID</param>
        /// <param name="timestamp">时间戳</param>
        public AsakiAudioHandle(int id, int timestamp)
        {
            Id = id;
            Timestamp = timestamp;
        }

        /// <summary>是否有效(ID不为0)</summary>
        public bool IsValid => Id != 0;

        public override bool Equals(object obj)
        {
            return obj is AsakiAudioHandle other && Equals(other);
        }

        public bool Equals(AsakiAudioHandle other)
        {
            return Id == other.Id && Timestamp == other.Timestamp;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Timestamp);
        }

        public static bool operator ==(AsakiAudioHandle left, AsakiAudioHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AsakiAudioHandle left, AsakiAudioHandle right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"AudioHandle(Id={Id}, Timestamp={Timestamp})";
        }
    }
}
