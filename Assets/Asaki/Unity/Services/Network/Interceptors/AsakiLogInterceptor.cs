using Asaki.Core.Logging;
using Asaki.Core.Network;
using System;
using UnityEngine.Networking;

namespace Asaki.Unity.Services.Network.Interceptors
{
	/// <summary>
	/// Asaki日志拦截器实现
	/// <para>职责：捕获网络请求异常并记录到ALog系统</para>
	/// <para>设计模式：拦截器模式（IAsakiWebInterceptor）</para>
	/// <para>线程安全：可在任意线程调用，日志写入由ALog保证线程安全</para>
	/// <para>使用场景：需要统一记录网络层错误的全局拦截</para>
	/// </summary>
	/// <remarks>
	/// <list type="bullet">
	/// <item><description>仅处理异常场景，不干预正常请求流程</description></item>
	/// <item><description>依赖ALog静态类，需在应用启动时初始化日志系统</description></item>
	/// <item><description>序列化标记仅用于配置化场景，当前实现无状态可忽略</description></item>
	/// </list>
	/// </remarks>
	[Serializable]
	public class AsakiLogInterceptor : IAsakiWebInterceptor
	{
		/// <summary>
		/// 请求预处理钩子（本实现不干预）
		/// </summary>
		/// <param name="uwr">即将发送的UnityWebRequest实例，参数永不null</param>
		/// <remarks>
		/// 设计决策：保持空实现以维持接口契约，避免null检查开销
		/// </remarks>
		public void OnRequest(UnityWebRequest uwr) { }

		/// <summary>
		/// 响应后处理钩子（本实现不干预）
		/// </summary>
		/// <param name="uwr">已完成请求的UnityWebRequest实例，参数永不null</param>
		/// <returns>始终返回true，表示不拦截响应</returns>
		/// <remarks>
		/// 安全考量：返回false会触发上层异常流程，本拦截器仅记录错误不干预控制流
		/// </remarks>
		public bool OnResponse(UnityWebRequest uwr) { return true; }

		/// <summary>
		/// 异常处理核心方法
		/// </summary>
		/// <param name="uwr">发生异常的UnityWebRequest实例，参数永不null</param>
		/// <param name="ex">捕获的异常对象，参数永不null</param>
		/// <remarks>
		/// <list type="table">
		/// <listheader>
		///   <term>日志级别</term>
		///   <description>决策依据</description>
		/// </listheader>
		/// <item>
		///   <term>ALog.Error</term>
		///   <description>网络错误属于功能异常，需立即关注</description>
		/// </item>
		/// </list>
		/// 性能注意：异常堆栈捕获存在性能开销，仅应在Error级别启用
		/// </remarks>
		public void OnError(UnityWebRequest uwr, Exception ex)
		{
			ALog.Error($"[Web Error] {uwr.url}: {ex.Message}", ex);
		}
	}
}
