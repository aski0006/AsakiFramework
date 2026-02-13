using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace Asaki.Core.Logging
{
    /// <summary>
    /// Asaki日志聚合器，负责收集、聚合和管理日志信息。
    /// 该类处理日志的输入、聚合以及与IO相关的操作，以实现高效的日志记录。
    /// </summary>
    /// <remarks>
    /// 线程安全说明：
    /// <para>1. <see cref="Log"/> 方法可在任意线程调用，日志会被安全地加入输入队列</para>
    /// <para>2. <see cref="Sync"/> 方法应在主线程调用（通常在LateUpdate中），处理日志聚合</para>
    /// <para>3. <see cref="SwapIOBuffer"/> 方法由写入线程调用，与主线程交换缓冲区</para>
    /// <para>4. <see cref="GetSnapshot"/> 方法可在任意线程调用，返回日志快照的副本</para>
    /// </remarks>
    public class AsakiLogAggregator
    {
        // === 内部结构体定义 ===
        /// <summary>
        /// 表示日志数据包的内部结构体。
        /// 包含日志的各种属性，如级别、消息、负载、文件、行号、异常、时间戳和堆栈跟踪。
        /// </summary>
        private struct LogPacket
        {
            /// <summary>
            /// 日志级别。
            /// </summary>
            public AsakiLogLevel Level;

            /// <summary>
            /// 日志消息。
            /// </summary>
            public string Message;

            /// <summary>
            /// 日志附带的负载信息。
            /// </summary>
            public string Payload;

            /// <summary>
            /// 记录日志的文件路径。
            /// </summary>
            public string File;

            /// <summary>
            /// 记录日志的行号。
            /// </summary>
            public int Line;

            /// <summary>
            /// 相关的异常对象，如果有的话。
            /// </summary>
            public Exception Exception;

            /// <summary>
            /// 日志记录的时间戳（以Utc时间的Ticks表示）。
            /// </summary>
            public long Timestamp;

            /// <summary>
            /// 调用方捕获的堆栈跟踪信息。
            /// 在日志入队时立即捕获，确保堆栈信息指向真实的调用位置。
            /// </summary>
            public StackTrace CapturedStackTrace;
        }

        /// <summary>
        /// 表示日志签名的内部结构体，用于唯一标识一条日志。
        /// 实现了 <see cref="IEquatable{T}"/> 接口，以便在字典中进行高效比较。
        /// </summary>
        private struct LogSignature : IEquatable<LogSignature>
        {
            private readonly int _hash;
            private readonly string _file;
            private readonly int _line;
            private readonly string _msg;
            private readonly string _exceptionType;

            /// <summary>
            /// 使用文件路径、行号、消息和异常信息初始化日志签名。
            /// 通过组合这些信息生成一个唯一的哈希值。
            /// 异常信息会被纳入签名，确保不同异常的日志不会被错误合并。
            /// </summary>
            /// <param name="file">记录日志的文件路径。</param>
            /// <param name="line">记录日志的行号。</param>
            /// <param name="msg">日志消息。</param>
            /// <param name="ex">异常对象，用于区分不同异常的日志。</param>
            public LogSignature(string file, int line, string msg, Exception ex)
            {
                _file = file;
                _line = line;
                _msg = msg;
                _exceptionType = ex?.GetType().FullName;
                unchecked
                {
                    int hash = (file?.GetHashCode() ?? 0) * 397;
                    hash ^= line;
                    hash ^= (msg?.GetHashCode() ?? 0) * 31;
                    hash ^= (_exceptionType?.GetHashCode() ?? 0) * 17;
                    _hash = hash;
                }
            }

            /// <summary>
            /// 获取日志签名的哈希值。
            /// </summary>
            /// <returns>哈希值。</returns>
            public override int GetHashCode()
            {
                return _hash;
            }

            /// <summary>
            /// 判断当前日志签名是否与另一个日志签名相等。
            /// </summary>
            /// <param name="other">要比较的另一个日志签名。</param>
            /// <returns>如果相等则返回 true，否则返回 false。</returns>
            public bool Equals(LogSignature other)
            {
                return _hash == other._hash
                    && _line == other._line
                    && string.Equals(_file, other._file)
                    && string.Equals(_msg, other._msg)
                    && string.Equals(_exceptionType, other._exceptionType);
            }
        }

        // === 1. 输入端 (生产者) ===
        /// <summary>
        /// 用于存储日志数据包的并发队列。
        /// 日志从这里进入聚合器。
        /// </summary>
        private readonly ConcurrentQueue<LogPacket> _inputQueue = new ConcurrentQueue<LogPacket>();

        /// <summary>
        /// 输入队列的最大深度限制，用于背压保护。
        /// 如果队列达到此深度，会根据日志级别进行相应处理（如丢弃Trace/Info级别日志）。
        /// </summary>
        private const int MAX_QUEUE_DEPTH = 5000;

        /// <summary>
        /// 使用Interlocked精确获取队列深度的辅助方法
        /// </summary>
        private long GetQueueDepth()
        {
            return _inputQueue.Count;
        }

        // === 2. 聚合端 (主线程状态) ===
        /// <summary>
        /// 用于存储日志签名与日志模型映射关系的字典。
        /// 通过日志签名快速查找和更新对应的日志模型。
        /// </summary>
        /// <remarks>访问此字典必须在 <see cref="_aggregationLock"/> 锁保护下进行</remarks>
        private readonly Dictionary<LogSignature, AsakiLogModel> _signatureMap =
            new Dictionary<LogSignature, AsakiLogModel>();

        /// <summary>
        /// 用于存储当前显示的日志模型列表。
        /// 提供给外部获取日志快照。
        /// </summary>
        /// <remarks>访问此列表必须在 <see cref="_aggregationLock"/> 锁保护下进行</remarks>
        private readonly List<AsakiLogModel> _displayList = new List<AsakiLogModel>();

        /// <summary>
        /// 日志模型的ID计数器，使用Interlocked确保线程安全
        /// </summary>
        private int _idCounter = 1;

        /// <summary>
        /// 用于保护聚合状态的读写锁，支持多读单写
        /// </summary>
        private readonly ReaderWriterLockSlim _aggregationLock = new ReaderWriterLockSlim(
            LockRecursionPolicy.NoRecursion
        );

        // === 3. IO 端 (双缓冲) ===
        // 这种模式下，MainThread 只写 current，Writer 只读 back，互不干扰
        /// <summary>
        /// 当前用于写入日志写入命令的缓冲区。
        /// 主线程将日志写入命令添加到此缓冲区。
        /// </summary>
        private List<LogWriteCommand> _ioBufferCurrent = new List<LogWriteCommand>(256);

        /// <summary>
        /// 备用的日志写入命令缓冲区。
        /// 当当前缓冲区已满时，与当前缓冲区交换，供写入线程处理。
        /// </summary>
        private List<LogWriteCommand> _ioBufferBack = new List<LogWriteCommand>(256);

        /// <summary>
        /// 用于交换IO缓冲区的锁。
        /// 仅在交换缓冲区指针的瞬间加锁，以确保线程安全。
        /// </summary>
        private readonly object _ioSwapLock = new object();

        // === API ===

        /// <summary>
        /// 接收日志信息并将其添加到输入队列。
        /// 同时进行简单的背压保护，当队列达到最大深度时，根据日志级别决定是否丢弃日志。
        /// </summary>
        /// <param name="level">日志级别。</param>
        /// <param name="message">日志消息。</param>
        /// <param name="payload">日志附带的负载信息。</param>
        /// <param name="file">记录日志的文件路径。</param>
        /// <param name="line">记录日志的行号。</param>
        /// <param name="ex">相关的异常对象，如果有的话。</param>
        /// <param name="stackTrace">调用方捕获的堆栈跟踪，用于保留真实的调用链。</param>
        /// <remarks>此方法线程安全，可在任意线程调用</remarks>
        public void Log(
            AsakiLogLevel level,
            string message,
            string payload,
            string file,
            int line,
            Exception ex,
            StackTrace stackTrace = null
        )
        {
            if (GetQueueDepth() >= MAX_QUEUE_DEPTH)
            {
                if (level < AsakiLogLevel.Error)
                    return;
            }

            _inputQueue.Enqueue(
                new LogPacket
                {
                    Level = level,
                    Message = message,
                    Payload = payload,
                    File = file,
                    Line = line,
                    Exception = ex,
                    Timestamp = DateTime.UtcNow.Ticks,
                    CapturedStackTrace = stackTrace,
                }
            );
        }

        /// <summary>
        /// 外部驱动调用，通常在LateUpdate中执行。
        /// 从输入队列中批量取出日志数据包，并进行处理。
        /// </summary>
        /// <param name="batchLimit">每次同步处理的最大日志数量，默认为1000。</param>
        /// <remarks>此方法应在主线程调用，使用 IO 缓冲区锁保护</remarks>
        public void Sync(int batchLimit = 1000)
        {
            lock (_ioSwapLock)
            {
                int count = 0;
                // 批量从并发队列取出
                while (count < batchLimit && _inputQueue.TryDequeue(out LogPacket packet))
                {
                    count++;
                    ProcessSingleLog(ref packet);
                }
            }
        }

        /// <summary>
        /// 获取当前显示的日志模型列表的快照。
        /// 此方法使用读写锁确保线程安全，返回日志列表的副本。
        /// </summary>
        /// <returns>当前显示的日志模型列表的副本。</returns>
        /// <remarks>此方法线程安全，可在任意线程调用</remarks>
        public List<AsakiLogModel> GetSnapshot()
        {
            _aggregationLock.EnterReadLock();
            try
            {
                // 创建深拷贝以避免外部修改影响内部状态
                var snapshot = new List<AsakiLogModel>(_displayList.Count);
                foreach (var model in _displayList)
                {
                    // 创建副本以避免引用被外部修改
                    snapshot.Add(CreateModelCopy(model));
                }
                return snapshot;
            }
            finally
            {
                _aggregationLock.ExitReadLock();
            }
        }

        /// <summary>
        /// 创建日志模型的浅拷贝，用于快照返回
        /// </summary>
        private AsakiLogModel CreateModelCopy(AsakiLogModel source)
        {
            return new AsakiLogModel
            {
                ID = source.ID,
                Level = source.Level,
                LastTimestamp = source.LastTimestamp,
                FlushedCount = source.FlushedCount,
                Count = source.Count,
                Message = source.Message,
                PayloadJson = source.PayloadJson,
                StackFrames =
                    source.StackFrames != null
                        ? new List<StackFrameModel>(source.StackFrames)
                        : null,
                CallerPath = source.CallerPath,
                CallerLine = source.CallerLine,
            };
        }

        /// <summary>
        /// 供写入线程调用，交换IO缓冲区。
        /// 写入线程拿走填满的当前缓冲区，给聚合器一个空的备用缓冲区。
        /// </summary>
        /// <returns>填满的当前缓冲区，如果当前缓冲区为空则返回null。</returns>
        /// <remarks>
        /// 此方法线程安全，可在任意线程调用。
        /// 使用 <see cref="_ioSwapLock"/> 保护，与 <see cref="Sync"/> 方法互斥访问 IO 缓冲区，
        /// 确保在高并发环境下不会出现数据竞争和丢失。
        /// </remarks>
        public List<LogWriteCommand> SwapIOBuffer()
        {
            lock (_ioSwapLock)
            {
                if (_ioBufferCurrent.Count == 0)
                    return null;

                var filledBuffer = _ioBufferCurrent;
                _ioBufferCurrent = _ioBufferBack; // 换上空盘子
                _ioBufferBack = filledBuffer; // 拿走满盘子

                // 确保换上来的 buffer 是干净的 (理论上 Writer 会清理，但防御性清空)
                _ioBufferCurrent.Clear();

                return filledBuffer;
            }
        }

        // === 内部逻辑 ===

        /// <summary>
        /// 处理单个日志数据包。
        /// 根据日志签名查找或创建对应的日志模型，并更新相关信息。
        /// 同时将日志写入命令添加到当前IO缓冲区。
        /// </summary>
        /// <param name="p">要处理的日志数据包。</param>
        /// <remarks>
        /// 此方法在主线程调用，由 <see cref="Sync"/> 方法的 <see cref="_ioSwapLock"/> 锁保护，
        /// 因此访问 IO 缓冲区时无需额外加锁。但访问聚合状态字典仍需使用 <see cref="_aggregationLock"/>。
        /// </remarks>
        private void ProcessSingleLog(ref LogPacket p)
        {
            LogSignature sig = new LogSignature(p.File, p.Line, p.Message, p.Exception);

            // 使用写锁保护聚合状态
            _aggregationLock.EnterWriteLock();
            try
            {
                if (_signatureMap.TryGetValue(sig, out AsakiLogModel model))
                {
                    // === Inc ===
                    Interlocked.Increment(ref model.Count);
                    model.LastTimestamp = p.Timestamp;

                    // 池化创建 Command
                    LogWriteCommand cmd = LogCommandPool.Get();
                    cmd.Type = LogWriteCommand.CmdType.Inc;
                    cmd.Id = model.ID;
                    cmd.IncAmount = 1;

                    // 写入 Current Buffer (此时只有主线程在访问 Current，无需锁)
                    _ioBufferCurrent.Add(cmd);
                }
                else
                {
                    // === Def ===
                    // 解析堆栈（优先使用调用方捕获的堆栈）
                    var stack = CaptureSmartStackTrace(p.Exception, p.CapturedStackTrace);
                    string stackJson =
                        stack != null && stack.Count > 0
                            ? JsonUtility.ToJson(new StackWrapper { F = stack })
                            : "{}";

                    // 使用Interlocked.Increment确保ID唯一性
                    int newId = Interlocked.Increment(ref _idCounter);

                    model = new AsakiLogModel
                    {
                        ID = newId,
                        LastTimestamp = p.Timestamp,
                        Level = p.Level,
                        Message = p.Message,
                        PayloadJson = p.Payload,
                        CallerPath = p.File,
                        CallerLine = p.Line,
                        Count = 1,
                        StackFrames = stack,
                    };

                    _signatureMap[sig] = model;
                    _displayList.Add(model);

                    // 池化创建 Command
                    LogWriteCommand cmd = LogCommandPool.Get();
                    cmd.Type = LogWriteCommand.CmdType.Def;
                    cmd.Id = model.ID;
                    cmd.LevelInt = (int)model.Level;
                    cmd.Timestamp = model.LastTimestamp;
                    cmd.Message = model.Message;
                    cmd.Payload = model.PayloadJson;
                    cmd.Path = model.CallerPath;
                    cmd.Line = model.CallerLine;
                    cmd.StackJson = stackJson; // [优化] 预序列化

                    _ioBufferCurrent.Add(cmd);
                }
            }
            finally
            {
                _aggregationLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 捕获智能堆栈跟踪信息。
        /// 优先使用调用方传入的堆栈跟踪（保留真实调用链），其次从异常获取，最后才使用当前调用栈。
        /// 对堆栈信息进行处理，过滤出用户代码相关的堆栈帧，并返回处理后的堆栈帧模型列表。
        /// </summary>
        /// <param name="ex">相关的异常对象，如果有的话。</param>
        /// <param name="capturedTrace">调用方捕获的堆栈跟踪，优先级最高。</param>
        /// <returns>处理后的堆栈帧模型列表。</returns>
        private List<StackFrameModel> CaptureSmartStackTrace(
            Exception ex,
            StackTrace capturedTrace = null
        )
        {
            var list = new List<StackFrameModel>();

            StackTrace trace;
            if (capturedTrace != null)
            {
                trace = capturedTrace;
            }
            else if (ex != null)
            {
                trace = new StackTrace(ex, true);
            }
            else
            {
                trace = new StackTrace(3, true);
            }

            var frames = trace.GetFrames();
            if (frames == null)
                return list;

            foreach (StackFrame frame in frames)
            {
                MethodBase method = frame.GetMethod();
                if (method == null)
                    continue;

                string fileName = frame.GetFileName();
                fileName = fileName?.Replace('\\', '/') ?? string.Empty;

                bool isUserCode =
                    fileName.Contains("/Assets/")
                    && !fileName.Contains("/Asaki/")
                    && !fileName.Contains("Library/PackageCache");

                list.Add(
                    new StackFrameModel
                    {
                        DeclaringType = method.DeclaringType?.Name ?? "Global",
                        MethodName = method.Name,
                        FilePath = fileName,
                        LineNumber = frame.GetFileLineNumber(),
                        IsUserCode = isUserCode,
                    }
                );
            }
            return list;
        }

        /// <summary>
        /// 清除聚合器中的所有日志数据。
        /// 包括日志签名与日志模型的映射关系、显示列表，并重置ID计数器。
        /// IO缓冲区会在写入线程下次交换时被清空。
        /// </summary>
        /// <remarks>此方法线程安全</remarks>
        public void Clear()
        {
            _aggregationLock.EnterWriteLock();
            try
            {
                _signatureMap.Clear();
                _displayList.Clear();
                Interlocked.Exchange(ref _idCounter, 1);
            }
            finally
            {
                _aggregationLock.ExitWriteLock();
            }
            // IO Buffer 会在 Writer 下次 Swap 时被清空
        }

        /// <summary>
        /// 释放聚合器使用的资源
        /// </summary>
        public void Dispose()
        {
            _aggregationLock?.Dispose();
        }

        /// <summary>
        /// 用于序列化堆栈帧模型列表的内部结构体。
        /// 包含一个 <see cref="List{StackFrameModel}"/> 类型的字段 F。
        /// </summary>
        [Serializable]
        private struct StackWrapper
        {
            /// <summary>
            /// 堆栈帧模型列表。
            /// </summary>
            public List<StackFrameModel> F;
        }
    }
}
