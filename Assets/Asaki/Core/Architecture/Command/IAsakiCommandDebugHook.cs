using System;

namespace Asaki.Core.Architecture.Command
{
    /// <summary>
    /// 命令执行信息
    /// </summary>
    public readonly struct CommandExecutionInfo
    {
        public readonly string CommandType;
        public readonly long Timestamp;
        public readonly double ExecutionTimeMs;
        public readonly bool HasResult;
        public readonly string ResultType;
        public readonly string ResultValue;
        public readonly bool IsAsync;
        public readonly bool IsUndoCommand;
        public readonly bool HasError;
        public readonly string ErrorMessage;

        public CommandExecutionInfo(
            string commandType,
            long timestamp,
            double executionTimeMs,
            bool hasResult,
            string resultType,
            string resultValue,
            bool isAsync,
            bool isUndoCommand,
            bool hasError,
            string errorMessage
        )
        {
            CommandType = commandType;
            Timestamp = timestamp;
            ExecutionTimeMs = executionTimeMs;
            HasResult = hasResult;
            ResultType = resultType;
            ResultValue = resultValue;
            IsAsync = isAsync;
            IsUndoCommand = isUndoCommand;
            HasError = hasError;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// 命令调试钩子接口 - 用于编辑器调试
    /// </summary>
    public interface IAsakiCommandDebugHook
    {
        void OnCommandExecuting(string commandType, bool isAsync, bool isUndoCommand);
        void OnCommandExecuted(CommandExecutionInfo info);
        void OnCommandUndo(string commandType);
        void OnCommandRedo(string commandType);
    }

    /// <summary>
    /// 命令调试器 - 静态入口
    /// </summary>
    public static class AsakiCommandDebugger
    {
#if UNITY_EDITOR
        private static IAsakiCommandDebugHook _hook;
        private static bool _isEnabled = false;

        public static bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        public static void SetHook(IAsakiCommandDebugHook hook)
        {
            _hook = hook;
        }

        public static void ClearHook()
        {
            _hook = null;
        }

        internal static void NotifyExecuting(string commandType, bool isAsync, bool isUndoCommand)
        {
            if (_isEnabled && _hook != null)
            {
                _hook.OnCommandExecuting(commandType, isAsync, isUndoCommand);
            }
        }

        internal static void NotifyExecuted(CommandExecutionInfo info)
        {
            if (_isEnabled && _hook != null)
            {
                _hook.OnCommandExecuted(info);
            }
        }

        internal static void NotifyUndo(string commandType)
        {
            if (_isEnabled && _hook != null)
            {
                _hook.OnCommandUndo(commandType);
            }
        }

        internal static void NotifyRedo(string commandType)
        {
            if (_isEnabled && _hook != null)
            {
                _hook.OnCommandRedo(commandType);
            }
        }
#else
        public static bool IsEnabled { get; set; }

        public static void SetHook(IAsakiCommandDebugHook hook) { }

        public static void ClearHook() { }

        internal static void NotifyExecuting(
            string commandType,
            bool isAsync,
            bool isUndoCommand
        ) { }

        internal static void NotifyExecuted(CommandExecutionInfo info) { }

        internal static void NotifyUndo(string commandType) { }

        internal static void NotifyRedo(string commandType) { }
#endif
    }
}
