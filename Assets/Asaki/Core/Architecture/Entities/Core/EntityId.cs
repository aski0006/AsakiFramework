using System;

namespace Asaki.Core.Architecture.Entities
{
    /// <summary>
    /// 实体标识符 - 使用魔法容器句柄 + 代际验证
    /// </summary>
    public readonly struct EntityId : IEquatable<EntityId>
    {
        /// <summary>
        /// 魔法容器句柄（索引）
        /// </summary>
        public readonly int Handle;

        /// <summary>
        /// 代际计数器 - 防止 ABA 问题
        /// </summary>
        public readonly int Generation;

        /// <summary>
        /// 创建实体标识符
        /// </summary>
        /// <param name="handle">魔法容器句柄</param>
        /// <param name="generation">代际计数</param>
        public EntityId(int handle, int generation)
        {
            Handle = handle;
            Generation = generation;
        }

        /// <summary>
        /// 是否为有效ID
        /// </summary>
        public bool IsValid => Handle >= 0;

        /// <summary>
        /// 无效实体ID
        /// </summary>
        public static readonly EntityId Invalid = new EntityId(-1, 0);

        /// <summary>
        /// 等于比较
        /// </summary>
        public bool Equals(EntityId other)
        {
            return Handle == other.Handle && Generation == other.Generation;
        }

        /// <summary>
        /// 等于比较
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is EntityId other && Equals(other);
        }

        /// <summary>
        /// 获取哈希码
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(Handle, Generation);
        }

        /// <summary>
        /// 相等运算符
        /// </summary>
        public static bool operator ==(EntityId left, EntityId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 不等运算符
        /// </summary>
        public static bool operator !=(EntityId left, EntityId right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// 字符串表示
        /// </summary>
        public override string ToString()
        {
            return $"Entity({Handle}:{Generation})";
        }
    }
}
