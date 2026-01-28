using System;
using System.IO;
using Asaki.Core.Serialization;
using UnityEngine;

// 确保引用了你的数学库

namespace Asaki.Unity.Services.Serialization
{
    /// <summary>
    /// 基于二进制格式的序列化写入器实现，提供极速、紧凑的二进制数据序列化。
    /// </summary>
    /// <remarks>
    /// 此实现忽略键名以优化性能和减小数据大小，适用于需要高性能和小体积的场景。
    /// 它使用标准的BinaryWriter进行底层数据写入，并针对Unity数学类型进行了优化。
    /// </remarks>
    public class AsakiBinaryWriter : IAsakiWriter
    {
        private BinaryWriter _bw;

        /// <summary>
        /// 初始化AsakiBinaryWriter的新实例。
        /// </summary>
        /// <param name="stream">用于写入数据的流。</param>
        /// <param name="ownsStream">是否在写入器释放时关闭流。</param>
        public AsakiBinaryWriter(Stream stream, bool ownsStream = false)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _ownsStream = ownsStream;
            _bw = new BinaryWriter(stream, System.Text.Encoding.UTF8, true);
        }

        /// <summary>
        /// 写入序列化数据的版本号。
        /// </summary>
        /// <param name="version">版本号，以short类型存储以节省空间。</param>
        public void WriteVersion(int version)
        {
            _bw.Write((short)version);
        }

        // --- 基础类型 ---
        /// <summary>
        /// 写入一个字节值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <param name="value">要写入的字节值。</param>
        public void WriteByte(string key, byte value)
        {
            _bw.Write(value);
        }

        /// <summary>
        /// 写入一个32位整数值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <param name="value">要写入的32位整数值。</param>
        public void WriteInt(string key, int value)
        {
            _bw.Write(value);
        }

        /// <summary>
        /// 写入一个64位整数值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <param name="value">要写入的64位整数值。</param>
        public void WriteLong(string key, long value)
        {
            _bw.Write(value);
        }

        /// <summary>
        /// 写入一个单精度浮点数值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <param name="value">要写入的单精度浮点数值。</param>
        public void WriteFloat(string key, float value)
        {
            _bw.Write(value);
        }

        /// <summary>
        /// 写入一个双精度浮点数值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <param name="value">要写入的双精度浮点数值。</param>
        public void WriteDouble(string key, double value)
        {
            _bw.Write(value);
        }

        /// <summary>
        /// 写入一个字符串值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <param name="value">要写入的字符串值，若为null则写入空字符串。</param>
        public void WriteString(string key, string value)
        {
            _bw.Write(value ?? string.Empty);
        }

        /// <summary>
        /// 写入一个布尔值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <param name="value">要写入的布尔值。</param>
        public void WriteBool(string key, bool value)
        {
            _bw.Write(value);
        }

        /// <summary>
        /// 写入一个无符号32位整数值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <param name="value">要写入的无符号32位整数值。</param>
        public void WriteUInt(string key, uint value)
        {
            _bw.Write(value);
        }

        /// <summary>
        /// 写入一个无符号64位整数值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <param name="value">要写入的无符号64位整数值。</param>
        public void WriteULong(string key, ulong value)
        {
            _bw.Write(value);
        }

        // --- Asaki Math (直接打平写入，Zero GC) ---
        /// <summary>
        /// 写入一个2D整数向量值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <param name="value">要写入的2D整数向量值，直接写入x和y分量以优化性能。</param>
        public void WriteVector2Int(string key, Vector2Int value)
        {
            _bw.Write(value.x);
            _bw.Write(value.y);
        }

        /// <summary>
        /// 写入一个3D整数向量值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <param name="value">要写入的3D整数向量值，直接写入x、y和z分量以优化性能。</param>
        public void WriteVector3Int(string key, Vector3Int value)
        {
            _bw.Write(value.x);
            _bw.Write(value.y);
            _bw.Write(value.z);
        }

        /// <summary>
        /// 写入一个2D向量值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <param name="value">要写入的2D向量值，直接写入x和y分量以优化性能。</param>
        public void WriteVector2(string key, Vector2 value)
        {
            _bw.Write(value.x);
            _bw.Write(value.y);
        }

