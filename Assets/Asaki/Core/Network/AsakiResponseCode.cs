namespace Asaki.Core.Network
{
    /// <summary>
    /// 标准响应状态码定义
    /// </summary>
    /// <remarks>
    /// 提供了一套通用的业务状态码规范，建议服务端和客户端遵循此约定
    /// </remarks>
    public static class AsakiResponseCode
    {
        /// <summary>
        /// 操作成功
        /// </summary>
        public const int Success = 0;

        /// <summary>
        /// 通用错误
        /// </summary>
        public const int GeneralError = 1;

        /// <summary>
        /// 参数错误
        /// </summary>
        public const int InvalidParameter = 1001;

        /// <summary>
        /// 缺少必要参数
        /// </summary>
        public const int MissingParameter = 1002;

        /// <summary>
        /// 参数格式错误
        /// </summary>
        public const int InvalidParameterFormat = 1003;

        /// <summary>
        /// 未授权
        /// </summary>
        public const int Unauthorized = 2001;

        /// <summary>
        /// Token过期
        /// </summary>
        public const int TokenExpired = 2002;

        /// <summary>
        /// Token无效
        /// </summary>
        public const int InvalidToken = 2003;

        /// <summary>
        /// 权限不足
        /// </summary>
        public const int InsufficientPermission = 2004;

        /// <summary>
        /// 资源不存在
        /// </summary>
        public const int ResourceNotFound = 3001;

        /// <summary>
        /// 资源已存在
        /// </summary>
        public const int ResourceAlreadyExists = 3002;

        /// <summary>
        /// 资源被占用
        /// </summary>
        public const int ResourceBusy = 3003;

        /// <summary>
        /// 网络错误
        /// </summary>
        public const int NetworkError = 4001;

        /// <summary>
        /// 请求超时
        /// </summary>
        public const int RequestTimeout = 4002;

        /// <summary>
        /// 服务器内部错误
        /// </summary>
        public const int ServerError = 5001;

        /// <summary>
        /// 服务不可用
        /// </summary>
        public const int ServiceUnavailable = 5002;

        /// <summary>
        /// 服务器维护中
        /// </summary>
        public const int ServerMaintenance = 5003;

        /// <summary>
        /// 判断状态码是否表示成功
        /// </summary>
        public static bool IsSuccess(int code) => code == Success;

        /// <summary>
        /// 判断状态码是否表示客户端错误（4xxx范围）
        /// </summary>
        public static bool IsClientError(int code) => code >= 1000 && code < 2000;

        /// <summary>
        /// 判断状态码是否表示授权错误（2xxx范围）
        /// </summary>
        public static bool IsAuthError(int code) => code >= 2000 && code < 3000;

        /// <summary>
        /// 判断状态码是否表示资源错误（3xxx范围）
        /// </summary>
        public static bool IsResourceError(int code) => code >= 3000 && code < 4000;

        /// <summary>
        /// 判断状态码是否表示网络错误（4xxx范围）
        /// </summary>
        public static bool IsNetworkError(int code) => code >= 4000 && code < 5000;

        /// <summary>
        /// 判断状态码是否表示服务器错误（5xxx范围）
        /// </summary>
        public static bool IsServerError(int code) => code >= 5000 && code < 6000;

        /// <summary>
        /// 获取状态码的默认描述信息
        /// </summary>
        public static string GetDefaultMessage(int code)
        {
            return code switch
            {
                Success => "操作成功",
                GeneralError => "操作失败",
                InvalidParameter => "参数错误",
                MissingParameter => "缺少必要参数",
                InvalidParameterFormat => "参数格式错误",
                Unauthorized => "未授权",
                TokenExpired => "登录已过期，请重新登录",
                InvalidToken => "登录信息无效",
                InsufficientPermission => "权限不足",
                ResourceNotFound => "请求的资源不存在",
                ResourceAlreadyExists => "资源已存在",
                ResourceBusy => "资源正忙，请稍后重试",
                NetworkError => "网络连接失败",
                RequestTimeout => "请求超时，请稍后重试",
                ServerError => "服务器内部错误",
                ServiceUnavailable => "服务暂不可用",
                ServerMaintenance => "服务器维护中",
                _ => "未知错误"
            };
        }
    }
}
