using System.Diagnostics;
using Asaki.Core.Logging;

namespace Asaki.Unity.Services.Audio
{
    /// <summary>
    /// 音频系统日志工具
    /// <para>使用ALog日志系统，支持条件编译和日志聚合。</para>
    /// <para>生产环境自动禁用调试日志，减少性能开销。</para>
    /// </summary>
    /// <author>Asaki Framework</author>
    /// <version>2.0</version>
    internal static class AudioLogger
    {
        private const string LogPrefix = "[AsakiAudio]";

        #region Service Lifecycle Logs

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogServiceInitialized()
        {
            ALog.Info($"{LogPrefix} Service initialized");
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogPoolInitialized(string statistics)
        {
            ALog.Info($"{LogPrefix} Pool initialized", statistics);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogServiceDisposed()
        {
            ALog.Info($"{LogPrefix} Service disposed");
        }

        #endregion

        #region Playback Logs

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogPlaybackStarted(int handleId, string clipPath)
        {
            ALog.Info($"{LogPrefix} Playback started", $"Handle={handleId}, Path={clipPath}");
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogPlaybackCompleted(int handleId)
        {
            ALog.Info($"{LogPrefix} Playback completed", $"Handle={handleId}");
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogPlaybackCanceled(int handleId)
        {
            ALog.Info($"{LogPrefix} Playback canceled", $"Handle={handleId}");
        }

        #endregion

        #region Error Logs

        public static void LogPlaybackError(int handleId, string errorMessage)
        {
            ALog.Error($"{LogPrefix} Playback error", $"Handle={handleId}, Error={errorMessage}");
        }

        public static void LogConfigError(string errorMessage)
        {
            ALog.Error($"{LogPrefix} Config error", errorMessage);
        }

        public static void LogPoolError(string errorMessage)
        {
            ALog.Error($"{LogPrefix} Pool error", errorMessage);
        }

        #endregion

        #region Warning Logs

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(string message)
        {
            ALog.Warn($"{LogPrefix} {message}");
        }

        #endregion

        #region Group Control Logs

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogGroupOperation(string operation, int groupId)
        {
            ALog.Info($"{LogPrefix} Group {operation}", $"GroupId={groupId}");
        }

        #endregion

        #region 3D Audio Logs

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log3DAudioCreated(int handleId, UnityEngine.Vector3 position)
        {
            ALog.Info($"{LogPrefix} 3D Audio created", $"Handle={handleId}, Position={position}");
        }

        #endregion
    }
}
