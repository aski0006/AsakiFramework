namespace Asaki.Core.Serialization
{
    /// <summary>
    /// 标记接口，用于标识可通过Asaki序列化系统处理的对象
    /// </summary>
    /// <remarks>
    /// 此接口继承自 <see cref="IAsakiSavable"/>，为网络请求/响应数据提供统一的序列化契约。
    /// 实现此接口的数据类可以被自动序列化为JSON或二进制格式。
    /// </remarks>
    public interface IAsakiSerializable : IAsakiSavable { }

    /// <summary>
    /// 可序列化数据基类，提供默认的序列化实现
    /// </summary>
    /// <remarks>
    /// 继承此类的数据对象只需实现具体的字段序列化逻辑，
    /// 基础框架（版本控制、类型信息等）由基类处理。
    /// </remarks>
    public abstract class AsakiSerializableBase : IAsakiSerializable
    {
        /// <summary>
        /// 数据版本号，用于版本兼容和迁移
        /// </summary>
        public virtual int Version => 1;

        /// <summary>
        /// 序列化对象数据
        /// </summary>
        public abstract void Serialize(IAsakiWriter writer);

        /// <summary>
        /// 反序列化对象数据
        /// </summary>
        public abstract void Deserialize(IAsakiReader reader);
    }
}
