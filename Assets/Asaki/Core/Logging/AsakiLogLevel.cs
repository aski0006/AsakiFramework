namespace Asaki.Core.Logging
{
    /// <summary>
    /// 定义了日志级别枚举，用于标识不同类型的日志信息。
    /// </summary>
    /// <remarks>
    /// 这些日志级别有助于在应用程序中对不同重要程度的日志进行分类和管理。
    /// </remarks>
    public enum AsakiLogLevel
    {
        /// <summary>
        /// 用于开发过程中的调试信息，帮助开发者排查问题。
        /// </summary>
        Debug = 1,

        /// <summary>
        /// 一般性的信息日志，用于记录程序正常运行时的关键事件。
        /// </summary>
        Info = 2,

        /// <summary>
        /// 表示潜在问题的警告日志，提示可能需要关注的情况。
        /// </summary>
        Warning = 3,

        /// <summary>
        /// 错误日志，记录程序执行过程中发生的错误，但不影响程序的整体运行。
        /// </summary>
        Error = 4,

        /// <summary>
        /// 严重错误日志，通常表示程序无法继续正常运行的致命错误。
        /// </summary>
        Fatal = 5,

        /// <summary>
        /// 表示不记录任何日志，用于关闭日志记录功能。
        /// </summary>
        None = 99,
    }
}