        /// <summary>
        /// 写入一个3D向量值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <param name="value">要写入的3D向量值，直接写入x、y和z分量以优化性能。</param>
        public void WriteVector3(string key, Vector3 value)
        {
            _bw.Write(value.x);
            _bw.Write(value.y);
            _bw.Write(value.z);
        }

        /// <summary>
        /// 写入一个4D向量值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <param name="value">要写入的4D向量值，直接写入x、y、z和w分量以优化性能。</param>
        public void WriteVector4(string key, Vector4 value)
        {
            _bw.Write(value.x);
            _bw.Write(value.y);
            _bw.Write(value.z);
            _bw.Write(value.w);
        }

        /// <summary>
        /// 写入一个四元数值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <param name="value">要写入的四元数值，直接写入x、y、z和w分量以优化性能。</param>
        public void WriteQuaternion(string key, Quaternion value)
        {
            _bw.Write(value.x);
            _bw.Write(value.y);
            _bw.Write(value.z);
            _bw.Write(value.w);
        }

        /// <summary>
        /// 写入一个边界值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <param name="value">要写入的边界值，通过写入min和max向量来表示。</param>
        public void WriteBounds(string key, Bounds value)
        {
            // 假设 AABB 包含 min 和 max 两个 float3
            WriteVector3(null, value.min);
            WriteVector3(null, value.max);
        }

        // --- 复杂对象 ---
        /// <summary>
        /// 写入一个实现了IAsakiSavable接口的复杂对象。
        /// </summary>
        /// <typeparam name="T">要写入的对象类型，必须实现IAsakiSavable接口。</typeparam>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <param name="value">要写入的对象实例，若为null则写入null标记。</param>
        public void WriteObject<T>(string key, T value)
            where T : IAsakiSavable
        {
            // 写入 Null 标记位 (False = Null, True = Not Null)
            if (value == null)
            {
                _bw.Write(false);
                return;
            }
            _bw.Write(true);
            value.Serialize(this); // 递归调用
        }

        /// <summary>
        /// 写入一个实现了IAsakiSavable接口的复杂对象。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <param name="value">要写入的对象实例，若为null则写入null标记。</param>
        public void WriteObject(string key, IAsakiSavable value)
        {
            if (value == null)
            {
                _bw.Write(false);
                return;
            }
            _bw.Write(true);
            value.Serialize(this); // 多态调用
        }

        // --- 集合与结构控制 ---
        /// <summary>
        /// 开始写入一个列表。
        /// </summary>
        /// <param name="key">列表的键名（在此实现中被忽略）。</param>
        /// <param name="count">列表中元素的数量。</param>
        public void BeginList(string key, int count)
        {
            _bw.Write(count);
            // 只写数量
        }

        /// <summary>
        /// 结束写入一个列表。
        /// </summary>
        /// <remarks>
        /// 在二进制实现中，列表不需要结束符，因此此方法为空实现。
        /// </remarks>
        public void EndList() { } // 二进制不需要结束符

        /// <summary>
        /// 开始写入一个对象。
        /// </summary>
        /// <param name="key">对象的键名（在此实现中被忽略）。</param>
        /// <remarks>
        /// 在二进制实现中，对象不需要结构标记，因此此方法为空实现。
        /// </remarks>
        public void BeginObject(string key) { } // 二进制不需要结构标记

        /// <summary>
        /// 结束写入一个对象。
        /// </summary>
        /// <remarks>
        /// 在二进制实现中，对象不需要结束标记，因此此方法为空实现。
        /// </remarks>
        public void EndObject() { }

