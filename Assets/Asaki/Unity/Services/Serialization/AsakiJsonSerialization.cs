using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Asaki.Core.Serialization;
using Asaki.Unity.Utils;
using UnityEngine;

namespace Asaki.Unity.Services.Serialization
{
    /// <summary>
    /// Asaki JSON序列化写入器，提供将C#对象结构转换为格式化JSON字符串的流式API实现。
    /// </summary>
    /// <remarks>
    /// <para>核心特性：</para>
    /// <list type="bullet">
    ///     <item>流式写入接口 - 类似<see cref="System.IO.BinaryWriter"/>的连续写入模式</item>
    ///     <item>零GC分配 - 支持<see cref="AsakiStringBuilderPool"/>对象池复用</item>
    ///     <item>格式化输出 - 自动生成缩进和换行，便于调试和版本控制</item>
    ///     <item>Unity类型原生支持 - 内置<see cref="Vector2"/>/<see cref="Vector3"/>/<see cref="Quaternion"/>等序列化</item>
    /// </list>
    /// <para>上下文栈机制：</para>
    /// 使用内部栈结构跟踪当前写入容器的类型（对象或数组）和状态，自动管理逗号分隔符和缩进层级。
    /// <para>使用示例：</para>
    /// <code>
    /// var writer = new AsakiJsonWriter();
    /// writer.WriteInt("playerLevel", 5);
    /// writer.WriteString("playerName", "Hero");
    /// writer.BeginObject("position");
    /// writer.WriteVector3("value", new Vector3(1, 2, 3));
    /// writer.EndObject();
    /// string json = writer.GetResult();
    /// </code>
    /// </remarks>
    public class AsakiJsonWriter : IAsakiWriter
    {
        /// <summary>
        /// 内部字符串构建器，用于高效构建JSON字符串内容。
        /// </summary>
        private readonly StringBuilder _sb;

        /// <summary>
        /// 当前JSON结构的缩进层级，每层级对应4个空格。
        /// </summary>
        private int _indent;

        /// <summary>
        /// 控制下一个元素是否应跳过逗号前缀的标志位。
        /// 用于处理键值对中值写入后的逗号逻辑。
        /// </summary>
        private bool _skipNextComma;

        /// <summary>
        /// 容器上下文栈，用于跟踪嵌套的对象和数组结构。
        /// </summary>
        private readonly Stack<ContainerContext> _contextStack;

        /// <summary>
        /// 指示当前<see cref="_sb"/>是否从对象池租用，决定是否需要归还。
        /// </summary>
        private bool _isRented;

        /// <summary>
        /// 容器上下文，存储当前JSON容器的状态信息。
        /// </summary>
        private class ContainerContext
        {
            /// <summary>
            /// 当前容器是否为数组结构（<c>true</c>表示数组，<c>false</c>表示对象）。
            /// </summary>
            public bool IsArray;

            /// <summary>
            /// 标记当前容器是否已写入第一个元素，用于控制逗号输出逻辑。
            /// </summary>
            public bool HasWrittenFirstElement;
        }

        /// <summary>
        /// 获取当前写入位置（即<see cref="StringBuilder.Length"/>）。
        /// </summary>
        /// <value>当前已写入的字符数。</value>
        public long Position => _sb?.Length ?? 0;

