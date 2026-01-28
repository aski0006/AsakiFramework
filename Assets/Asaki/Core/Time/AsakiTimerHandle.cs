using System;

namespace Asaki.Core.Time
{
    /// <summary>
    /// [Asaki Native] 定时器句柄 (值类型)
    /// 用于取消或暂停定时器，无需持有对象引用。
    /// </summary>
    public readonly struct AsakiTimerHandle : IEquatable<AsakiTimerHandle>
    {
        public readonly int Id; // 唯一 ID
        public readonly ulong Version; // 版本号 (解决 ID 复用导致的"错误的取消"问题)

        public AsakiTimerHandle(int id, ulong version)
        {
            Id = id;
            Version = version;
        }

        public static AsakiTimerHandle Invalid => new AsakiTimerHandle(0, 0);

        public bool Equals(AsakiTimerHandle other)
        {
            return Id == other.Id && Version == other.Version;
        }

        public override bool Equals(object obj)
        {
            return obj is AsakiTimerHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Version);
        }

        public static bool operator ==(AsakiTimerHandle left, AsakiTimerHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AsakiTimerHandle left, AsakiTimerHandle right)
        {
            return !left.Equals(right);
        }
    }
}
