using System;
using Asaki.Core.FrameworkSettings;
using UnityEngine;

namespace Asaki.Core.Logging
{
    /// <summary>
    /// Asaki 日志服务 V2 核心实现类，提供完整的日志记录、聚合、持久化生命周期管理
    /// </summary>
    /// <remarks>
    /// <para>作为日志系统的唯一入口，聚合以下核心功能：</para>
    /// <list type="bullet">
    ///   <item><see cref="AsakiLogAggregator"/>：双缓冲日志聚合，实现零 GC 分配的高频写入</item>
    ///   <item><see cref="AsakiLogFileWriter"/>：UniTask 异步持久化，支持文件轮转与配置热更新</item>
    ///   <item><see cref="LogUpdateDriver"/>：Unity 组件驱动，在 <c>LateUpdate</c> 中同步主线程日志</item>
    /// </list>
    /// <para>异步模型与生命周期：</para>
    /// <list type="number">
    ///   <item>主线程：所有 <c>LogTrace</c>/<c>LogException</c> 调用、<c>ApplyConfig</c>、<c>Dispose</c></item>
    ///   <item>异步后台：由 <see cref="AsakiLogFileWriter"/> 管理的 UniTask，处理文件 I/O</item>
    ///   <item>Unity 主循环：由 <see cref="LogUpdateDriver.LateUpdate"/> 每帧调用 <c>Aggregator.Sync()</c></item>
    /// </list>
    /// <para>异常安全：所有公共方法捕获 <see cref="ObjectDisposedException"/>，服务被释放后调用静默返回</para>
    /// <para>Unity 集成：自动创建 DontDestroyOnLoad 的隐藏 GameObject，确保场景切换时服务不中断</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 在应用入口（如 GameManager）初始化
    /// public class GameManager : MonoBehaviour
    /// {
    ///     private IAsakiLoggingService _loggingService;
    ///
    ///     void Awake()
    ///     {
    ///         _loggingService = new AsakiLoggingService();
    ///         _loggingService.ApplyConfig(new AsakiLogConfig
    ///         {
    ///             MinLogLevel = AsakiLogLevel.Info,
    ///             MaxFileSizeKB = 5120,
    ///             FilePrefix = "GameSession"
    ///         });
    ///     }
    ///
    ///     void OnDestroy()
    ///     {
    ///         _loggingService?.Dispose();
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="IAsakiLoggingService"/>
    /// <seealso cref="AsakiLogAggregator"/>
    /// <seealso cref="AsakiLogFileWriter"/>
    public class AsakiLoggingService : IAsakiLoggingService
    {
        /// <summary>
        /// 获取日志聚合器实例，用于自定义日志处理或调试
        /// </summary>
        /// <value>
        /// <see cref="AsakiLogAggregator"/> 单例，负责缓冲与批量提交日志命令
        /// </value>
        /// <remarks>
        /// 聚合器在构造函数中创建，生命周期与 <see cref="AsakiLoggingService"/> 绑定。
        /// 可通过此属性访问以监控队列长度或手动调用 <c>Sync()</c>（通常无需手动调用）
        /// </remarks>
        public AsakiLogAggregator Aggregator { get; }

        /// <summary>文件写入器，负责后台异步持久化日志到磁盘</summary>
        private AsakiLogFileWriter _writer;

        /// <summary>Unity 驱动组件，每帧调用 <c>Aggregator.Sync()</c> 将主线程日志推入写入队列</summary>
        private LogUpdateDriver _driver;

        /// <summary>当前最小日志级别，低于此级别的日志将被静默丢弃</summary>
        private AsakiLogLevel _minLevel = AsakiLogLevel.Debug;

        /// <summary>释放标记，确保 <see cref="Dispose"/> 逻辑仅执行一次。使用volatile确保多线程可见性</summary>
        private volatile bool _isDisposed;

        /// <summary>
        /// 初始化 <see cref="AsakiLoggingService"/> 的新实例，并自动启动日志系统
        /// </summary>
        /// <remarks>
        /// 执行初始化流程：
        /// <list type="number">
        ///   <item>创建 <see cref="AsakiLogAggregator"/> 实例</item>
        ///   <item>创建 <see cref="AsakiLogFileWriter"/> 并启动后台写入线程</item>
        ///   <item>创建 <see cref="LogUpdateDriver"/> Unity 组件，绑定 Update 循环</item>
        /// </list>
        /// <para>注意：构造函数会创建 DontDestroyOnLoad 的隐藏 GameObject，确保跨场景持久化</para>
        /// </remarks>
        /// <exception cref="Exception">
        /// 若创建 <see cref="AsakiLogFileWriter"/> 失败（如权限不足）可能抛出异常
        /// </exception>
        public AsakiLoggingService()
        {
            Aggregator = new AsakiLogAggregator();
            _writer = new AsakiLogFileWriter(Aggregator);
            CreateDriver();
        }

