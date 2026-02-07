using System;
using Asaki.Core.Serialization;

namespace Asaki.Core.Network
{
    /// <summary>
    /// Asaki请求基类，提供请求的基础实现
    /// </summary>
    /// <remarks>
    /// 所有具体的请求类都应继承此类，以获得统一的请求标识和时间戳管理。
    /// 继承此类时需实现 <see cref="SerializeCore"/> 和 <see cref="DeserializeCore"/> 方法。
    /// </remarks>
    public abstract class AsakiRequestBase : IAsakiRequest
    {
        private string _requestId;
        private long _timestamp;

        /// <summary>
        /// 获取请求的唯一标识符
        /// </summary>
        public string RequestId => _requestId;

        /// <summary>
        /// 获取请求时间戳（Unix时间戳，毫秒）
        /// </summary>
        public long Timestamp => _timestamp;

        /// <summary>
        /// 初始化请求基类
        /// </summary>
        /// <remarks>
        /// 自动生成请求ID和当前时间戳
        /// </remarks>
        protected AsakiRequestBase()
        {
            _requestId = GenerateRequestId();
            _timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 生成请求唯一标识符
        /// </summary>
        protected virtual string GenerateRequestId()
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// 验证请求数据的有效性
        /// </summary>
        /// <remarks>
        /// 基类实现始终返回成功，子类应重写此方法添加具体验证逻辑
        /// </remarks>
        public virtual AsakiRequestValidationResult Validate()
        {
            return AsakiRequestValidationResult.Success;
        }

        /// <summary>
        /// 序列化请求对象
        /// </summary>
        /// <param name="writer">序列化写入器</param>
        /// <remarks>
        /// 此方法自动序列化基础字段（RequestId, Timestamp），然后调用 <see cref="SerializeCore"/> 序列化子类特定数据
        /// </remarks>
        public void Serialize(IAsakiWriter writer)
        {
            writer.WriteString("requestId", _requestId);
            writer.WriteLong("timestamp", _timestamp);
            SerializeCore(writer);
        }

        /// <summary>
        /// 反序列化请求对象
        /// </summary>
        /// <param name="reader">序列化读取器</param>
        /// <remarks>
        /// 此方法自动反序列化基础字段（RequestId, Timestamp），然后调用 <see cref="DeserializeCore"/> 反序列化子类特定数据
        /// </remarks>
        public void Deserialize(IAsakiReader reader)
        {
            _requestId = reader.ReadString("requestId");
            _timestamp = reader.ReadLong("timestamp");
            DeserializeCore(reader);
        }

        /// <summary>
        /// 序列化子类特定的数据
        /// </summary>
        /// <param name="writer">序列化写入器</param>
        protected abstract void SerializeCore(IAsakiWriter writer);

        /// <summary>
        /// 反序列化子类特定的数据
        /// </summary>
        /// <param name="reader">序列化读取器</param>
        protected abstract void DeserializeCore(IAsakiReader reader);
    }

    /// <summary>
    /// 带数据的请求基类
    /// </summary>
    /// <typeparam name="TData">请求数据的类型</typeparam>
    public abstract class AsakiRequestBase<TData> : AsakiRequestBase
        where TData : IAsakiSavable, new()
    {
        private TData _data;

        /// <summary>
        /// 获取或设置请求数据
        /// </summary>
        public TData Data
        {
            get => _data;
            set => _data = value;
        }

        /// <summary>
        /// 初始化带数据的请求基类
        /// </summary>
        protected AsakiRequestBase()
        {
            _data = new TData();
        }

        /// <summary>
        /// 使用指定数据初始化请求
        /// </summary>
        protected AsakiRequestBase(TData data)
        {
            _data = data ?? new TData();
        }

        /// <inheritdoc/>
        protected override void SerializeCore(IAsakiWriter writer)
        {
            writer.WriteObject("data", _data);
            SerializeRequestCore(writer);
        }

        /// <inheritdoc/>
        protected override void DeserializeCore(IAsakiReader reader)
        {
            _data = reader.ReadObject<TData>("data");
            DeserializeRequestCore(reader);
        }

        /// <summary>
        /// 序列化请求特定的额外数据（除Data外）
        /// </summary>
        protected virtual void SerializeRequestCore(IAsakiWriter writer) { }

        /// <summary>
        /// 反序列化请求特定的额外数据（除Data外）
        /// </summary>
        protected virtual void DeserializeRequestCore(IAsakiReader reader) { }
    }
}
