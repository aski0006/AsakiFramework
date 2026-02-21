using System.Collections.Concurrent;
using Asaki.Core.FrameworkSettings;

namespace Asaki.Core.Logging
{
    /// <summary>
    /// 日志写入命令对象，封装单条日志序列化所需的所有数据，支持对象池复用以实现零 GC 分配
    /// </summary>
    /// <remarks>
    /// <para>设计优化：</para>
    /// <list type="bullet">
    ///   <item>由结构体改为类，支持引用传递，避免大结构体拷贝开销</item>
    ///   <item>复用单个实例处理 Def（定义）和 Inc（递增）两种命令类型</item>
    ///   <item>所有字段公开，消除属性访问器开销</item>
    ///   <item>轻量级 <see cref="Reset"/> 方法，快速清理引用便于池化</item>
    /// </list>
    /// <para>线程模型：实例本身非线程安全，由 <see cref="LogCommandPool"/> 保证线程安全的获取与归还</para>
    /// <para>生命周期：必须从 <see cref="LogCommandPool.Get"/> 获取，使用完毕后必须调用 <see cref="LogCommandPool.Return"/> 归还</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 正确用法：获取 → 填充 → 提交 → 归还
    /// var cmd = LogCommandPool.Get();
    /// cmd.Type = LogWriteCommand.CmdType.Def;
    /// cmd.Id = logId;
    /// cmd.Message = "User login";
    /// cmd.Payload = "{'userId': 123}";
    /// aggregator.Submit(cmd);
    /// // 注意：cmd 在 Submit 后由系统负责归还，不应手动 Return
    ///
    /// // Inc 命令用法
    /// var incCmd = LogCommandPool.Get();
    /// incCmd.Type = LogWriteCommand.CmdType.Inc;
    /// incCmd.Id = existingLogId;
    /// incCmd.IncAmount = 1;
    /// aggregator.Submit(incCmd);
    /// </code>
    /// </example>
    /// <seealso cref="LogCommandPool"/>
    /// <seealso cref="AsakiLogAggregator"/>
    public class LogWriteCommand
    {
        /// <summary>
        /// 命令类型枚举：Def（定义新日志）或 Inc（递增已存在日志的计数）
        /// </summary>
        public enum CmdType
        {
            /// <summary>定义命令：创建新的完整日志条目</summary>
            Def,

            /// <summary>递增命令：对指定日志 ID 的计数进行累加</summary>
            Inc,
        }

        /// <summary>命令类型，决定后续字段的解析方式</summary>
        /// <remarks>
        /// <list type="bullet">
        ///   <item><see cref="CmdType.Def"/>：使用 <c>LevelInt</c>, <c>Message</c>, <c>Payload</c> 等字段</item>
        ///   <item><see cref="CmdType.Inc"/>：仅使用 <c>Id</c> 和 <c>IncAmount</c> 字段</item>
        /// </list>
        /// </remarks>
        public CmdType Type;

        /// <summary>日志唯一标识符，用于 Inc 命令定位目标日志</summary>
        public int Id;

        // Def Data
        /// <summary>日志级别整数值（存储 int 避免枚举转换开销）</summary>
        /// <remarks>合法值参考 <see cref="AsakiLogLevel"/> 枚举定义</remarks>
        public int LevelInt;

        /// <summary>Unix 毫秒时间戳，记录日志产生的精确时间</summary>
        public long Timestamp;

        /// <summary>日志消息主体（已转义处理，不含 '|' 和 '\n'）</summary>
        public string Message;

        /// <summary>附加载荷的预处理 JSON 字符串（可能为 null）</summary>
        public string Payload;

        /// <summary>调用者源文件路径（如 "Assets/Scripts/Player.cs"）</summary>
        public string Path;

        /// <summary>调用者行号（编译器自动传入）</summary>
        public int Line;

        /// <summary>序列化后的堆栈信息 JSON（由 <see cref="StackFrameModel"/> 数组转换）</summary>
        public string StackJson;

        // Inc Data
        /// <summary>递增数量，用于 Inc 命令（Def 命令忽略此字段）</summary>
        public int IncAmount;

        /// <summary>
        /// 重置对象状态，清理所有引用字段以便池化复用
        /// </summary>
        /// <remarks>
        /// <para>调用时机：在 <see cref="LogCommandPool.Return"/> 内部自动调用</para>
        /// <para>性能：仅清空引用字段，不重置值类型字段（<c>Type</c>, <c>Id</c> 等），避免多余开销</para>
        /// <para>注意：此方法不应由业务代码直接调用</para>
        /// </remarks>
        public void Reset()
        {
            Message = null;
            Payload = null;
            Path = null;
            StackJson = null;
        }
    }

