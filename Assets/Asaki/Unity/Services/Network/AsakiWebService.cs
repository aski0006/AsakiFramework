using Asaki.Core.Configs;
using Asaki.Core.Logging;
using Asaki.Core.Network;
using Asaki.Core.Serialization;
using Asaki.Unity.Extensions;
using Asaki.Unity.Services.Serialization;
using Asaki.Unity.Utils;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Asaki.Unity.Services.Network
{
	/// <summary>
	/// Asaki Web服务主实现
	/// <para><b>架构模式：</b>门面模式 + 拦截器链</para>
	/// <para><b>生命周期：</b>单例或Scoped，需显式调用Dispose释放拦截器</para>
	/// <para><b>线程安全：</b>非线程安全，禁止并发调用Setup/AddInterceptor</para>
	/// </summary>
	/// <remarks>
	/// <b>设计亮点：</b>
	/// - 使用HashSet自动去重，防止拦截器重复注册
	/// - 基于接口的配置驱动（AsakiWebConfig）
	/// - 统一的异常处理与日志记录
	/// - 对象池优化序列化内存分配
	/// 
	/// <b>资源管理警告：</b>
	/// - 必须调用Dispose()释放拦截器引用，防止内存泄漏
	/// - _isDisposed标记防止重复释放，但非线程安全
	/// 
	/// <b>拦截器执行顺序：</b>不保证顺序，依赖HashSet遍历顺序（当前为插入顺序）
	/// </remarks>
	public class AsakiWebService : IAsakiWebService
	{
		private string _baseUrl = "";
		private int _timeout = 10;
		/// <summary>拦截器集合，使用HashSet防止重复</summary>
		private readonly HashSet<IAsakiWebInterceptor> _interceptors = new HashSet<IAsakiWebInterceptor>();

		private bool _isDisposed = false;

		/// <summary>
		/// 配置服务（幂等操作）
		/// </summary>
		/// <param name="config">配置对象，null时使用默认值</param>
		/// <remarks>
		/// <b>线程安全警告：</b>不应在运行期间并发调用此方法
		/// <b>BaseUrl处理：</b>自动移除尾部斜杠，避免双斜杠问题
		/// </remarks>
		public void Setup(AsakiWebConfig config)
		{
			if (config == null) return;
			_baseUrl = config.BaseUrl?.TrimEnd('/') ?? "";
			_timeout = config.TimeoutSeconds;

			if (config.InitialInterceptors == null) return;
			foreach (IAsakiWebInterceptor i in config.InitialInterceptors)
				AddInterceptor(i);
		}

		/// <summary>
		/// 析构方法（兼容旧代码，建议改用Dispose）
		/// </summary>
		/// <remarks>标记为过时，统一使用Dispose模式</remarks>
		public void OnDispose()
		{
			Dispose();
		}

		/// <summary>
		/// 释放资源
		/// </summary>
		/// <remarks>
		/// 实现IDisposable模式，清空拦截器集合释放引用
		/// 重复调用安全（通过_isDisposed标记）
		/// </remarks>
		public void Dispose()
		{
			if (_isDisposed) return;
			_isDisposed = true;
			_interceptors.Clear();
		}

		/// <summary>
		/// 注册拦截器
		/// </summary>
		/// <param name="interceptor">拦截器实例，null时静默返回</param>
		/// <remarks>
		/// <b>去重机制：</b>HashSet自动保证同一实例不重复添加
		/// <b>注册时机：</b>应在Setup后、首次请求前完成注册
		/// </remarks>
		public void AddInterceptor(IAsakiWebInterceptor interceptor)
		{
			if (interceptor != null) _interceptors.Add(interceptor);
		}

		/// <summary>
		/// 移除拦截器
		/// </summary>
		/// <param name="interceptor">待移除的拦截器实例</param>
		public void RemoveInterceptor(IAsakiWebInterceptor interceptor)
		{
			if (interceptor != null) _interceptors.Remove(interceptor);
		}

		/// <summary>
		/// 执行GET请求并反序列化响应
		/// </summary>
		/// <typeparam name="TResponse">响应类型，必须有无参构造函数并实现IAsakiSavable</typeparam>
		/// <param name="apiPath">API路径，支持绝对URL或相对路径</param>
		/// <param name="cancellationToken">请求取消标记</param>
		/// <returns>反序列化后的响应对象</returns>
		/// <exception cref="ObjectDisposedException">服务已被释放</exception>
		/// <exception cref="AsakiWebException">网络错误、协议错误或业务拦截</exception>
		/// <exception cref="SerializationException">JSON反序列化失败</exception>
		/// <remarks>
		/// <b>完整流程：</b>
		/// 1. 构建URL → 2. 配置超时 → 3. 执行拦截器OnRequest → 4. 发送请求 → 5. 执行拦截器OnResponse → 6. 反序列化
		/// 
		/// <b>性能优化：</b>使用SendWebRequestAsTask扩展避免协程GC分配
		/// </remarks>
		public async UniTask<TResponse> GetAsync<TResponse>(string apiPath, CancellationToken token = default)
			where TResponse : IAsakiSavable, new()
		{
			CheckDisposed();
			string url = BuildUrl(apiPath);

			// 使用using确保资源释放
			using (UnityWebRequest uwr = UnityWebRequest.Get(url))
			{
				ConfigureRequest(uwr);
				foreach (IAsakiWebInterceptor i in _interceptors) i.OnRequest(uwr);

				// 创建取消注册，确保取消时调用Abort
			using (token.Register(() => uwr.Abort()))
				{
					try
					{
						await uwr.SendWebRequestAsTask();
					}
					catch (UnityWebRequestException ex) when (token.IsCancellationRequested)
					{
						// 当取消被触发时，Abort会产生异常，需要识别并转换
						throw new OperationCanceledException("Request was cancelled", ex, token);
					}
				}

				return ProcessResponse<TResponse>(uwr);
			}
		}


		/// <summary>
		/// 执行POST请求（JSON Body）
		/// </summary>
		/// <typeparam name="TRequest">请求体类型，必须实现IAsakiSavable</typeparam>
		/// <typeparam name="TResponse">响应类型，必须有无参构造函数并实现IAsakiSavable</typeparam>
		/// <param name="apiPath">API路径</param>
		/// <param name="body">请求体对象</param>
		/// <param name="cancellationToken">请求取消标记</param>
		/// <returns>反序列化后的响应对象</returns>
		/// <remarks>
		/// <b>Content-Type：</b>固定为application/json，不支持多媒体表单
		/// <b>序列化：</b>使用AsakiJsonWriter和StringBuilder池化
		/// </remarks>
		public async UniTask<TResponse> PostAsync<TRequest, TResponse>(string apiPath, TRequest body, CancellationToken token = default) 
			where TRequest : IAsakiSavable 
			where TResponse : IAsakiSavable, new()
		{
			CheckDisposed();
			string url = BuildUrl(apiPath);
			byte[] bodyRaw = SerializeRequestToBytes(body);

			using (UnityWebRequest uwr = new UnityWebRequest(url, "POST"))
			{
				uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
				uwr.downloadHandler = new DownloadHandlerBuffer();
				uwr.SetRequestHeader("Content-Type", "application/json");

				ConfigureRequest(uwr);
				foreach (IAsakiWebInterceptor i in _interceptors) i.OnRequest(uwr);

				using (token.Register(() => uwr.Abort()))
				{
					try
					{
						await uwr.SendWebRequestAsTask();
					}
					catch (UnityWebRequestException ex) when (token.IsCancellationRequested)
					{
						throw new OperationCanceledException("Request was cancelled", ex, token);
					}
				}

				return ProcessResponse<TResponse>(uwr);
			}
		}


		/// <summary>
		/// 执行POST请求（WWWForm）
		/// </summary>
		/// <typeparam name="TResponse">响应类型</typeparam>
		/// <param name="apiPath">API路径</param>
		/// <param name="form">WWWForm表单数据</param>
		/// <param name="cancellationToken">请求取消标记</param>
		/// <returns>反序列化后的响应对象</returns>
		/// <remarks>
		/// <b>适用场景：</b>上传文件或multipart/form-data
		/// <b>注意：</b>UnityWebRequest.Post会自动设置Content-Type为multipart/form-data
		/// </remarks>
		public async UniTask<TResponse> PostFormAsync<TResponse>(string apiPath, WWWForm form, CancellationToken token = default) 
			where TResponse : IAsakiSavable, new()
		{
			CheckDisposed();
			string url = BuildUrl(apiPath);
    
			using (UnityWebRequest uwr = UnityWebRequest.Post(url, form))
			{
				ConfigureRequest(uwr);
				foreach (IAsakiWebInterceptor i in _interceptors) i.OnRequest(uwr);

				using (token.Register(() => uwr.Abort()))
				{
					try
					{
						await uwr.SendWebRequestAsTask();
					}
					catch (UnityWebRequestException ex) when (token.IsCancellationRequested)
					{
						throw new OperationCanceledException("Request was cancelled", ex, token);
					}
				}

				return ProcessResponse<TResponse>(uwr);
			}
		}

		/// <summary>
		/// 检查服务是否已释放
		/// </summary>
		/// <exception cref="ObjectDisposedException">已释放时抛出</exception>
		private void CheckDisposed()
		{
			if (_isDisposed) throw new ObjectDisposedException(nameof(AsakiWebService));
		}

		/// <summary>
		/// 构建完整URL
		/// </summary>
		/// <param name="apiPath">原始路径</param>
		/// <returns>处理后的URL</returns>
		/// <remarks>
		/// <b>智能判断：</b>
		/// - 已包含http/https → 直接返回
		/// - BaseUrl为空 → 直接返回apiPath
		/// - 相对路径 → 拼接BaseUrl并去除双斜杠
		/// </remarks>
		private string BuildUrl(string apiPath)
		{
			if (apiPath.StartsWith("http", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(_baseUrl)) return apiPath;
			return $"{_baseUrl}/{apiPath.TrimStart('/')}";
		}

		/// <summary>
		/// 配置请求基础参数
		/// </summary>
		/// <param name="uwr">UnityWebRequest实例</param>
		private void ConfigureRequest(UnityWebRequest uwr)
		{
			uwr.timeout = _timeout;
		}

		/// <summary>
		/// 序列化请求体为字节数组
		/// </summary>
		/// <typeparam name="T">请求体类型</typeparam>
		/// <param name="body">请求体对象</param>
		/// <returns>UTF-8编码的字节数组</returns>
		/// <exception cref="SerializationException">序列化失败时抛出</exception>
		/// <remarks>
		/// <b>内存优化：</b>使用StringBuilder池避免重复分配
		/// <b>异常安全：</b>finally确保池化对象归还
		/// </remarks>
		private byte[] SerializeRequestToBytes<T>(T body) where T : IAsakiSavable
		{
			StringBuilder sb = AsakiStringBuilderPool.Rent();
			try
			{
				AsakiJsonWriter writer = new AsakiJsonWriter(sb);
				body.Serialize(writer);
				return AsakiStringBuilderPool.GetBytesAndRelease(sb);
			}
			catch
			{
				AsakiStringBuilderPool.Return(sb); // 确保异常时也归还
				throw;
			}
		}

		/// <summary>
		/// 统一响应处理管道
		/// </summary>
		/// <typeparam name="T">目标响应类型</typeparam>
		/// <param name="uwr">已完成的UnityWebRequest</param>
		/// <returns>反序列化后的响应对象</returns>
		/// <exception cref="AsakiWebException">网络/协议/拦截错误</exception>
		/// <exception cref="SerializationException">JSON解析失败</exception>
		/// <remarks>
		/// <b>处理流程：</b>
		/// 1. 检查uwr.result → 2. 执行OnError拦截 → 3. 执行OnResponse拦截 → 4. JSON反序列化
		/// 
		/// <b>空响应处理：</b>返回default(T)，对于struct返回零值实例
		/// </remarks>
		private T ProcessResponse<T>(UnityWebRequest uwr) where T : IAsakiSavable, new()
		{
			// 错误检查
			if (uwr.result == UnityWebRequest.Result.ConnectionError ||
			    uwr.result == UnityWebRequest.Result.ProtocolError)
			{
				AsakiWebException ex = new AsakiWebException(uwr.error, uwr.responseCode, uwr.url);
				foreach (IAsakiWebInterceptor i in _interceptors) i.OnError(uwr, ex);
				throw ex;
			}

			// 业务拦截
			foreach (IAsakiWebInterceptor i in _interceptors)
			{
				if (!i.OnResponse(uwr))
					throw new AsakiWebException("Intercepted", uwr.responseCode, uwr.url);
			}

			// 反序列化
			string json = uwr.downloadHandler.text;
			if (string.IsNullOrEmpty(json)) return default(T);

			try
			{
				AsakiJsonReader reader = AsakiJsonReader.FromJson(json);
				T response = new T();
				response.Deserialize(reader);
				return response;
			}
			catch (Exception ex)
			{
				ALog.Error($"[AsakiWeb] Parse Error: {ex.Message}\n", json);
				throw;
			}
		}
	}
}
