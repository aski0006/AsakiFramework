using System;
using System.IO;
using System.Linq;
using System.Text;
using Asaki.Core.Configs;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Core.Logging
{
    /// <summary>
    /// Asaki 日志文件异步写入器，负责将聚合后的日志数据持久化到本地文件
    /// </summary>
    /// <remarks>
    /// <para>功能特性：</para>
    /// <list type="bullet">
    ///   <item>双缓冲 + 后台线程写入，零卡顿主线程</item>
    ///   <item>基于文件大小自动轮转（默认 2 MB）</item>
    ///   <item>保留指定数量的历史文件（默认 10 个）</item>
    ///   <item>可热更新配置（文件大小、前缀、保留数）</item>
    ///   <item>进程安全：使用 <c>FileShare.Read</c> 允许外部实时查看</item>
    /// </list>
    /// <para>线程模型：</para>
    /// <list type="number">
    ///   <item>主线程：调用 <see cref="ApplyConfig"/> 与 <see cref="Dispose"/></item>
    ///   <item>Writer 线程：独占执行 <see cref="WriteLoopAsync"/> 与文件 I/O</item>
    ///   <item>ThreadPool：异步执行历史清理，避免阻塞 Writer</item>
    /// </list>
    /// <para>异常策略：所有 I/O 异常仅记录到 Unity 控制台，永不抛出；Writer 线程崩溃后自动退出并释放资源</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 在应用启动时创建
    /// var writer = new AsakiLogFileWriter(aggregator);
    ///
    /// // 运行时动态调整配置
    /// writer.ApplyConfig(new AsakiLogConfig
    /// {
    ///     MaxFileSizeKB = 5120,   // 5 MB
    ///     MaxHistoryFiles = 20,
    ///     FilePrefix = "Client"
    /// });
    ///
    /// // 应用退出时释放
    /// writer.Dispose();
    /// </code>
    /// </example>
    public class AsakiLogFileWriter : IDisposable
    {
        /// <summary>日志聚合器引用，通过双缓冲获取待写入日志命令</summary>
        private readonly AsakiLogAggregator _aggregator;

        /// <summary>复用的 <see cref="StringBuilder"/> 实例，容量 4096 字符，减少 GC 分配</summary>
        private readonly StringBuilder _sb = new StringBuilder(4096);

        // 异步控制
        /// <summary>写入循环的 UniTask</summary>
        private UniTask _writeTask;

        /// <summary>取消令牌，用于停止异步操作</summary>
        private readonly System.Threading.CancellationTokenSource _cts =
            new System.Threading.CancellationTokenSource();

        /// <summary>退出标志，确保 <see cref="Dispose"/> 逻辑仅执行一次，防止重复释放。使用volatile确保多线程可见性</summary>
        private volatile bool _isDisposed;

        // 文件状态
        /// <summary>日志文件存储目录，通常位于 <c>Application.persistentDataPath/Logs</c></summary>
        private string _logDir;

        /// <summary>当前正在写入的日志文件完整路径</summary>
        private string _currentFilePath;

        /// <summary>当前日志文件的独占写入流</summary>
        private FileStream _fileStream;

        /// <summary>包装 <see cref="_fileStream"/> 的 UTF-8 文本写入器</summary>
        private StreamWriter _streamWriter;

        /// <summary>当前文件已写入的字节数（包含文件头），用于触发大小轮转</summary>
        private long _currentWrittenBytes;

        // 配置 (使用volatile和锁确保线程安全)
        /// <summary>配置锁，保护所有配置字段的读写</summary>
        private readonly object _configLock = new object();

        /// <summary>文件操作锁，保护文件创建、写入和销毁的互斥访问</summary>
        private readonly object _fileOperationLock = new object();

        /// <summary>单个日志文件的最大字节数，默认 2 MB</summary>
        private volatile int _maxFileSize = 2 * 1024 * 1024;

        /// <summary>保留的历史日志文件数量，默认 10 个</summary>
        private volatile int _maxHistoryFiles = 10;

        /// <summary>日志文件名前缀，默认 "Log"</summary>
        /// <remarks>使用volatile确保写入线程能读取到最新值</remarks>
        private volatile string _filePrefix = "Log";

        /// <summary>
        /// 初始化 <see cref="AsakiLogFileWriter"/> 的新实例，并立即启动后台写入线程
        /// </summary>
        /// <param name="aggregator">日志聚合器，用于获取双缓冲数据。不得为 <see langword="null"/></param>
        /// <exception cref="ArgumentNullException">当 <paramref name="aggregator"/> 为 <see langword="null"/> 时抛出</exception>
        /// <remarks>
        /// 构造函数内部完成以下步骤：
        /// <list type="number">
        ///   <item>创建 <see cref="Application.persistentDataPath"/>/Logs 目录</item>
        ///   <item>启动后台线程，循环等待写入信号</item>
        /// </list>
        /// 注意：日志文件不会立即创建，而是在首次 ApplyConfig 或第一次写入时创建
        /// 文件命名格式：<c>{FilePrefix}_yyyyMMdd_HHmmss_fff_{Guid}.asakilog</c>
        /// </remarks>
        public AsakiLogFileWriter(AsakiLogAggregator aggregator)
        {
            _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));

            // 1. 准备目录
            _logDir = Path.Combine(Application.persistentDataPath, "Logs");
            if (!Directory.Exists(_logDir))
                Directory.CreateDirectory(_logDir);

            // 2. 启动异步写入循环（文件延迟到 ApplyConfig 时创建）
            _writeTask = WriteLoopAsync(_cts.Token);
        }

        /// <summary>
        /// 动态应用运行时配置，并触发异步历史文件清理
        /// </summary>
        /// <param name="config">日志配置对象。为 <see langword="null"/> 时方法立即返回，不做任何操作</param>
        /// <remarks>
        /// <para>配置变更立即生效：</para>
        /// <list type="bullet">
        ///   <item><see cref="AsakiLogConfig.MaxFileSizeKB"/> → 触发下一次文件大小检查时生效</item>
        ///   <item><see cref="AsakiLogConfig.FilePrefix"/> → 下一次文件轮转时生效</item>
        ///   <item><see cref="AsakiLogConfig.MaxHistoryFiles"/> → 立即异步清理多余历史文件</item>
        /// </list>
        /// <para>清理操作在 ThreadPool 中执行，不会阻塞 Writer 线程或主线程</para>
        /// <para>线程安全：使用锁保护配置更新，确保写入线程读取到一致的数据</para>
        /// </remarks>
        public void ApplyConfig(AsakiLogConfig config)
        {
            if (config == null)
                return;

            // 使用锁保护配置更新，确保原子性
            lock (_configLock)
            {
                _maxFileSize = config.MaxFileSizeKB * 1024;
                _maxHistoryFiles = config.MaxHistoryFiles;
                _filePrefix = string.IsNullOrEmpty(config.FilePrefix) ? "Log" : config.FilePrefix;
            }

            // 异步触发一次历史清理
            // 不要在写入过程中做 IO 删除操作，避免阻塞写入
            CleanupHistoryAsync().Forget();
        }

        /// <summary>
        /// 安全地读取配置值
        /// </summary>
        private (int maxFileSize, int maxHistoryFiles, string filePrefix) GetConfigSnapshot()
        {
            lock (_configLock)
            {
                return (_maxFileSize, _maxHistoryFiles, _filePrefix);
            }
        }

        /// <summary>
        /// 异步清理旧日志
        /// </summary>
        private async UniTaskVoid CleanupHistoryAsync()
        {
            await UniTask.SwitchToThreadPool();
            CleanupHistory();
        }

        /// <summary>
        /// 创建新的日志文件并关闭旧文件（如果存在）
        /// </summary>
        /// <remarks>
        /// <para>文件命名：<c>{_filePrefix}_yyyyMMdd_HHmmss_fff_{Guid}.asakilog</c>（包含毫秒和GUID确保唯一性）</para>
        /// <para>文件头写入：<c>#VERSION:2.3</c> 与 <c>#SESSION:{DateTime.Now}</c></para>
        /// <para>使用 <see cref="FileShare.Read"/> 允许外部工具（如日志查看器）在写入时读取文件</para>
        /// <para>异常处理：任何 I/O 异常仅记录到 Unity 控制台，不向上抛出</para>
        /// <para>线程安全：必须持有 <see cref="_fileOperationLock"/> 锁才能调用</para>
        /// </remarks>
        private void OpenNewFile()
        {
            lock (_fileOperationLock)
            {
                try
                {
                    // 检查是否已释放，避免在 Dispose 后创建新文件
                    if (_isDisposed)
                        return;

                    // 关闭旧的
                    _streamWriter?.Dispose();
                    _fileStream?.Dispose();
                    _streamWriter = null;
                    _fileStream = null;

                    // 获取当前配置快照
                    var config = GetConfigSnapshot();

                    // 创建新的（包含毫秒和GUID确保文件名唯一，避免并发冲突）
                    string fileName =
                        $"{config.filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.asakilog";
                    _currentFilePath = Path.Combine(_logDir, fileName);

                    // 使用 FileShare.Read 允许外部查看
                    _fileStream = new FileStream(
                        _currentFilePath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.Read
                    );
                    _streamWriter = new StreamWriter(_fileStream, Encoding.UTF8);
                    _streamWriter.AutoFlush = false; // 手动 Flush 提高性能

                    // 写入头信息
                    _streamWriter.Write($"#VERSION:2.3\n#SESSION:{DateTime.Now}\n");
                    _streamWriter.Flush();
                    _currentWrittenBytes = _fileStream.Length;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AsakiWriter] Failed to create log file: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 异步写入循环，负责消费双缓冲数据并持久化到磁盘
        /// </summary>
        /// <param name="cancellationToken">取消令牌，用于停止循环</param>
        /// <returns>表示异步操作的 UniTask</returns>
        /// <remarks>
        /// <para>循环逻辑：</para>
        /// <list type="number">
        ///   <item>等待 500 ms 或直到取消令牌触发</item>
        ///   <item>调用 <see cref="FlushBufferAsync"/> 处理并写入数据</item>
        ///   <item>重复上述步骤直到取消</item>
        /// </list>
        /// <para>异常处理：捕获所有异常并记录，循环自动恢复</para>
        /// <para>资源清理：确保在取消时释放文件句柄</para>
        /// </remarks>
        private async UniTask WriteLoopAsync(System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                // 看门狗机制：如果发生非致命异常，尝试恢复
                while (!cancellationToken.IsCancellationRequested)
                {
                    await UniTask.Delay(500, cancellationToken: cancellationToken);
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    await FlushBufferAsync();
                }

                // 退出前最后一次 Flush
                await FlushBufferAsync();
            }
            catch (OperationCanceledException)
            {
                // 正常取消，忽略
            }
            catch (Exception e)
            {
                // 甚至可以把自己的错误写到 stderr 或者独立的 fallback 文件
                Debug.LogError($"[AsakiWriter] Crash: {e.Message}");
            }
            finally
            {
                // 退出时清理资源
                _streamWriter?.Dispose();
                _fileStream?.Dispose();
            }
        }

        /// <summary>
        /// 将聚合器双缓冲中的日志命令批量写入当前日志文件，并在达到大小限制时触发轮转
        /// </summary>
        /// <returns>表示异步操作的 UniTask</returns>
        /// <remarks>
        /// <para>执行步骤：</para>
        /// <list type="number">
        ///   <item>主动调用 <see cref="AsakiLogAggregator.Sync"/> 将输入队列数据转移到 IO 缓冲区</item>
        ///   <item>通过 <see cref="AsakiLogAggregator.SwapIOBuffer"/> 获取待写入数据</item>
        ///   <item>使用 <see cref="_sb"/> 批量构建输出字符串，减少 I/O 次数</item>
        ///   <item>对消息、Payload、堆栈 JSON 调用 <see cref="Sanitize"/> 转义特殊字符</item>
        ///   <item>写入文件并强制 <see cref="StreamWriter.Flush"/> 到磁盘</item>
        ///   <item>检查文件大小，若超出 <see cref="_maxFileSize"/> 则调用 <see cref="OpenNewFile"/> 轮转</item>
        ///   <item>将用过的 <see cref="LogWriteCommand"/> 对象归还对象池</item>
        /// </list>
        /// <para>异常处理：任何写入异常仅记录到控制台，不中断后续日志</para>
        /// <para>线程安全：使用 <see cref="_fileOperationLock"/> 保护文件写入操作</para>
        /// </remarks>
        private async UniTask FlushBufferAsync()
        {
            // 0. 主动触发 Sync，将输入队列中的日志转移到 IO 缓冲区
            // 这对于测试环境很重要，因为测试可能不会手动调用 Sync
            _aggregator.Sync();

            // 1. 获取数据 (双缓冲交换)
            var buffer = _aggregator.SwapIOBuffer();
            if (buffer == null || buffer.Count == 0)
                return;

            try
            {
                _sb.Clear();

                foreach (LogWriteCommand cmd in buffer)
                {
                    // 序列化逻辑
                    if (cmd.Type == LogWriteCommand.CmdType.Def)
                    {
                        // 直接使用预处理好的数据
                        _sb.Append("$DEF|")
                            .Append(cmd.Id)
                            .Append('|')
                            .Append(cmd.LevelInt)
                            .Append('|')
                            .Append(cmd.Timestamp)
                            .Append('|')
                            .Append(Sanitize(cmd.Message))
                            .Append('|')
                            .Append(Sanitize(cmd.Payload))
                            .Append('|')
                            .Append(cmd.Path)
                            .Append(':')
                            .Append(cmd.Line)
                            .Append('|')
                            .Append(Sanitize(cmd.StackJson))
                            .AppendLine(); // 注意：也需要对 StackJson 进行清理
                    }
                    else // Inc
                    {
                        _sb.Append("$INC|")
                            .Append(cmd.Id)
                            .Append('|')
                            .Append(cmd.IncAmount)
                            .AppendLine();
                    }

                    // 用完即回收
                    LogCommandPool.Return(cmd);
                }

                if (_sb.Length > 0)
                {
                    // 2. 写入文件（使用文件操作锁保护）
                    await UniTask.SwitchToThreadPool();
                    lock (_fileOperationLock)
                    {
                        // 再次检查 Dispose 状态，避免在 Dispose 后写入
                        if (_isDisposed)
                        {
                            return;
                        }

                        // 如果文件流未创建，先创建文件（首次写入时）
                        if (_streamWriter == null || _fileStream == null)
                        {
                            OpenNewFile();
                            // 如果创建失败，放弃本次写入
                            if (_streamWriter == null || _fileStream == null)
                                return;
                        }

                        _streamWriter.Write(_sb.ToString());
                        _streamWriter.Flush(); // 确保刷入磁盘

                        // 3. 更新长度并检查轮转
                        _currentWrittenBytes = _fileStream.Length; // 获取真实文件大小

                        // 读取配置快照进行大小检查
                        var config = GetConfigSnapshot();
                        if (_currentWrittenBytes > config.maxFileSize)
                        {
                            OpenNewFile(); // 触发轮转
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AsakiWriter] Write error: {ex.Message}");
            }
            finally
            {
                buffer.Clear();
            }
        }

        /// <summary>
        /// 清理旧日志 (保留最近的 N 个)
        /// </summary>
        /// <remarks>
        /// <para>扫描 <see cref="_logDir"/> 下当前前缀的 <c>{prefix}_*.asakilog</c> 文件，按创建时间降序排序</para>
        /// <para>删除超出 <see cref="_maxHistoryFiles"/> 限制的旧文件</para>
        /// <para>异常处理：任何 I/O 异常均被忽略，防止清理失败影响日志写入</para>
        /// <para>线程安全：方法可在任意线程调用，内部无共享状态写操作</para>
        /// </remarks>
        private void CleanupHistory()
        {
            try
            {
                // 读取配置快照
                var config = GetConfigSnapshot();

                DirectoryInfo dirInfo = new DirectoryInfo(_logDir);
                // 只清理当前前缀的文件，避免影响其他测试/实例
                var files = dirInfo
                    .GetFiles($"{config.filePrefix}_*.asakilog")
                    .OrderByDescending(f => f.CreationTime) // 最新的在前
                    .ToList();

                if (files.Count <= config.maxHistoryFiles)
                    return;
                for (int i = config.maxHistoryFiles; i < files.Count; i++)
                {
                    try
                    {
                        files[i].Delete();
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
            catch
            {
                /* 忽略清理错误 */
            }
        }

        /// <summary>
        /// 对输入文本进行转义处理，确保日志文件格式安全
        /// </summary>
        /// <param name="input">原始文本，可能包含特殊字符</param>
        /// <returns>
        /// 转义后的文本：竖线 <c>'|'</c> → <c>'¦'</c>；换行 <c>'\n'</c> → <c>' '</c>；
        /// 若输入为 <see langword="null"/> 或空，返回空字符串
        /// </returns>
        /// <remarks>
        /// 转义规则与 <see cref="AsakiLogFileReader.LoadFile"/> 中的还原逻辑相对应，
        /// 确保读写两端数据一致性
        /// </remarks>
        private string Sanitize(string input)
        {
            return string.IsNullOrEmpty(input) ? "" : input.Replace("|", "¦").Replace("\n", " ");
        }

        /// <summary>
        /// 优雅关闭写入器：停止异步操作、刷新剩余数据并释放所有资源
        /// </summary>
        /// <remarks>
        /// <para>幂等性：可安全多次调用，仅首次调用生效</para>
        /// <para>执行流程：</para>
        /// <list type="number">
        ///   <item>设置 Dispose 标志</item>
        ///   <item>触发取消令牌停止异步操作</item>
        ///   <item>使用文件操作锁保护文件资源释放</item>
        /// </list>
        /// <para>异常处理：忽略等待过程中的异常，确保资源释放</para>
        /// <para>线程安全：使用 <see cref="_fileOperationLock"/> 保护文件资源释放</para>
        /// </remarks>
        public void Dispose()
        {
            // 使用简单的赋值，因为volatile确保可见性，且双重检查避免竞态
            if (_isDisposed)
                return;

            _isDisposed = true;

            try
            {
                // 触发取消
                _cts.Cancel();

                // 不等待异步操作结束，直接清理资源
                // 因为在退出 PlayMode 时，PlayerLoop 已经停止，等待可能会导致问题
            }
            catch
            {
                // 忽略异常
            }
            finally
            {
                _cts.Dispose();
                // 使用文件操作锁保护文件资源释放，确保与 FlushBufferAsync 和 OpenNewFile 的互斥访问
                lock (_fileOperationLock)
                {
                    _streamWriter?.Dispose();
                    _streamWriter = null;
                    _fileStream?.Dispose();
                    _fileStream = null;
                }
            }
        }
    }
}
