using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Asaki.Unity.Utils
{
    /// <summary>
    /// [工具类] StringBuilder 对象池。
    /// 用于避免字符串拼接时的临时内存分配。
    /// </summary>
    public static class AsakiStringBuilderPool
    {
        // 使用 Stack 配合 lock 实现轻量级线程安全
        private static readonly Stack<StringBuilder> _pool = new Stack<StringBuilder>(32);
        private static readonly object _lock = new object();

        // 扩容限制提升至 64KB (覆盖绝大多数游戏内 JSON)
        // 只有超过 64KB 的超大包才会被 GC 此时 GC 是合理的代价
        private const int MAX_RETAIN_CAPACITY = 64 * 1024;

        public static StringBuilder Rent()
        {
            lock (_lock)
            {
                if (_pool.Count > 0)
                {
                    return _pool.Pop();
                }
            }
            return new StringBuilder(1024); // 初始容量提升，减少初期扩容次数
        }

        public static void Return(StringBuilder sb)
        {
            if (sb == null)
                return;

            // 策略调整：如果容量过大，尝试通过 Capacity 属性缩容 (.NET 优化)
            // 或者直接丢弃以释放大块内存
            if (sb.Capacity > MAX_RETAIN_CAPACITY)
            {
                // 选项 A: 丢弃 (防止大内存长期驻留) -> 选这个，简单安全
                return;

                // 选项 B: 强制缩容 (取决于 .NET 版本实现，有时不一定释放内存)
                // sb.Capacity = MAX_RETAIN_CAPACITY;
            }

            sb.Clear();

            lock (_lock)
            {
                _pool.Push(sb);
            }
        }

        /// <summary>
        /// 快捷方法：借出 -> 转换 -> 归还
        /// </summary>
        public static string GetStringAndRelease(StringBuilder sb)
        {
            string result = sb.ToString();
            Return(sb);
            return result;
        }

        /// <summary>
        /// 直接将 StringBuilder 内容编码为 UTF8 字节数组，避免产生 string 对象。
        /// </summary>
        public static byte[] GetBytesAndRelease(StringBuilder sb)
        {
            try
            {
                if (sb.Length == 0)
                    return Array.Empty<byte>();

                // 1. 获取字符数组缓冲区 (避免 new char[])
                // 这里的 ArrayPool<char>.Shared 是 .NET Standard 2.1 自带的神器
                char[] charBuffer = ArrayPool<char>.Shared.Rent(sb.Length);

                try
                {
                    // 2. 将 StringBuilder 内容复制到 char 数组
                    sb.CopyTo(0, charBuffer, 0, sb.Length);

                    // 3. 编码为 byte[]
                    // 注意：这里依然会产生一个 byte[]，这是 UnityWebRequest.uploadHandler 必须的
                    // 但我们省去了中间那个巨大的 string 对象的分配！
                    return Encoding.UTF8.GetBytes(charBuffer, 0, sb.Length);
                }
                finally
                {
                    // 归还 char 数组
                    ArrayPool<char>.Shared.Return(charBuffer);
                }
            }
            finally
            {
                // 归还 StringBuilder
                Return(sb);
            }
        }
    }
}