    /// <summary>
    /// 日志命令对象池，提供线程安全的 <see cref="LogWriteCommand"/> 复用机制，实现零 GC 分配的高性能日志
    /// </summary>
    /// <remarks>
    /// <para>线程安全实现：</para>
    /// <list type="bullet">
    ///   <item>使用 <see cref="ConcurrentStack{T}"/> 保证无锁的并发获取与归还</item>
    ///   <item>支持多线程同时 <see cref="Get"/> 和 <see cref="Return"/> 而不产生竞态</item>
    ///   <item>无容量上限，池大小随并发压力自动增长</item>
    /// </list>
    /// <para>性能特征：</para>
    /// <list type="bullet">
    ///   <item>获取操作平均时间复杂度 O(1)，无锁竞争时接近零开销</item>
    ///   <item>归还操作平均时间复杂度 O(1)，仅原子操作</item>
    ///   <item>自动扩容：池为空时直接创建新实例，无等待</item>
    /// </list>
    /// <para>内存管理：必须与 <see cref="AsakiLogAggregator"/> 配合使用，确保命令最终归还</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 从池中获取命令对象
    /// var cmd = LogCommandPool.Get();
    ///
    /// // 填充数据（示例为 Def 类型）
    /// cmd.Type = LogWriteCommand.CmdType.Def;
    /// cmd.Id = logIdCounter++;
    /// cmd.LevelInt = (int)AsakiLogLevel.Info;
    /// cmd.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    /// cmd.Message = "Player collected item";
    /// cmd.Path = "PlayerController.cs";
    /// cmd.Line = 128;
    ///
    /// // 提交到聚合器（聚合器负责后续归还）
    /// aggregator.Submit(cmd);
    ///
    /// // 错误示范：不要手动归还提交后的命令
    /// // LogCommandPool.Return(cmd); // ❌ 重复归还导致池污染
    /// </code>
    /// </example>
    /// <seealso cref="LogWriteCommand"/>
    /// <seealso cref="AsakiLogAggregator.Sync"/>
    public static class LogCommandPool
    {
        /// <summary>
        /// 获取对象池最大容量（从全局配置获取），防止高负载场景下内存无限增长
        /// </summary>
        private static int MaxPoolSize => AsakiPoolGlobalConfig.Instance.LogCommandPoolMaxSize;

        /// <summary>线程安全的并发栈，存储可复用的命令对象</summary>
        /// <remarks>
        /// <para>使用 <see cref="ConcurrentStack{T}"/> 而非普通 Stack + lock，减少锁竞争</para>
        /// <para>初始为空，随 <see cref="Return"/> 调用逐渐填充</para>
        /// </remarks>
        private static readonly ConcurrentStack<LogWriteCommand> _pool =
            new ConcurrentStack<LogWriteCommand>();

        /// <summary>
        /// 从池中获取一个 <see cref="LogWriteCommand"/> 实例
        /// </summary>
        /// <returns>
        /// <para>若池中有可用对象，返回已复用的实例（状态未初始化）</para>
        /// <para>若池为空，则创建全新实例，Get 操作永不返回 <see langword="null"/></para>
        /// </returns>
        /// <remarks>
        /// <para>返回的对象可能包含残留数据，调用方必须重置所有字段</para>
        /// <para>线程安全：可在任意线程调用，无锁竞争性能最优</para>
        /// <para>内存保证：返回对象一定有效，无需空检查</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // 获取后必须设置 Type 和其他字段
        /// var cmd = LogCommandPool.Get();
        /// cmd.Type = LogWriteCommand.CmdType.Def; // 必须先设置 Type
        /// // ... 设置其他字段
        /// </code>
        /// </example>
        public static LogWriteCommand Get()
        {
            return _pool.TryPop(out LogWriteCommand cmd) ? cmd : new LogWriteCommand();
        }

        /// <summary>
        /// 将使用完毕的 <see cref="LogWriteCommand"/> 归还到池中以便复用
        /// </summary>
        /// <param name="cmd">要归还的命令对象。允许为 <see langword="null"/>（方法直接返回）</param>
        /// <remarks>
        /// <para>内部逻辑：调用 <paramref name="cmd"/>.<see cref="LogWriteCommand.Reset"/> 清理引用，然后压入栈</para>
        /// <para>线程安全：可在任意线程调用，与 <see cref="Get"/> 无锁并发</para>
        /// <para>调用责任：通常由 <see cref="AsakiLogAggregator"/> 自动调用，业务代码不应直接调用</para>
        /// <para>空安全：若 <paramref name="cmd"/> 为 <see langword="null"/> 则静默忽略，不抛出异常</para>
        /// <para>容量限制：池大小超过 <see cref="MAX_POOL_SIZE"/> 时，对象将被丢弃而非归还</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // 在自定义聚合器中手动管理命令生命周期
        /// var cmd = GetCommandFromSomewhere();
        /// try
        /// {
        ///     ProcessCommand(cmd);
        /// }
        /// finally
        /// {
        ///     // 确保命令最终归还
        ///     LogCommandPool.Return(cmd);
        /// }
        /// </code>
        /// </example>
        public static void Return(LogWriteCommand cmd)
        {
            if (cmd == null)
                return;
            cmd.Reset();
            if (_pool.Count < MaxPoolSize)
                _pool.Push(cmd);
        }
    }
}
