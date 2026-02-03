// File: Asaki/Core/Time/AsakiTimerDebugInfo.cs

using System;

namespace Asaki.Core.Time
{
    /// <summary>
    /// [编辑器专用] 定时器调试信息
    /// </summary>
    public struct AsakiTimerDebugInfo
    {
        public int Id;
        public ulong Version;
        public string Tag;
        public float Duration;
        public float Elapsed;
        public float Progress;
        public bool IsPaused;
        public bool IsLooped;
        public bool UseUnscaledTime;
        public bool HasCompleteCallback;
        public bool HasUpdateCallback;
        public string CallbackTargetType;

        public float RemainingTime => Duration - Elapsed;
        public bool IsCompleted => Elapsed >= Duration;
    }
}
