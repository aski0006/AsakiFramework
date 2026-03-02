using System;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.Compilation;

namespace Asaki.Editor.Utilities.Tools
{
    /// <summary>
    /// Unity 编译耗时监控器。
    /// 在每次编译完成后输出编译耗时到 Unity 控制台。
    /// </summary>
    [InitializeOnLoad]
    internal static class CompilationTimeMonitor
    {
        private static readonly Stopwatch _stopwatch = new();
        private static bool _isCompiling;

        /// <summary>
        /// 是否启用编译耗时监控
        /// </summary>
        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool("Asaki.CompilationTimeMonitor.Enabled", true);
            set => EditorPrefs.SetBool("Asaki.CompilationTimeMonitor.Enabled", value);
        }

        static CompilationTimeMonitor()
        {
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            EditorApplication.quitting += OnQuitting;
        }

        private static void OnCompilationStarted(object obj)
        {
            if (!IsEnabled)
                return;

            _isCompiling = true;
            _stopwatch.Restart();
        }

        private static void OnCompilationFinished(object obj)
        {
            if (!_isCompiling || !IsEnabled)
                return;

            _isCompiling = false;
            _stopwatch.Stop();

            double totalSeconds = _stopwatch.Elapsed.TotalSeconds;
            string timeStr = FormatTime(totalSeconds);
            string color = GetColorForTime(totalSeconds);

            UnityEngine.Debug.Log($"<color={color}>[Asaki] 编译完成，耗时: {timeStr}</color>");
        }

        /// <summary>
        /// 格式化时间显示
        /// </summary>
        private static string FormatTime(double seconds)
        {
            if (seconds < 1)
                return $"{seconds * 1000:F0}ms";
            if (seconds < 60)
                return $"{seconds:F2}s";
            int minutes = (int)(seconds / 60);
            double remainingSeconds = seconds % 60;
            return $"{minutes}m {remainingSeconds:F1}s";
        }

        /// <summary>
        /// 根据编译时间获取显示颜色
        /// </summary>
        private static string GetColorForTime(double seconds)
        {
            if (seconds < 5)
                return "#00FF00"; // 绿色：快速
            if (seconds < 15)
                return "#FFFF00"; // 黄色：正常
            if (seconds < 30)
                return "#FFA500"; // 橙色：较慢
            return "#FF0000"; // 红色：很慢
        }

        private static void OnQuitting()
        {
            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
        }
    }
}
