using Asaki.Core.Context;
using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Network
{
   /// <summary>
   /// [M-005] 详细下载进度结构体 (值类型，零GC)
   /// <para>采用readonly struct实现，确保不可变性并避免不必要的内存分配</para>
   /// </summary>
   /// <remarks>
   /// 该结构体设计为零GC分配，适合在性能敏感场景下高频传递进度信息
   /// </remarks>
   public readonly struct AsakiDownloadProgress
   {
   	/// <summary>
   	/// 下载进度百分比，取值范围 0.0 到 1.0
   	/// <para>0.0 表示尚未开始，1.0 表示下载完成</para>
   	/// </summary>
   	public readonly float Progress;

   	/// <summary>
   	/// 已下载的字节数
   	/// </summary>
   	public readonly ulong DownloadedBytes;

   	/// <summary>
   	/// 文件总字节数
   	/// <para>如果服务器未返回Content-Length标头，则该值为 0</para>
   	/// </summary>
   	public readonly ulong TotalBytes;

   	/// <summary>
   	/// 当前下载速度，单位：字节/秒 (Bytes/s)
   	/// </summary>
   	public readonly float Speed;

   	/// <summary>
   	/// 初始化 AsakiDownloadProgress 结构体的新实例
   	/// </summary>
   	/// <param name="progress">下载进度 (0.0f - 1.0f)</param>
   	/// <param name="downloaded">已下载字节数</param>
   	/// <param name="total">总字节数（未知则为0）</param>
   	/// <param name="speed">下载速度（Bytes/s）</param>
   	public AsakiDownloadProgress(float progress, ulong downloaded, ulong total, float speed)
   	{
   		Progress = progress;
   		DownloadedBytes = downloaded;
   		TotalBytes = total;
   		Speed = speed;
   	}
   }

   /// <summary>
   /// Asaki下载服务接口，提供文件下载及进度监控功能
   /// </summary>
   /// <seealso cref="IAsakiService"/>
   public interface IAsakiDownloadService : IAsakiService
   {
   	/// <summary>
   	/// 异步下载文件（支持断点续传预留，支持流式写入本地磁盘）
   	/// </summary>
   	/// <param name="url">要下载的文件URL地址</param>
   	/// <param name="localPath">本地保存路径（包含文件名）</param>
   	/// <param name="progress">下载进度回调（传递 AsakiDownloadProgress 结构体），可设为 null</param>
   	/// <param name="token">取消下载操作的 <see cref="CancellationToken"/></param>
   	/// <returns>表示异步下载任务的 <see cref="UniTask"/></returns>
   	/// <exception cref="AsakiWebException">网络请求失败时抛出</exception>
   	/// <exception cref="System.IO.IOException">文件写入失败时抛出</exception>
   	UniTask DownloadAsync(string url, string localPath, IProgress<AsakiDownloadProgress> progress = null, CancellationToken token = default);

    /// <summary>
    /// 异步获取远程文件的大小（通过HTTP HEAD请求）
    /// </summary>
    /// <param name="url">目标文件的URL地址</param>
    /// <param name="token">取消请求的 <see cref="CancellationToken"/></param>
    /// <returns>文件大小（字节数）</returns>
    /// <exception cref="AsakiWebException">请求失败或服务器不支持时抛出</exception>
    /// <remarks>
    /// 该方法通常发送HTTP HEAD请求以高效获取文件元信息，不会下载实际内容
    /// </remarks>
    UniTask<long> GetFileSizeAsync(string url, CancellationToken token = default);
   }
}