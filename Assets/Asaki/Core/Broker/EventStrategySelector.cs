using System;
using System.Runtime.InteropServices;

namespace Asaki.Core.Broker
{
    /// <summary>
    /// 事件类型策略
    /// </summary>
    public enum EventStrategy
    {
        /// <summary>
        /// 自动选择
        /// </summary>
        Auto,

        /// <summary>
        /// 使用结构体
        /// </summary>
        Struct,

        /// <summary>
        /// 使用类+对象池
        /// </summary>
        ClassPool,
    }

    /// <summary>
    /// 事件策略选择器
    /// </summary>
    public static class EventStrategySelector
    {
        /// <summary>
        /// 类型策略缓存
        /// </summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<
            Type,
            EventStrategy
        > _strategyCache = new();

        /// <summary>
        /// 获取事件类型的策略
        /// </summary>
        public static EventStrategy GetStrategy<T>()
        {
            return GetStrategy(typeof(T));
        }

        /// <summary>
        /// 获取事件类型的策略
        /// </summary>
        public static EventStrategy GetStrategy(Type type)
        {
            return _strategyCache.GetOrAdd(type, t => CalculateStrategy(t));
        }

        /// <summary>
        /// 计算事件类型的策略
        /// </summary>
        private static EventStrategy CalculateStrategy(Type type)
        {
            // 检查特性标记
            if (Attribute.IsDefined(type, typeof(LargeEventAttribute)))
            {
                return EventStrategy.ClassPool;
            }

            if (Attribute.IsDefined(type, typeof(SmallEventAttribute)))
            {
                return EventStrategy.Struct;
            }

            // 如果是类，使用对象池
            if (!type.IsValueType)
            {
                return EventStrategy.ClassPool;
            }

            // 估算结构体大小
            int size = EstimateStructSize(type);

            // 根据大小选择策略
            return size > EventPool.Threshold ? EventStrategy.ClassPool : EventStrategy.Struct;
        }

        /// <summary>
        /// 估算结构体大小（字节）
        /// </summary>
        private static int EstimateStructSize(Type type)
        {
            try
            {
                // 尝试使用 Marshal.SizeOf
                return Marshal.SizeOf(type);
            }
            catch
            {
                // 如果失败，使用字段估算
                return EstimateSizeByFields(type);
            }
        }

        /// <summary>
        /// 通过字段估算大小
        /// </summary>
        private static int EstimateSizeByFields(Type type)
        {
            int size = 0;
            var fields = type.GetFields(
                System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.NonPublic
            );

            foreach (var field in fields)
            {
                size += GetFieldSize(field.FieldType);
            }

            // 至少返回8字节作为保守估计
            return Math.Max(size, 8);
        }

        /// <summary>
        /// 获取字段类型大小
        /// </summary>
        private static int GetFieldSize(Type type)
        {
            if (type == typeof(bool))
                return 1;
            if (type == typeof(byte))
                return 1;
            if (type == typeof(sbyte))
                return 1;
            if (type == typeof(short))
                return 2;
            if (type == typeof(ushort))
                return 2;
            if (type == typeof(char))
                return 2;
            if (type == typeof(int))
                return 4;
            if (type == typeof(uint))
                return 4;
            if (type == typeof(float))
                return 4;
            if (type == typeof(long))
                return 8;
            if (type == typeof(ulong))
                return 8;
            if (type == typeof(double))
                return 8;
            if (type == typeof(decimal))
                return 16;
            if (type.IsEnum)
                return 4;

            // 引用类型
            if (!type.IsValueType)
            {
                return IntPtr.Size; // 4或8字节
            }

            // 嵌套结构体，递归估算
            return EstimateSizeByFields(type);
        }

        /// <summary>
        /// 清除策略缓存
        /// </summary>
        public static void ClearCache()
        {
            _strategyCache.Clear();
        }
    }
}
