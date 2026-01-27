using Asaki.Core.Broker;
using Asaki.Core.Logging;
using Asaki.Core.Serialization;
using Asaki.Unity.Utils;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Asaki.Unity.Services.Serialization
{
	/// <summary>
	/// Asaki框架的核心存档服务实现类，提供基于Slot的异步存档管理功能。
	/// <para>
	/// <b>核心职责：</b>
	/// <list type="bullet">
	///     <item>管理本地存档文件的创建、读取、写入和删除</item>
	///     <item>支持二进制数据和JSON元数据的双轨存储策略</item>
	///     <item>提供主线程与后台线程的智能调度，确保UnityEngine对象安全访问</item>
	///     <item>集成事件系统，实现保存状态的全局通知</item>
	/// </list>
	/// </para>
	/// 
	/// <para>
	/// <b>存储结构：</b>
	/// <code>
	/// persistentDataPath/
	/// └── Saves/
	///     ├── Slot_0/
	///     │   ├── data.bin      (二进制存档数据)
	///     │   └── meta.json     (可读性元数据，Editor/Debug模式)
	///     ├── Slot_1/
	///     └── ...
	/// </code>
	/// </para>
	/// 
	/// <para>
	/// <b>线程安全策略：</b>
	/// <list type="number">
	///     <item><b>序列化阶段：</b>必须在主线程执行，防止访问UnityEngine对象时发生交叉线程错误</item>
	///     <item><b>IO操作阶段：</b>通过UniTask.SwitchToThreadPool()切换到后台线程，避免阻塞主线程</item>
	///     <item><b>反序列化阶段：</b>IO完成后切换回主线程，安全地重建UnityEngine对象</item>
	/// </list>
	/// </para>
	/// 
	/// <para>
	/// <b>使用示例：</b>
	/// <code>
	/// public class GameScene
	/// {
	///     private IAsakiSaveService _saveService;
	///     
	///     async Task SaveGame(int slotId)
	///     {
	///         var meta = new GameSlotMeta 
	///         { 
	///             SaveName = "Level 5 Checkpoint",
	///             PlayerLevel = 12
	///         };
	///         
	///         var data = new GameSaveData
	///         {
	///             PlayerPosition = transform.position,
	///             Inventory = playerInventory
	///         };
	///         
	///         await _saveService.SaveSlotAsync(slotId, meta, data);
	///     }
	///     
	///     async Task LoadGame(int slotId)
	///     {
	///         try
	///         {
	///             var (meta, data) = await _saveService.LoadSlotAsync&lt;GameSlotMeta, GameSaveData&gt;(slotId);
	///             transform.position = data.PlayerPosition;
	///             playerInventory = data.Inventory;
	///         }
	///         catch (FileNotFoundException)
	///         {
	///             Debug.LogError($"存档槽位 {slotId} 不存在");
	///         }
	///     }
	/// }
	/// </code>
	/// </para>
	/// 
	/// <remarks>
	/// <b>设计考量：</b>
	/// <list type="bullet">
	///     <item>采用接口隔离原则(IAsakiSaveService)，便于单元测试和模拟</item>
	///     <item>元数据与主数据分离：JSON元数据便于调试和外部工具读取，二进制数据保证性能和空间效率</item>
	///     <item>使用StringBuilder池化减少GC压力，适合高频保存场景</item>
	///     <item>UniTask条件编译支持，允许项目根据需求选择Task或UniTask</item>
	/// </list>
	/// </remarks>
	/// </summary>
	public class AsakiSaveService : IAsakiSaveService
	{
		/// <summary>
		/// 存档根目录的完整路径，存储在Application.persistentDataPath下的"Saves"文件夹中。
		/// 该路径在OnInit()方法中初始化，确保跨平台兼容性（Windows, macOS, Android, iOS等）。
		/// </summary>
		private string _rootPath;

		/// <summary>
		/// 调试模式标志位，决定是否在保存时生成可读的JSON元数据文件。
		/// 在编辑器环境或Debug构建中自动启用，Release构建中禁用以减少IO开销。
		/// </summary>
		private bool _isDebug;

		/// <summary>
		/// 事件服务引用，用于发布存档相关的生命周期事件（开始、成功、失败）。
		/// 允许UI、音频系统等模块订阅并响应存档状态变化。
		/// </summary>
		private IAsakiEventService _eventService;

		/// <summary>
		/// 构造函数，通过依赖注入获取事件服务实例。
		/// 遵循依赖倒置原则，确保服务可测试和解耦。
		/// </summary>
		/// <param name="eventService">事件发布服务，用于通知存档状态变更</param>
		public AsakiSaveService(IAsakiEventService eventService)
		{
			_eventService = eventService;
		}

		/// <summary>
		/// 服务初始化入口，由Asaki框架的模块管理器自动调用。
		/// <para>
		/// <b>初始化流程：</b>
		/// <list type="number">
		///     <item>构建跨平台的存档根目录路径</item>
		///     <item>检测当前是否处于调试环境</item>
		///     <item>确保存档根目录存在（首次运行时创建）</item>
		/// </list>
		/// </para>
		/// <b>调用时机：</b>游戏启动阶段，早于任何存档操作。
		/// </summary>
		public void OnInit()
		{
			// 使用Unity的persistentDataPath确保跨平台兼容性
			_rootPath = Path.Combine(Application.persistentDataPath, "Saves");

			// 编辑器或Debug构建时启用详细日志和元数据保存
			_isDebug = Application.isEditor || Debug.isDebugBuild;

			// 惰性创建根目录，避免不必要的IO操作
			if (!Directory.Exists(_rootPath)) Directory.CreateDirectory(_rootPath);
		}

		/// <summary>
		/// 异步初始化方法，当前实现为同步完成。
		/// 预留接口以便于未来可能添加的异步初始化逻辑（如云存储同步验证）。
		/// </summary>
		/// <returns>已完成的Task实例</returns>
		public UniTask OnInitAsync()
		{
			return UniTask.CompletedTask;
		}

		/// <summary>
		/// 服务释放方法，当前为空实现。
		/// 预留接口以便未来需要清理资源（如文件句柄、缓存等）。
		/// </summary>
		public void OnDispose() { }

		// =========================================================
		// 路径策略 (Encapsulation)
		// 将路径构建逻辑封装在私有方法中，避免硬编码字符串散落在业务逻辑中
		// =========================================================

		/// <summary>
		/// 获取指定槽位的目录路径。
		/// 采用命名约定"Slot_{id}"确保目录名的可读性和唯一性。
		/// </summary>
		/// <param name="id">存档槽位ID（通常从0开始）</param>
		/// <returns>完整的槽位目录路径</returns>
		/// <example>
		/// GetSlotDir(0) → "C:/Users/.../AppData/LocalLow/GameName/Saves/Slot_0"
		/// </example>
		private string GetSlotDir(int id)
		{
			return Path.Combine(_rootPath, $"Slot_{id}");
		}

		/// <summary>
		/// 获取二进制存档数据文件的完整路径。
		/// data.bin存储主要的游戏状态数据，采用二进制格式保证性能和紧凑性。
		/// </summary>
		/// <param name="id">存档槽位ID</param>
		/// <returns>二进制数据文件路径</returns>
		private string GetDataPath(int id)
		{
			return Path.Combine(GetSlotDir(id), "data.bin");
		}

		/// <summary>
		/// 获取JSON元数据文件的完整路径。
		/// meta.json仅在调试模式下生成，包含人类可读的存档信息（如保存时间、关卡名称等）。
		/// </summary>
		/// <param name="id">存档槽位ID</param>
		/// <returns>JSON元数据文件路径</returns>
		private string GetMetaPath(int id)
		{
			return Path.Combine(GetSlotDir(id), "meta.json");
		}

		// =========================================================
		// 核心 Slot 逻辑
		// 提供类型安全的异步保存和加载操作，支持泛型约束确保数据完整性
		// =========================================================

		/// <summary>
		/// 异步保存游戏数据到指定槽位。
		/// <para>
		/// <b>操作流程：</b>
		/// <list type="number">
		///     <item>在元数据中自动填充槽位ID和保存时间</item>
		///     <item>发布保存开始事件(AsakiSaveBeginEvent)</item>
		///     <item><b>主线程：</b>序列化数据到内存缓冲区（防止Unity对象被修改）</item>
		///     <item><b>后台线程：</b>异步写入二进制文件和JSON元数据</item>
		///     <item>切换回主线程并发布保存成功事件</item>
		/// </list>
		/// </para>
		/// 
		/// <para>
		/// <b>异常处理：</b>
		/// - 捕获所有异常并记录详细日志
		/// - 发布保存失败事件供UI层处理
		/// - 重新抛出异常，确保调用方可以实施重试逻辑
		/// </para>
		/// 
		/// <para>
		/// <b>性能优化：</b>
		/// - 使用StringBuilder池避免频繁的内存分配
		/// - 二进制格式最小化存储空间
		/// - 异步IO操作不阻塞主线程
		/// </para>
		/// </summary>
		/// <typeparam name="TMeta">元数据类型，必须实现IAsakiSlotMeta接口</typeparam>
		/// <typeparam name="TData">存档数据类型，必须实现IAsakiSavable接口</typeparam>
		/// <param name="slotId">目标存档槽位ID</param>
		/// <param name="meta">存档元数据（自动填充SlotId和LastSaveTime）</param>
		/// <param name="data">要保存的游戏数据</param>
		/// <param name="cancellationToken">取消保存操作的取消令牌</param>
		/// <returns>表示异步保存操作的Task</returns>
		/// <exception cref="IOException">磁盘空间不足或路径非法时抛出</exception>
		/// <exception cref="UnauthorizedAccessException">无写入权限时抛出</exception>
		public async UniTask SaveSlotAsync<TMeta, TData>(int slotId, TMeta meta, TData data, CancellationToken token = default(CancellationToken))
			where TMeta : IAsakiSlotMeta where TData : IAsakiSavable
		{
			// 早期取消检查：避免不必要的目录创建
			token.ThrowIfCancellationRequested();

			string dir = GetSlotDir(slotId);
			string dataPath = GetDataPath(slotId);
			string metaPath = GetMetaPath(slotId);

			// 确保槽位目录存在
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

			// 预设元数据（必须在主线程执行）
			meta.SlotId = slotId;
			meta.LastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

			// 发布保存开始事件
			string filename = $"Slot_{slotId}";
			_eventService.Publish(new AsakiSaveBeginEvent { Filename = filename });

			try
			{
				// ===== 步骤1：主线程内存快照 =====
				byte[] dataBuffer;
				using (MemoryStream ms = new MemoryStream())
				{
					AsakiBinaryWriter writer = new AsakiBinaryWriter(ms);
					data.Serialize(writer);
					dataBuffer = ms.ToArray();
				}

				// 序列化后检查：避免不必要的IO切换
				token.ThrowIfCancellationRequested();

				// ===== 步骤2：后台线程异步IO =====
				#if ASAKI_USE_UNITASK
				await UniTask.SwitchToThreadPool();
				#endif

				// 线程切换后检查：及时响应取消
				token.ThrowIfCancellationRequested();

				// 异步写入二进制数据（支持取消）
				await File.WriteAllBytesAsync(dataPath, dataBuffer, token);

				// 仅在调试模式下生成JSON元数据
				if (_isDebug)
				{
					// 取消检查：避免不必要的字符串操作
					token.ThrowIfCancellationRequested();

					StringBuilder sb = AsakiStringBuilderPool.Rent();
					try
					{
						AsakiJsonWriter jsonWriter = new AsakiJsonWriter(sb);
						meta.Serialize(jsonWriter);
						// 传递取消令牌给异步IO
						await File.WriteAllTextAsync(metaPath, jsonWriter.GetResult(), token);
					}
					finally
					{
						AsakiStringBuilderPool.Return(sb);
					}
				}

				// ===== 步骤3：回到主线程并发布成功事件 =====
				#if ASAKI_USE_UNITASK
				await UniTask.SwitchToMainThread();
				#endif

				// 最终取消检查：防止事件处理器在取消状态下执行
				token.ThrowIfCancellationRequested();

				_eventService.Publish(new AsakiSaveSuccessEvent { Filename = filename });
			}
			catch (OperationCanceledException)
			{
				// 清理不完整文件：保证原子性
				try
				{
					if (File.Exists(dataPath)) File.Delete(dataPath);
					if (File.Exists(metaPath)) File.Delete(metaPath);

					// 如果目录为空，删除目录
					if (Directory.Exists(dir) && Directory.GetFiles(dir).Length == 0)
						Directory.Delete(dir);
				}
				catch (Exception cleanupEx)
				{
					ALog.Warn($"[AsakiSave] Cleanup failed after cancel: {cleanupEx.Message}");
				}

				// 发布取消事件（视为失败）
				_eventService.Publish(new AsakiSaveFailedEvent
				{
					Filename = filename,
					ErrorMessage = "Operation was cancelled by user",
				});

				// 重新抛出取消异常，符合TPL规范
				throw;
			}
			catch (Exception ex)
			{
				// 记录详细错误日志
				ALog.Error($"[AsakiSave] Slot {slotId} Save Failed: {ex.Message}", ex);

				// 发布失败事件
				_eventService.Publish(new AsakiSaveFailedEvent
				{
					Filename = filename,
					ErrorMessage = ex.Message,
				});

				throw;
			}
		}

		/// <summary>
		/// 异步从指定槽位加载游戏数据。
		/// <para>
		/// <b>操作流程：</b>
		/// <list type="number">
		///     <item><b>后台线程：</b>并行读取二进制数据和JSON元数据文件</item>
		///     <item>切换回主线程准备反序列化</item>
		///     <item><b>主线程：</b>反序列化二进制数据到TData对象</item>
		///     <item><b>主线程：</b>反序列化JSON元数据到TMeta对象</item>
		/// </list>
		/// </para>
		/// 
		/// <para>
		/// <b>性能优化：</b>
		/// 使用Task.WhenAll并行读取两个文件，减少IO等待时间，特别适合机械硬盘。
		/// </para>
		/// </summary>
		/// <typeparam name="TMeta">元数据类型，必须有无参构造函数</typeparam>
		/// <typeparam name="TData">存档数据类型，必须有无参构造函数</typeparam>
		/// <param name="slotId">源存档槽位ID</param>
		/// <param name="cancellationToken">取消令牌</param>
		/// <returns>包含元数据和游戏数据的元组</returns>
		/// <exception cref="FileNotFoundException">槽位不存在时抛出，调用方应提前用SlotExists检查</exception>
		/// <exception cref="SerializationException">数据格式不兼容或损坏时抛出</exception>
		public async UniTask<(TMeta Meta, TData Data)> LoadSlotAsync<TMeta, TData>(int slotId, CancellationToken token = default(CancellationToken))
			where TMeta : IAsakiSlotMeta, new() where TData : IAsakiSavable, new()
		{
			// 早期取消检查
			token.ThrowIfCancellationRequested();

			if (!SlotExists(slotId)) throw new FileNotFoundException($"Slot {slotId} not found.");

			try
			{
				// ===== 步骤1：后台线程并行读取 =====
				#if ASAKI_USE_UNITASK
				await UniTask.SwitchToThreadPool();
				#endif

				// 线程切换后检查
				token.ThrowIfCancellationRequested();

				// 启动两个独立的读取任务（均支持取消）
				var dataTask = File.ReadAllBytesAsync(GetDataPath(slotId), token).AsUniTask().AttachExternalCancellation(token);
				var metaTask = File.ReadAllTextAsync(GetMetaPath(slotId), token).AsUniTask().AttachExternalCancellation(token);

				// 等待两个任务都完成，支持取消并获取结果
				(byte[] dataBytes, string metaText) = await UniTask.WhenAll(dataTask, metaTask);

				// IO完成后检查：避免不必要的反序列化
				token.ThrowIfCancellationRequested();

				await UniTask.SwitchToMainThread();
				// 反序列化前检查
				token.ThrowIfCancellationRequested();

				// 反序列化二进制游戏数据
				TData data = new TData();
				using (MemoryStream ms = new MemoryStream(dataBytes))
				{
					AsakiBinaryReader reader = new AsakiBinaryReader(ms);
					data.Deserialize(reader);
				}

				// 反序列化JSON元数据
				TMeta meta = new TMeta();
				AsakiJsonReader jsonReader = AsakiJsonReader.FromJson(metaText);
				meta.Deserialize(jsonReader);

				return (meta, data);
			}
			catch (OperationCanceledException)
			{
				// 加载时取消无需清理，直接抛出
				ALog.Info($"[AsakiSave] Slot {slotId} Load Cancelled");
				throw;
			}
			catch (Exception ex)
			{
				ALog.Error($"[AsakiSave] Slot {slotId} Load Failed: {ex.Message}", ex);
				throw;
			}
		}

		// =========================================================
		// Slot 管理工具
		// 提供槽位查询、存在性检查和删除等辅助功能
		// =========================================================

		/// <summary>
		/// 扫描存档根目录，获取所有已使用的存档槽位ID列表。
		/// <para>
		/// <b>实现细节：</b>
		/// 1. 使用Directory.GetDirectories查找所有"Slot_*"命名的文件夹
		/// 2. 解析文件夹名称中的数字部分
		/// 3. 过滤非法命名并转换为整数列表
		/// 4. 返回的列表可能无序，调用方需自行排序
		/// </para>
		/// 
		/// <para>
		/// <b>性能注意：</b>
		/// 该方法涉及文件系统遍历，避免在Update等高频调用中使用。
		/// 建议在存档菜单初始化时调用一次并缓存结果。
		/// </para>
		/// </summary>
		/// <returns>已使用的槽位ID列表（可能为空）</returns>
		public List<int> GetUsedSlots()
		{
			// 防御性检查，防止根目录被意外删除导致异常
			if (!Directory.Exists(_rootPath)) return new List<int>();

			return Directory.GetDirectories(_rootPath, "Slot_*")
			                // 提取文件夹名称中的数字部分
			                .Select(d => Path.GetFileName(d).Replace("Slot_", ""))
			                // 过滤无法解析为数字的无效文件夹
			                .Where(s => int.TryParse(s, out _))
			                .Select(int.Parse)
			                .ToList();
		}

		/// <summary>
		/// 检查指定槽位是否存在。
		/// <para>
		/// <b>实现细节：</b>
		/// 通过检查二进制数据文件(data.bin)是否存在来判断，而非仅检查目录。
		/// 这样可以确保即使目录存在但数据文件损坏/丢失时也能正确返回false。
		/// </para>
		/// </summary>
		/// <param name="slotId">要检查的槽位ID</param>
		/// <returns>true表示槽位有效存在，false表示不存在或数据不完整</returns>
		public bool SlotExists(int slotId)
		{
			// 以data.bin存在为准，避免空目录被误认为有效槽位
			return File.Exists(GetDataPath(slotId));
		}

		/// <summary>
		/// 删除指定存档槽位及其所有数据。
		/// <para>
		/// <b>安全机制：</b>
		/// - 检查目录是否存在，避免不必要的异常
		/// - 使用Directory.Delete的recursive=true参数确保彻底删除
		/// - 返回bool值而非抛出异常，适合UI层直接调用
		/// </para>
		/// 
		/// <para>
		/// <b>注意事项：</b>
		/// 删除操作不可逆，调用前应向玩家显示确认对话框。
		/// </para>
		/// </summary>
		/// <param name="slotId">要删除的槽位ID</param>
		/// <returns>成功删除返回true，槽位不存在返回false</returns>
		public bool DeleteSlot(int slotId)
		{
			string dir = GetSlotDir(slotId);
			if (Directory.Exists(dir))
			{
				Directory.Delete(dir, true); // recursive=true删除目录及所有内容
				return true;
			}
			return false;
		}
	}
}
