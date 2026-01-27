using System;
using System.Runtime.CompilerServices;

namespace Asaki.Core.Blackboard
{
	/// <summary>
	/// 代表黑板系统中使用的确定性哈希键。
	/// [Core Constraint 1] 强制使用 FNV - 1a 算法替代 string.GetHashCode，确保跨平台/跨进程的哈希一致性。
	/// 此结构体用于唯一标识黑板中的数据项，通过哈希值进行快速查找和比较。
	/// </summary>
	public readonly struct AsakiBlackboardKey : IEquatable<AsakiBlackboardKey>
	{
		/// <summary>
		/// 获取此键的哈希值。
		/// 该哈希值用于在黑板系统中唯一标识数据项，通过 FNV - 1a 算法生成，保证跨平台和跨进程的一致性。
		/// </summary>
		public readonly int Hash;

		#if UNITY_EDITOR
		/// <summary>
		/// 获取此键的调试名称（仅在 Unity 编辑器中可用）。
		/// 用于在编辑器环境下更直观地识别和调试键，在发布版本中该字段不参与实际逻辑。
		/// </summary>
		public readonly string DebugName;
		#endif

		// FNV - 1a 32 - bit Constants
		private const uint FNV_OFFSET_BASIS = 2166136261;
		private const uint FNV_PRIME = 16777619;

		/// <summary>
		/// 使用指定的键名初始化 <see cref="AsakiBlackboardKey"/> 结构体。
		/// 通过 FNV - 1a 算法计算键名的哈希值，并在 Unity 编辑器环境下记录调试名称。
		/// </summary>
		/// <param name="keyName">用于生成哈希值的键名。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public AsakiBlackboardKey(string keyName)
		{
			if (string.IsNullOrEmpty(keyName))
			{
				Hash = 0;
				#if UNITY_EDITOR
				DebugName = "NULL";
				#endif
				return;
			}

			unchecked
			{
				uint hash = FNV_OFFSET_BASIS;
				// 手动遍历字符字节，避免依赖平台特定的 string 实现
				for (int i = 0; i < keyName.Length; i++)
				{
					hash ^= keyName[i];
					hash *= FNV_PRIME;
				}
				Hash = (int)hash;
			}

			#if UNITY_EDITOR
			DebugName = keyName;
			#endif
		}

		/// <summary>
		/// 使用指定的哈希值初始化 <see cref="AsakiBlackboardKey"/> 结构体。
		/// 在 Unity 编辑器环境下，将哈希值转换为字符串作为调试名称。
		/// </summary>
		/// <param name="hash">此键的哈希值。</param>
		public AsakiBlackboardKey(int hash)
		{
			Hash = hash;
			#if UNITY_EDITOR
			DebugName = hash.ToString();
			#endif
		}

		/// <summary>
		/// 提供从字符串到 <see cref="AsakiBlackboardKey"/> 的隐式转换。
		/// 方便在需要使用 <see cref="AsakiBlackboardKey"/> 的地方直接传入字符串，自动创建对应的键。
		/// </summary>
		/// <param name="name">要转换的字符串。</param>
		/// <returns>由字符串生成的 <see cref="AsakiBlackboardKey"/> 实例。</returns>
		public static implicit operator AsakiBlackboardKey(string name)
		{
			return new AsakiBlackboardKey(name);
		}

		/// <summary>
		/// 提供从整数到 <see cref="AsakiBlackboardKey"/> 的隐式转换。
		/// 方便在需要使用 <see cref="AsakiBlackboardKey"/> 的地方直接传入整数哈希值，自动创建对应的键。
		/// </summary>
		/// <param name="hash">要转换的整数哈希值。</param>
		/// <returns>由整数哈希值生成的 <see cref="AsakiBlackboardKey"/> 实例。</returns>
		public static implicit operator AsakiBlackboardKey(int hash)
		{
			return new AsakiBlackboardKey(hash);
		}

		/// <summary>
		/// 判断当前 <see cref="AsakiBlackboardKey"/> 实例与另一个 <see cref="AsakiBlackboardKey"/> 实例是否相等。
		/// 两个实例相等当且仅当它们的哈希值相等。
		/// </summary>
		/// <param name="other">要比较的另一个 <see cref="AsakiBlackboardKey"/> 实例。</param>
		/// <returns>如果两个实例的哈希值相等，则返回 true；否则返回 false。</returns>
		public bool Equals(AsakiBlackboardKey other)
		{
			return Hash == other.Hash;
		}

		/// <summary>
		/// 判断当前 <see cref="AsakiBlackboardKey"/> 实例与指定对象是否相等。
		/// 如果对象是 <see cref="AsakiBlackboardKey"/> 类型，则进一步比较哈希值；否则返回 false。
		/// </summary>
		/// <param name="obj">要比较的对象。</param>
		/// <returns>如果对象与当前实例相等，则返回 true；否则返回 false。</returns>
		public override bool Equals(object obj)
		{
			return obj is AsakiBlackboardKey other && Equals(other);
		}

		/// <summary>
		/// 返回此 <see cref="AsakiBlackboardKey"/> 实例的哈希代码。
		/// 由于哈希值已经作为结构体的成员，直接返回该哈希值即可。
		/// </summary>
		/// <returns>此实例的哈希代码，即 <see cref="Hash"/>。</returns>
		public override int GetHashCode()
		{
			return Hash;
		}

		/// <summary>
		/// 判断两个 <see cref="AsakiBlackboardKey"/> 实例是否相等。
		/// 两个实例相等当且仅当它们的哈希值相等。
		/// </summary>
		/// <param name="left">要比较的第一个 <see cref="AsakiBlackboardKey"/> 实例。</param>
		/// <param name="right">要比较的第二个 <see cref="AsakiBlackboardKey"/> 实例。</param>
		/// <returns>如果两个实例的哈希值相等，则返回 true；否则返回 false。</returns>
		public static bool operator ==(AsakiBlackboardKey left, AsakiBlackboardKey right)
		{
			return left.Hash == right.Hash;
		}

		/// <summary>
		/// 判断两个 <see cref="AsakiBlackboardKey"/> 实例是否不相等。
		/// 两个实例不相等当且仅当它们的哈希值不相等。
		/// </summary>
		/// <param name="left">要比较的第一个 <see cref="AsakiBlackboardKey"/> 实例。</param>
		/// <param name="right">要比较的第二个 <see cref="AsakiBlackboardKey"/> 实例。</param>
		/// <returns>如果两个实例的哈希值不相等，则返回 true；否则返回 false。</returns>
		public static bool operator !=(AsakiBlackboardKey left, AsakiBlackboardKey right)
		{
			return left.Hash != right.Hash;
		}

		/// <summary>
		/// 返回此 <see cref="AsakiBlackboardKey"/> 实例的字符串表示形式。
		/// 在 Unity 编辑器中，如果调试名称不为空，则返回调试名称及哈希值；否则仅返回哈希值。
		/// 在非编辑器环境下，仅返回哈希值。
		/// </summary>
		/// <returns>此实例的字符串表示形式。</returns>
		public override string ToString()
		{
			#if UNITY_EDITOR
			return string.IsNullOrEmpty(DebugName) ? Hash.ToString() : $"{DebugName} ({Hash})";
			#else
            return Hash.ToString();
			#endif
		}
	}
}
