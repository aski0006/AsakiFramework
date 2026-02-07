using Asaki.Core.Network;
using Asaki.Core.Serialization;

namespace Asaki.Unity.Services.Network
{
    /// <summary>
    /// Unity环境下的标准响应实现
    /// </summary>
    /// <remarks>
    /// 适用于简单的无数据响应场景，如操作确认、状态返回等
    /// </remarks>
    public class AsakiResponse : AsakiResponseBase
    {
        /// <summary>
        /// 创建成功的响应
        /// </summary>
        public static AsakiResponse Success(string message = null)
        {
            var response = new AsakiResponse();
            response.SetSuccess(message);
            return response;
        }

        /// <summary>
        /// 创建失败的响应
        /// </summary>
        public static AsakiResponse Failure(int code, string message = null)
        {
            var response = new AsakiResponse();
            response.SetError(code, message);
            return response;
        }

        /// <inheritdoc/>
        protected override void SerializeCore(IAsakiWriter writer)
        {
            // 基础响应无额外数据需要序列化
        }

        /// <inheritdoc/>
        protected override void DeserializeCore(IAsakiReader reader)
        {
            // 基础响应无额外数据需要反序列化
        }
    }

    /// <summary>
    /// Unity环境下的标准数据响应实现
    /// </summary>
    /// <typeparam name="TData">响应数据类型</typeparam>
    /// <remarks>
    /// 适用于需要返回数据的响应场景，如查询结果、配置数据等
    /// </remarks>
    public class AsakiResponse<TData> : AsakiResponseBase<TData>
        where TData : IAsakiSavable, new()
    {
        /// <summary>
        /// 创建成功的响应
        /// </summary>
        public static AsakiResponse<TData> Success(TData data, string message = null)
        {
            var response = new AsakiResponse<TData>();
            response.SetSuccess(data, message);
            return response;
        }

        /// <summary>
        /// 创建失败的响应
        /// </summary>
        public static AsakiResponse<TData> Failure(int code, string message = null)
        {
            var response = new AsakiResponse<TData>();
            response.SetError(code, message);
            return response;
        }
    }
}
