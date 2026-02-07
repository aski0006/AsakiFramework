using Asaki.Core.Serialization;

namespace Asaki.Core.Network
{
    /// <summary>
    /// Asaki网络响应接口，定义所有HTTP响应的基础契约
    /// </summary>
    /// <remarks>
    /// 该接口继承自 <see cref="IAsakiSavable"/>，支持响应对象的序列化/反序列化。
    /// 所有具体的响应类都应实现此接口，以确保与Asaki网络服务兼容。
    /// </remarks>
    public interface IAsakiResponse : IAsakiSavable
    {
        /// <summary>
        /// 业务状态码
        /// </summary>
        /// <remarks>
        /// 与HTTP状态码不同，这是服务端定义的业务逻辑状态码
        /// 0 通常表示成功，非0值表示各种业务错误
        /// </remarks>
        int Code { get; }

        /// <summary>
        /// 响应消息
        /// </summary>
        /// <remarks>
        /// 通常用于显示给用户的友好提示信息
        /// </remarks>
        string Message { get; }

        /// <summary>
        /// 请求是否成功
        /// </summary>
        /// <remarks>
        /// 基于 <see cref="Code"/> 判断，通常为 Code == 0
        /// </remarks>
        bool IsSuccess { get; }

        /// <summary>
        /// 获取关联的请求ID
        /// </summary>
        string RequestId { get; }
    }

    /// <summary>
    /// 泛型响应接口，包含具体的数据载荷
    /// </summary>
    /// <typeparam name="TData">响应数据的类型</typeparam>
    public interface IAsakiResponse<out TData> : IAsakiResponse
    {
        /// <summary>
        /// 响应数据载荷
        /// </summary>
        /// <remarks>
        /// 当 <see cref="IAsakiResponse.IsSuccess"/> 为 true 时，此属性包含有效数据
        /// </remarks>
        TData Data { get; }
    }
}