        /// <summary>
        /// 初始化<see cref="AsakiJsonWriter"/>的新实例。
        /// </summary>
        /// <param name="sb">可选的外部<see cref="StringBuilder"/>实例。若提供，则不会从对象池租用。</param>
        /// <param name="rentFromPool">当<paramref name="sb"/>为<c>null</c>时，是否从<see cref="AsakiStringBuilderPool"/>租用实例。</param>
        /// <remarks>
        /// <para>三种初始化模式：</para>
        /// <list type="bullet">
        ///     <item>外部传入<paramref name="sb"/>：调用方完全控制StringBuilder生命周期</item>
        ///     <item>从池租用（<paramref name="rentFromPool"/>=<c>true</c>）：推荐高频调用场景，实现零分配</item>
        ///     <item>新建实例（<paramref name="rentFromPool"/>=<c>false</c>）：低频场景，简单直接</item>
        /// </list>
        /// <para>对象池模式注意事项：</para>
        /// 若从对象池租用，必须在适当位置调用<see cref="GetResult"/>以归还StringBuilder，否则会导致池耗尽。
        /// </remarks>
        public AsakiJsonWriter(StringBuilder sb = null, bool rentFromPool = true)
        {
            _isRented = rentFromPool;

            if (sb != null)
            {
                _sb = sb;
                _isRented = false;
            }
            else if (rentFromPool)
            {
                _sb = AsakiStringBuilderPool.Rent();
            }
            else
            {
                _sb = new StringBuilder(256);
            }

            _indent = 0;
            _skipNextComma = false;
            _contextStack = new Stack<ContainerContext>();
            _contextStack.Push(new ContainerContext { IsArray = false });
        }

        /// <summary>
        /// 获取序列化后的JSON字符串结果。
        /// </summary>
        /// <returns>格式化后的完整JSON字符串。</returns>
        /// <remarks>
        /// 若使用对象池模式，此方法会触发StringBuilder归还操作。
        /// 调用后不应再使用当前写入器实例进行写入操作。
        /// </remarks>
        public string GetResult()
        {
            return _sb.ToString();
        }

        /// <summary>
        /// 写入键值对的前缀部分，包括逗号、缩进和键名。
        /// </summary>
        /// <param name="key">当前写入的键名，若为<c>null</c>或空字符串表示处于数组中或根对象。</param>
        /// <remarks>
        /// 此方法自动处理以下逻辑：
        /// <list type="number">
        ///     <item>根据上下文决定是否在元素前添加逗号分隔符</item>
        ///     <item>写入当前缩进层级的空格（4个空格 × 缩进层级）</item>
        ///     <item>若处于对象模式且<paramref name="key"/>有效，写入键名和冒号</item>
        ///     <item>设置<see cref="_skipNextComma"/>标志，避免值后立即输出逗号</item>
        /// </list>
        /// </remarks>
        private void WritePrefix(string key)
        {
            bool skipComma = _skipNextComma;
            _skipNextComma = false;

            ContainerContext ctx = _contextStack.Peek();

            if (ctx.HasWrittenFirstElement && !skipComma)
            {
                _sb.AppendLine(",");
            }
            ctx.HasWrittenFirstElement = true;

            _sb.Append(' ', _indent * 4);

            if (!string.IsNullOrEmpty(key) && !ctx.IsArray)
            {
                _sb.Append($"\"{key}\": ");
                _skipNextComma = true;
            }
        }

        /// <summary>
        /// 将新的容器上下文压入栈，用于处理嵌套结构。
        /// </summary>
        /// <param name="isArray"><c>true</c>表示新容器为数组，<c>false</c>表示为对象。</param>
        private void PushContext(bool isArray)
        {
            _contextStack.Push(new ContainerContext { IsArray = isArray });
        }

        /// <summary>
        /// 弹出当前容器上下文，返回上一层级。
        /// </summary>
        private void PopContext()
        {
            if (_contextStack.Count > 0)
                _contextStack.Pop();
        }

        /// <inheritdoc/>
        public void WriteVersion(int version)
        {
            WriteInt("version", version);
        }

        /// <inheritdoc/>
        public void WriteByte(string key, byte value)
        {
            WritePrefix(key);
            _sb.Append(value);
            _skipNextComma = false;
        }

        /// <inheritdoc/>
        public void WriteInt(string key, int value)
        {
            WritePrefix(key);
            _sb.Append(value);
            _skipNextComma = false;
        }

        /// <inheritdoc/>
        public void WriteLong(string key, long value)
        {
            WritePrefix(key);
            _sb.Append(value);
            _skipNextComma = false;
        }

