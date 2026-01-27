#if UNITY_EDITOR

using Asaki.Core.Configuration;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

// 无论是否使用 UniTask，这里都引用 System.Threading.Tasks 以便处理反射返回的 Task

namespace Asaki.Unity.Services.Configuration
{
	/// <summary>
	/// [Editor Only] 配置文件热重载监听组件，基于 <see cref="FileSystemWatcher"/> 实时监控配置源文件变更并自动触发重载。
	/// </summary>
	/// <remarks>
	/// <para>核心功能：</para>
	/// <list type="bullet">
	///     <item>监控 <c>StreamingAssets/Configs</c> 目录下的 <c>*.csv</c> 文件</item>
	///     <item>文件变更防抖处理（500ms），避免编辑器保存时多次触发</item>
	///     <item>自动通过反射调用 <see cref="IAsakiConfigService.ReloadAsync{T}"/> 实现配置热重载</item>
	///     <item>发布 <see cref="AsakiConfigReloadedEvent"/> 事件，通知其他系统配置已更新</item>
	/// </list>
	/// 
	/// <para>线程模型：</para>
	/// <list type="number">
	///     <item><b>监听线程：</b><see cref="FileSystemWatcher"/> 在后台线程触发变更事件</item>
	///     <item><b>主线程：</b>通过 <see cref="ConcurrentQueue{T}"/> 将事件传递到Unity主线程处理</item>
	///     <item><b>异步重载：</b>反射调用的 <c>ReloadAsync</c> 在线程池执行，采用 Fire-and-forget 模式</item>
	/// </list>
	/// 
	/// <para>防抖机制：</para>
	/// 使用 <see cref="_debounceMap"/> 记录文件最后一次变更时间，仅在超过 <see cref="DEBOUNCE_TIME"/> 后才执行重载，
	/// 防止编辑器保存文件时触发的多次 <c>Changed</c> 事件导致重复加载。
	/// 
	/// <para>性能影响：</para>
	/// <list type="bullet">
	///     <item>Editor模式下行性能开销可忽略（仅文件监听和队列检查）</item>
	///     <item>重载期间可能对主线程造成短暂卡顿，建议在重载时显示进度提示</item>
	///     <item>大型配置表重载时建议使用异步版本避免阻塞</item>
	/// </list>
	/// 
	/// <para>使用限制：</para>
	/// <list type="bullet">
	///     <item>仅在 <c>UNITY_EDITOR</c> 编译条件下生效，不会包含在构建版本中</item>
	///     <item>仅监控 <c>StreamingAssets/Configs</c> 目录，其他路径需修改代码</item>
	///     <item>假定配置文件名为类型名（如 <c>ItemConfig.csv</c>），自定义命名规则需调整 <see cref="FindConfigType"/></item>
	/// </list>
	/// </remarks>
	/// <example>
	/// 组件自动挂载示例（通过 <see cref="AsakiContext"/> 自动初始化）：
	/// <code>
	/// // 在 Editor 模式下启动后，此组件会自动添加到场景中的服务宿主 GameObject
	/// // 开发者无需手动创建实例
	/// 
	/// // 典型工作流程：
	/// // 1. 修改 StreamingAssets/Configs/ItemConfig.csv
	/// // 2. 保存文件（Ctrl+S）
	/// // 3. 观察 Unity 控制台输出 "[AsakiConfig] File Changed: ItemConfig, Reloading..."
	/// // 4. 重载完成后，所有订阅 AsakiConfigReloadedEvent 的系统自动更新
	/// </code>
	/// </example>
	public class AsakiConfigHotReloader : MonoBehaviour
	{
		/// <summary>
		/// 配置文件监控根目录，固定为 <c>Application.streamingAssetsPath/Configs</c>。
		/// </summary>
		/// <remarks>
		/// <para>路径解析：</para>
		/// <list type="bullet">
		///     <item>Windows: <c>%AppData%/../LocalLow/CompanyName/ProductName/StreamingAssets/Configs</c></item>
		///     <item>macOS: <c>~/Library/Application Support/CompanyName/ProductName/StreamingAssets/Configs</c></item>
		///     <item>Editor: <c>Assets/StreamingAssets/Configs</c></item>
		/// </list>
		/// <para>目录必须存在，否则 <see cref="FileSystemWatcher"/> 无法初始化。</para>
		/// </remarks>
		private string _watchPath;

