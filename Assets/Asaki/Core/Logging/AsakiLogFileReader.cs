using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Asaki.Core.Logging
{
    /// <summary>
    /// Asaki 日志文件读取工具类，提供从持久化日志文件中加载和解析日志数据的功能
    /// </summary>
    /// <remarks>
    /// <para>支持读取由 Asaki 日志系统生成的优化格式文件，自动处理日志合并计数和堆栈信息反序列化</para>
    /// <para>文件格式说明：</para>
    /// <list type="bullet">
    ///   <item><term>$DEF|</term><description>定义新的日志条目（包含完整日志信息）</description></item>
    ///   <item><term>$INC|</term><description>递增现有日志条目的出现次数</description></item>
    ///   <item><term>#</term><description>注释行，将被忽略</description></item>
    /// </list>
    /// <para>此类是线程安全的，可在主线程或后台线程中调用</para>
    /// </remarks>
    public static class AsakiLogFileReader
    {
        /// <summary>
        /// 从指定路径加载并解析 Asaki 日志文件
        /// </summary>
        /// <param name="path">日志文件的绝对路径或相对路径。路径不存在时返回空列表而非抛出异常</param>
        /// <returns>
        /// <see cref="AsakiLogModel"/> 对象的列表，按文件中的出现顺序排列。
        /// 如果文件不存在、格式无效或解析失败，返回空列表。
        /// </returns>
        /// <remarks>
        /// <para>解析流程：</para>
        /// <list type="number">
        ///   <item>按行读取整个文件内容</item>
        ///   <item>跳过空行和注释行（以#开头）</item>
        ///   <item>解析$DEF定义行，构建基础日志模型</item>
        ///   <item>解析$INC递增行，更新对应日志的计数</item>
        ///   <item>使用JsonUtility反序列化堆栈帧信息（V2.1+格式）</item>
        /// </list>
        /// <para>分隔符处理：消息和Payload中的竖线'|'被转义为'¦'，解析时会自动还原</para>
        /// <para>错误处理：所有解析异常会被捕获并记录到Unity控制台，方法始终返回有效列表（可能为空）</para>
        /// </remarks>
        /// <seealso cref="AsakiLogModel"/>
        /// <seealso cref="AsakiLogLevel"/>
        /// <seealso cref="StackFrameModel"/>
        public static List<AsakiLogModel> LoadFile(string path)
        {
            var list = new List<AsakiLogModel>();
            var idMap = new Dictionary<int, AsakiLogModel>();

            if (!File.Exists(path))
                return list;

            try
            {
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    if (line.StartsWith("$DEF|"))
                    {
                        string[] parts = line.Split('|');
                        if (parts.Length < 7)
                            continue;

                        // 解析基础信息
                        AsakiLogModel model = new AsakiLogModel
                        {
                            ID = int.Parse(parts[1]),
                            Level = (AsakiLogLevel)int.Parse(parts[2]),
                            LastTimestamp = long.Parse(parts[3]),
                            Message = parts[4].Replace("¦", "|"), // 还原分隔符
                            PayloadJson = parts[5].Replace("¦", "|"),
                            Count = 1,
                        };

                        // 解析 Caller (Path:Line)
                        string[] caller = parts[6].Split(':');
                        if (caller.Length >= 2)
                        {
                            model.CallerPath = caller[0];
                            int.TryParse(caller[1], out model.CallerLine);
                        }

                        // [V2.1] 解析堆栈
                        if (parts.Length >= 8)
                        {
                            try
                            {
                                string json = parts[7].Replace("¦", "|");
                                StackWrapper wrapper = JsonUtility.FromJson<StackWrapper>(json);
                                model.StackFrames = wrapper.F;
                            }
                            catch (Exception stackEx)
                            {
                                Debug.LogWarning(
                                    $"[AsakiReader] Failed to parse stack trace for log ID {model.ID}: {stackEx.Message}"
                                );
                            }
                        }

                        idMap[model.ID] = model;
                        list.Add(model);
                    }
                    else if (line.StartsWith("$INC|"))
                    {
                        string[] parts = line.Split('|');
                        if (
                            parts.Length >= 3
                            && int.TryParse(parts[1], out int id)
                            && int.TryParse(parts[2], out int inc)
                        )
                        {
                            if (idMap.TryGetValue(id, out AsakiLogModel model))
                            {
                                model.Count += inc;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AsakiReader] Failed to load log: {ex.Message}");
            }

            return list;
        }

        /// <summary>
        /// 内部堆栈包装结构体，用于JSON反序列化
        /// </summary>
        /// <remarks>
        /// 此结构体是<see cref="JsonUtility.FromJson{T}(string)"/>反序列化过程的必要包装，
        /// 因为Unity JSON无法直接序列化/反序列化泛型List的根对象。
        /// 仅用于<see cref="LoadFile"/>方法内部，不对外暴露。
        /// </remarks>
        [Serializable]
        private struct StackWrapper
        {
            /// <summary>堆栈帧列表。字段名"F"为缩短JSON体积而采用的简短命名</summary>
            public List<StackFrameModel> F;
        }
    }
}
