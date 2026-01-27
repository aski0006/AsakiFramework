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
	public class AsakiLogAggregator
	{
		// === 内部结构体定义 ===
		/// <summary>
		/// 表示日志数据包的内部结构体。
		/// 包含日志的各种属性，如级别、消息、负载、文件、行号、异常和时间戳。
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
		}

		/// <summary>
		/// 表示日志签名的内部结构体，用于唯一标识一条日志。
		/// 实现了 <see cref="IEquatable{T}"/> 接口，以便在字典中进行高效比较。
		/// </summary>
		private struct LogSignature : IEquatable<LogSignature>
		{
			private readonly int _hash;

			/// <summary>
			/// 使用文件路径、行号和消息初始化日志签名。
			/// 通过组合这些信息生成一个唯一的哈希值。
			/// </summary>
			/// <param name="file">记录日志的文件路径。</param>
			/// <param name="line">记录日志的行号。</param>
			/// <param name="msg">日志消息。</param>
			public LogSignature(string file, int line, string msg)
			{
				unchecked
				{
					_hash = (file?.GetHashCode() ?? 0) * 397 ^ line ^ (msg?.GetHashCode() ?? 0);
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
				return _hash == other._hash;
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

		// === 2. 聚合端 (主线程状态) ===
		/// <summary>
		/// 用于存储日志签名与日志模型映射关系的字典。
		/// 通过日志签名快速查找和更新对应的日志模型。
		/// </summary>
		private readonly Dictionary<LogSignature, AsakiLogModel> _signatureMap = new Dictionary<LogSignature, AsakiLogModel>();
		/// <summary>
		/// 用于存储当前显示的日志模型列表。
		/// 提供给外部获取日志快照。
		/// </summary>
		private readonly List<AsakiLogModel> _displayList = new List<AsakiLogModel>();
		/// <summary>
		/// 日志模型的ID计数器，用于为每个新的日志模型分配唯一ID。
		/// </summary>
		private int _idCounter = 1;

		/// <summary>
		/// 用于保护获取快照操作的私有锁。
		/// 确保在获取快照时，显示列表不会被其他操作修改。
		/// </summary>
		private readonly object _snapshotLock = new object();

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
		public void Log(AsakiLogLevel level, string message, string payload, string file, int line, Exception ex)
		{
			// [优化] 简单的背压保护
			if (_inputQueue.Count >= MAX_QUEUE_DEPTH)
			{
				// 队列爆了，丢弃 Trace/Info，保留 Error，或者直接丢弃并报错
				if (level < AsakiLogLevel.Error) return;
			}

			_inputQueue.Enqueue(new LogPacket
			{
				Level = level,
				Message = message,
				Payload = payload,
				File = file,
				Line = line,
				Exception = ex,
				Timestamp = DateTime.UtcNow.Ticks,
			});
		}

		/// <summary>
		/// 外部驱动调用，通常在LateUpdate中执行。
		/// 从输入队列中批量取出日志数据包，并进行处理。
		/// </summary>
		/// <param name="batchLimit">每次同步处理的最大日志数量，默认为1000。</param>
		public void Sync(int batchLimit = 1000)
		{
			int count = 0;
			// 批量从并发队列取出
			while (count < batchLimit && _inputQueue.TryDequeue(out LogPacket packet))
			{
				count++;
				ProcessSingleLog(ref packet);
			}
		}

		/// <summary>
		/// 获取当前显示的日志模型列表的快照。
		/// 此方法在获取快照时会锁定显示列表，以确保数据的一致性。
		/// </summary>
		/// <returns>当前显示的日志模型列表的副本。</returns>
		public List<AsakiLogModel> GetSnapshot()
		{
			lock (_snapshotLock)
			{
				return new List<AsakiLogModel>(_displayList);
			}
		}

		/// <summary>
		/// 供写入线程调用，交换IO缓冲区。
		/// 写入线程拿走填满的当前缓冲区，给聚合器一个空的备用缓冲区。
		/// </summary>
		/// <returns>填满的当前缓冲区，如果当前缓冲区为空则返回null。</returns>
		public List<LogWriteCommand> SwapIOBuffer()
		{
			lock (_ioSwapLock)
			{
				if (_ioBufferCurrent.Count == 0) return null;

				var filledBuffer = _ioBufferCurrent;
				_ioBufferCurrent = _ioBufferBack; // 换上空盘子
				_ioBufferBack = filledBuffer;     // 拿走满盘子

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
		private void ProcessSingleLog(ref LogPacket p)
		{
			LogSignature sig = new LogSignature(p.File, p.Line, p.Message);

			// 锁定显示列表 (为了 Snapshot 安全)
			lock (_snapshotLock)
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
					// 解析堆栈
					var stack = CaptureSmartStackTrace(p.Exception);
					string stackJson = stack != null && stack.Count > 0
						? JsonUtility.ToJson(new StackWrapper { F = stack })
						: "{}";

					model = new AsakiLogModel
					{
						ID = _idCounter++,
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
		}

		/// <summary>
		/// 捕获智能堆栈跟踪信息。
		/// 如果有异常，从异常中获取堆栈信息；否则抓取当前调用栈。
		/// 对堆栈信息进行处理，过滤出用户代码相关的堆栈帧，并返回处理后的堆栈帧模型列表。
		/// </summary>
		/// <param name="ex">相关的异常对象，如果有的话。</param>
		/// <returns>处理后的堆栈帧模型列表。</returns>
		private List<StackFrameModel> CaptureSmartStackTrace(Exception ex)
		{
			var list = new List<StackFrameModel>();

			// 如果有异常，从异常取；否则抓取当前调用栈
			// 3 表示跳过: CaptureSmartStackTrace -> Log -> ALog.Trace -> UserCode
			StackTrace trace = ex != null ? new StackTrace(ex, true) : new StackTrace(3, true);

			var frames = trace.GetFrames();
			if (frames == null) return list;

			foreach (StackFrame frame in frames)
			{
				MethodBase method = frame.GetMethod();
				if (method == null) continue;

				string fileName = frame.GetFileName();
				fileName = fileName?.Replace('\\', '/') ?? string.Empty;

				bool isUserCode = fileName.Contains("/Assets/") &&
				                  !fileName.Contains("/Asaki/") &&
				                  !fileName.Contains("Library/PackageCache");

				list.Add(new StackFrameModel
				{
					DeclaringType = method.DeclaringType?.Name ?? "Global",
					MethodName = method.Name,
					FilePath = fileName,
					LineNumber = frame.GetFileLineNumber(),
					IsUserCode = isUserCode,
				});
			}
			return list;
		}

		/// <summary>
		/// 清除聚合器中的所有日志数据。
		/// 包括日志签名与日志模型的映射关系、显示列表，并重置ID计数器。
		/// IO缓冲区会在写入线程下次交换时被清空。
		/// </summary>
		public void Clear()
		{
			lock (_snapshotLock)
			{
				_signatureMap.Clear();
				_displayList.Clear();
				_idCounter = 1;
			}
			// IO Buffer 会在 Writer 下次 Swap 时被清空
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
