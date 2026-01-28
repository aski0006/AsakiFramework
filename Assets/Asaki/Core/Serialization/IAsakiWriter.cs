using System;
using System.IO;
using UnityEngine;

namespace Asaki.Core.Serialization
{
    /// <summary>
    /// 定义序列化写入器的接口，负责将各种类型的数据写入到序列化流中。
    /// </summary>
    /// <remarks>
    /// 此接口提供了一系列方法来写入不同类型的数据，包括基本类型、Unity数学类型、复杂对象和集合。
    /// 实现此接口的类可以支持不同的序列化格式，如二进制、JSON等。
    /// </remarks>
    public interface IAsakiWriter
    {
        /// <summary>
        /// 写入序列化数据的版本号。
        /// </summary>
        /// <param name="version">版本号，用于支持版本兼容和迁移。</param>
        /// <remarks>
        /// 版本号应该在序列化数据的开头写入，以便在反序列化时可以根据版本号处理不同的格式。
        /// 这对于实现向后兼容的序列化系统至关重要。
        /// </remarks>
        void WriteVersion(int version);

        // --- 基础类型 ---
        /// <summary>
        /// 写入一个字节值。
        /// </summary>
        /// <param name="key">数据的键名，用于标识此值。</param>
        /// <param name="value">要写入的字节值。</param>
        void WriteByte(string key, byte value);

        /// <summary>
        /// 写入一个32位整数值。
        /// </summary>
        /// <param name="key">数据的键名，用于标识此值。</param>
        /// <param name="value">要写入的32位整数值。</param>
        void WriteInt(string key, int value);

        /// <summary>
        /// 写入一个64位整数值。
        /// </summary>
        /// <param name="key">数据的键名，用于标识此值。</param>
        /// <param name="value">要写入的64位整数值。</param>
        void WriteLong(string key, long value);

        /// <summary>
        /// 写入一个单精度浮点数值。
        /// </summary>
        /// <param name="key">数据的键名，用于标识此值。</param>
        /// <param name="value">要写入的单精度浮点数值。</param>
        void WriteFloat(string key, float value);

        /// <summary>
        /// 写入一个双精度浮点数值。
        /// </summary>
        /// <param name="key">数据的键名，用于标识此值。</param>
        /// <param name="value">要写入的双精度浮点数值。</param>
        void WriteDouble(string key, double value);

        /// <summary>
        /// 写入一个字符串值。
        /// </summary>
        /// <param name="key">数据的键名，用于标识此值。</param>
        /// <param name="value">要写入的字符串值。</param>
        void WriteString(string key, string value);

        /// <summary>
        /// 写入一个布尔值。
        /// </summary>
        /// <param name="key">数据的键名，用于标识此值。</param>
        /// <param name="value">要写入的布尔值。</param>
        void WriteBool(string key, bool value);

        /// <summary>
        /// 写入一个无符号32位整数值。
        /// </summary>
        /// <param name="key">数据的键名，用于标识此值。</param>
        /// <param name="value">要写入的无符号32位整数值。</param>
        void WriteUInt(string key, uint value);

        /// <summary>
        /// 写入一个无符号64位整数值。
        /// </summary>
        /// <param name="key">数据的键名，用于标识此值。</param>
        /// <param name="value">要写入的无符号64位整数值。</param>
        void WriteULong(string key, ulong value);

        // --- Asaki Math ---
        /// <summary>
        /// 写入一个2D整数向量值。
        /// </summary>
        /// <param name="key">数据的键名，用于标识此值。</param>
        /// <param name="value">要写入的2D整数向量值。</param>
        /// <remarks>
        /// 这是针对Vector2Int结构体的特定优化，避免了使用WriteObject的开销。
        /// </remarks>
        void WriteVector2Int(string key, Vector2Int value);

        /// <summary>
        /// 写入一个3D整数向量值。
        /// </summary>
        /// <param name="key">数据的键名，用于标识此值。</param>
        /// <param name="value">要写入的3D整数向量值。</param>
        /// <remarks>
        /// 这是针对Vector3Int结构体的特定优化，避免了使用WriteObject的开销。
        /// </remarks>
        void WriteVector3Int(string key, Vector3Int value);