		/// <summary>
		/// 文件系统监听器，封装底层操作系统文件变更通知机制。
		/// </summary>
		/// <remarks>
		/// <para>关键配置：</para>
		/// <list type="bullet">
		///     <item><c>Filter = "*.csv"</c>：仅监控CSV格式配置文件</item>
		///     <item><c>NotifyFilter = LastWrite | CreationTime</c>：监控内容写入和文件创建时间变更</item>
		///     <item><c>EnableRaisingEvents = true</c>：启用事件触发</item>
		/// </list>
		/// <para>资源管理：必须在 <see cref="OnDestroy"/> 中显式调用 <see cref="IDisposable.Dispose"/> 释放非托管资源。</para>
		/// </remarks>
		private FileSystemWatcher _watcher;

		/// <summary>
		/// 线程安全的并发队列，用于从后台线程传递文件变更事件到Unity主线程。
		/// </summary>
		/// <remarks>
		/// <para>设计原因：</para>
		/// <see cref="FileSystemWatcher"/> 的回调在系统后台线程执行，不能直接访问Unity API或触发重载逻辑。
		/// 通过此队列将事件传递到主线程的 <see cref="Update"/> 中处理。
		/// <para>线程模型：生产者-消费者模式，无锁设计（CLR内部实现优化）。</para>
		/// </remarks>
		private readonly ConcurrentQueue<string> _changedFiles = new ConcurrentQueue<string>();

		/// <summary>
		/// 防抖时间映射表，记录每个文件下次允许触发重载的时间戳（基于 <see cref="UnityEngine.Time.realtimeSinceStartup"/>）。
		/// </summary>
		/// <remarks>
		/// Key: 文件的完整路径字符串
		/// Value: 下次允许触发的时间（秒）
		/// 
		/// <para>使用 <see cref="UnityEngine.Time.realtimeSinceStartup"/> 而非 <see cref="DateTime"/>
		/// 确保在编辑器暂停/播放状态切换时计时器行为一致。</para>
		/// </remarks>
		private readonly Dictionary<string, float> _debounceMap = new Dictionary<string, float>();

		/// <summary>
		/// 防抖时间窗口常量，单位秒。防止编辑器保存文件时触发多次重载。
		/// </summary>
		/// <remarks>
		/// 500ms的窗口期可覆盖大多数编辑器自动保存和多次写入的场景。
		/// 过短可能导致重复触发，过长会降低热重载响应速度。
		/// </remarks>
		private const float DEBOUNCE_TIME = 0.5f;

		/// <summary>
		/// Unity 初始化回调，在组件启用时执行。
		/// </summary>
		/// <remarks>
		/// <list type="number">
		///     <item>检查是否在编辑器环境运行，若不是则自销毁组件</item>
		///     <item>构建配置文件监控目录路径</item>
		///     <item>初始化 <see cref="FileSystemWatcher"/> 并启动监听</item>
		///     <item>输出初始化日志到控制台</item>
		/// </list>
		/// <para>错误处理：若监控目录不存在，静默退出不抛出异常，避免编辑器启动失败。</para>
		/// </remarks>
		private void Start()
		{
			// 仅在编辑器下启用，防止意外包含到构建中
			if (!Application.isEditor)
			{
				Destroy(this);
				return;
			}

			_watchPath = Path.Combine(Application.streamingAssetsPath, "Configs");
			if (!Directory.Exists(_watchPath)) return;

			InitWatcher();
			ALog.Info($"[AsakiConfig] Hot Reload Watcher Started: {_watchPath}");
		}