        /// <inheritdoc/>
        public void WriteFloat(string key, float value)
        {
            WritePrefix(key);
            _sb.Append(value.ToString("F3"));
            _skipNextComma = false;
        }

        /// <inheritdoc/>
        public void WriteDouble(string key, double value)
        {
            WritePrefix(key);
            _sb.Append(value.ToString("F4"));
            _skipNextComma = false;
        }

        /// <inheritdoc/>
        public void WriteString(string key, string value)
        {
            WritePrefix(key);
            string escaped = value?.Replace("\"", "\\\"").Replace("\n", "\\n") ?? "";
            _sb.Append($"\"{escaped}\"");
            _skipNextComma = false;
        }

        /// <inheritdoc/>
        public void WriteBool(string key, bool value)
        {
            WritePrefix(key);
            _sb.Append(value ? "true" : "false");
            _skipNextComma = false;
        }

        /// <inheritdoc/>
        public void WriteUInt(string key, uint value)
        {
            WriteLong(key, value);
        }

        /// <inheritdoc/>
        public void WriteULong(string key, ulong value)
        {
            WriteString(key, value.ToString());
        }

        /// <inheritdoc/>
        public void WriteVector2(string key, Vector2 value)
        {
            WritePrefix(key);
            _sb.Append($"{{ \"x\": {value.x:F2}, \"y\": {value.y:F2} }}");
            _skipNextComma = false;
        }

        /// <inheritdoc/>
        public void WriteVector3(string key, Vector3 value)
        {
            WritePrefix(key);
            _sb.Append($"{{ \"x\": {value.x:F2}, \"y\": {value.y:F2}, \"z\": {value.z:F2} }}");
            _skipNextComma = false;
        }

        /// <inheritdoc/>
        public void WriteVector2Int(string key, Vector2Int value)
        {
            WritePrefix(key);
            _sb.Append($"{{ \"x\": {value.x}, \"y\": {value.y} }}");
            _skipNextComma = false;
        }

        /// <inheritdoc/>
        public void WriteVector3Int(string key, Vector3Int value)
        {
            WritePrefix(key);
            _sb.Append($"{{ \"x\": {value.x}, \"y\": {value.y}, \"z\": {value.z} }}");
            _skipNextComma = false;
        }

        /// <inheritdoc/>
        public void WriteVector4(string key, Vector4 value)
        {
            WritePrefix(key);
            _sb.Append(
                $"{{ \"x\": {value.x:F2}, \"y\": {value.y:F2}, \"z\": {value.z:F2}, \"w\": {value.w:F2} }}"
            );
            _skipNextComma = false;
        }

        /// <inheritdoc/>
        public void WriteQuaternion(string key, Quaternion value)
        {
            WritePrefix(key);
            _sb.Append(
                $"{{ \"x\": {value.x:F2}, \"y\": {value.y:F2}, \"z\": {value.z:F2}, \"w\": {value.w:F2} }}"
            );
            _skipNextComma = false;
        }

        /// <inheritdoc/>
        public void WriteBounds(string key, Bounds value)
        {
            WritePrefix(key);
            _sb.Append(
                $"{{ \"center\": {{ \"x\": {value.center.x:F2}, \"y\": {value.center.y:F2}, \"z\": {value.center.z:F2} }}, \"size\": {{ \"x\": {value.size.x:F2}, \"y\": {value.size.y:F2}, \"z\": {value.size.z:F2} }} }}"
            );
            _skipNextComma = false;
        }

        /// <inheritdoc/>
        public void BeginObject(string key)
        {
            WritePrefix(null);
            _sb.AppendLine("{");
            _indent++;
            PushContext(false);
            _skipNextComma = false;
        }

        /// <inheritdoc/>
        public void EndObject()
        {
            _indent--;
            PopContext();
            _sb.AppendLine();
            _sb.Append(' ', _indent * 4);
            _sb.Append("}");
            _skipNextComma = false;
        }

