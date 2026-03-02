using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Asaki.Core.Context;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.Logging;
using UnityEditor;
using UnityEngine;

namespace Asaki.Unity.Logging
{
    /// <summary>
    /// ALog 的 Unity 控制台桥接器实现。
    /// 将 ALog 日志同时转发到 Unity 控制台，获得原生堆栈跟踪和双击跳转功能。
    /// 支持富文本多颜色展示和时间戳信息。
    /// </summary>
    /// <remarks>
    /// <para>核心优势：</para>
    /// <list type="bullet">
    ///   <item>原生 Unity 控制台体验：双击日志自动跳转到源代码</item>
    ///   <item>完整堆栈跟踪：显示完整调用链，不受聚合影响</item>
    ///   <item>零成本：仅在编辑器下编译，Release 构建自动剔除</item>
    ///   <item>富文本颜色：根据日志级别显示不同颜色</item>
    ///   <item>精确时间戳：显示精确到毫秒的时间信息</item>
    /// </list>
    /// <para>依赖关系：实现 Asaki.Core.Logging.IALogUnityBridge 接口，打破循环依赖</para>
    /// </remarks>
    public class ALogUnityBridge : IALogUnityBridge
    {
        private static ALogUnityBridge _instance;
        private static readonly Dictionary<AsakiLogLevel, LogType> LogTypeMap =
            new()
            {
                { AsakiLogLevel.Debug, LogType.Log },
                { AsakiLogLevel.Info, LogType.Log },
                { AsakiLogLevel.Warning, LogType.Warning },
                { AsakiLogLevel.Error, LogType.Error },
                { AsakiLogLevel.Fatal, LogType.Error },
            };

        /// <summary>
        /// 日志级别对应的富文本颜色
        /// </summary>
        private static readonly Dictionary<AsakiLogLevel, string> LevelColors =
            new()
            {
                { AsakiLogLevel.Debug, "#808080" },
                { AsakiLogLevel.Info, "#FFFFFF" },
                { AsakiLogLevel.Warning, "#FFDD00" },
                { AsakiLogLevel.Error, "#FF5555" },
                { AsakiLogLevel.Fatal, "#FF0000" },
            };

        /// <summary>
        /// 日志级别对应的标签文本
        /// </summary>
        private static readonly Dictionary<AsakiLogLevel, string> LevelLabels =
            new()
            {
                { AsakiLogLevel.Debug, "🔍 DBG" },
                { AsakiLogLevel.Info, "ℹ️ INF" },
                { AsakiLogLevel.Warning, "⚠️ WRN" },
                { AsakiLogLevel.Error, "❌ ERR" },
                { AsakiLogLevel.Fatal, "💀 FTL" },
            };

        private bool? _isEnabled;

        /// <summary>
        /// 是否启用 Unity 控制台输出（从配置读取）
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                // 延迟初始化，从配置读取
                if (!_isEnabled.HasValue)
                {
                    if (
                        AsakiContext.TryGet(out AsakiFrameworkSetting config)
                        && config.LogConfig != null
                    )
                    {
                        _isEnabled = config.LogConfig.OutputToUnityConsole;
                    }
                    else
                    {
                        _isEnabled = true; // 默认启用
                    }
                }
                return _isEnabled.Value;
            }
            set => _isEnabled = value;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器下初始化并注册桥接器
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            _instance = new ALogUnityBridge();
            ALogBridgeManager.RegisterBridge(_instance);
        }
#endif

        /// <summary>
        /// 将日志转发到 Unity 控制台
        /// </summary>
        void IALogUnityBridge.ForwardToUnityConsole(
            AsakiLogLevel level,
            string message,
            string payload,
            string callerPath,
            int callerLine,
            Exception exception,
            bool isHighFrequency
        )
        {
            if (!IsEnabled)
                return;

            string color = LevelColors.TryGetValue(level, out string c) ? c : "#FFFFFF";
            string label = LevelLabels.TryGetValue(level, out string l) ? l : "???";

            var sb = new StringBuilder();

            // 高频日志（Trace）跳过时间戳输出以提升性能
            if (!isHighFrequency)
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                sb.Append("<color=#888888>[").Append(timestamp).Append("]</color> ");
            }

            // 级别标签（带颜色）
            sb.Append("<color=").Append(color).Append(">").Append(label).Append("</color> ");

            // 消息内容
            sb.Append("<color=").Append(color).Append(">").Append(message).Append("</color>");

            // 添加 payload
            if (!string.IsNullOrEmpty(payload))
            {
                sb.Append(" <color=#88FF88>| ").Append(payload).Append("</color>");
            }

            // 添加调用位置（关键：让 Unity 控制台可以识别跳转）
            if (!string.IsNullOrEmpty(callerPath))
            {
                // Unity 控制台的跳转格式: "(at Assets/Path/File.cs:123)"
                var relativePath = ToRelativePath(callerPath);
                sb.Append(" <color=#666666>(at ")
                    .Append(relativePath)
                    .Append(":")
                    .Append(callerLine)
                    .Append(")</color>");
            }

            string finalMessage = sb.ToString();

            // 根据级别选择对应的 Debug 方法
            switch (level)
            {
                case AsakiLogLevel.Debug:
                case AsakiLogLevel.Info:
                    UnityEngine.Debug.Log(finalMessage, context: null);
                    break;

                case AsakiLogLevel.Warning:
                    UnityEngine.Debug.LogWarning(finalMessage, context: null);
                    break;

                case AsakiLogLevel.Error:
                case AsakiLogLevel.Fatal:
                    if (exception != null)
                    {
                        // 如果有异常，用 LogException 获得完整堆栈
                        // 但先把 ALog 的消息输出
                        UnityEngine.Debug.LogError(finalMessage);
                        UnityEngine.Debug.LogException(exception);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError(finalMessage);
                    }
                    break;
            }
        }

        /// <summary>
        /// 将完整路径转换为相对于项目的路径
        /// </summary>
        private static string ToRelativePath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return string.Empty;

            // 标准化路径分隔符
            fullPath = fullPath.Replace('\\', '/');

            // 找到 Assets 目录的位置
            int assetsIndex = fullPath.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
            {
                // 返回从 Assets 开始的路径
                return fullPath.Substring(assetsIndex + 1);
            }

            // 如果找不到 Assets，返回文件名
            return Path.GetFileName(fullPath);
        }

        /// <summary>
        /// 手动设置启用状态
        /// </summary>
        public static void SetEnabled(bool enabled)
        {
            if (_instance != null)
            {
                _instance.IsEnabled = enabled;
            }
        }
    }
}
