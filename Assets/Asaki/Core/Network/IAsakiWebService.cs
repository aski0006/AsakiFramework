using Asaki.Core.Configs;
using Asaki.Core.Context;
using Asaki.Core.Serialization;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Network
{
	/// <summary>
	/// Asaki网络服务接口，提供HTTP请求的发送与拦截功能
	/// </summary>
	/// <remarks>
	/// 继承自 <see cref="IAsakiService"/> 和 <see cref="IDisposable"/>，支持服务生命周期管理
	/// </remarks>
	public interface IAsakiWebService : IAsakiService, IDisposable
	{
		/// <summary>
		/// 初始化并配置Web服务
		/// </summary>
		/// <param name="config">包含API基础地址、超时设置等的 <see cref="AsakiWebConfig"/> 配置对象</param>
		void Setup(AsakiWebConfig config);

		/// <summary>
		/// 添加请求/响应拦截器
		/// </summary>
		/// <param name="interceptor">实现 <see cref="IAsakiWebInterceptor"/> 接口的拦截器实例</param>
		/// <remarks>
		/// 拦截器可用于添加认证头、日志记录、错误处理等横切关注点
		/// </remarks>
		void AddInterceptor(IAsakiWebInterceptor interceptor);

		/// <summary>
		/// 移除已注册的拦截器
		/// </summary>
		/// <param name="interceptor">要移除的拦截器实例</param>
		void RemoveInterceptor(IAsakiWebInterceptor interceptor);

		/// <summary>
		/// 发送HTTP GET请求并反序列化响应
		/// </summary>
		/// <typeparam name="TResponse">响应类型，必须实现 <see cref="IAsakiSavable"/> 并具有无参构造函数</typeparam>
		/// <param name="apiPath">API路径（相对于配置中的基础地址）</param>
		/// <param name="cancellationToken">请求取消标记</param>
		/// <returns>反序列化后的响应对象</returns>
		/// <exception cref="AsakiWebException">请求失败或反序列化失败时抛出</exception>
		UniTask<TResponse> GetAsync<TResponse>(string apiPath, CancellationToken token = default)
			where TResponse : IAsakiSavable, new();

		/// <summary>
		/// 发送HTTP POST请求（JSON Body）并反序列化响应
		/// </summary>
		/// <typeparam name="TRequest">请求体类型，必须实现 <see cref="IAsakiSavable"/></typeparam>
		/// <typeparam name="TResponse">响应类型，必须实现 <see cref="IAsakiSavable"/> 并具有无参构造函数</typeparam>
		/// <param name="apiPath">API路径（相对于配置中的基础地址）</param>
		/// <param name="body">请求体对象，将被序列化为JSON</param>
		/// <param name="token">请求取消标记</param>
		/// <returns>反序列化后的响应对象</returns>
		/// <exception cref="AsakiWebException">请求失败或反序列化失败时抛出</exception>
		UniTask<TResponse> PostAsync<TRequest, TResponse>(string apiPath, TRequest body, CancellationToken token = default)
			where TRequest : IAsakiSavable
			where TResponse : IAsakiSavable, new();

		/// <summary>
		/// 发送HTTP POST请求（WWWForm表单）并反序列化响应
		/// </summary>
		/// <typeparam name="TResponse">响应类型，必须实现 <see cref="IAsakiSavable"/> 并具有无参构造函数</typeparam>
		/// <param name="apiPath">API路径（相对于配置中的基础地址）</param>
		/// <param name="form">包含表单数据的 <see cref="WWWForm"/> 对象</param>
		/// <param name="token">请求取消标记</param>
		/// <returns>反序列化后的响应对象</returns>
		/// <exception cref="AsakiWebException">请求失败或反序列化失败时抛出</exception>
		UniTask<TResponse> PostFormAsync<TResponse>(string apiPath, WWWForm form, CancellationToken token = default)
			where TResponse : IAsakiSavable, new();
	}

	/// <summary>
	/// 网络请求拦截器接口，用于在请求发送前后注入自定义逻辑
	/// </summary>
	/// <remarks>
	/// 典型应用场景包括：自动添加Token认证、统一错误处理、请求日志记录等
	/// </remarks>
	public interface IAsakiWebInterceptor
	{
		/// <summary>
		/// 在请求发送前调用，可修改 <see cref="UnityEngine.Networking.UnityWebRequest"/> 对象
		/// </summary>
		/// <param name="uwr">即将发送的UnityWebRequest实例</param>
		void OnRequest(UnityEngine.Networking.UnityWebRequest uwr);

		/// <summary>
		/// 在收到响应后调用，可检查并决定是否继续处理
		/// </summary>
		/// <param name="uwr">已接收响应的UnityWebRequest实例</param>
		/// <returns>
		/// 返回 <c>true</c> 表示继续正常处理流程，
		/// 返回 <c>false</c> 表示拦截该响应（通常用于自定义错误处理）
		/// </returns>
		bool OnResponse(UnityEngine.Networking.UnityWebRequest uwr);

		/// <summary>
		/// 在请求发生异常时调用
		/// </summary>
		/// <param name="uwr">发生异常的UnityWebRequest实例</param>
		/// <param name="ex">捕获的异常对象</param>
		void OnError(UnityEngine.Networking.UnityWebRequest uwr, Exception ex);
	}
}