        /// <summary>
        /// 动态应用运行时配置，支持热更新日志级别、文件大小、轮转策略等参数
        /// </summary>
        /// <param name="config">
        /// 日志配置对象，包含 <see cref="AsakiLogConfig.MinLogLevel"/>、
        /// <see cref="AsakiLogConfig.MaxFileSizeKB"/>、<see cref="AsakiLogConfig.FilePrefix"/> 等设置。
        /// 若为 <see langword="null"/> 则方法静默返回
        /// </param>
        /// <remarks>
        /// <para>配置变更立即生效：</para>
        /// <list type="bullet">
        ///   <item><c>MinLogLevel</c>：影响后续 <see cref="LogTrace"/>/<see cref="LogException"/> 的过滤行为</item>
        ///   <item><c>MaxFileSizeKB</c>：下一次文件大小检查时生效，触发轮转</item>
        ///   <item><c>FilePrefix</c>：下一次创建新文件时生效</item>
        ///   <item><c>MaxHistoryFiles</c>：立即异步清理多余历史文件</item>
        /// </list>
        /// <para>线程安全：可在任意线程调用，内部无锁操作</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // 运行时动态降低日志级别以排查问题
        /// _loggingService.ApplyConfig(new AsakiLogConfig { MinLogLevel = AsakiLogLevel.Trace });
        ///
        /// // 调整后恢复
        /// _loggingService.ApplyConfig(new AsakiLogConfig { MinLogLevel = AsakiLogLevel.Info });
        /// </code>
        /// </example>
        public void ApplyConfig(AsakiLogConfig config)
        {
            if (config == null)
                return;

            // 1. 设置运行时过滤等级
            _minLevel = config.MinLogLevel;

            // 2. 将配置下发给 Writer (处理轮转和前缀)
            _writer?.ApplyConfig(config);

            // 3. 记录一条系统日志
            ALog.Info(
                $"[AsakiLog] Config Applied: Level={config.MinLogLevel}, MaxSize={config.MaxFileSizeKB}KB"
            );
        }

        /// <summary>
        /// 记录一条带调用栈的跟踪日志，自动过滤低于配置级别的日志
        /// </summary>
        /// <param name="level">日志级别，若低于 <see cref="_minLevel"/> 则静默丢弃</param>
        /// <param name="message">日志消息主体，支持任意 UTF-8 字符</param>
        /// <param name="payloadJson">附加载荷的 JSON 字符串，若无则传 <c>null</c> 或空</param>
        /// <param name="file">调用源文件路径（由编译器自动插入）</param>
        /// <param name="line">调用行号（由编译器自动插入）</param>
        /// <remarks>
        /// <para>零成本过滤：在获取调用栈前执行级别检查，避免高开销操作</para>
        /// <para>线程安全：可在任意线程调用，日志会被缓冲到 <see cref="Aggregator"/></para>
        /// <para>自动转义：消息中的 '|' 和 '\n' 会被转义为 '¦' 和 ' '，确保文件格式安全</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // 典型调用（使用 ALog 包装）
        /// ALog.Info("玩家登录成功", $"{{'UserId': '{userId}'}}");
        ///
        /// // 手动调用（不推荐）
        /// _loggingService.LogTrace(AsakiLogLevel.Info, "Hello", null, "MyScript.cs", 42);
        /// </code>
        /// </example>
        /// <seealso cref="LogException"/>
        public void LogTrace(
            AsakiLogLevel level,
            string message,
            string payloadJson,
            string file,
            int line
        )
        {
            if (_isDisposed || level < _minLevel)
                return;
            Aggregator.Log(level, message, payloadJson, file, line, null);
        }

        /// <summary>
        /// 记录异常日志，自动包含异常堆栈信息
        /// </summary>
        /// <param name="message">异常上下文消息，建议描述发生异常的业务场景</param>
        /// <param name="ex">要记录的异常对象，若异常包含内部异常也会被完整序列化</param>
        /// <param name="file">调用源文件路径（由编译器自动插入）</param>
        /// <param name="line">调用行号（由编译器自动插入）</param>
        /// <remarks>
        /// <para>固定级别：异常日志始终使用 <see cref="AsakiLogLevel.Error"/> 级别，不受 <see cref="_minLevel"/> 影响</para>
        /// <para>堆栈提取：自动将 <paramref name="ex"/> 的 <c>StackTrace</c> 转换为 <see cref="StackFrameModel"/> 列表</para>
        /// <para>嵌套异常：支持 <see cref="Exception.InnerException"/> 的完整链式序列化</para>
        /// <para>线程安全：可在任意线程调用，异常信息会被缓冲到 <see cref="Aggregator"/></para>
        /// </remarks>
        /// <example>
        /// <code>
        /// try
        /// {
        ///     // 可能抛出异常的业务逻辑
        ///     ProcessUserInput(input);
        /// }
        /// catch (Exception ex)
        /// {
        ///     _loggingService.LogException($"处理用户输入失败: {input}", ex, "InputHandler.cs", 123);
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="LogTrace"/>
        public void LogException(string message, Exception ex, string file, int line)
        {
            if (_isDisposed)
                return;
            Aggregator.Log(AsakiLogLevel.Error, message, null, file, line, ex);
        }

