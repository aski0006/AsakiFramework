using System;
using System.Collections.Generic;

namespace Asaki.Core.Logging
{
    /// <summary>
    /// V2 日志核心单元 (纯数据)，用于存储日志相关的各种信息。
    /// </summary>
    /// <remarks>
    /// </remarks>
    [Serializable]
    public class AsakiLogModel
    {
        /// <summary>
        /// 聚合计数，记录该日志项出现的次数，确保在多线程环境下的可见性。
        /// </summary>
        public volatile int Count = 1;

        /// <summary>
        /// 最后一次发生时间，以长整型时间戳表示。
        /// </summary>
        public long LastTimestamp;

        /// <summary>
        /// 已刷新的计数，可能用于记录已处理或已输出的日志次数。
        /// </summary>
        public int FlushedCount;

        /// <summary>
        /// 运行时唯一ID，用于在应用程序运行期间唯一标识该日志项。
        /// </summary>
        public int ID;

        /// <summary>
        /// 日志级别，用于表示日志的重要程度或类型，如 <see cref="AsakiLogLevel.Error"/>、<see cref="AsakiLogLevel.Debug"/> 等。
        /// </summary>
        public AsakiLogLevel Level;

        /// <summary>
        /// 日志消息内容，是对日志事件的文本描述。
        /// </summary>
        public string Message;

        /// <summary>
        /// 负载 JSON 数据，可能包含与日志相关的额外结构化信息。
        /// </summary>
        public string PayloadJson;

        /// <summary>
        /// 智能堆栈，存储一系列 <see cref="StackFrameModel"/>，用于UI渲染时展示堆栈信息。
        /// </summary>
        public List<StackFrameModel> StackFrames;

        /// <summary>
        /// 调用者路径，记录产生该日志的代码文件的路径。
        /// </summary>
        public string CallerPath;

        /// <summary>
        /// 调用者行号，记录产生该日志的代码在文件中的行号。
        /// </summary>
        public int CallerLine;

        /// <summary>
        /// 获取以本地时间格式化后的显示时间，格式为 "HH:mm:ss"。
        /// </summary>
        /// <returns>格式化后的本地时间字符串。</returns>
        public string DisplayTime => new DateTime(LastTimestamp).ToLocalTime().ToString("HH:mm:ss");
    }

    /// <summary>
    /// 单个堆栈帧的信息，用于记录堆栈跟踪中的特定帧的详细信息。
    /// </summary>
    /// <remarks>
    /// 方便在不同环境下存储和传输堆栈帧数据。
    /// </remarks>
    [Serializable]
    public struct StackFrameModel
    {
        /// <summary>
        /// 声明类型，即包含产生该堆栈帧的方法的类名，例如 "PlayerController"。
        /// </summary>
        public string DeclaringType;

        /// <summary>
        /// 方法名，例如 "Update"，表示产生该堆栈帧的方法。
        /// </summary>
        public string MethodName;

        /// <summary>
        /// 文件绝对路径，记录产生该堆栈帧的代码所在文件的完整路径。
        /// </summary>
        public string FilePath;

        /// <summary>
        /// 行号，记录产生该堆栈帧的代码在文件中的行号。
        /// </summary>
        public int LineNumber;

        /// <summary>
        /// 是否是用户代码，判断依据为是否在 Assets 目录下且不属于 Asaki 相关代码。
        /// </summary>
        public bool IsUserCode;
    }
}
