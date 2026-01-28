using System; // [重要] 必须引用 System 以支持 Type 参数
using System.Collections.Generic;
using Asaki.Core.Serialization;
using UnityEngine;

namespace Asaki.Unity.Services.Configuration
{
    /// <summary>
    /// CSV 适配器：将 CSV 行数据伪装成 IAsakiReader 流
    /// 核心目的：利用 Roslyn 生成的 Deserialize 代码，实现零反射解析
    /// </summary>
    public class AsakiCsvReader : IAsakiReader
    {
        private readonly string[] _rowData;
        private readonly Dictionary<string, int> _headerMap;

        // 构造时传入当前行数据和表头映射
        public AsakiCsvReader(string[] rowData, Dictionary<string, int> headerMap)
        {
            _rowData = rowData;
            _headerMap = headerMap;
        }

        // --- 核心魔法：列查找 ---
        private string GetCol(string key)
        {
            // 如果 Key 为空（某些生成的代码可能不传Key），或者找不到列，返回空
            if (string.IsNullOrEmpty(key) || !_headerMap.TryGetValue(key, out int index))
                return string.Empty;

            // 防止越界
            if (index >= _rowData.Length)
                return string.Empty;

            return _rowData[index];
        }

        public int ReadVersion()
        {
            return 1;
            // CSV 不处理版本控制，默认兼容
        }

        // --- 基础类型解析 ---

        public byte ReadByte(string key)
        {
            string s = GetCol(key);
            return byte.TryParse(s, out byte v) ? v : (byte)0;
        }

        public int ReadInt(string key)
        {
            string s = GetCol(key);
            return int.TryParse(s, out int v) ? v : 0;
        }

        public long ReadLong(string key)
        {
            string s = GetCol(key);
            return long.TryParse(s, out long v) ? v : 0L;
        }

        public float ReadFloat(string key)
        {
            string s = GetCol(key);
            return float.TryParse(s, out float v) ? v : 0f;
        }

        public double ReadDouble(string key)
        {
            string s = GetCol(key);
            return double.TryParse(s, out double v) ? v : 0d;
        }

        public string ReadString(string key)
        {
            // 处理 CSV 中常见的转义引号 "" -> "
            string s = GetCol(key);
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            return s.Replace("\"\"", "\"");
        }

        public bool ReadBool(string key)
        {
            string s = GetCol(key).ToLower();
            return s == "1" || s == "true" || s == "yes";
        }

        public uint ReadUInt(string key)
        {
            return uint.TryParse(GetCol(key), out uint v) ? v : 0;
        }

        public ulong ReadULong(string key)
        {
            return ulong.TryParse(GetCol(key), out ulong v) ? v : 0;
        }

        // --- Unity 原生类型解析 (Native Support) ---

        public Vector3 ReadVector3(string key)
        {
            return ParseVector3(GetCol(key));
        }

        public Vector2 ReadVector2(string key)
        {
            return ParseVector2(GetCol(key));
        }

        public Vector3Int ReadVector3Int(string key)
        {
            Vector3 v = ParseVector3(GetCol(key));
            return new Vector3Int((int)v.x, (int)v.y, (int)v.z);
        }

        public Vector2Int ReadVector2Int(string key)
        {
            Vector2 v = ParseVector2(GetCol(key));
            return new Vector2Int((int)v.x, (int)v.y);
        }

        public Vector4 ReadVector4(string key)
        {
            return Vector4.zero;
            // 暂不支持
        }

        public Quaternion ReadQuaternion(string key)
        {
            return Quaternion.identity;
            // 暂不支持
        }

        public Bounds ReadBounds(string key)
        {
            return default(Bounds);
            // 暂不支持
        }

        // --- 解析辅助 ---
        private Vector3 ParseVector3(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Vector3.zero;
            string[] parts = content.Trim('"').Split(','); // 简单处理
            if (parts.Length < 3)
                return Vector3.zero;

            float.TryParse(parts[0], out float x);
            float.TryParse(parts[1], out float y);
            float.TryParse(parts[2], out float z);
            return new Vector3(x, y, z);
        }

        private Vector2 ParseVector2(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Vector2.zero;
            string[] parts = content.Trim('"').Split(',');
            if (parts.Length < 2)
                return Vector2.zero;

            float.TryParse(parts[0], out float x);
            float.TryParse(parts[1], out float y);
            return new Vector2(x, y);
        }

        // --- 复杂对象与集合 (CSV 不支持嵌套) ---

        public T ReadObject<T>(string key, T existingObj = default(T))
            where T : IAsakiSavable, new()
        {
            // CSV 是扁平的，不支持嵌套对象读取
            return default(T);
        }

        // [修复] 实现新增的弱类型接口
        public object ReadObject(string key, Type type)
        {
            // CSV 不支持复杂的嵌套对象解析，保持与泛型版本一致，返回 null
            // 如果未来支持，可能需要读取 JSON String 并反序列化，但这超出了 CSV Reader 的职责
            return null;
        }

        public int BeginList(string key)
        {
            return 0;
            // 不支持 List
        }

        public void EndList() { }
    }
}
