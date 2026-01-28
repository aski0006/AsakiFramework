using System;
using Asaki.Core.Logging;
using UnityEngine;

namespace Asaki.Core.Configs
{
    /// <summary>
    /// 表示Asaki日志配置的可序列化类，用于管理日志记录的相关设置。
    /// </summary>
    [Serializable]
    public class AsakiLogConfig
    {
        /// <summary>
        /// 获取或设置最低日志等级，默认值为AsakiLogLevel.Debug。
        /// 低于此等级的日志将不会被记录。
        /// </summary>
        [Tooltip("最低日志等级")]
        public AsakiLogLevel MinLogLevel = AsakiLogLevel.Debug;

        /// <summary>
        /// 获取或设置单个日志文件的最大尺寸（单位为KB），默认值为2048（即2MB）。
        /// 当日志文件大小达到此限制时，将进行文件轮换。
        /// </summary>
        [Header("File Rotation")]
        [Tooltip("单个日志文件最大尺寸 (KB)")]
        public int MaxFileSizeKB = 2048;

        /// <summary>
        /// 获取或设置保留的历史日志文件数量，默认值为10。
        /// 当历史文件数量超过此限制时，将自动删除最旧的文件。
        /// </summary>
        [Tooltip("保留的历史文件数量")]
        public int MaxHistoryFiles = 10;

        /// <summary>
        /// 获取或设置日志文件名的前缀，默认值为"GameLog"。
        /// 日志文件名将以此前缀开头，后跟日期时间等信息。
        /// </summary>
        [Tooltip("日志文件名前缀")]
        public string FilePrefix = "GameLog";
    }
}