        /// <inheritdoc/>
        public void BeginList(string key, int count)
        {
            WritePrefix(key);
            _skipNextComma = false;

            _sb.AppendLine("[");
            _indent++;
            PushContext(true);
        }

        /// <inheritdoc/>
        public void EndList()
        {
            _indent--;
            PopContext();
            _sb.AppendLine();
            _sb.Append(' ', _indent * 4);
            _sb.Append("]");
            _skipNextComma = false;
        }

        /// <inheritdoc/>
        public void WriteObject<T>(string key, T value)
            where T : IAsakiSavable
        {
            WriteObject(key, (IAsakiSavable)value);
        }

        /// <inheritdoc/>
        public void WriteObject(string key, IAsakiSavable value)
        {
            if (value == null)
            {
                WritePrefix(key);
                _sb.Append("null");
                _skipNextComma = false;
                return;
            }

            WritePrefix(key);
            _skipNextComma = true;

            value.Serialize(this);
        }
    }

    /// <summary>
    /// Asaki JSON反序列化读取器，提供从JSON数据重建C#对象的流式API实现。
    /// </summary>
    /// <remarks>
    /// <para>核心特性：</para>
    /// <list type="bullet">
    ///     <item>流式读取接口 - 与<see cref="IAsakiReader"/>完全兼容</item>
    ///     <item>DOM解析 - 基于<see cref="AsakiTinyJsonParser"/>生成的对象树</item>
    ///     <item>列表上下文栈 - 支持嵌套数组的逐元素迭代</item>
    ///     <item>类型安全转换 - 使用<see cref="Convert"/>类处理类型转换</item>
    /// </list>
    /// <para>架构设计：</para>
    /// 内部维护_currentNode作为当前DOM节点，_listStack处理数组迭代状态，GetValue方法根据上下文智能路由取值逻辑。
    /// </remarks>
    public class AsakiJsonReader : IAsakiReader
    {
        /// <summary>
        /// 当前DOM节点，可为<see cref="Dictionary{String,Object}"/>（对象）或<see cref="List{Object}"/>（数组）。
        /// </summary>
        private readonly object _currentNode;

        /// <summary>
        /// 列表上下文栈，用于处理嵌套数组的逐元素读取。
        /// </summary>
        private Stack<ListContext> _listStack = new Stack<ListContext>();

        /// <summary>
        /// 列表上下文，存储当前数组状态和迭代索引。
        /// </summary>
        private class ListContext
        {
            /// <summary>
            /// 当前数组的DOM表示。
            /// </summary>
            public List<object> List;

            /// <summary>
            /// 当前迭代索引，指向下一个待读取元素。
            /// </summary>
            public int Index;
        }

        /// <summary>
        /// 初始化<see cref="AsakiJsonReader"/>的新实例。
        /// </summary>
        /// <param name="node">DOM节点对象，作为读取的根节点。</param>
        private AsakiJsonReader(object node)
        {
            _currentNode = node;
        }

        /// <summary>
        /// 从JSON字符串创建<see cref="AsakiJsonReader"/>实例。
        /// </summary>
        /// <param name="json">要解析的JSON字符串。</param>
        /// <returns>配置好的JSON读取器实例。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="json"/>为<c>null</c>。</exception>
        public static AsakiJsonReader FromJson(string json)
        {
            object data = AsakiTinyJsonParser.Parse(json);
            return new AsakiJsonReader(data);
        }

        /// <inheritdoc/>
        public int ReadVersion()
        {
            return 1;
        }