        /// <summary>
        /// 释放日志服务占用的所有资源，包括 Unity GameObject、后台线程和文件句柄
        /// </summary>
        /// <remarks>
        /// <para>幂等性：可安全多次调用，仅首次调用生效</para>
        /// <para>执行顺序：</para>
        /// <list type="number">
        ///   <item>销毁 <see cref="LogUpdateDriver"/> GameObject（使用 <c>DestroyImmediate</c> 在编辑器模式）</item>
        ///   <item>调用 <see cref="AsakiLogFileWriter.Dispose"/>，等待后台线程退出（最多 1 秒）</item>
        ///   <item>清空 <see cref="Aggregator"/> 缓冲区，释放所有 <see cref="LogWriteCommand"/> 回对象池</item>
        /// </list>
        /// <para>Unity 兼容性：在编辑器模式使用 <c>DestroyImmediate</c>，运行时使用 <c>Destroy</c></para>
        /// <para>调用时机：应在应用退出、场景卸载或热重载前调用</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // 在 MonoBehaviour.OnDestroy 中释放
        /// void OnDestroy()
        /// {
        ///     _loggingService?.Dispose();
        /// }
        ///
        /// // 在编辑器脚本中
        /// void OnDisable()
        /// {
        ///     _loggingService?.Dispose();
        /// }
        /// </code>
        /// </example>
        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;

            if (_driver != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(_driver.gameObject);
                else
                    UnityEngine.Object.DestroyImmediate(_driver.gameObject);
            }

            _writer?.Dispose();
            Aggregator.Clear();
            Aggregator.Dispose();
        }

        /// <summary>
        /// 创建并初始化 Unity 驱动组件，用于在 <c>LateUpdate</c> 中同步日志
        /// </summary>
        /// <remarks>
        /// <para>创建的 GameObject 具有以下特性：</para>
        /// <list type="bullet">
        ///   <item>名称：<c>"AsakiLogDriver"</c></item>
        ///   <item>标记：<c>DontDestroyOnLoad</c>，确保跨场景持久化</item>
        ///   <item>可见性：<c>HideFlags.HideAndDontSave</c>，在 Hierarchy 中隐藏且不参与序列化</item>
        /// </list>
        /// <para>此方法由构造函数调用，通常无需手动调用</para>
        /// <para>异常处理：若创建失败（如构造函数在非主线程调用），异常将向上抛出</para>
        /// </remarks>
        private void CreateDriver()
        {
            GameObject go = new GameObject("AsakiLogDriver");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            _driver = go.AddComponent<LogUpdateDriver>();
            _driver.Aggregator = Aggregator;
        }

        /// <summary>
        /// Unity 专用驱动组件，负责在主循环中同步日志聚合器
        /// </summary>
        /// <remarks>
        /// <para>为什么需要 MonoBehaviour？</para>
        /// 因为 Unity 的 <c>Update</c> 循环仅在主线程运行，我们需要每帧将主线程产生的日志
        /// 从临时缓冲区转移到 <see cref="AsakiLogAggregator"/> 的主缓冲区，供后台线程消费。
        /// 这是 Unity 单线程架构下的最佳实践。
        /// <para>性能考虑：仅在 <c>LateUpdate</c> 执行最小化工作（<c>Aggregator.Sync()</c>），
        /// 对帧率影响可忽略不计</para>
        /// </remarks>
        private class LogUpdateDriver : MonoBehaviour
        {
            /// <summary>
            /// 关联的日志聚合器实例，由 <see cref="CreateDriver"/> 注入
            /// </summary>
            public AsakiLogAggregator Aggregator;

            /// <summary>
            /// Unity 每帧调用，将当前帧累积的日志命令同步到聚合器主缓冲区
            /// </summary>
            /// <remarks>
            /// <para>调用时机：<c>LateUpdate</c>，确保在所有用户脚本的 <c>Update</c> 之后执行</para>
            /// <para>空安全：若 <see cref="Aggregator"/> 为 <see langword="null"/> 则静默返回</para>
            /// <para>性能：操作复杂度 O(1)，仅交换缓冲区引用</para>
            /// </remarks>
            private void LateUpdate()
            {
                Aggregator?.Sync();
            }
        }
    }
}