        /// <summary>
        /// 写入一个2D向量值。
        /// </summary>
        /// <param name="key">数据的键名，用于标识此值。</param>
        /// <param name="value">要写入的2D向量值。</param>
        /// <remarks>
        /// 这是针对Vector2结构体的特定优化，避免了使用WriteObject的开销。
        /// </remarks>
        void WriteVector2(string key, Vector2 value);

        /// <summary>
        /// 写入一个3D向量值。
        /// </summary>
        /// <param name="key">数据的键名，用于标识此值。</param>
        /// <param name="value">要写入的3D向量值。</param>
        /// <remarks>
        /// 这是针对Vector3结构体的特定优化，避免了使用WriteObject的开销。
        /// </remarks>
        void WriteVector3(string key, Vector3 value);

        /// <summary>
        /// 写入一个4D向量值。
        /// </summary>
        /// <param name="key">数据的键名，用于标识此值。</param>
        /// <param name="value">要写入的4D向量值。</param>
        /// <remarks>
        /// 这是针对Vector4结构体的特定优化，避免了使用WriteObject的开销。
        /// </remarks>
        void WriteVector4(string key, Vector4 value);

        /// <summary>
        /// 写入一个边界值。
        /// </summary>
        /// <param name="key">数据的键名，用于标识此值。</param>
        /// <param name="value">要写入的边界值。</param>
        /// <remarks>
        /// 这是针对Bounds结构体的特定优化，避免了使用WriteObject的开销。
        /// </remarks>
        void WriteBounds(string key, Bounds value);

        /// <summary>
        /// 写入一个四元数值。
        /// </summary>
        /// <param name="key">数据的键名，用于标识此值。</param>
        /// <param name="value">要写入的四元数值。</param>
        /// <remarks>
        /// 这是针对Quaternion结构体的特定优化，避免了使用WriteObject的开销。
        /// </remarks>
        void WriteQuaternion(string key, Quaternion value);

        // --- 复杂对象 ---
        /// <summary>
        /// 写入一个实现了IAsakiSavable接口的复杂对象。
        /// </summary>
        /// <typeparam name="T">要写入的对象类型，必须实现IAsakiSavable接口。</typeparam>
        /// <param name="key">数据的键名，用于标识此对象。</param>
        /// <param name="value">要写入的对象实例。</param>
        void WriteObject<T>(string key, T value)
            where T : IAsakiSavable;

        /// <summary>
        /// 写入一个实现了IAsakiSavable接口的复杂对象。
        /// </summary>
        /// <param name="key">数据的键名，用于标识此对象。</param>
        /// <param name="value">要写入的对象实例。</param>
        void WriteObject(string key, IAsakiSavable value);

        // --- 结构控制 ---
        /// <summary>
        /// 开始写入一个列表。
        /// </summary>
        /// <param name="key">列表的键名，用于标识此列表。</param>
        /// <param name="count">列表中元素的数量。</param>
        /// <remarks>
        /// 在写入列表元素之前必须调用此方法，结束时必须调用EndList方法。
        /// </remarks>
        void BeginList(string key, int count);

        /// <summary>
        /// 结束写入一个列表。
        /// </summary>
        /// <remarks>
        /// 在调用BeginList方法后，必须调用此方法来结束列表的写入。
        /// </remarks>
        void EndList();

        /// <summary>
        /// 开始写入一个对象。
        /// </summary>
        /// <param name="key">对象的键名，用于标识此对象。</param>
        /// <remarks>
        /// 在写入对象的属性之前必须调用此方法，结束时必须调用EndObject方法。
        /// </remarks>
        void BeginObject(string key);

        /// <summary>
        /// 结束写入一个对象。
        /// </summary>
        /// <remarks>
        /// 在调用BeginObject方法后，必须调用此方法来结束对象的写入。
        /// </remarks>
        void EndObject();
    }
}
