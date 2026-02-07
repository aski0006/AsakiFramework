using Asaki.Core.Network;
using Asaki.Core.Serialization;

namespace Asaki.Unity.Services.Network
{
    /// <summary>
    /// Unity环境下的标准请求实现
    /// </summary>
    /// <remarks>
    /// 适用于简单的无数据请求场景，如心跳检测、状态查询等
    /// </remarks>
    public class AsakiRequest : AsakiRequestBase
    {
        /// <summary>
        /// 创建新的请求实例
        /// </summary>
        public static AsakiRequest Create()
        {
            return new AsakiRequest();
        }

        /// <inheritdoc/>
        protected override void SerializeCore(IAsakiWriter writer)
        {
            // 基础请求无额外数据需要序列化
        }

        /// <inheritdoc/>
        protected override void DeserializeCore(IAsakiReader reader)
        {
            // 基础请求无额外数据需要反序列化
        }
    }

    /// <summary>
    /// Unity环境下的标准数据请求实现
    /// </summary>
    /// <typeparam name="TData">请求数据类型</typeparam>
    /// <remarks>
    /// 适用于需要携带数据的请求场景，如表单提交、数据更新等
    /// </remarks>
    public class AsakiRequest<TData> : AsakiRequestBase<TData>
        where TData : IAsakiSavable, new()
    {
        /// <summary>
        /// 创建新的请求实例
        /// </summary>
        public static AsakiRequest<TData> Create()
        {
            return new AsakiRequest<TData>();
        }

        /// <summary>
        /// 使用指定数据创建新的请求实例
        /// </summary>
        public static AsakiRequest<TData> Create(TData data)
        {
            var request = new AsakiRequest<TData>();
            request.Data = data;
            return request;
        }

        /// <summary>
        /// 设置请求数据
        /// </summary>
        public AsakiRequest<TData> WithData(TData data)
        {
            Data = data;
            return this;
        }
    }
}
