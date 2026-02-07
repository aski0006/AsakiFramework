using Asaki.Core.Serialization;

namespace Asaki.Core.Network
{
    /// <summary>
    /// Asaki网络请求接口，定义所有HTTP请求的基础契约
    /// </summary>
    /// <remarks>
    /// 该接口继承自 <see cref="IAsakiSavable"/>，支持请求对象的序列化/反序列化。
    /// 所有具体的请求类都应实现此接口，以确保与Asaki网络服务兼容。
    /// </remarks>
    public interface IAsakiRequest : IAsakiSavable
    {
        /// <summary>
        /// 获取请求的唯一标识符
        /// </summary>
        /// <remarks>
        /// 用于请求追踪、日志记录和幂等性控制
        /// </remarks>
        string RequestId { get; }

        /// <summary>
        /// 获取请求时间戳（Unix时间戳，毫秒）
        /// </summary>
        long Timestamp { get; }

        /// <summary>
        /// 验证请求数据的有效性
        /// </summary>
        /// <returns>验证结果，包含是否有效及错误信息</returns>
        AsakiRequestValidationResult Validate();
    }

    /// <summary>
    /// 请求验证结果
    /// </summary>
    public readonly struct AsakiRequestValidationResult
    {
        /// <summary>
        /// 验证是否通过
        /// </summary>
        public readonly bool IsValid;

        /// <summary>
        /// 验证失败时的错误信息
        /// </summary>
        public readonly string ErrorMessage;

        /// <summary>
        /// 初始化验证结果
        /// </summary>
        public AsakiRequestValidationResult(bool isValid, string errorMessage = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// 成功的验证结果
        /// </summary>
        public static AsakiRequestValidationResult Success => new AsakiRequestValidationResult(true);

        /// <summary>
        /// 失败的验证结果
        /// </summary>
        public static AsakiRequestValidationResult Failure(string message) => new AsakiRequestValidationResult(false, message);
    }
}
