using System;

namespace Asaki.Core.Audio
{
	/// <summary>
	/// 表示音频处理的句柄结构。
	/// 该句柄用于标识和管理音频相关操作，通过唯一 ID 和时间戳确保其有效性，防止回收后误操作。
	/// </summary>
	public readonly struct AsakiAudioHandle : IEquatable<AsakiAudioHandle>
	{
		/// <summary>
		/// 获取句柄的唯一 ID。
		/// 此 ID 用于在系统中唯一标识音频句柄，以进行特定音频操作的定位和管理。
		/// </summary>
		public readonly int Id;
		/// <summary>
		/// 获取句柄的创建时间戳。
		/// 该时间戳用于校验句柄的有效性，防止在句柄被回收后进行误操作。
		/// </summary>
		public readonly int Timestamp;

		/// <summary>
		/// 表示无效的音频句柄。
		/// 其 ID 和时间戳均为 0，可用于初始化或表示未初始化的句柄状态。
		/// </summary>
		public static readonly AsakiAudioHandle Invalid = new AsakiAudioHandle(0, 0);

		/// <summary>
		/// 使用指定的 ID 和时间戳初始化 <see cref="AsakiAudioHandle"/> 结构。
		/// </summary>
		/// <param name="id">句柄的唯一 ID。</param>
		/// <param name="timestamp">句柄的创建时间戳。</param>
		public AsakiAudioHandle(int id, int timestamp)
		{
			Id = id;
			Timestamp = timestamp;
		}

		/// <summary>
		/// 获取一个值，指示该句柄是否有效。
		/// 如果句柄的 ID 不为 0，则认为该句柄是有效的。
		/// </summary>
		public bool IsValid => Id != 0;

		/// <summary>
		/// 判断当前 <see cref="AsakiAudioHandle"/> 实例与指定对象是否相等。
		/// </summary>
		/// <param name="obj">要比较的对象。如果该对象是 <see cref="AsakiAudioHandle"/> 类型，则继续比较；否则返回 false。</param>
		/// <returns>如果对象相等则返回 true，否则返回 false。</returns>
		public override bool Equals(object obj)
		{
			if (obj is AsakiAudioHandle other)
			{
				return Equals(other);
			}
			return false;
		}

		/// <summary>
		/// 判断当前 <see cref="AsakiAudioHandle"/> 实例与另一个 <see cref="AsakiAudioHandle"/> 实例是否相等。
		/// </summary>
		/// <param name="other">要比较的另一个 <see cref="AsakiAudioHandle"/> 实例。</param>
		/// <returns>如果两个实例的 ID 和时间戳都相等，则返回 true；否则返回 false。</returns>
		public bool Equals(AsakiAudioHandle other)
		{
			return Id == other.Id && Timestamp == other.Timestamp;
		}

		/// <summary>
		/// 返回此 <see cref="AsakiAudioHandle"/> 实例的哈希代码。
		/// 该哈希代码由 ID 和时间戳组合生成，用于在哈希表等集合中高效地进行查找和比较。
		/// </summary>
		/// <returns>基于 ID 和时间戳生成的哈希代码。</returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(Id, Timestamp);
		}

		/// <summary>
		/// 判断两个 <see cref="AsakiAudioHandle"/> 实例是否相等。
		/// </summary>
		/// <param name="left">要比较的第一个 <see cref="AsakiAudioHandle"/> 实例。</param>
		/// <param name="right">要比较的第二个 <see cref="AsakiAudioHandle"/> 实例。</param>
		/// <returns>如果两个实例相等则返回 true，否则返回 false。</returns>
		public static bool operator ==(AsakiAudioHandle left, AsakiAudioHandle right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// 判断两个 <see cref="AsakiAudioHandle"/> 实例是否不相等。
		/// </summary>
		/// <param name="left">要比较的第一个 <see cref="AsakiAudioHandle"/> 实例。</param>
		/// <param name="right">要比较的第二个 <see cref="AsakiAudioHandle"/> 实例。</param>
		/// <returns>如果两个实例不相等则返回 true，否则返回 false。</returns>
		public static bool operator !=(AsakiAudioHandle left, AsakiAudioHandle right)
		{
			return !left.Equals(right);
		}
	}
}