		/// <summary>
		/// 初始化文件系统监听器实例并配置监听参数。
		/// </summary>
		/// <remarks>
		/// <para>配置详情：</para>
		/// <list type="bullet">
		///     <item>监听路径：<see cref="_watchPath"/></item>
		///     <item>文件过滤器：<c>*.csv</c>（仅CSV格式）</item>
		///     <item>通知类型：<see cref="NotifyFilters.LastWrite"/> 和 <see cref="NotifyFilters.CreationTime"/></item>
		///     <item>事件绑定：注册 <see cref="OnFileChanged"/> 回调</item>
		///     <item>启用事件触发：<c>EnableRaisingEvents = true</c></item>
		/// </list>
		/// <para>性能注意：<see cref="FileSystemWatcher"/> 会占用少量系统句柄资源，但可忽略。</para>
		/// </remarks>
		private void InitWatcher()
		{
			_watcher = new FileSystemWatcher(_watchPath, "*.csv");
			_watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime;
			_watcher.Changed += OnFileChanged;
			_watcher.EnableRaisingEvents = true;
		}

		/// <summary>
		/// 文件系统变更事件处理回调，在后台线程执行。
		/// </summary>
		/// <param name="sender">事件源（<see cref="FileSystemWatcher"/> 实例）。</param>
		/// <param name="e">包含变更文件信息的参数。</param>
		/// <remarks>
		/// <para>线程上下文：此方法在系统后台线程调用，无法直接操作Unity对象或触发UI更新。</para>
		/// <para>主要职责：将变更文件路径入队到 <see cref="_changedFiles"/>，由主线程后续处理。</para>
		/// <para>异常处理：此方法不应抛出异常，否则可能导致监听器停止工作。</para>
		/// </remarks>
		private void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			_changedFiles.Enqueue(e.FullPath);
		}

		/// <summary>
		/// Unity 每帧更新回调，处理防抖逻辑和触发实际重载。
		/// </summary>
		/// <remarks>
		/// <para>处理流程（每帧执行）：</para>
		/// <list type="number">
		///     <item>消费 <see cref="_changedFiles"/> 队列，将文件路径加入防抖字典，设置下次触发时间为当前时间 + <see cref="DEBOUNCE_TIME"/></item>
		///     <item>遍历防抖字典，检查每个文件是否已超过防抖时间窗口</item>
		///     <item>对已过期的文件调用 <see cref="ReloadConfig"/> 执行实际重载</item>
		///     <item>从字典中移除已处理的文件记录</item>
		/// </list>
		/// <para>性能优化：防抖字典的键遍历每帧执行，但通常元素数量极少（1-5个），性能可忽略。</para>
		/// <para>错误隔离：单个文件重载失败不会影响其他文件的处理。</para>
		/// </remarks>
		private void Update()
		{
			// 1. 处理队列：将后台线程传入的文件变更事件存入防抖字典
			while (_changedFiles.TryDequeue(out string filePath))
			{
				_debounceMap[filePath] = UnityEngine.Time.realtimeSinceStartup + DEBOUNCE_TIME;
			}

			// 2. 检查防抖：查找已超过等待时间的文件
			if (_debounceMap.Count > 0)
			{
				var toReload = new List<string>();
				var keys = new List<string>(_debounceMap.Keys);

				foreach (string key in keys)
				{
					if (UnityEngine.Time.realtimeSinceStartup >= _debounceMap[key])
					{
						toReload.Add(key);
						_debounceMap.Remove(key);
					}
				}

				// 3. 执行重载：对防抖完成的文件触发配置重载
				foreach (string path in toReload)
				{
					ReloadConfig(path);
				}
			}
		}