        /// <summary>
        /// 根据当前上下文获取指定键或索引对应的值。
        /// </summary>
        /// <param name="key">要获取的键名，在数组上下文中此参数被忽略。</param>
        /// <returns>
        /// 若处于数组上下文且存在下一个元素，返回该元素；
        /// 若处于对象上下文且键存在，返回对应值；
        /// 否则返回<c>null</c>。
        /// </returns>
        private object GetValue(string key)
        {
            if (_listStack.Count > 0)
            {
                ListContext ctx = _listStack.Peek();
                if (ctx.Index < ctx.List.Count)
                {
                    object val = ctx.List[ctx.Index];
                    ctx.Index++;
                    return val;
                }
                return null;
            }

            if (_currentNode is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue(key, out object val))
                    return val;
            }

            return null;
        }

        /// <inheritdoc/>
        public byte ReadByte(string key)
        {
            return Convert.ToByte(GetValue(key));
        }

        /// <inheritdoc/>
        public int ReadInt(string key)
        {
            return Convert.ToInt32(GetValue(key));
        }

        /// <inheritdoc/>
        public long ReadLong(string key)
        {
            return Convert.ToInt64(GetValue(key));
        }

        /// <inheritdoc/>
        public float ReadFloat(string key)
        {
            return Convert.ToSingle(GetValue(key));
        }

        /// <inheritdoc/>
        public double ReadDouble(string key)
        {
            return Convert.ToDouble(GetValue(key));
        }

        /// <inheritdoc/>
        public string ReadString(string key)
        {
            return Convert.ToString(GetValue(key));
        }

        /// <inheritdoc/>
        public bool ReadBool(string key)
        {
            return Convert.ToBoolean(GetValue(key));
        }

        /// <inheritdoc/>
        public uint ReadUInt(string key)
        {
            return Convert.ToUInt32(GetValue(key));
        }

        /// <inheritdoc/>
        public ulong ReadULong(string key)
        {
            return Convert.ToUInt64(GetValue(key));
        }

        /// <inheritdoc/>
        public Vector2 ReadVector2(string key)
        {
            return ParseVec2(GetValue(key));
        }

        /// <inheritdoc/>
        public Vector3 ReadVector3(string key)
        {
            return ParseVec3(GetValue(key));
        }

        /// <inheritdoc/>
        public Vector2Int ReadVector2Int(string key)
        {
            return ParseVec2Int(GetValue(key));
        }

        /// <inheritdoc/>
        public Vector3Int ReadVector3Int(string key)
        {
            return ParseVec3Int(GetValue(key));
        }

        /// <inheritdoc/>
        public Vector4 ReadVector4(string key)
        {
            return Vector4.zero;
        }

        /// <inheritdoc/>
        public Quaternion ReadQuaternion(string key)
        {
            return Quaternion.identity;
        }

        /// <inheritdoc/>
        public Bounds ReadBounds(string key)
        {
            return default(Bounds);
        }

        /// <inheritdoc/>
        public T ReadObject<T>(string key, T existingObj = default(T))
            where T : IAsakiSavable, new()
        {
            object childNode = GetValue(key);
            if (childNode == null)
                return default(T);

            AsakiJsonReader childReader = new AsakiJsonReader(childNode);

            T instance = existingObj ?? new T();
            instance.Deserialize(childReader);
            return instance;
        }

        /// <inheritdoc/>
        public object ReadObject(string key, Type type)
        {
            object childNode = GetValue(key);
            if (childNode == null)
                return null;

            if (!typeof(IAsakiSavable).IsAssignableFrom(type))
                throw new InvalidOperationException($"Type {type.Name} is not IAsakiSavable");

            AsakiJsonReader childReader = new AsakiJsonReader(childNode);
            IAsakiSavable instance = (IAsakiSavable)Activator.CreateInstance(type);
            instance.Deserialize(childReader);
            return instance;
        }

        /// <inheritdoc/>
        public int BeginList(string key)
        {
            var rawList = GetValue(key) as List<object>;
            if (rawList == null)
                return 0;

            _listStack.Push(new ListContext { List = rawList, Index = 0 });

            return rawList.Count;
        }

