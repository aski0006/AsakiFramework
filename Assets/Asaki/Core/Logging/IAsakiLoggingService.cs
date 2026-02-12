using System;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.Context;

namespace Asaki.Core.Logging
{
    /// <summary>
    /// V2 日志服务接口。
    /// <para>核心职责是连接 ALog 静态入口与 Aggregator 聚合引擎，为日志记录和管理提供统一的接口。</para>
    /// </summary>
    /// <remarks>
    /// 该接口继承自 <see cref="IAsakiService"/> 和 <see cref="IDisposable"/>，意味着实现该接口的类需要提供 Asaki 服务相关的功能，并在不再使用时进行资源释放。
    /// </remarks>
    public interface IAsakiLoggingService : IAsakiService, IDisposable
    {
        /// <summary>
        /// [V2核心] 获取聚合器实例。
        /// <para>Editor Dashboard 将直接访问此属性以获取显示列表，实现零延迟调试。通过此属性，其他组件可以方便地获取日志聚合器，对聚合后的日志进行处理和展示。</para>
        /// </summary>
        AsakiLogAggregator Aggregator { get; }

        /// <summary>
        /// 记录追踪日志 (Trace/Info/Warning/Error 无异常版)。
        /// <para>适用于 Update/高频逻辑，会自动聚合。此方法用于在高频执行的逻辑中记录不同级别的日志，由于其高频特性，日志会自动进行聚合处理以减少资源消耗。</para>
        /// <param name="level">要记录的日志级别，如 <see cref="AsakiLogLevel.Debug"/>、<see cref="AsakiLogLevel.Info"/> 等。</param>
        /// <param name="message">日志消息内容，用于描述发生的事件。</param>
        /// <param name="payloadJson">负载 JSON 数据，可用于传递与日志相关的额外结构化信息。</param>
        /// <param name="file">产生日志的代码文件路径，有助于定位问题。</param>
        /// <param name="line">产生日志的代码在文件中的行号，有助于定位问题。</param>
        /// </summary>
        void LogTrace(
            AsakiLogLevel level,
            string message,
            string payloadJson,
            string file,
            int line
        );

        /// <summary>
        /// 记录异常日志 (Exception)。
        /// <para>强制捕获堆栈，适用于 try - catch 块。此方法专门用于在捕获到异常时记录日志，会强制捕获堆栈信息，方便开发者分析异常发生的上下文。</para>
        /// <param name="message">日志消息内容，用于描述异常发生的情况。</param>
        /// <param name="ex">捕获到的异常对象，包含详细的异常信息。</param>
        /// <param name="file">产生异常的代码文件路径，有助于定位问题。</param>
        /// <param name="line">产生异常的代码在文件中的行号，有助于定位问题。</param>
        /// </summary>
        void LogException(string message, Exception ex, string file, int line);

        /// <summary>
        /// 应用日志配置。
        /// <param name="serviceLogConfig">要应用的日志配置对象，包含各种日志相关的设置。</param>
        /// </summary>
        void ApplyConfig(AsakiLogConfig serviceLogConfig);
    }
}