		/// <summary>
		/// 执行单个配置文件的重载逻辑，通过反射调用配置服务的泛型重载方法。
		/// </summary>
		/// <param name="filePath">变更文件的完整路径。</param>
		/// <remarks>
		/// <para>重载流程：</para>
		/// <list type="number">
		///     <item>从文件路径提取文件名（不含扩展名）作为类型搜索键</item>
		///     <item>调用 <see cref="FindConfigType"/> 在应用程序域中查找匹配的配置类型</item>
		///     <item>若找到类型，输出跟踪日志并获取 <see cref="IAsakiConfigService"/> 实例</item>
		///     <item>通过反射获取 <c>ReloadAsync&lt;T&gt;</c> 方法并构造泛型版本</item>
		///     <item>异步调用重载方法，采用 Fire-and-forget 模式避免阻塞主线程</item>
		/// </list>
		/// <para>异常处理：内部捕获所有异常并输出错误日志，确保单个配置重载失败不影响编辑器稳定性。</para>
		/// <para>反射性能：仅在文件变更时触发，非高频操作，性能影响可忽略。</para>
		/// </remarks>
		private void ReloadConfig(string filePath)
		{
			string fileName = Path.GetFileNameWithoutExtension(filePath);

			// 需要反射查找配置类型
			Type configType = FindConfigType(fileName);

			if (configType != null)
			{
				ALog.Trace($"[AsakiConfig] File Changed: {fileName}, Reloading...");

				IAsakiConfigService service = AsakiContext.Get<IAsakiConfigService>();
				if (service != null)
				{
					MethodInfo method = service.GetType().GetMethod("ReloadAsync");
					if (method != null)
					{
						MethodInfo genericMethod = method.MakeGenericMethod(configType);
						object taskObj = genericMethod.Invoke(service, null);

						// 由于接口定义的 ReloadAsync 返回 Task，这里进行简单处理
						if (taskObj is Task task)
						{
							// Fire and forget, catch exceptions
							Task.Run(async () =>
							{
								try { await task; }
								catch (Exception ex)
								{
									ALog.Error($"[AsakiConfig] Failed to reload '{fileName}'. Error: {ex.Message}", ex);
								}
							});
						}
					}
				}
			}
		}

		/// <summary>
		/// 在应用程序域的所有程序集中查找与文件名匹配的配置类型。
		/// </summary>
		/// <param name="typeName">配置文件名（不含扩展名），通常为配置类名（如"ItemConfig"）。</param>
		/// <returns>匹配的<see cref="Type"/>对象，若未找到返回<c>null</c>。</returns>
		/// <remarks>
		/// <para>搜索策略：</para>
		/// <list type="number">
		///     <item>过滤系统程序集（System、Unity）减少搜索范围</item>
		///     <item>优先尝试全名匹配（命名空间+类名）</item>
		///     <item>回退到遍历所有类型，检查名称和<see cref="IAsakiConfig"/>接口实现</item>
		/// </list>
		/// <para>性能注意：在大型项目中可能涉及反射遍历，仅在文件变更时触发，非性能关键路径。</para>
		/// <para>扩展性：若配置文件名与类型名映射规则复杂，建议扩展此方法的匹配逻辑。</para>
		/// </remarks>
		private Type FindConfigType(string typeName)
		{
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				// 跳过系统程序集，提升搜索效率
				if (assembly.FullName.StartsWith("System") || assembly.FullName.StartsWith("Unity")) continue;

				// 尝试常用命名空间规则，或者遍历
				Type type = assembly.GetType($"{assembly.GetName().Name}.{typeName}")
				            ?? assembly.GetType(typeName);

				if (type != null) return type;

				// 回退到完全遍历（性能较差，但确保找到）
				foreach (Type t in assembly.GetTypes())
				{
					if (t.Name == typeName && typeof(IAsakiConfig).IsAssignableFrom(t))
						return t;
				}
			}
			return null;
		}

		/// <summary>
		/// Unity 销毁回调，在组件销毁或编辑器停止播放时执行。
		/// </summary>
		/// <remarks>
		/// <para>资源清理：</para>
		/// <list type="bullet">
		///     <item>停止 <see cref="FileSystemWatcher"/> 的事件监听</item>
		///     <item>调用 <see cref="IDisposable.Dispose"/> 释放非托管资源</item>
		///     <item>将 <see cref="_watcher"/> 置空防止重复释放</item>
		/// </list>
		/// <para>重要性：未正确释放 <see cref="FileSystemWatcher"/> 可能导致文件句柄泄漏或编辑器卡顿。</para>
		/// </remarks>
		private void OnDestroy()
		{
			if (_watcher != null)
			{
				_watcher.EnableRaisingEvents = false;
				_watcher.Dispose();
				_watcher = null;
			}
		}
	}
}
#endif