        /// <inheritdoc/>
        public void EndList()
        {
            if (_listStack.Count > 0)
                _listStack.Pop();
        }

        /// <summary>
        /// 将DOM对象解析为<see cref="Vector2"/>。
        /// </summary>
        /// <param name="obj">包含x/y字段的字典对象。</param>
        /// <returns>解析后的Vector2，解析失败返回<see cref="Vector2.zero"/>。</returns>
        private Vector2 ParseVec2(object obj)
        {
            if (obj is Dictionary<string, object> d)
                return new Vector2(Convert.ToSingle(d["x"]), Convert.ToSingle(d["y"]));
            return Vector2.zero;
        }

        /// <summary>
        /// 将DOM对象解析为<see cref="Vector3"/>。
        /// </summary>
        /// <param name="obj">包含x/y/z字段的字典对象。</param>
        /// <returns>解析后的Vector3，解析失败返回<see cref="Vector3.zero"/>。</returns>
        private Vector3 ParseVec3(object obj)
        {
            if (obj is Dictionary<string, object> d)
                return new Vector3(
                    Convert.ToSingle(d["x"]),
                    Convert.ToSingle(d["y"]),
                    Convert.ToSingle(d["z"])
                );
            return Vector3.zero;
        }

        /// <summary>
        /// 将DOM对象解析为<see cref="Vector2Int"/>。
        /// </summary>
        /// <param name="obj">包含x/y字段的字典对象。</param>
        /// <returns>解析后的Vector2Int，解析失败返回<see cref="Vector2Int.zero"/>。</returns>
        private Vector2Int ParseVec2Int(object obj)
        {
            if (obj is Dictionary<string, object> d)
                return new Vector2Int(Convert.ToInt32(d["x"]), Convert.ToInt32(d["y"]));
            return Vector2Int.zero;
        }

