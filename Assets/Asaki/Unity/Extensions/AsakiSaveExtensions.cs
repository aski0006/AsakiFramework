using System;
using System.Collections.Generic;
using Asaki.Core.Serialization;
using UnityEngine;

namespace Asaki.Unity.Extensions
{
    /// <summary>
    /// [Asaki Kernel] 泛型类型分发器 (V5 Native Edition)
    /// 彻底移除 System.Reflection 依赖，利用接口多态与 JIT 常量折叠实现高性能分发。
    /// </summary>
    public static class AsakiTypeDispatcher
    {
        // ========================================================================
        // 写操作
        // ========================================================================
        public static void Write<T>(IAsakiWriter writer, string key, T value)
        {
            // [JIT 优化] typeof(T) 在运行时是常量，这些分支会被 JIT 消除，等同于直接调用
            if (typeof(T) == typeof(int))
                writer.WriteInt(key, (int)(object)value);
            else if (typeof(T) == typeof(float))
                writer.WriteFloat(key, (float)(object)value);
            else if (typeof(T) == typeof(string))
                writer.WriteString(key, (string)(object)value);
            else if (typeof(T) == typeof(bool))
                writer.WriteBool(key, (bool)(object)value);
            else if (typeof(T) == typeof(long))
                writer.WriteLong(key, (long)(object)value);
            else if (typeof(T) == typeof(Vector3))
                writer.WriteVector3(key, (Vector3)(object)value);
            else if (typeof(T) == typeof(Vector2))
                writer.WriteVector2(key, (Vector2)(object)value);
            else if (typeof(T) == typeof(Vector2Int))
                writer.WriteVector2Int(key, (Vector2Int)(object)value);
            else if (typeof(T) == typeof(Vector3Int))
                writer.WriteVector3Int(key, (Vector3Int)(object)value);
            // [接口检测] 使用 is 运算符，无需反射
            else if (value is IAsakiSavable savable)
            {
                // 调用新增的弱类型接口
                writer.WriteObject(key, savable);
            }
            else
            {
                throw new NotSupportedException(
                    $"[Asaki] Unsupported type in Generic Container: {typeof(T).Name}"
                );
            }
        }

        // ========================================================================
        // 读操作
        // ========================================================================
        public static T Read<T>(IAsakiReader reader, string key)
        {
            // [JIT 优化]
            if (typeof(T) == typeof(int))
                return (T)(object)reader.ReadInt(key);
            if (typeof(T) == typeof(float))
                return (T)(object)reader.ReadFloat(key);
            if (typeof(T) == typeof(string))
                return (T)(object)reader.ReadString(key);
            if (typeof(T) == typeof(bool))
                return (T)(object)reader.ReadBool(key);
            if (typeof(T) == typeof(long))
                return (T)(object)reader.ReadLong(key);
            if (typeof(T) == typeof(Vector3))
                return (T)(object)reader.ReadVector3(key);
            if (typeof(T) == typeof(Vector2))
                return (T)(object)reader.ReadVector2(key);
            if (typeof(T) == typeof(Vector2Int))
                return (T)(object)reader.ReadVector2Int(key);
            if (typeof(T) == typeof(Vector3Int))
                return (T)(object)reader.ReadVector3Int(key);

            // [类型检测] 这里的 IsAssignableFrom 是唯一的运行时类型元数据操作 (RTTI)，不属于重度反射
            if (typeof(IAsakiSavable).IsAssignableFrom(typeof(T)))
            {
                // [核心解法] 调用新增的弱类型接口，传入 Type，绕过泛型约束
                object result = reader.ReadObject(key, typeof(T));
                return (T)result;
            }

            throw new NotSupportedException(
                $"[Asaki] Unsupported type in Generic Container: {typeof(T).Name}"
            );
        }
    }

    /// <summary>
    /// 支持 Asaki 序列化的字典容器。
    /// 建议：对于高频读写的数据，直接继承 Dictionary 或实现 IDictionary 接口以保持 API 一致性。
    /// </summary>
    public class AsakiSaveDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IAsakiSavable
        where TValue : new() // 如果 TValue 是对象，通常需要 new() 约束
    // 注意：TKey 也可以是对象，但通常建议用 string 或 int
    {
        public AsakiSaveDictionary() { }

        public AsakiSaveDictionary(int capacity)
            : base(capacity) { }

        public void Serialize(IAsakiWriter writer)
        {
            // 1. 写入数量
            writer.BeginList("Entries", Count);

            // 2. 遍历写入键值对
            // 为了保证二进制紧凑，我们不开启 Object 结构，而是平铺写入：Key, Value, Key, Value...
            foreach (var kvp in this)
            {
                // 写入 Key
                AsakiTypeDispatcher.Write(writer, "k", kvp.Key);
                // 写入 Value
                AsakiTypeDispatcher.Write(writer, "v", kvp.Value);
            }

            writer.EndList();
        }

        public void Deserialize(IAsakiReader reader)
        {
            Clear();

            // 1. 读取数量
            int count = reader.BeginList("Entries");

            // 2. 循环读取
            for (int i = 0; i < count; i++)
            {
                // 顺序必须与 Serialize 严格一致
                TKey key = AsakiTypeDispatcher.Read<TKey>(reader, "k");
                TValue value = AsakiTypeDispatcher.Read<TValue>(reader, "v");

                if (key != null) // 防止 Key 为 null 导致崩溃
                {
                    this[key] = value;
                }
            }

            reader.EndList();
        }
    }

    public class AsakiSaveHashSet<T> : HashSet<T>, IAsakiSavable
        where T : new()
    {
        public void Serialize(IAsakiWriter writer)
        {
            writer.BeginList("Items", Count);
            foreach (T item in this)
            {
                AsakiTypeDispatcher.Write(writer, "i", item);
            }
            writer.EndList();
        }

        public void Deserialize(IAsakiReader reader)
        {
            Clear();
            int count = reader.BeginList("Items");
            for (int i = 0; i < count; i++)
            {
                T item = AsakiTypeDispatcher.Read<T>(reader, "i");
                Add(item);
            }
            reader.EndList();
        }
    }
}
