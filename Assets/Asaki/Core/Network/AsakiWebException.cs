namespace Asaki.Core.Network
{
    /// <summary>
    /// Asaki网络异常基类，用于封装HTTP请求过程中发生的错误信息
    /// </summary>
    public class AsakiWebException : System.Exception
    {
        /// <summary>
        /// HTTP响应状态码（如404、500等）
        /// </summary>
        public long ResponseCode { get; }

        /// <summary>
        /// 发生异常的请求URL地址
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// 初始化AsakiWebException的新实例
        /// </summary>
        /// <param name="message">异常描述信息</param>
        /// <param name="code">HTTP响应状态码</param>
        /// <param name="url">请求的目标URL</param>
        public AsakiWebException(string message, long code, string url)
            : base(message)
        {
            ResponseCode = code;
            Url = url;
        }
    }
}
