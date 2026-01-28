using System;
using System.IO;
using UnityEngine;

namespace Asaki.Core.Serialization
{
    /// <summary>
    /// 定义从存档文件中读取序列化数据的接口。
    /// </summary>
    /// <remarks>
    /// 此接口的实现提供了从存档文件中反序列化各种数据类型的方法。
    /// 支持基本类型、Unity数学类型、复杂对象和集合。
    /// 读取器通常与 <see cref="IAsakiSavable"/> 对象一起使用，用于从存档数据中重建游戏状态。
    /// </remarks>
    public interface IAsakiReader
    {
        /// <summary>
        /// 从存档数据中读取版本号。
        /// </summary>
        /// <returns>存档数据的版本号。</returns>
        /// <remarks>
        /// 读取存档数据时应首先调用此方法，以处理特定版本的序列化逻辑。
        /// 它允许在存档格式更改时保持向后兼容性。
        /// </remarks>
        int ReadVersion();

        // --- 基础类型 ---
        /// <summary>
        /// 读取与指定键关联的字节值。
        /// </summary>
        /// <param name="key">值的唯一标识符。</param>
        /// <returns>从存档数据中读取的字节值。</returns>
        byte ReadByte(string key);

        /// <summary>
        /// 读取与指定键关联的整数值。
        /// </summary>
        /// <param name="key">值的唯一标识符。</param>
        /// <returns>从存档数据中读取的整数值。</returns>
        int ReadInt(string key);

        /// <summary>
        /// 读取与指定键关联的长整数值。
        /// </summary>
        /// <param name="key">值的唯一标识符。</param>
        /// <returns>从存档数据中读取的长整数值。</returns>
        long ReadLong(string key);

        /// <summary>
        /// 读取与指定键关联的单精度浮点值。
        /// </summary>
        /// <param name="key">值的唯一标识符。</param>
        /// <returns>从存档数据中读取的浮点值。</returns>
        float ReadFloat(string key);

        /// <summary>
        /// 读取与指定键关联的双精度浮点值。
        /// </summary>
        /// <param name="key">值的唯一标识符。</param>
        /// <returns>从存档数据中读取的双精度浮点值。</returns>
        double ReadDouble(string key);

        /// <summary>
        /// 读取与指定键关联的字符串值。
        /// </summary>
        /// <param name="key">值的唯一标识符。</param>
        /// <returns>从存档数据中读取的字符串值。</returns>
        string ReadString(string key);

        /// <summary>
        /// 读取与指定键关联的布尔值。
        /// </summary>
        /// <param name="key">值的唯一标识符。</param>
        /// <returns>从存档数据中读取的布尔值。</returns>
        bool ReadBool(string key);

        /// <summary>
        /// 读取与指定键关联的无符号整数值。
        /// </summary>
        /// <param name="key">值的唯一标识符。</param>
        /// <returns>从存档数据中读取的无符号整数值。</returns>
        uint ReadUInt(string key);

        /// <summary>
        /// 读取与指定键关联的无符号长整数值。
        /// </summary>
        /// <param name="key">值的唯一标识符。</param>
        /// <returns>从存档数据中读取的无符号长整数值。</returns>
        ulong ReadULong(string key);

        // --- Asaki Math ---
        /// <summary>
        /// 读取与指定键关联的2D整数向量值。
        /// </summary>
        /// <param name="key">值的唯一标识符。</param>
        /// <returns>从存档数据中读取的Vector2Int值。</returns>
        Vector2Int ReadVector2Int(string key);

        /// <summary>
        /// 读取与指定键关联的3D整数向量值。
        /// </summary>
        /// <param name="key">值的唯一标识符。</param>
        /// <returns>从存档数据中读取的Vector3Int值。</returns>
        Vector3Int ReadVector3Int(string key);

        /// <summary>
        /// 读取与指定键关联的2D向量值。
        /// </summary>
        /// <param name="key">值的唯一标识符。</param>
        /// <returns>从存档数据中读取的Vector2值。</returns>
        Vector2 ReadVector2(string key);

        /// <summary>
        /// 读取与指定键关联的3D向量值。
        /// </summary>
        /// <param name="key">值的唯一标识符。</param>
        /// <returns>从存档数据中读取的Vector3值。</returns>
        Vector3 ReadVector3(string key);

        /// <summary>
        /// 读取与指定键关联的4D向量值。
        /// </summary>
        /// <param name="key">值的唯一标识符。</param>
        /// <returns>从存档数据中读取的Vector4值。</returns>
        Vector4 ReadVector4(string key);

        /// <summary>
        /// 读取与指定键关联的边界值。
        /// </summary>
        /// <param name="key">值的唯一标识符。</param>
        /// <returns>从存档数据中读取的Bounds值。</returns>
        Bounds ReadBounds(string key);

        /// <summary>
        /// 读取与指定键关联的四元数值。
        /// </summary>
        /// <param name="key">值的唯一标识符。</param>
        /// <returns>从存档数据中读取的Quaternion值。</returns>
        Quaternion ReadQuaternion(string key);

        // --- 复杂对象 ---
        /// <summary>
        /// 读取实现了 <see cref="IAsakiSavable"/> 接口的复杂对象。
        /// </summary>
        /// <typeparam name="T">要读取的对象类型，必须实现 <see cref="IAsakiSavable"/> 接口。</typeparam>
        /// <param name="key">对象的唯一标识符。</param>
        /// <param name="existingObj">用于填充数据的可选现有对象。</param>
        /// <returns>从存档文件中读取数据的类型T的新对象或已填充数据的现有对象。</returns>
        /// <remarks>
        /// 如果提供了现有对象，该方法将用读取的数据填充它。否则，将创建一个新实例。
        /// 读取器会自动处理对象边界，不需要调用EndObject方法。
        /// </remarks>
        T ReadObject<T>(string key, T existingObj = default(T))
            where T : IAsakiSavable, new();

        /// <summary>
        /// 读取指定类型的复杂对象。
        /// </summary>
        /// <param name="key">对象的唯一标识符。</param>
        /// <param name="type">要读取的对象类型。</param>
        /// <returns>从存档文件中读取数据的指定类型的对象。</returns>
        object ReadObject(string key, Type type);

        // --- 集合控制 ---
        /// <summary>
        /// 开始读取与指定键关联的项目列表。
        /// </summary>
        /// <param name="key">列表的唯一标识符。</param>
        /// <returns>列表中的项目数量。</returns>
        /// <remarks>
        /// 调用此方法后，读取器将定位到列表中的第一个项目。
        /// 读取所有项目后，必须调用 <see cref="EndList"/> 以正确重置读取器状态。
        /// </remarks>
        int BeginList(string key);

        /// <summary>
        /// 结束读取项目列表。
        /// </summary>
        /// <remarks>
        /// 必须在读取完所有用 <see cref="BeginList"/> 启动的列表项目后调用此方法。
        /// 它将重置读取器状态，以继续读取其他数据。
        /// </remarks>
        void EndList();
    }
}
