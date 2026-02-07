using System;
using Asaki.Core.Serialization;

namespace Asaki.Core.Network
{
    /// <summary>
    /// Asaki响应基类，提供响应的基础实现
    /// </summary>
    /// <remarks>
    /// 所有具体的响应类都应继承此类，以获得统一的状态码和消息管理。
    /// 继承此类时需实现 <see cref="SerializeCore"/> 和 <see cref="DeserializeCore"/> 方法。
    /// </remarks>
    public abstract class AsakiResponseBase : IAsakiResponse
    {
        private int _code;
        private string _message;
        private string _requestId;

        /// <summary>
        /// 获取业务状态码
        /// </summary>
        public int Code => _code;

        /// <summary>
        /// 获取响应消息
        /// </summary>
        public string Message => _message;

        /// <summary>
        /// 获取请求是否成功
        /// </summary>
        public bool IsSuccess => AsakiResponseCode.IsSuccess(_code);

        /// <summary>
        /// 获取关联的请求ID
        /// </summary>
        public string RequestId => _requestId;

        /// <summary>
        /// 初始化响应基类
        /// </summary>
        protected AsakiResponseBase()
        {
            _code = AsakiResponseCode.Success;
            _message = string.Empty;
        }

        /// <summary>
        /// 使用指定状态码和消息初始化响应
        /// </summary>
        protected AsakiResponseBase(int code, string message = null)
        {
            _code = code;
            _message = message ?? AsakiResponseCode.GetDefaultMessage(code);
        }

        /// <summary>
        /// 设置响应状态
        /// </summary>
        protected void SetResponse(int code, string message = null)
        {
            _code = code;
            _message = message ?? AsakiResponseCode.GetDefaultMessage(code);
        }

        /// <summary>
        /// 设置成功响应
        /// </summary>
        protected void SetSuccess(string message = null)
        {
            SetResponse(AsakiResponseCode.Success, message);
        }

        /// <summary>
        /// 设置错误响应
        /// </summary>
        protected void SetError(int code, string message = null)
        {
            SetResponse(code, message);
        }

        /// <summary>
        /// 序列化响应对象
        /// </summary>
        public void Serialize(IAsakiWriter writer)
        {
            writer.WriteInt("code", _code);
            writer.WriteString("message", _message);
            writer.WriteString("requestId", _requestId);
            SerializeCore(writer);
        }

        /// <summary>
        /// 反序列化响应对象
        /// </summary>
        public void Deserialize(IAsakiReader reader)
        {
            _code = reader.ReadInt("code");
            _message = reader.ReadString("message");
            _requestId = reader.ReadString("requestId");
            DeserializeCore(reader);
        }

        /// <summary>
        /// 序列化子类特定的数据
        /// </summary>
        protected abstract void SerializeCore(IAsakiWriter writer);

        /// <summary>
        /// 反序列化子类特定的数据
        /// </summary>
        protected abstract void DeserializeCore(IAsakiReader reader);
    }

    /// <summary>
    /// 带数据的响应基类
    /// </summary>
    /// <typeparam name="TData">响应数据的类型</typeparam>
    public abstract class AsakiResponseBase<TData> : AsakiResponseBase, IAsakiResponse<TData>
        where TData : IAsakiSavable, new()
    {
        private TData _data;

        /// <summary>
        /// 获取响应数据载荷
        /// </summary>
        public TData Data => _data;

        /// <summary>
        /// 初始化带数据的响应基类
        /// </summary>
        protected AsakiResponseBase()
        {
            _data = new TData();
        }

        /// <summary>
        /// 使用指定状态码、消息和数据初始化响应
        /// </summary>
        protected AsakiResponseBase(int code, string message = null, TData data = default)
            : base(code, message)
        {
            _data = data ?? new TData();
        }

        /// <summary>
        /// 设置成功响应及数据
        /// </summary>
        protected void SetSuccess(TData data, string message = null)
        {
            SetSuccess(message);
            _data = data;
        }

        /// <inheritdoc/>
        protected override void SerializeCore(IAsakiWriter writer)
        {
            writer.WriteObject("data", _data);
            SerializeResponseCore(writer);
        }

        /// <inheritdoc/>
        protected override void DeserializeCore(IAsakiReader reader)
        {
            _data = reader.ReadObject<TData>("data");
            DeserializeResponseCore(reader);
        }

        /// <summary>
        /// 序列化响应特定的额外数据（除Data外）
        /// </summary>
        protected virtual void SerializeResponseCore(IAsakiWriter writer) { }

        /// <summary>
        /// 反序列化响应特定的额外数据（除Data外）
        /// </summary>
        protected virtual void DeserializeResponseCore(IAsakiReader reader) { }
    }
}