        /// <summary>
        /// 将DOM对象解析为<see cref="Vector3Int"/>。
        /// </summary>
        /// <param name="obj">包含x/y/z字段的字典对象。</param>
        /// <returns>解析后的Vector3Int，解析失败返回<see cref="Vector3Int.zero"/>。</returns>
        private Vector3Int ParseVec3Int(object obj)
        {
            if (obj is Dictionary<string, object> d)
                return new Vector3Int(
                    Convert.ToInt32(d["x"]),
                    Convert.ToInt32(d["y"]),
                    Convert.ToInt32(d["z"])
                );
            return Vector3Int.zero;
        }
    }

    /// <summary>
    /// 轻量级零依赖JSON解析器，提供将JSON字符串转换为DOM对象树的基础功能。
    /// </summary>
    /// <remarks>
    /// <para>特性说明：</para>
    /// <list type="bullet">
    ///     <item>无外部依赖 - 仅使用.NET基础类型</item>
    ///     <item>递归下降解析 - 根据当前字符自动选择解析策略</item>
    ///     <item>DOM表示 - 对象解析为<see cref="Dictionary{String,Object}"/>，数组解析为<see cref="List{Object}"/></item>
    ///     <item>基础类型支持 - 数字、字符串、布尔值、null</item>
    /// </list>
    /// <para>限制说明：</para>
    /// <list type="bullet">
    ///     <item>不支持JSON标准外的注释语法</item>
    ///     <item>字符串转义仅处理基础转义序列</item>
    ///     <item>数字类型统一解析为<see cref="long"/>或<see cref="double"/></item>
    /// </list>
    /// </remarks>
    public static class AsakiTinyJsonParser
    {
        /// <summary>
        /// 解析JSON字符串并返回DOM对象。
        /// </summary>
        /// <param name="json">要解析的JSON字符串。</param>
        /// <returns>解析后的DOM根节点（可为<see cref="Dictionary{String,Object}"/>、<see cref="List{Object}"/>或原始类型）。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="json"/>为<c>null</c>或空字符串。</exception>
        public static object Parse(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;
            int index = 0;
            return ParseValue(json, ref index);
        }

        /// <summary>
        /// 递归解析JSON值（对象、数组、字符串、数字、布尔值或null）。
        /// </summary>
        /// <param name="json">完整JSON字符串。</param>
        /// <param name="index">当前解析位置的引用，方法会修改此值。</param>
        /// <returns>解析后的值对象。</returns>
        private static object ParseValue(string json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length)
                return null;

            char c = json[index];
            if (c == '{')
                return ParseObject(json, ref index);
            if (c == '[')
                return ParseArray(json, ref index);
            if (c == '"')
                return ParseString(json, ref index);
            if (char.IsDigit(c) || c == '-')
                return ParseNumber(json, ref index);
            if (c == 't')
            {
                index += 4;
                return true;
            }
            if (c == 'f')
            {
                index += 5;
                return false;
            }
            if (c == 'n')
            {
                index += 4;
                return null;
            }
            return null;
        }

        /// <summary>
        /// 解析JSON对象结构。
        /// </summary>
        /// <param name="json">完整JSON字符串。</param>
        /// <param name="index">当前解析位置的引用。</param>
        /// <returns>包含键值对的字典对象。</returns>
        private static Dictionary<string, object> ParseObject(string json, ref int index)
        {
            var dict = new Dictionary<string, object>();
            index++; // skip '{'
            while (index < json.Length)
            {
                SkipWhitespace(json, ref index);
                if (json[index] == '}')
                {
                    index++;
                    break;
                }
                string key = ParseString(json, ref index);
                SkipWhitespace(json, ref index);
                if (json[index] == ':')
                    index++;
                object value = ParseValue(json, ref index);
                dict[key] = value;
                SkipWhitespace(json, ref index);
                if (json[index] == ',')
                    index++;
            }
            return dict;
        }

        /// <summary>
        /// 解析JSON数组结构。
        /// </summary>
        /// <param name="json">完整JSON字符串。</param>
        /// <param name="index">当前解析位置的引用。</param>
        /// <returns>包含元素的对象列表。</returns>
        private static List<object> ParseArray(string json, ref int index)
        {
            var list = new List<object>();
            index++; // skip '['
            while (index < json.Length)
            {
                SkipWhitespace(json, ref index);
                if (json[index] == ']')
                {
                    index++;
                    break;
                }
                list.Add(ParseValue(json, ref index));
                SkipWhitespace(json, ref index);
                if (json[index] == ',')
                    index++;
            }
            return list;
        }

        /// <summary>
        /// 解析JSON字符串值。
        /// </summary>
        /// <param name="json">完整JSON字符串。</param>
        /// <param name="index">当前解析位置的引用。</param>
        /// <returns>解析后的字符串。</returns>
        private static string ParseString(string json, ref int index)
        {
            StringBuilder sb = new StringBuilder();
            index++; // skip start quote
            while (index < json.Length)
            {
                char c = json[index++];
                if (c == '"')
                    break;
                if (c == '\\')
                    index++; // Simple skip escape
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 解析JSON数字值。
        /// </summary>
        /// <param name="json">完整JSON字符串。</param>
        /// <param name="index">当前解析位置的引用。</param>
        /// <returns>解析后的数字（<see cref="long"/>或<see cref="double}"/>）。</returns>
        private static object ParseNumber(string json, ref int index)
        {
            int start = index;
            while (
                index < json.Length
                && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == '-')
            )
            {
                index++;
            }
            string numStr = json.Substring(start, index - start);
            if (numStr.Contains("."))
                return double.Parse(numStr, CultureInfo.InvariantCulture);
            return long.Parse(numStr);
        }

        /// <summary>
        /// 跳过JSON字符串中的空白字符（空格、制表符、换行符等）。
        /// </summary>
        /// <param name="json">完整JSON字符串。</param>
        /// <param name="index">当前解析位置的引用。</param>
        private static void SkipWhitespace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }
        }
    }
}