        private Stream _stream;
        private bool _ownsStream;
    }

    // ==================================================================================
    // 读取器：顺序读取、对象复用
    // ==================================================================================
    /// <summary>
    /// 基于二进制格式的序列化读取器实现，提供顺序读取和对象复用功能。
    /// </summary>
    /// <remarks>
    /// 此实现提供了高效的二进制数据反序列化，支持对象复用以减少垃圾回收。
    /// 它与AsakiBinaryWriter配对使用，必须按照与写入时相同的顺序读取数据。
    /// </remarks>
    public class AsakiBinaryReader : IAsakiReader
    {
        private BinaryReader _br;
        private Stream _stream;
        private bool _ownsStream;

        /// <summary>
        /// 获取当前流的位置。
        /// </summary>
        public long Position => _stream?.Position ?? 0;

        /// <summary>
        /// 获取流中剩余的字节数。
        /// </summary>
        public long BytesRemaining => _stream?.Length - _stream?.Position ?? 0;

        /// <summary>
        /// 初始化AsakiBinaryReader的新实例。
        /// </summary>
        /// <param name="stream">用于读取数据的流。</param>
        /// <param name="ownsStream">是否在读取器释放时关闭流。</param>
        public AsakiBinaryReader(Stream stream, bool ownsStream = false)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _ownsStream = ownsStream;
            _br = new BinaryReader(stream, System.Text.Encoding.UTF8, true);
        }

        /// <summary>
        /// 读取序列化数据的版本号。
        /// </summary>
        /// <returns>版本号，以short类型读取并转换为int。</returns>
        public int ReadVersion()
        {
            return _br.ReadInt16();
        }

        // --- 基础类型 ---
        /// <summary>
        /// 读取一个字节值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <returns>从流中读取的字节值。</returns>
        public byte ReadByte(string key)
        {
            return _br.ReadByte();
        }

        /// <summary>
        /// 读取一个32位整数值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <returns>从流中读取的32位整数值。</returns>
        public int ReadInt(string key)
        {
            return _br.ReadInt32();
        }

        /// <summary>
        /// 读取一个64位整数值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <returns>从流中读取的64位整数值。</returns>
        public long ReadLong(string key)
        {
            return _br.ReadInt64();
        }

        /// <summary>
        /// 读取一个单精度浮点数值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <returns>从流中读取的单精度浮点数值。</returns>
        public float ReadFloat(string key)
        {
            return _br.ReadSingle();
        }

        /// <summary>
        /// 读取一个双精度浮点数值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <returns>从流中读取的双精度浮点数值。</returns>
        public double ReadDouble(string key)
        {
            return _br.ReadDouble();
        }

        /// <summary>
        /// 读取一个字符串值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <returns>从流中读取的字符串值。</returns>
        public string ReadString(string key)
        {
            return _br.ReadString();
        }

        /// <summary>
        /// 读取一个布尔值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <returns>从流中读取的布尔值。</returns>
        public bool ReadBool(string key)
        {
            return _br.ReadBoolean();
        }

        /// <summary>
        /// 读取一个无符号32位整数值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <returns>从流中读取的无符号32位整数值。</returns>
        public uint ReadUInt(string key)
        {
            return _br.ReadUInt32();
        }

        /// <summary>
        /// 读取一个无符号64位整数值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <returns>从流中读取的无符号64位整数值。</returns>
        public ulong ReadULong(string key)
        {
            return _br.ReadUInt64();
        }

        // --- Asaki Math ---
        /// <summary>
        /// 读取一个2D整数向量值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <returns>从流中读取的2D整数向量值，通过读取x和y分量构造。</returns>
        public Vector2Int ReadVector2Int(string key)
        {
            return new Vector2Int(_br.ReadInt32(), _br.ReadInt32());
        }

        /// <summary>
        /// 读取一个3D整数向量值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <returns>从流中读取的3D整数向量值，通过读取x、y和z分量构造。</returns>
        public Vector3Int ReadVector3Int(string key)
        {
            return new Vector3Int(_br.ReadInt32(), _br.ReadInt32(), _br.ReadInt32());
        }

        /// <summary>
        /// 读取一个2D向量值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <returns>从流中读取的2D向量值，通过读取x和y分量构造。</returns>
        public Vector2 ReadVector2(string key)
        {
            return new Vector2(_br.ReadSingle(), _br.ReadSingle());
        }

        /// <summary>
        /// 读取一个3D向量值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <returns>从流中读取的3D向量值，通过读取x、y和z分量构造。</returns>
        public Vector3 ReadVector3(string key)
        {
            return new Vector3(_br.ReadSingle(), _br.ReadSingle(), _br.ReadSingle());
        }

        /// <summary>
        /// 读取一个4D向量值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <returns>从流中读取的4D向量值，通过读取x、y、z和w分量构造。</returns>
        public Vector4 ReadVector4(string key)
        {
            return new Vector4(
                _br.ReadSingle(),
                _br.ReadSingle(),
                _br.ReadSingle(),
                _br.ReadSingle()
            );
        }

        /// <summary>
        /// 读取一个四元数值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <returns>从流中读取的四元数值，通过读取x、y、z和w分量构造。</returns>
        public Quaternion ReadQuaternion(string key)
        {
            return new Quaternion(
                _br.ReadSingle(),
                _br.ReadSingle(),
                _br.ReadSingle(),
                _br.ReadSingle()
            );
        }

        /// <summary>
        /// 读取一个边界值。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <returns>从流中读取的边界值，通过读取min和max向量构造。</returns>
        /// <remarks>
        /// 读取顺序必须与写入顺序一致。
        /// </remarks>
        public Bounds ReadBounds(string key)
        {
            // 顺序必须与 Write 一致
            Vector3 min = ReadVector3(null);
            Vector3 max = ReadVector3(null);
            return new Bounds(min, max);
        }

        // --- 复杂对象 ---
        /// <summary>
        /// 读取一个实现了IAsakiSavable接口的复杂对象。
        /// </summary>
        /// <typeparam name="T">要读取的对象类型，必须实现IAsakiSavable接口。</typeparam>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <param name="existingObj">用于填充数据的可选现有对象，用于实现对象复用。</param>
        /// <returns>从流中读取数据的类型T的对象，若提供了existingObj则复用该对象。</returns>
        /// <remarks>
        /// 此方法支持对象复用，是实现Zero GC的关键。
        /// </remarks>
        public T ReadObject<T>(string key, T existingObj = default(T))
            where T : IAsakiSavable, new()
        {
            // 1. 检查 Null 标记
            bool isNotNull = _br.ReadBoolean();
            if (!isNotNull)
                return default(T);

            // 2. 实例复用 (Zero GC 关键)
            T instance = existingObj;
            if (instance == null)
                instance = new T();

            // 3. 填充数据
            instance.Deserialize(this);
            return instance;
        }

        /// <summary>
        /// 读取指定类型的复杂对象。
        /// </summary>
        /// <param name="key">数据的键名（在此实现中被忽略）。</param>
        /// <param name="type">要读取的对象类型，必须实现IAsakiSavable接口。</param>
        /// <returns>从流中读取数据的指定类型的对象。</returns>
        /// <exception cref="InvalidOperationException">当指定类型未实现IAsakiSavable接口时抛出。</exception>
        public object ReadObject(string key, Type type)
        {
            // 必须手动检查是否实现了 IAsakiSavable (安全网)
            // 在 Dispatcher 中我们已经检查过了，这里可以由 Release 宏屏蔽，但为了安全保留
            if (!typeof(IAsakiSavable).IsAssignableFrom(type))
                throw new InvalidOperationException($"Type {type.Name} is not IAsakiSavable");

            bool isNotNull = _br.ReadBoolean();
            if (!isNotNull)
                return null;

            // [核心] 使用 Activator 创建实例
            // 这是本框架中唯一允许的“运行时实例化”手段，它是为了支持泛型容器所必须的代价
            IAsakiSavable instance = (IAsakiSavable)Activator.CreateInstance(type);

            instance.Deserialize(this);
            return instance;
        }

        // --- 集合控制 ---
        /// <summary>
        /// 开始读取一个列表。
        /// </summary>
        /// <param name="key">列表的键名（在此实现中被忽略）。</param>
        /// <returns>列表中元素的数量。</returns>
        public int BeginList(string key)
        {
            return _br.ReadInt32();
            // 读取数量
        }

        /// <summary>
        /// 结束读取一个列表。
        /// </summary>
        /// <remarks>
        /// 在二进制实现中，列表不需要结束符，因此此方法为空实现。
        /// </remarks>
        public void EndList() { }
    }
}
